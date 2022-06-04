using System;
using WpfLib;

namespace GameApp
{
    class CreatSudokuProblem
    {
        //  http://www.net.c.dendai.ac.jp/~ynakajima/index.htm
        //  http://www.net.c.dendai.ac.jp/~ynakajima/furoku.html#7

        private int mBordSize = 9;          //  盤サイズ
        private int mBlockSize = 3;         //  ブロックサイズ
        private int mRepeat = 1000;         //  反復回数
        private Random mRand = new Random();

        public int[] mBoardDef;             //  完成形のパターン
        public int[] mBoardMax;             //  空白数の最も多いパターン
        public int mMaxCount;                //  空白セルの数
        public int mRepeatCount;            //  作成までマ繰り返し数

        public CreatSudokuProblem()
        {
            mBoardDef = new int[mBordSize * mBordSize];
            mBoardMax = new int[mBordSize * mBordSize];
        }

        /// <summary>
        /// 問題の取得
        /// ランダムで完成形を作成し、空白を追加でできた問題の内
        /// 指定空白数を超えたものか、作成数の内で最も空白の多いのを取得する
        /// </summary>
        /// <param name="n">空白の上限</param>
        /// <param name="repeat">問題作成回数</param>
        /// <returns></returns>
        public int[] getCreatProblem(int n, int repeat)
        {
            int[] board = new int[mBordSize * mBordSize];
            int countSp = 0;
            mMaxCount = 0;

            mRepeat = repeat;

            for (int j = 0; j < mRepeat; j++) {
                //  盤を0で初期化
                for (int i = 0; i < mBordSize * mBordSize; i++)
                    board[i] = 0;

                check(board, 0);
                cloneBoard(board, mBoardDef);
                maker2(board);
                countSp = count0(board);

                if (mMaxCount < countSp) {
                    mMaxCount = countSp;
                    cloneBoard(board, mBoardMax);
                }

                mRepeatCount = j + 1;

                if (n < countSp) {
                    return board;
                }
            }
            return mBoardMax;
        }

        /// <summary>
        /// 完成形の作成
        /// 左上から順に数を入れていくバックトラックのアルゴリズムを使用
        /// 数を入れてcanBePlaced()で正当性をチェック
        /// </summary>
        /// <param name="board"></param>
        /// <param name="pos"></param>
        private bool check(int[] board, int pos)
        {
            //  完成形を作成
            int i;
            int x;
            int newPos;
            int j = 0;

            //  Solution is found
            if (pos >= mBordSize * mBordSize)
                return false;
            //throw new Exception();      //  例外処理で抜ける

            //  Find a blank
            for (newPos = pos; newPos < mBordSize * mBordSize; ++newPos) {
                if (board[newPos] == 0)
                    break;
            }

            //  Check recursively
            for (x = 0; x < mBordSize; ++x) {
                int[] randBoard9 = new int[mBordSize];
                //  randBoard9 = 1-9 の値がランダムに入った配列、 n は　randomのシード値を変える
                rand9(randBoard9);
                int y = randBoard9[x];
                if (canBePlaced(board, newPos, y)) {
                    //  if （boardのnewPosにrand9(x)が入れられるなら）
                    board[newPos] = y;
                    if (!check(board, newPos + 1))
                        return false;
                    board[newPos] = 0;  //  backtracking
                }
            }
            return true;
        }

        /// <summary>
        /// １～９までの数字がランダム順に格納された配列を作成する関数
        /// １～９までの数字が順に入った配列を作成し、その中身をシャッフルする
        /// シャッフルは１番目とランダム番目をスワップ、２番目とランダム番目をスワップ…という行為を９番目まで行う
        /// </summary>
        /// <param name="randBoard"></param>
        private void rand9(int[] randBoard)
        {
            //  1-9の値をランダムに並び替える
            for (int x = 0; x < mBordSize; x++)
                randBoard[x] = x + 1;
            //  numbersをランダムに並び替える
            for (int i = 0; i < mBordSize; i++) {
                int j = mRand.Next(mBordSize);
                if (i != j) {
                    YLib.Swap<int>(ref randBoard[i], ref randBoard[j]);
                }
            }
        }

        /// <summary>
        /// 0～80までの数字がランダム順に格納された配列を作成
        /// maker1でランダム順に穴を空けていくために使用
        /// </summary>
        /// <param name="randBoard"></param>
        private void rand81(int[] randBoard)
        {
            //  0-80の値をランダムに並び替える
            for (int x = 0; x < mBordSize * mBordSize; x++)
                randBoard[x] = x;
            //  numbersをランダムに並び替える
            for (int i = 0; i < mBordSize * mBordSize; i++) {
                int j = mRand.Next(mBordSize * mBordSize);
                if (i != j) {
                    YLib.Swap<int>(ref randBoard[i], ref randBoard[j]);
                }
            }
        }

        /// <summary>
        /// 仮に入れた値が縦・横・その値の所属3x3マス(大枠)にあれば0返す。なければ1を返す
        /// </summary>
        /// <param name="board">盤データ</param>
        /// <param name="pos">指定位置</param>
        /// <param name="x">指定位置の値</param>
        /// <returns></returns>
        private bool canBePlaced(int[] board, int pos, int x)
        {
            //  check関数で仮に入れた値が縦・横・その値の所属3x3マス(大枠)にあれば0返す。なければ1を返す
            int row = pos / mBordSize;      //  行
            int col = pos % mBordSize;      //  列
            int i, j, topLeft;

            //  縦と横の重複チェック
            for (i = 0; i < mBordSize; ++i) {
                if (board[row * mBordSize + i] == x)
                    return false;
                if (board[col + i * mBordSize] == x)
                    return false;
            }
            //  ブロック内の重複チェック()ブロックの左上の位置を求めて行う)
            topLeft = mBordSize * (row / mBlockSize) * mBlockSize + (col / mBlockSize) * mBlockSize;
            for (i = 0; i < mBlockSize; ++i) {
                for (j = 0; j < mBlockSize; ++j) {
                    if (board[topLeft + i * mBordSize + j] == x)
                        return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 配列内の0の数を数える
        /// 作成した問題の空欄の数を調べるのに使用
        /// </summary>
        /// <param name="board"></param>
        /// <returns></returns>
        public int count0(int[] board)
        {
            int c = 0;
            for (int i = 0; i < mBordSize * mBordSize; i++)
                if (board[i] == 0)
                    c++;
            return c;
        }

        /// <summary>
        /// 配列のコピーを行う
        /// </summary>
        /// <param name="boardOriginal">複製元</param>
        /// <param name="boardCopy">複製先</param>
        private void cloneBoard(int[] boardOriginal, int[] boardCopy)
        {
            //  前の配列を後ろの配列にコピー
            for (int i = 0; i < mBordSize * mBordSize; i++)
                boardCopy[i] = boardOriginal[i];
        }

        /// <summary>
        /// 穴を開けるプログラム(完全にランダム順に穴を開けていくプログラム)
        /// ランダムに指定した場所を仮に0で置き換える
        /// そこに１～９の数字を入れていき、canBePlaced関数を用いてそこに入れることができるかを調べ、
        /// その結果そこに入る数字が元々入っていた数字のみならばそこは０で置き換えたままで、
        /// そうでない場合は元の数字に戻す
        /// </summary>
        /// <param name="board"></param>
        /// <param name="n"></param>
        private void maker1(int[] board)
        {
            //  問題作成1 ランダム
            int[] randBoard81 = new int[mBordSize * mBordSize];
            rand81(randBoard81);

            for (int x = 0; x < mBordSize * mBordSize; x++) {
                int c = 0;
                int tmp = board[randBoard81[x]];
                board[randBoard81[x]] = 0;
                for (int i = 1; i <= mBordSize; i++) {
                    if (canBePlaced(board, randBoard81[x], i))
                        c++;
                }
                if (c != 1) {
                    board[randBoard81[x]] = tmp;
                }
            }
        }

        /// <summary>
        /// 穴を開けるプログラム
        /// 穴を開ける順所を完全に指定し、真ん中→上→左→右→下→四隅の順に穴を開けるようにしたもの
        /// 81個の要素をもつ配列を全て手入力で入力することで順番の指定を行った
        /// </summary>
        /// <param name="board"></param>
        /// <param name="n"></param>
        private void maker2(int[] board)
        {
            //  問題作成 順番指定
            int[,] cntBoard = new int[,] { {
                46,47,48,10,11,12,55,56,57,
                49,50,51,13,14,15,58,59,60,
                52,53,54,16,17,18,61,62,63,
                19,20,21, 1, 2, 3,28,29,30,
                22,23,24, 4, 5, 6,31,32,33,
                25,26,27, 7, 8, 9,34,35,36,
                64,65,66,37,38,39,73,74,75,
                67,68,69,40,41,42,76,77,78,
                70,71,72,43,44,45,79,80,81,
                }, {
                01,02,03,10,11,12,19,20,21,
                04,05,06,13,14,15,22,23,24,
                07,08,09,16,17,18,25,26,27,
                55,56,57,64,65,66,73,74,75,
                58,59,60,67,68,69,76,77,78,
                61,62,63,70,71,72,79,80,81,
                28,29,30,37,38,39,46,47,48,
                31,32,33,40,41,42,49,50,51,
                34,35,36,43,44,45,52,53,54,
                }
            };
            int cntBoardNo = 0;
            for (int x = 0; x < mBordSize * mBordSize; x++) {
                int c = 0;
                int tmp = board[cntBoard[cntBoardNo, x] - 1];
                board[cntBoard[cntBoardNo, x] - 1] = 0;
                for (int i = 1; i <= mBordSize; i++) {
                    if (canBePlaced(board, cntBoard[0, x] - 1, i))
                        c++;
                }
                if (c != 1)
                    board[cntBoard[cntBoardNo, x] - 1] = tmp;
            }
        }
    }
}
