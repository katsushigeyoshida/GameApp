using System;
using System.Collections.Generic;
using WpfLib;

namespace GameApp
{
    class SlideGameBoard
    {
        public ulong mPreBoard { get; set; } //  ボードの状態(移動前状態)
        public int mLoc { get; set; }       //  移動セル位置アドレス(row*bordSize + col)

        /// <summary>
        /// コンストラクタ ボードの状態と反転基準となる位置アドレス
        /// </summary>
        /// <param name="board">盤の状態(反転前)</param>
        /// <param name="loc">反転基準位置アドレス</param>
        public SlideGameBoard(ulong board, int loc)
        {
            mPreBoard = board;
            mLoc = loc;
        }

    }

    class SlideGameScore : IComparable
    {
        public int mScore { get; set; }     //  ボードの評価点(完成と不一致のセルの数)
        public ulong mBorad { get; set; }    //  ボードの状態

        /// <summary>
        /// コンストラクタ ボードの評価点とボード状態
        /// </summary>
        /// <param name="score">評価点</param>
        /// <param name="board">盤の状態</param>
        public SlideGameScore(int score, ulong board)
        {
            mScore = score;
            mBorad = board;
        }
        public int getScore()
        {
            return mScore;
        }
        public int CompareTo(SlideGameScore other)
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
            return CompareTo((SlideGameScore)other);
        }
    }

    /// <summary>
    /// A*探索による解法を行う(評価関数は一致している駒の数)
    /// 盤データをulong(64bit=4bitx16)で扱っているため4x4の15ゲームまでしか扱えません
    /// </summary>
    class SlideBoardSolver2
    {
        private int mBoardSize;                                 //  ボードのサイズ
        private ulong mBoardPattern;                            //  問題パターン
        private ulong mCompletePattern;                         //  完成形
        private ulong mUnComletePattern;                        //  非完成形
        private Dictionary<ulong, SlideGameBoard> mBoards;      //  探索したボードの登録8重複不可)
        private PriorityQueue<SlideGameScore> mScoreBoards;     //  評価点の優先順位キュー
        public string mErrorMsg;                                //  エラーメッセージ
        public enum Status { COMPLETE, UNCOMPLETE, UNNON };    //  盤の状態
        public Status mStat = Status.UNNON;

        private YLib ylib = new YLib();

        /// <summary>
        /// コンストラクタ 初期化
        /// </summary>
        /// <param name="tboard">問題パターン</param>
        public SlideBoardSolver2(sbyte[] tboard)
        {
            mBoardSize = (int)Math.Sqrt(tboard.Length);                 //  盤の大きさ(boardSize x boardSize)
            mBoardPattern = cnvArray2Ulong(tboard);                     //  盤の状態を2元配列からビットアドレスに変換する
            mBoards = new Dictionary<ulong, SlideGameBoard>();          //  探索した盤状態の登録リスト
            mScoreBoards = new PriorityQueue<SlideGameScore>(1024, 0);  //  優先順位キューに評価点を登録
            mCompletePattern = getCompletePattern();
            mUnComletePattern = getUnCompletePattern();
        }

        /// <summary>
        /// 解法処理
        /// 結果がfalseの時は例外エラーが発生
        /// </summary>
        /// <returns>結果</returns>
        public bool Solver()
        {
            //  初期登録
            SlideGameBoard board = new SlideGameBoard(mBoardPattern, -1);   //  盤状態の登録
            mBoards.Add(mBoardPattern, board);
            SlideGameScore scoreBoard = new SlideGameScore(getPriority(mBoardPattern), mBoardPattern);  //  評価点の登録
            mScoreBoards.Push(scoreBoard);

            try {
                while (0 < mScoreBoards.Count()) {
                    if (mScoreBoards.Peek().mScore == 0) {
                        mStat = Status.COMPLETE;
                        break;
                    }
                    //  評価点のもっとも高いものから探索する
                    if (nextPattern(mScoreBoards.Pop().mBorad)) //  現状の盤状態から次の盤状態を検索する
                        break;                                  //  解法できた場合
                    if (0 == mScoreBoards.Count())              //  登録データがなくなったら完了
                        break;
                }
            } catch (Exception e) {
                mErrorMsg = e.Message;
                return false;
            }
            return true;
        }

        /// <summary>
        /// 探索数の取得
        /// </summary>
        /// <returns>探索数</returns>
        public int getCount()
        {
            return mBoards.Count;
        }

        /// <summary>
        /// 現在の盤状態から次の盤の状態を求める
        /// 登録された盤のパターンを覗いて盤の状態と優先度を別々に登録する
        /// </summary>
        /// <param name="boardPattern">盤の状態</param>
        /// <returns>完成形または非完成形であればtrue</returns>
        private bool nextPattern(ulong boardPattern)
        {
            //printPattern(boardPattern);
            int spaceLoc = getSpaceLoc(boardPattern);
            List<int> locList = getSwapLocList(spaceLoc);           //  空白駒の位置から移動できる駒のリストを作る
            for (int i = 0; i < locList.Count; i++) {
                ulong nextPattern = swapLocData(boardPattern, spaceLoc, locList[i]);    //  駒リストから盤状態を求める
                if (!mBoards.ContainsKey(nextPattern)) {
                    //printPattern(nextPattern);
                    //  Boardリストにデータを登録
                    mBoards.Add(nextPattern, new SlideGameBoard(boardPattern, locList[i]));
                    //  Scoreリストにデータを登録
                    SlideGameScore scoreBoard = new SlideGameScore(getPriority(nextPattern), nextPattern);
                    mScoreBoards.Push(scoreBoard);
                    //  完成チェック
                    if (scoreBoard.mScore == 0 || nextPattern == mCompletePattern) {
                        mStat = Status.COMPLETE;                    //  解法が完了
                        return true;
                    } else if (nextPattern == mUnComletePattern) {
                        mStat = Status.UNCOMPLETE;                  //  解法できないパターンの場合
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// 探索結果の盤の移動駒の数値をリストにする
        /// 出力の順番は完成から問題パターンへ移動する駒の数値
        /// 逆順にすると解法順となる
        /// </summary>
        /// <returns>数値リスト</returns>
        public List<int> getResultList()
        {
            List<int> result = new List<int>();
            ulong board = getCompletePattern();
            int loc = 0;
            do {
                if (mBoards.ContainsKey(board)) {
                    loc = mBoards[board].mLoc;
                    board = mBoards[board].mPreBoard;
                    result.Add(get4BitData(board, loc));
                } else
                    break;

            } while (0 <= loc);

            return result;
        }

        /// <summary>
        /// 完成形の4bit配列を求める
        /// </summary>
        /// <returns>4bit配列</returns>
        private ulong getCompletePattern()
        {
            sbyte[] pat = new sbyte[mBoardSize * mBoardSize];
            for (int i = 0; i < pat.Length - 1; i++) {
                pat[i] = (sbyte)(i + 1);
            }
            pat[pat.Length - 1] = 0;
            return cnvArray2Ulong(pat);
        }

        /// <summary>
        /// 非完成形の4bit配列を求める(解法できないパターン)
        /// </summary>
        /// <returns>4bit配列</returns>
        private ulong getUnCompletePattern()
        {
            sbyte[] pat = new sbyte[mBoardSize * mBoardSize];
            for (int i = 0; i < pat.Length; i++) {
                if (pat.Length - 3 == i) {
                    pat[i] = (sbyte)(pat.Length - 1);
                } else if (pat.Length - 2 == i) {
                    pat[i] = (sbyte)(pat.Length - 2);
                } else if (pat.Length - 1 == i) {
                    pat[i] = 0;
                } else {
                    pat[i] = (sbyte)(i + 1);
                }
            }
            return cnvArray2Ulong(pat);
        }

        /// <summary>
        /// 盤の状態とプライオリティをデバッグ出力する
        /// </summary>
        /// <param name="pattern">盤の状態</param>
        private void printPattern(ulong pattern)
        {
            sbyte[] t = cnvUlong2Array(pattern);
            string buf = "";
            for (int i = 0; i < t.Length; i++)
                buf += t[i] + " ";
            buf += ": " + getPriority(pattern).ToString();
            System.Diagnostics.Debug.WriteLine(buf);
        }

        /// <summary>
        /// 盤の指定位置同士の値を入れ替える
        /// </summary>
        /// <param name="board">盤の状態</param>
        /// <param name="loc1">位置アドレス1</param>
        /// <param name="loc2">位置アドレス2</param>
        /// <returns>変更後の盤の状態</returns>
        private ulong swapLocData(ulong board, int loc1, int loc2)
        {
            int t1 = get4BitData(board, loc1);
            int t2 = get4BitData(board, loc2);
            board = set4BitData(board, loc2, t1);
            board = set4BitData(board, loc1, t2);
            return board;
        }

        /// <summary>
        /// 指定位置から上下左右の位置アドレスリストを求める
        /// </summary>
        /// <param name="loc">指定位置</param>
        /// <returns>位置アドレスリスト</returns>
        private List<int> getSwapLocList(int loc)
        {
            List<int> locList = new List<int>();
            if (0 < (loc / mBoardSize))                 //  上
                locList.Add(loc - mBoardSize);
            if ((loc / mBoardSize) < (mBoardSize - 1))  //  下
                locList.Add(loc + mBoardSize);
            if (0 < (loc % mBoardSize))                 //  左
                locList.Add(loc - 1);
            if ((loc % mBoardSize) < (mBoardSize - 1))  //  右
                locList.Add(loc + 1);
            return locList;
        }

        /// <summary>
        /// 盤の中で空白(0)の位置を求める
        /// </summary>
        /// <param name="board">盤の状態</param>
        /// <returns>空白の位置アドレス</returns>
        private int getSpaceLoc(ulong board)
        {
            for (int i = 0; i < mBoardSize * mBoardSize; i++) {
                if (get4BitData(board, i) == 0)
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// 盤の評価点を求める
        /// 評価点は完成形と不一致の数を求める
        /// </summary>
        /// <param name="board">盤の状態</param>
        /// <returns>評価点</returns>
        private int getPriority(ulong board)
        {
            int count = 0;
            int i = 0;
            for (i = 0; i < mBoardSize * mBoardSize - 1; i++) {
                if (get4BitData(board, i) != i + 1)
                    count++;
            }
            if (get4BitData(board, i) != 0)
                count++;
            return count;
        }

        /// <summary>
        /// 4bit配列のn番目に値を設定する
        /// </summary>
        /// <param name="bitVal"></param>
        /// <param name="n"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        private ulong set4BitData(ulong bitVal, int n, int data)
        {
            ulong t = (ulong)0x0f << (n * 4);
            bitVal &= ~t;
            bitVal |= (ulong)data << (n * 4);
            return bitVal;
        }

        /// <summary>
        /// 4bit配列からn番目の値を取り出す
        /// </summary>
        /// <param name="bitVal">4bit配列</param>
        /// <param name="n">取出し位置</param>
        /// <returns>値</returns>
        private int get4BitData(ulong bitVal, int n)
        {
            return (int)((bitVal >> (n * 4)) & 0xf);
        }

        /// <summary>
        /// byte配列をulong(4bitx16)配列に変換する
        /// </summary>
        /// <param name="byteArray">byte配列</param>
        /// <returns>ulong(4bit配列)</returns>
        private ulong cnvArray2Ulong(sbyte[] byteArray)
        {
            ulong board = 0;
            for (int i = 0; i < byteArray.Length; i++) {
                board = board | (ulong)byteArray[i] << (i * 4);
            }
            return board;
        }

        /// <summary>
        /// ulong(4Bitx16)配列をbyte配列に変換する
        /// </summary>
        /// <param name="bitVal">bit配列</param>
        /// <returns>byte配列</returns>
        private sbyte[] cnvUlong2Array(ulong bitVal)
        {
            sbyte[] array = new sbyte[mBoardSize * mBoardSize];
            for (int i = 0; i < mBoardSize * mBoardSize; i++) {
                array[i] = (sbyte)get4BitData(bitVal, i);
            }
            return array;
        }
    }
}
