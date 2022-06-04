using System;
using System.Collections.Generic;

namespace GameApp
{
    class SlideBoardSolver
    {
        private sbyte[] mBoard;                 //  問題のパターン
        private List<SlideBoard> mBoards;
        private int mLevel = 0;                 //  検索レベル
        public string mErrorMsg;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="board">盤の状態(問題パターン)</param>
        public SlideBoardSolver(sbyte[] board)
        {
            mBoard = board;
            mErrorMsg = "";
        }

        /// <summary>
        /// 問題を解法する
        /// </summary>
        /// <returns>成功(true)</returns>
        public bool solver()
        {
            mBoards = SlidePuzzleSolver(mBoard);    //  22手ぐらいまでがメモリの限界
            return mBoards == null ? false : true;
        }

        /// <summary>
        /// 解法結果の手順リストの取得
        /// (動かす駒のNoリスト)
        /// </summary>
        /// <returns>手順リスト</returns>
        public List<int> getResult()
        {
            return getSolverResult(mBoards);
        }

        /// <summary>
        /// 解法までの探索数を取得
        /// </summary>
        /// <returns>探索数</returns>
        public int getCount()
        {
            return mBoards.Count;
        }

        /// <summary>
        /// 解法までのレベル(解法手順数)を取得
        /// </summary>
        /// <returns>解法レベル8手順数)</returns>
        public int getLevel()
        {
            return mLevel;
        }

        /// <summary>
        /// 解法の結果(COMPLETE, UNCOMPLETE, UNNON)
        /// </summary>
        /// <returns>結果</returns>
        public SlideBoard.Status getStat()
        {
            return mBoards[mBoards.Count - 1].mStat;
        }

        /// <summary>
        ///  「15パズル」(スライディングパズル)の解法関数 幅優先探索
        ///  探索した結果、最終盤が完成であればそこから逆に探索すると解法手順が求められる
        ///  最大手順数(3x3で程度、それ以上だとメモリ不足になる可能性大)
        /// </summary>
        /// <param name="tboard">盤の状態(問題の盤)2次元配列を1次元にしたものの</param>
        /// <returns>探索結果の盤ののリスト</returns>
        private List<SlideBoard> SlidePuzzleSolver(sbyte[] tboard)
        {
            List<SlideBoard> boards = new List<SlideBoard>();
            int boardSize = (int)Math.Sqrt(tboard.Length);
            bool complete = false;                              //  完了の有無

            SlideBoard board = new SlideBoard(boardSize);
            board.setBoadData(tboard);                          //  パターンコピー
            boards.Add(board);
            int n = 0;
            mLevel = board.mLevel;
            try {
                while (!complete && n < boards.Count) {
                    List<SlideBoard> tempBoards = boards[n].nextPatterns();
                    if (tempBoards == null) {
                        complete = false;
                        mErrorMsg = boards[n].mErrorMsg;
                        break;
                    }
                    for (int i = 0; i < tempBoards.Count; i++) {
                        mLevel = tempBoards[i].mLevel;
                        tempBoards[i].mCurPos = boards.Count;
                        boards.Add(tempBoards[i]);
                        if (tempBoards[i].mStat != SlideBoard.Status.UNNON) {
                            complete = true;
                            break;
                        }
                    }
                    n++;
                }
            } catch (Exception e) {
                mErrorMsg = e.Message;
                complete = false;
                boards = null;
            }
            return boards;
        }

        /// <summary>
        /// 解を探索した結果、動かす駒のリスト
        /// </summary>
        /// <param name="boards">探索結果のボードリスト</param>
        /// <returns>駒のリスト</returns>
        private List<int> getSolverResult(List<SlideBoard> boards)
        {
            List<int> result = new List<int>();
            int n = boards.Count - 1;
            while (0 <= n) {
                SlideBoard tempBoard = boards[n];
                result.Add((int)tempBoard.mTitle);
                n = boards[n].mPrevPos;
            }
            return result;
        }
    }


    class SlideBoard
    {
        private int mBoardSize;     //  盤の大きさ
        private sbyte[] mBoard;     //  盤(2次元を1次元で保管)(row*borsSize + col)
        private sbyte mLoc;         //  座標(1次元での位置)移動したブロック移動前の位置(ブランクの位置)
        private sbyte mPrevLoc;     //  前回のブロック伊藤前の位置(1次元にした値)
        public sbyte mTitle;        //  移動したブロックの番号
        public int mPrevPos;        //  変更前のデータ保管位置
        public int mCurPos;         //  現在のデータ位置
        public int mLevel;          //  検索レベル(手順数)
        public enum Status { COMPLETE, UNCOMPLETE, UNNON };    //  盤の状態
        public Status mStat = Status.UNNON;
        public string mErrorMsg;

        /// <summary>
        /// コンストラクタ(初期化)
        /// </summary>
        /// <param name="size">ボードサイズ</param>
        public SlideBoard(int size)
        {
            mBoardSize = size;
            mBoard = new sbyte[mBoardSize * mBoardSize];
            initBoard();        //  盤面を0にする
            mTitle = -1;        //  ブロックの番号
            mLoc = -1;          //  ブランクの位置
            mPrevLoc = -1;      //  一つ前のブランクの位置
            mPrevPos = -1;
            mCurPos = 0;
            mLevel = 0;
        }

        /// <summary>
        /// 次のパターンを作る
        /// </summary>
        /// <returns>パターンリスト</returns>
        public List<SlideBoard> nextPatterns()
        {
            mErrorMsg = "";
            List<SlideBoard> boards = new List<SlideBoard>();
            int curPos = mCurPos;
            sbyte[] locs = getNextPosition();
            try {
                for (int i = 0; i < locs.Length; i++) {
                    if (mPrevLoc != locs[i]) {    //  もとの状態を除く
                        SlideBoard tempBoard = copyBoard();
                        tempBoard.swapBlank(locs[i]);
                        tempBoard.mTitle = mBoard[locs[i]];
                        tempBoard.mPrevLoc = tempBoard.mLoc;
                        tempBoard.mLoc = locs[i];
                        tempBoard.mCurPos = ++curPos;
                        tempBoard.setStat();
                        boards.Add(tempBoard);
                    }
                }
            } catch (Exception e) {
                mErrorMsg = e.Message;
                boards = null;
            }
            return boards;
        }

        /// <summary>
        /// 盤の状態を設定する(完成/完成不可/不明)
        /// </summary>
        public void setStat()
        {
            if (completeChk())
                mStat = Status.COMPLETE;
            else if (unCompleteChk())
                mStat = Status.UNCOMPLETE;
            else
                mStat = Status.UNNON;
        }

        /// <summary>
        /// 盤が完成していればtrueを返す
        /// </summary>
        /// <returns>完成(true)</returns>
        public bool completeChk()
        {
            for (int i = 0; i < mBoard.Length - 1; i++) {
                if (mBoard[i] != i + 1)
                    return false;
            }
            if (mBoard[mBoard.Length - 1] != 0)
                return false;
            return true;
        }

        /// <summary>
        /// 盤が完成しないパターンの確認
        /// 最後の2つのみが逆の場合完成しない(13,14,15,* ⇒ 13,15,14,*)
        /// </summary>
        /// <returns>完成しない(true)</returns>
        public bool unCompleteChk()
        {
            for (int i = 0; i < mBoard.Length - 3; i++)
                if (mBoard[i] != i + 1)
                    return false;
            if (mBoard[mBoard.Length - 3] != mBoard.Length - 1)
                return false;
            if (mBoard[mBoard.Length - 2] != mBoard.Length - 2)
                return false;
            if (mBoard[mBoard.Length - 1] != 0)
                return false;
            return true;
        }

        /// <summary>
        /// 盤を比較してすべて同じであればtrueを返す
        /// </summary>
        /// <param name="board">比較する盤のパターン</param>
        /// <returns>同じ(true)</returns>
        private bool compareChk(sbyte[] board)
        {
            for (int i = 0; i < mBoard.Length; i++)
                if (mBoard[i] != board[i])
                    return false;
            return true;
        }

        /// <summary>
        /// 盤のコピーを作成する
        /// </summary>
        /// <returns>盤クラス</returns>
        public SlideBoard copyBoard()
        {
            SlideBoard board = new SlideBoard(mBoardSize);
            board.mBoardSize = mBoardSize;
            for (int i = 0; i < mBoard.Length; i++)
                board.mBoard[i] = mBoard[i];
            board.mTitle = mTitle;
            board.mPrevLoc = mPrevLoc;
            board.mLoc = mLoc;
            board.mPrevPos = mCurPos;
            board.mCurPos = mCurPos + 1;
            board.mLevel = mLevel + 1;

            return board;
        }

        /// <summary>
        /// ブランク位置とデータを交換する
        /// </summary>
        /// <param name="loc">位置アドレス</param>
        public void swapBlank(sbyte loc)
        {
            sbyte n = mBoard[loc];
            mBoard[getBlankLoc()] = n;
            mBoard[loc] = 0;
        }

        /// <summary>
        /// ブランクと交換できる位置のリストを求める
        /// </summary>
        /// <returns>位置リスト</returns>
        public sbyte[] getNextPosition()
        {
            sbyte loc = getBlankLoc();
            return getNextPosition(loc);
        }

        /// <summary>
        /// 指定した位置と前後左右の位置のリストを作る
        /// </summary>
        /// <param name="loc">位置アドレス</param>
        /// <returns>位置リスト</returns>
        public sbyte[] getNextPosition(sbyte loc)
        {
            int row = loc / mBoardSize;
            int col = loc % mBoardSize;
            int size = 4;
            if (row == 0 || row == mBoardSize - 1)
                size--;
            if (col == 0 || col == mBoardSize - 1)
                size--;
            sbyte[] nextId = new sbyte[size];
            int i = 0;
            if (0 < col)
                nextId[i++] = (sbyte)(row * mBoardSize + col - 1);
            if (col < mBoardSize - 1)
                nextId[i++] = (sbyte)(row * mBoardSize + col + 1);
            if (0 < row)
                nextId[i++] = (sbyte)((row - 1) * mBoardSize + col);
            if (row < mBoardSize - 1)
                nextId[i++] = (sbyte)((row + 1) * mBoardSize + col);
            return nextId;
        }

        /// <summary>
        /// ブランクデータの位置を取得する
        /// </summary>
        /// <returns>位置アドレス</returns>
        public sbyte getBlankLoc()
        {
            for (sbyte i = 0; i < mBoard.Length; i++)
                if (mBoard[i] == 0)
                    return i;
            return -1;
        }

        /// <summary>
        /// 指定座標が盤内にあるかを確認する
        /// </summary>
        /// <param name="loc">位置アドレス</param>
        /// <returns>盤kの中(true)</returns>
        private bool chkPoint(byte loc)
        {
            if (loc < 0 || mBoard.Length <= loc)
                return false;
            return true;
        }

        /// <summary>
        /// 現在の盤のデータ位置を設定
        /// </summary>
        /// <param name="n">データ位置</param>
        public void setCurPos(int n)
        {
            mCurPos = n;
        }

        /// <summary>
        /// もととなった(前の状態)盤のNoを設定する
        /// </summary>
        /// <param name="n">データ位置</param>
        public void setPrevPos(int n)
        {
            mPrevPos = n;
        }

        /// <summary>
        /// 盤のパターンだけを設定する
        /// </summary>
        /// <param name="board">盤データ</param>
        public void setBoadData(sbyte[] board)
        {
            for (int i = 0; i < mBoardSize * mBoardSize; i++)
                mBoard[i] = board[i];
        }

        /// <summary>
        /// 盤の状態を出力する
        /// </summary>
        public void printStatus()
        {
            System.Diagnostics.Debug.WriteLine("CurNo: " + mCurPos + " prev: " + mPrevPos + " level: " + mLevel +
                    " loc: " + mLoc + " PrevLoc:" + mPrevLoc + " raw:" + mLoc / mBoardSize + " col: " + mLoc % mBoardSize +
                    " Title: " + mTitle + " Stat: " + mStat);
        }

        /// <summary>
        /// 盤ののパターンを出力する
        /// </summary>
        public void printBoard()
        {
            int i = 0;
            for (int row = 0; row < mBoardSize; row++) {
                String txt = "";
                for (int col = 0; col < mBoardSize; col++) {
                    txt += mBoard[i++] + " ";
                }
                System.Diagnostics.Debug.WriteLine(txt);
            }
        }

        /// <summary>
        /// 盤を初期化(1からインクリメントしながら数値を入れる、最後のマスは0にする)
        /// </summary>
        private void initBoard()
        {
            for (int i = 0; i < mBoard.Length; i++)
                mBoard[i] = (sbyte)(i + 1);
            mBoard[mBoard.Length - 1] = 0;
        }
    }
}
