using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameApp
{
    class AllWhiteSolver
    {
        private string mErrorMsg;
        private List<AllWhiteBoard> mBoards;    //  検索パターンの盤リスト
        private byte[,] mBoardPattern;          //  盤のデータ(問題) 最大5x5まで
        private int mLevel = 0;                 //  探索の深さ

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="boardPattern">盤のデータ(問題)</param>
        public AllWhiteSolver(byte[,] boardPattern)
        {
            mErrorMsg = "";
            mBoardPattern = boardPattern;
            mBoards = new List<AllWhiteBoard>();
        }

        /// <summary>
        /// 「白にしろ」の解法関数 幅優先探索で完成手順を求める
        /// </summary>
        /// <returns>探索結果</returns>
        public bool Solver()
        {
            int boardSize = mBoardPattern.GetLength(0);         //  盤の大きさ(boardSize x boardSize)
            int prevNo = 0;
            bool complete = false;                              //  完了の有無

            AllWhiteBoard board = new AllWhiteBoard(boardSize); //  問題の盤のパターン
            board.setBoadData(mBoardPattern);                   //  パターンコピー
            mBoards.Add(board);                                 //  盤の状態をリストに追加
            int n = 0;

            //  パターン探索処理
            try {
                while (!complete && n < mBoards.Count) {
                    if (mBoards[n].mPrevNo == prevNo - 1) {
                        //  現盤の状態から次の盤のパターンを求める(反転や回転で同じになるものは除く)
                        List<AllWhiteBoard> tempBoards = mBoards[n].nextPatterns();
                        if (tempBoards == null) {
                            complete = false;
                            mErrorMsg = mBoards[n].mErrmsg;
                            break;
                        }
                        for (int i = 0; i < tempBoards.Count; i++) {
                            mLevel = Math.Max(mLevel, tempBoards[i].mLevel); //  検索の深さ
                            tempBoards[i].mCurNo = mBoards.Count;
                            mBoards.Add(tempBoards[i]);
                            //  盤がすべて白になったか確認
                            if (tempBoards[i].completeChk()) {
                                complete = true;
                                break;
                            }
                        }
                        n++;
                    } else {
                        prevNo++;
                    }
                }
            } catch (Exception e) {
                complete = false;
                //System.Diagnostics.Debug.WriteLine(TAG + " AllWhiteSolver: " + e.Message + " Size: " + n + " level: " + level);
                mErrorMsg = e.Message;
            }
            return complete;
        }

        /// <summary>
        /// 探索数の取得
        /// </summary>
        /// <returns></returns>
        public int getCount()
        {
            return mBoards.Count;
        }

        /// <summary>
        /// 探索の最大深さ
        /// </summary>
        /// <returns>深さレベル</returns>
        public int getLevel()
        {
            return mLevel;
        }
        /// <summary>
        /// 探索結果リストから解法手順を取得
        /// </summary>
        /// <returns>操作手順リスト([row][col]リスト)</returns>
        public List<int> getSolverResult()
        {
            List<int> result = new List<int>();
            int n = mBoards.Count - 1;
            while (0 <= n) {
                int loc = mBoards[n].mLoc;
                result.Add(loc);
                n = mBoards[n].mPrevNo;
            }
            return result;
        }

        public string getErrorMsg()
        {
            return mErrorMsg;
        }

    }

    /// <summary>
    /// パズル「白にしろ」の解法クラス
    /// 幅優先探索に解法
    /// </summary>
    class AllWhiteBoard
    {
        //  mBoard のデータによってメモリ使用量が変わる
        //  データサイズ 盤サイズ 5x5 → 1 + 25 + 2 + 12 = 40byte x パターン数(33,554,432) = 1.34GB
        //  アライメント(4byte)をすると 　　4 + 100 + 8 + 12 = 124bye x 33,554,432 = 4.15GB ?
        //  アライメント(8byte)をすると 　　8 + 200 + 16 + 24 = 248bye x 33,554,432 = 8.32GB ?
        //  byte[5,5]の時は 3,0402,166回、 4GB超のメモリで OutOfMemory 発生
        //
        //  盤状態をbit 配列として使う(loc = row * mBoardSize + col) MboardSize=5 ⇒ bitSize = 25bit
        sbyte mBoardSize;       //  盤の大きさ(1byte)
        //byte[,] mBoard;         //  盤        (5x5=25bye,4x4=16byte,3x3=9bye)
        uint mBoard;            //  盤状態をbit 配列として使う(loc = row * mBoardSize + col) MboardSize=5 ⇒ bitSize = 25bit
        public int mLoc;        //  座標(row * mBoardSize + col)
        public int mPrevNo;     //  変更前のNo(4byte)
        public int mCurNo;      //  現在のNo  (4byte)
        public int mLevel;      //  検索レベル(4byte)

        public string mErrmsg;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="size">盤のサイズ</param>
        public AllWhiteBoard(int size)
        {
            mBoardSize = (sbyte)size;
            //mBoard = new byte[mBoardSize,mBoardSize];
            mBoard = 0;         //  盤面の初期化
            //initBoard();        //  盤面を0にする
            mLoc = -1;
            mPrevNo = -1;
            mCurNo = 0;
            mLevel = 0;
        }

        //  現在の盤から次の盤のパターンを検索する
        //  検索したパターンの内ミラーや回転したときに同じパターンは除く
        public List<AllWhiteBoard> nextPatterns()
        {
            List<AllWhiteBoard> boards = new List<AllWhiteBoard>();
            int curNo = mCurNo;
            int bitCount = numofbits5(mBoard);
            //  盤上のすべての位置に指定してできたパターンから反転や回転で重複するものを除いて登録する
            try {
                for (int row = 0; row < mBoardSize; row++) {
                    for (int col = 0; col < mBoardSize; col++) {
                        //  前回の指定位置を除いて対象パターンを取得する
                        if (mLoc != bitLoc(row, col)) {
                            AllWhiteBoard tempBoard = copyBoard();
                            tempBoard.reverseBoard(row, col);               //  指定値の上下左右のセルを反転する
                            //if (bitCount < numofbits5(tempBoard.mBoard))    //  反転セルが増えるときは候補から外す
                            //    continue;

                            //  反転または回転したパターンが今までにあれば登録から除外
                            int i;
                            for (i = 0; i < boards.Count(); i++) {
                                if (boards[i].mirrorRotateChk(tempBoard))
                                    break;
                            }
                            if (boards.Count() <= i) {
                                //  パターンを登録(反転、回転パターンがないものだけ)
                                tempBoard.setCurNo(++curNo);
                                boards.Add(tempBoard);
                            }
                        }
                    }
                }
            } catch (Exception e) {
                System.Diagnostics.Debug.WriteLine("nextPatterns: " + e.Message);
                mErrmsg = "nextPatterns: " + e.Message;
                boards = null;
            }
            return boards;
        }

        public void incCurNo()
        {
            mCurNo++;
        }

        public void setCurNo(int n)
        {
            mCurNo = n;
        }

        /// <summary>
        /// 完成状態(盤がすべて[0])を確認
        /// </summary>
        /// <returns>完成</returns>
        public bool completeChk()
        {
            if (mBoard == 0)
                return true;
            else
                return false;
        }

        /// <summary>
        /// 指定の盤と比較してすべて同じであればtrueを返す
        /// </summary>
        /// <param name="board">盤データ</param>
        /// <returns></returns>
        private bool compareChk(uint board)
        {
            if (mBoard == board)
                return true;
            else
                return false;
        }

        /// <summary>
        /// 盤(Boardクラス)のコピーを作成する
        /// </summary>
        /// <returns></returns>
        public AllWhiteBoard copyBoard()
        {
            AllWhiteBoard board = new AllWhiteBoard(mBoardSize);
            board.mBoard = mBoard;          //  盤の状態をコピー
            board.mBoardSize = mBoardSize;
            board.mLoc = mLoc;

            board.mPrevNo = mCurNo;
            board.mCurNo = mCurNo + 1;
            board.mLevel = mLevel + 1;

            return board;
        }

        /// <summary>
        /// 現盤の指定位置の上下左右を反転する
        /// </summary>
        /// <param name="row">行</param>
        /// <param name="col">列</param>
        /// <returns>成否</returns>
        public bool reverseBoard(int row, int col)
        {
            if (!chkPoint(row, col))
                return false;
            mLoc = bitLoc(row, col);
            mBoard = reversePoint(row, col, mBoard);
            mBoard = reversePoint(row, col - 1, mBoard);
            mBoard = reversePoint(row, col + 1, mBoard);
            mBoard = reversePoint(row - 1, col, mBoard);
            mBoard = reversePoint(row + 1, col, mBoard);
            return true;
        }

        /// <summary>
        /// 指定位置の符号を反転
        /// </summary>
        /// <param name="row">行</param>
        /// <param name="col">列</param>
        /// <param name="board">盤データ(bit配列)</param>
        /// <returns>変更後の盤データ</returns>
        private uint reversePoint(int row, int col, uint board)
        {
            if (!chkPoint(row, col))
                return board;
            if (bitGet(board, bitLoc(row, col)) == 0)
                board = bitOn(board, bitLoc(row, col));
            else
                board = bitOff(board, bitLoc(row, col));
            return board;
        }

        /// <summary>
        /// 検索パターンを減らすために反転や回転で同じものがあるかを確認
        /// 反転や回転でできたパターンとの比較する
        /// 同じパターンが一つでもあればtrueを返す
        /// </summary>
        /// <param name="board">盤のクラス</param>
        /// <returns>反転/回転で同じものがあればtrue</returns>
        private bool mirrorRotateChk(AllWhiteBoard board)
        {
            bool[] chk = { true, true, true, true, true, true, true, true };
            for (int row = 0; row < mBoardSize; row++) {
                for (int col = 0; col < mBoardSize; col++) {
                    // System.out.println("mirrorChk: "+row+" "+col+" "+mBoardSize+" : ");
                    //+src[row][col]+" "+dest[row][mBoardSize - col - 1]);
                    if (bitGet(mBoard, bitLoc(row, col)) != bitGet(board.mBoard, bitLoc(row, col)))                    //  そのまま比較
                        chk[0] = false;
                    if (bitGet(mBoard, bitLoc(row, col)) != bitGet(board.mBoard, bitLoc(row, mBoardSize - col - 1)))   //  Y軸ミラー比較
                        chk[1] = false;
                    if (bitGet(mBoard, bitLoc(row, col)) != bitGet(board.mBoard, bitLoc(mBoardSize - row - 1, col)))   //  X軸ミラー比較
                        chk[2] = false;
                    if (bitGet(mBoard, bitLoc(row, col)) != bitGet(board.mBoard, bitLoc(mBoardSize - row - 1, mBoardSize - col - 1)))  //  XY軸ミラー
                        chk[3] = false;
                    if (bitGet(mBoard, bitLoc(row, col)) != bitGet(board.mBoard, bitLoc(col, mBoardSize - row - 1)))   //  90°回転比較
                        chk[4] = false;
                    if (bitGet(mBoard, bitLoc(row, col)) != bitGet(board.mBoard, bitLoc(mBoardSize - col - 1, row)))   //  270°回転比較
                        chk[5] = false;
                    if (bitGet(mBoard, bitLoc(row, col)) != bitGet(board.mBoard, bitLoc(col, row)))                    //  対角線ミラー比較
                        chk[6] = false;
                    if (bitGet(mBoard, bitLoc(row, col)) != bitGet(board.mBoard, bitLoc(mBoardSize - col - 1, mBoardSize - row - 1)))  //  逆対角線ミラー比較
                        chk[7] = false;
                }
            }
            for (int i = 0; i < chk.Length; i++) {
                if (chk[i] == true)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 指定座標が盤内にあるかを確認する
        /// </summary>
        /// <param name="row">行</param>
        /// <param name="col">列</param>
        /// <returns></returns>
        private bool chkPoint(int row, int col)
        {
            if (row < 0 || mBoardSize <= row)
                return false;
            if (col < 0 || mBoardSize <= col)
                return false;
            return true;
        }

        /// <summary>
        ///  もととなった盤のNoを設定する
        /// </summary>
        /// <param name="n">盤No</param>
        public void setPrevNo(int n)
        {
            mPrevNo = n;
        }

        //  盤のパターンだけを設定する
        /// <summary>
        /// 行列の盤のパターンをbit配列に変換して格納する
        /// </summary>
        /// <param name="board">盤のパターン</param>
        public void setBoadData(byte[,] board)
        {
            mBoard = 0;
            for (int row = 0; row < mBoardSize; row++)
                for (int col = 0; col < mBoardSize; col++) {
                    if (board[row, col] != 0)
                        mBoard = bitOn(mBoard, bitLoc(row, col));
                }

        }

        //  盤ののパターンを出力する
        public void printBoard()
        {
            System.Diagnostics.Debug.WriteLine("CurNo: " + mCurNo + " prev: " + mPrevNo + " row:" + bitLoc2Row(mLoc) + " col: " + bitLoc2Col(mLoc));
            for (int row = 0; row < mBoardSize; row++) {
                String txt = "";
                for (int col = 0; col < mBoardSize; col++) {
                    txt += bitGet(mBoard, bitLoc(row, col)) + " ";
                }
                System.Diagnostics.Debug.WriteLine(txt);
            }
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

        /// <summary>
        /// nビット目の値を1にする
        /// </summary>
        /// <param name="a">bit配列データ(uint)</param>
        /// <param name="n">nビット目</param>
        /// <returns>^変更後のデータ</returns>
        private uint bitOn(uint a, int n)
        {
            uint b = 1;
            b <<= n;
            return a | b;
        }

        /// <summary>
        /// nビット目の値を0にする
        /// </summary>
        /// <param name="a">bit配列データ(uint)</param>
        /// <param name="n">nビット目</param>
        /// <returns>^変更後のデータ</returns>
        private uint bitOff(uint a, int n)
        {
            uint b = 1;
            b <<= n;
            return a & (~b);
        }

        /// <summary>
        /// nビット目の値(0/1)を取得
        /// </summary>
        /// <param name="a">bit配列データ(uint)</param>
        /// <param name="n">nビット目</param>
        /// <returns>値(0/1)</returns>
        private int bitGet(uint a, int n)
        {
            uint b = 1;
            b <<= n;
            return (int)(0 == (a & b) ? 0 : 1);
        }

        /// <summary>
        /// bitの数を数える
        /// </summary>
        /// <param name="bits">数値</param>
        /// <returns>bit数</returns>
        private int numofbits5(long bits)
        {
            bits = (bits & 0x55555555) + (bits >> 1 & 0x55555555);
            bits = (bits & 0x33333333) + (bits >> 2 & 0x33333333);
            bits = (bits & 0x0f0f0f0f) + (bits >> 4 & 0x0f0f0f0f);
            bits = (bits & 0x00ff00ff) + (bits >> 8 & 0x00ff00ff);
            return (int)((bits & 0x0000ffff) + (bits >> 16 & 0x0000ffff));
        }
    }
}
