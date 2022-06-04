using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WpfLib;

namespace GameApp
{
    /// <summary>
    /// ボードの状態と反転基準となる位置アドレス
    /// </summary>
    class Board
    {
        public uint mPreBoard { get; set; } //  ボードの状態(反転前状態)
        public int mLoc { get; set; }       //  反転位置アドレス(row*bordSize + col)

        /// <summary>
        /// コンストラクタ ボードの状態と反転基準となる位置アドレス
        /// </summary>
        /// <param name="board">盤の状態(反転前)</param>
        /// <param name="loc">反転基準位置アドレス</param>
        public Board(uint board, int loc)
        {
            mPreBoard = board;
            mLoc = loc;
        }
    }

    /// <summary>
    /// ボードの評価点とボード状態
    /// </summary>
    class Score : IComparable
    {
        public int mScore { get; set; }     //  ボードの評価点(反転したセルの数)
        public uint mBorad { get; set; }    //  ボードの状態

        /// <summary>
        /// コンストラクタ ボードの評価点とボード状態
        /// </summary>
        /// <param name="score">評価点</param>
        /// <param name="board">盤の状態</param>
        public Score(int score, uint board)
        {
            mScore = score;
            mBorad = board;
        }

        /// <summary>
        /// 評価点の取得
        /// </summary>
        /// <returns>評価点</returns>
        public int getScore()
        {
            return mScore;
        }

        /// <summary>
        /// Score Object での比較関数
        /// </summary>
        /// <param name="other">比較対象</param>
        /// <returns>比較結果</returns>
        public int CompareTo(Score other)
        {
            return mScore - other.mScore;
        }

        /// <summary>
        /// IComparableの比較関数
        /// </summary>
        /// <param name="other">比較対象Object</param>
        /// <returns>比較結果</returns>
        public int CompareTo(Object other)
        {
            return CompareTo((Score)other);
        }
    }



    /// <summary>
    /// A*探索による「白にしろ」の解法プログラム
    /// 幅優先探索に評価点による優先順位をつけて探索を行う。
    /// 評価点は反転したセルの数で少ないほど優先順位を上げる
    /// 最短の解法を見つけるものではないが早めに解答を見つけることができる
    /// 盤サイズが大きい場合(4x4以上)に有効となる
    /// </summary>
    class AllWhiteSolver2
    {
        private int mBoardSize;                     //  ボードのサイズ
        private uint mBoardPattern;                 //  問題パターン
        private Dictionary<uint, Board> mBoards;    //  探索したボードの登録8重複不可)
        private PriorityQueue<Score> mScoreBoards;  //  評価点の優先順位キュー
        private string mErrorMsg;                   //  エラーメッセージ

        private YLib ylib = new YLib();

        /// <summary>
        /// コンストラクタ (初期登録)
        /// </summary>
        /// <param name="boardPattern">問題パターン</param>
        public AllWhiteSolver2(byte[,] boardPattern)
        {
            mBoardSize = boardPattern.GetLength(0);             //  盤の大きさ(boardSize x boardSize)
            mBoardPattern = cnvBoadData(boardPattern);          //  盤の状態を2元配列からビットアドレスに変換する
            mBoards = new Dictionary<uint, Board>();            //  探索した盤状態の登録リスト
            mScoreBoards = new PriorityQueue<Score>(1012, 0);   //  評価点を優先順位キューに登録
        }

        /// <summary>
        /// 解法の実施
        /// </summary>
        /// <returns></returns>
        public bool Solver()
        {
            //  初期値登録
            Score scoreBoard = new Score(ylib.bitsCount(mBoardPattern), mBoardPattern);
            mScoreBoards.Push(scoreBoard);
            Board board = new Board(mBoardPattern, -1);
            mBoards.Add(mBoardPattern, board);

            try {
                while (true) {
                    //  評価点のもっとも高いものから探索する
                    if (getNextPattern(mScoreBoards.Pop().mBorad))
                        break;                                  //  解法できた場合
                    if (0 == mScoreBoards.Count())
                        break;
                }
            } catch (Exception e) {
                mErrorMsg = e.Message;
                return false;
            }
            return true;
        }

        /// <summary>
        /// エラーメッセージの取得
        /// </summary>
        /// <returns></returns>
        public string getErrorMsg()
        {
            return mErrorMsg;
        }

        /// <summary>
        /// 探索数の取得
        /// </summary>
        /// <returns></returns>
        public int getCount()
        {
            return mBoards.Count();
        }

        /// <summary>
        /// 探索結果のリストを出力
        /// すべて白の状態から逆順で問題パターンにいたる反転位置のリスト
        /// </summary>
        /// <returns>反転位置リスト</returns>
        public List<int[]> getResultList()
        {
            List<int[]> result = new List<int[]>();
            uint board = 0;
            int loc = 0;
            do {
                loc = mBoards[board].mLoc;
                board = mBoards[board].mPreBoard;
                int[] locs = new int[2];
                locs[0] = bitLoc2Row(loc);
                locs[1] = bitLoc2Col(loc);
                result.Add(locs);
            } while (0 <= loc);

            return result;
        }

        /// <summary>
        /// 指定のパターンから派生するパターンを作成し登録する
        /// パターンの中に完成型(0)があれば終了する
        /// 既に登録されているパターンは登録しない
        /// </summary>
        /// <param name="boardPattern">派生元のパターン</param>
        /// <returns>完成の有無</returns>
        private bool getNextPattern(uint boardPattern)
        {
            for (int row = 0; row < mBoardSize; row++) {
                for (int col = 0; col < mBoardSize; col++) {
                    uint nextPattern = reverseBoard(boardPattern, bitLoc(row, col), mBoardSize);
                    //  既に登録されているパターンを除いてBoardデータとScoreデータを登録
                    if (!mBoards.ContainsKey(nextPattern)) {
                        //  Boardリストにデータを登録
                        mBoards.Add(nextPattern, new Board(boardPattern, bitLoc(row, col)));
                        //  Scoreキューにデータを登録
                        Score scoreBoard = new Score(ylib.bitsCount(nextPattern), nextPattern);
                        mScoreBoards.Push(scoreBoard);
                        if (scoreBoard.mScore == 0)         //  反転データがなければ完了
                            return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 指定した位置とその上下左右のセルを反転
        /// </summary>
        /// <param name="board">反転前の盤の状態</param>
        /// <param name="loc">反転の起点アドレス</param>
        /// <param name="boardSize">盤のサイズ</param>
        /// <returns>反転後の盤の状態</returns>
        private uint reverseBoard(uint board, int loc, int boardSize)
        {
            board = ylib.bitRevers(board, loc);
            if (0 < (loc / boardSize))
                board = ylib.bitRevers(board, loc - boardSize);
            if ((loc / boardSize) < (boardSize - 1))
                board = ylib.bitRevers(board, loc + boardSize);
            if (0 < (loc % boardSize))
                board = ylib.bitRevers(board, loc - 1);
            if ((loc % boardSize) < (boardSize - 1))
                board = ylib.bitRevers(board, loc + 1);
            return board;
        }

        /// <summary>
        /// 2次元配列の盤のパターンからビット配列に変換する
        /// </summary>
        /// <param name="board">2次元配列の盤状態</param>
        /// <returns>ビット配列の盤状態</returns>
        public uint cnvBoadData(byte[,] board)
        {
            uint cboard = 0;
            for (int row = 0; row < mBoardSize; row++)
                for (int col = 0; col < mBoardSize; col++) {
                    if (board[row, col] != 0)
                        cboard = ylib.bitOn(cboard, bitLoc(row, col));
                }

            return cboard;
        }

        /// <summary>
        /// bitアドレスから行を求める
        /// </summary>
        /// <param name="bitloc">bitアドレス</param>
        /// <returns>行</returns>
        private int bitLoc2Row(int bitloc)
        {
            return bitloc / mBoardSize;
        }

        /// <summary>
        /// bitアドレスから列を求める
        /// </summary>
        /// <param name="bitloc">bitアドレス</param>
        /// <returns>列</returns>
        private int bitLoc2Col(int bitloc)
        {
            return bitloc % mBoardSize;
        }

        /// <summary>
        /// ボードサイズに合わせて行列をbit位置に変換する
        /// </summary>
        /// <param name="row"></param>
        /// <param name="col"></param>
        /// <returns></returns>
        private int bitLoc(int row, int col)
        {
            return row * mBoardSize + col;
        }

    }
}
