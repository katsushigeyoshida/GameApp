namespace GameApp
{
    /// <summary>
    /// 【数独の解法】
    /// 解法手順(バックトラックによる)
    ///  1.各セルに対してその行と列、3x3のブロックに対して使用していない数値を候補として記録する
    ///  2.候補が1つしかない場合は確定値として盤データに記録し再度を候補値を検索する
    ///  3.1つだけの候補値がなくなるまで1と2を繰り返す。
    ///  4.各セルに対して順番に候補値をいれて盤の完成を確認する
    ///  5.候補値を入れるとき1と同じように重複がないことを確認しあれば次の候補値を使う
    ///  6.最終セルまで行って完成していなければ一つ戻って次の候補値を使う
    ///  7.候補値がなければさらに戻って同じことを行う
    /// 
    /// </summary>
    class SudokuSolver
    {
        private int[,,] mBoard = new int[9, 9, 11];   //  行y,列x,候補値(候補値が1つとなった時確定)
        private int count;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="board">問題パターン</param>
        public SudokuSolver(int[] board)
        {
            initBoard();
            for (int y = 0; y < mBoard.GetLength(0); y++) {
                for (int x = 0; x < mBoard.GetLength(1); x++) {
                    mBoard[y, x, 0] = board[y * mBoard.GetLength(0) + x];
                }
            }
            count = 0;
        }

        /// <summary>
        /// 実行結果の取得
        /// </summary>
        /// <returns>盤の状態</returns>
        public int[] getResult()
        {
            int[] result = new int[81];
            int n = 0;
            for (int y = 0; y < 9; y++)
                for (int x = 0; x < 9; x++)
                    result[n++] = mBoard[y, x, 0];
            return result;
        }

        /// <summary>
        /// 解法手順の操作回数
        /// </summary>
        /// <returns>操作回数</returns>
        public int getCount()
        {
            return count;
        }

        /// <summary>
        /// 数独の解法処理(preCheck()を実行した後におこなう)
        /// 解法は再帰処理で行う
        /// </summary>
        /// <param name="n">セルの番号(位置)</param>
        /// <returns>完成の成否</returns>
        public bool solver(int n)
        {
            int x = n % 9;
            int y = n / 9;
            // System.out.println("No."+n+" y="+y+" x="+x);
            if (8 < y) {
                if (completeCheck())        //  完成チェック
                    return true;
                else
                    return false;
            }
            if (mBoard[y, x, 0] == 0) {
                int i = 0;
                do {
                    i++;
                    // System.out.println("No."+n+" "+i+" "+mBoard[y][x][i]);
                    if (rowChexk(x, mBoard[y, x, i]) && columnChexk(y, mBoard[y, x, i]) && blockChexk(x, y, mBoard[y, x, i])) {
                        mBoard[y, x, 0] = mBoard[y, x, i];
                        // dispBoard();
                        count++;
                        if (x == 8 && y == 8) {
                            if (completeCheck())
                                return true;
                            else {
                                mBoard[y, x, 0] = 0;
                                return false;
                            }
                        }
                        if (solver(n + 1))
                            return true;
                    }
                } while (mBoard[y, x, i + 1] != 0);
            } else {
                return solver(n + 1);
            }
            mBoard[y, x, 0] = 0;
            return false;
        }

        /// <summary>
        /// 盤が完成したかをチェックする
        /// </summary>
        /// <returns> true: 完成</returns>
        private bool completeCheck()
        {
            for (int x = 0; x < 9; x++)
                if (!rowChexk(x))
                    return false;
            for (int y = 0; y < 9; y++)
                if (!columnChexk(y))
                    return false;
            for (int x = 0; x < 9; x += 3) {
                for (int y = 0; y < 9; y += 3) {
                    if (!blockChexk(x, y))
                        return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 列単位で重複チェックを行う
        /// </summary>
        /// <param name="x">列番</param>
        /// <returns>true:重複なし</returns>
        private bool rowChexk(int x)
        {
            int[] check = new int[10];
            for (int y = 0; y < 9; y++) {
                if (mBoard[y, x, 0] == 0)
                    return false;
                if (check[mBoard[y, x, 0]] == 1)
                    return false;
                else
                    check[mBoard[y, x, 0]] = 1;
            }
            return true;
        }

        /// <summary>
        /// 行単位で重複のチェックを行う
        /// </summary>
        /// <param name="y">行番</param>
        /// <returns>true:重複なし</returns>
        private bool columnChexk(int y)
        {
            int[] check = new int[10];
            for (int x = 0; x < 9; x++) {
                if (mBoard[y, x, 0] == 0)
                    return false;
                if (check[mBoard[y, x, 0]] == 1)
                    return false;
                else
                    check[mBoard[y, x, 0]] = 1;
            }
            return true;
        }

        /// <summary>
        /// ブロック単位で重複のチェックを行う
        /// </summary>
        /// <param name="x">列番</param>
        /// <param name="y">行番</param>
        /// <returns>true:重複なし</returns>
        private bool blockChexk(int x, int y)
        {
            int[] check = new int[10];
            int ox = x / 3 * 3;
            int oy = y / 3 * 3;
            for (x = ox; x < ox + 3; x++) {
                for (y = oy; y < oy + 3; y++) {
                    if (mBoard[y, x, 0] == 0)
                        return false;
                    if (check[mBoard[y, x, 0]] == 1)
                        return false;
                    else
                        check[mBoard[y, x, 0]] = 1;
                }
            }
            return true;
        }

        /// <summary>
        /// 一列の中に同じ値があるチェック
        /// </summary>
        /// <param name="x">列番</param>
        /// <param name="val">値</param>
        /// <returns>true:重複なし</returns>
        private bool rowChexk(int x, int val)
        {
            for (int y = 0; y < 9; y++) {
                if (mBoard[y, x, 0] == val)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// 一行の中に同じ値があるかチェック
        /// </summary>
        /// <param name="y">行番</param>
        /// <param name="val">値</param>
        /// <returns>true:重複なし</returns>
        private bool columnChexk(int y, int val)
        {
            for (int x = 0; x < 9; x++) {
                if (mBoard[y, x, 0] == val)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// ブロックの中に同じ値があるかチェック
        /// </summary>
        /// <param name="x">列番</param>
        /// <param name="y">行番</param>
        /// <param name="val">値</param>
        /// <returns>true:重複なし</returns>
        private bool blockChexk(int x, int y, int val)
        {
            int ox = x / 3 * 3;
            int oy = y / 3 * 3;
            for (x = ox; x < ox + 3; x++) {
                for (y = oy; y < oy + 3; y++) {
                    if (mBoard[y, x, 0] == val)
                        return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 事前チェックで確定する候補がなくなるまで候補地を求める
        /// </summary>
        public void preCheck()
        {
            do {
                preSubCheck();
            } while (0 < fixCheck());
        }

        /// <summary>
        /// 各セルに対して候補となる値を求める
        /// 各セルに対して使用できる数値を確認して複数登録する
        /// </summary>
        private void preSubCheck()
        {
            for (int y = 0; y < mBoard.GetLength(0); y++) {
                for (int x = 0; x < mBoard.GetLength(1); x++) {
                    if (mBoard[y, x, 0] == 0) {
                        setPass(x, y);
                    }
                }
            }
        }

        /// <summary>
        /// 候補が一つしかない場合は確定値とする
        /// </summary>
        /// <returns>確定値に変更した数</returns>
        private int fixCheck()
        {
            int n = 0;
            for (int y = 0; y < mBoard.GetLength(0); y++) {
                for (int x = 0; x < mBoard.GetLength(1); x++) {
                    if (mBoard[y, x, 0] == 0) {
                        if (0 < mBoard[y, x, 1] && mBoard[y, x, 2] == 0) {
                            mBoard[y, x, 0] = mBoard[y, x, 1];
                            n++;
                        }
                    }
                }
            }
            return n;
        }

        /// <summary>
        /// 候補となる値を登録
        /// </summary>
        /// <param name="x">列番</param>
        /// <param name="y">行番</param>
        private void setPass(int x, int y)
        {
            int n = 1;
            for (int k = 1; k <= 9; k++)
                mBoard[y, x, k] = 0;
            for (int val = 1; val <= 9; val++) {
                if (rowChexk(x, val) && columnChexk(y, val) && blockChexk(x, y, val))
                    mBoard[y, x, n++] = val;
            }
        }


        /// <summary>
        /// 盤データの初期化
        /// </summary>
        private void initBoard()
        {
            for (int i = 0; i < mBoard.GetLength(0); i++) {
                for (int j = 0; j < mBoard.GetLength(1); j++) {
                    for (int k = 0; k < mBoard.GetLength(2); k++)
                        mBoard[i, j, k] = 0;
                }
            }
        }
    }
}
