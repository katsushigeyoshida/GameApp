using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using WpfLib;

namespace GameApp
{
    /// <summary>
    /// AllWhite.xaml の相互作用ロジック
    /// </summary>
    public partial class AllWhite : Window
    {
        private double mWindowWidth;
        private double mWindowHeight;
        private WindowState mWindowState = WindowState.Normal;

        YGButton ydraw;
        DispatcherTimer dispatcherTimer;        // タイマーオブジェクト

        private int mWidth = 1000;              //  Viewの論理サイズ
        private int mHeight = 1000;             //  Viewの論理サイズ
        private int mBoardSize = 3;             //  盤の数
        private float mBoardSizeRatio = 0.7f;   //  画面に対する盤の大きさの比
        private float mHaba;                    //  盤のマスの大きさ
        private float mOx, mOy;                 //  盤の原点
        private double mTextSize = 0;           //  文字の大きさ
        private int mCount = 0;                 //  実施回数
        private bool mDisp = false;

        private Random mRandom;
        private int mCreateBoardCount = 0;      //  盤の問題作成のための繰り返し数


        public AllWhite()
        {
            InitializeComponent();

            mWindowWidth = WindowForm.Width;
            mWindowHeight = WindowForm.Height;

            WindowFormLoad();

            ydraw = new YGButton(canvas);
            mRandom = new Random();

            //  タイマーインスタンスの作成
            dispatcherTimer = new DispatcherTimer(DispatcherPriority.Normal);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 300); //  日,時,分,秒,m秒
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);

            //  盤の大きさをコンボボックスに登録
            boardSizeCb.Items.Add("2");
            boardSizeCb.Items.Add("3");
            boardSizeCb.Items.Add("4");
            boardSizeCb.Items.Add("5");
            boardSizeCb.SelectedIndex = 1;
            mBoardSize = int.Parse(boardSizeCb.Text);
            //  操作回数を初期化
            mCount = 0;
            solverList.Text = "　　　　　　　";
        }

        private void WindowForm_Loaded(object sender, RoutedEventArgs e)
        {
            mWindowWidth = WindowForm.Width;
            mWindowHeight = WindowForm.Height;
            //  盤の初期表示
            mDisp = true;
            drawBoard();
        }

        /// <summary>
        /// ウィンドウサイズが変更になった時
        /// ウィンドウサイズに合わせて盤を再表示する
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WindowForm_LayoutUpdated(object sender, EventArgs e)
        {
            string txt = solverList.Text;

            //  最大化時の処理
            if (this.WindowState != mWindowState &&
                this.WindowState == WindowState.Maximized) {
                mWindowWidth = System.Windows.SystemParameters.WorkArea.Width;
                mWindowHeight = System.Windows.SystemParameters.WorkArea.Height;
            } else if (this.WindowState != mWindowState ||
                mWindowWidth != WindowForm.Width ||
                mWindowHeight != WindowForm.Height) {
                mWindowWidth = WindowForm.Width;
                mWindowHeight = WindowForm.Height;
            } else {
                return;
            }
            mWindowState = this.WindowState;
            drawBoard();    //  盤の再表示
            solverList.Text = txt;
        }

        private void WindowForm_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            WindowFormSave();
        }

        /// <summary>
        /// Windowの状態を前回の状態にする
        /// </summary>
        private void WindowFormLoad()
        {
            //  前回のWindowの位置とサイズを復元する(登録項目をPropeties.settingsに登録して使用する)
            Properties.Settings.Default.Reload();
            if (Properties.Settings.Default.AllWhiteWindowWidth < 100 || Properties.Settings.Default.AllWhiteWindowHeight < 100 ||
                System.Windows.SystemParameters.WorkArea.Height < Properties.Settings.Default.AllWhiteWindowHeight) {
                Properties.Settings.Default.AllWhiteWindowWidth = mWindowWidth;
                Properties.Settings.Default.AllWhiteWindowHeight = mWindowHeight;
            } else {
                WindowForm.Top = Properties.Settings.Default.AllWhiteWindowTop;
                WindowForm.Left = Properties.Settings.Default.AllWhiteWindowLeft;
                WindowForm.Width = Properties.Settings.Default.AllWhiteWindowWidth;
                WindowForm.Height = Properties.Settings.Default.AllWhiteWindowHeight;
            }
        }

        /// <summary>
        /// Window状態を保存する
        /// </summary>
        private void WindowFormSave()
        {
            //  Windowの位置とサイズを保存(登録項目をPropeties.settingsに登録して使用する)
            Properties.Settings.Default.AllWhiteWindowTop = WindowForm.Top;
            Properties.Settings.Default.AllWhiteWindowLeft = WindowForm.Left;
            Properties.Settings.Default.AllWhiteWindowWidth = WindowForm.Width;
            Properties.Settings.Default.AllWhiteWindowHeight = WindowForm.Height;
            Properties.Settings.Default.Save();
        }

        /// <summary>
        /// 盤サイズの変更コンボボックス
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void boardSizeCb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (0 < boardSizeCb.Text.Length) {
                mBoardSize = int.Parse(boardSizeCb.Items[boardSizeCb.SelectedIndex].ToString());
                mCount = 0;
                drawBoard();        //  盤の再表示
            }
        }

        /// <summary>
        /// [問題作成]ボタン 問題となる盤のパターンを作成
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void creatBtn_Click(object sender, RoutedEventArgs e)
        {
            createProblem();
        }

        /// <summary>
        /// [解法]ボタン 解法手順を左の欄に表示する
        /// 解放できるのは盤サイズ4まで、それ以上はメモリオーバーとなる
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (4 < mBoardSize) {
                solver2();
            } else {
                solver();
            }
            //if (MessageBox.Show("解法に時間がかかります(数十分)、またメモリ不足で解けない場合もあります\n" +
            //        "それでも実行しますか?", "警告", MessageBoxButton.OKCancel) == MessageBoxResult.Cancel)
            //        return;
        }

        /// <summary>
        /// [?]ボタン　ヘルプの表示
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void help_Click(object sender, RoutedEventArgs e)
        {
            HelpView help = new HelpView();
            help.mHelpText = HelpText.mAllWhiteHelp;
            help.Show();
        }

        /// <summary>
        /// マウス操作
        /// マウスをクリックした位置とその上下左右を反転する
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed) {
                Point sp = e.GetPosition(this);         //  マウスの位置
                //  上部と左部の分だけマウスの位置を調節する
                sp.X -= solverList.ActualWidth;
                sp.Y -= menuBar.ActualHeight;
                Point pt = ydraw.cnvScreen2World(sp);   //  マウスの位置を論理座標に変換
                //System.Diagnostics.Debug.WriteLine("MouseLeftDown: " + e.GetPosition(this).ToString() + "  " + pt.ToString());
                int id = ydraw.GButtonDownId(pt);       //  マウスの位置からセルのIDを求める
                if (0 <= id) {
                    reversBoard(id);                    //  IDから盤の関係するセルを反転
                    countMessage(mCount++);
                    //  全部白になったか確認する
                    if (ydraw.GButtonDownCount() == 0) {
                        completeMessage("完成");
                    } else {
                        completeMessage("　　");
                        axisTitle();
                    }
                }
            }
        }

        /// <summary>
        /// 盤の状態の取得
        /// </summary>
        /// <returns>盤の状態</returns>
        private byte[,] getBoard()
        {
            byte[,] board = new byte[mBoardSize, mBoardSize];
            for (int x = 0; x < mBoardSize; x++) {
                for (int y = 0; y < mBoardSize; y++) {
                    board[y, x] = (byte)(ydraw.GButtonDownGet(x * 100 + y) == true ? 1 : 0);
                }
            }
            return board;
        }

        /// <summary>
        /// タイマーTick処理 問題作成中のパターンを表示する
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            createProblemBoard();
            //throw new NotImplementedException();
        }

        /// <summary>
        /// 問題パターンの作成
        /// 乱数を使って問題を作成する
        /// </summary>
        public void createProblem()
        {
            mCount = 0;
            drawBoard();
            mCreateBoardCount = (int)Math.Floor(mRandom.NextDouble() * 10) + mBoardSize;

            dispatcherTimer.Start();    //  タイマースタート

            mCount = 1;
        }

        /// <summary>
        /// タイマー処理によって問題作成パターンをアニメーション表示する
        /// </summary>
        private void createProblemBoard()
        {
            int x = (int)Math.Floor(mRandom.NextDouble() * mBoardSize);
            int y = (int)Math.Floor(mRandom.NextDouble() * mBoardSize);
            reversBoard(x * 100 + y);       //  反転表示
            mCreateBoardCount--;

            if (mCreateBoardCount <= 0) {
                dispatcherTimer.Stop();     //  タイマー処理終了
            }
        }


        /// <summary>
        /// 盤の初期化
        /// </summary>
        private void drawBoard()
        {
            if (!mDisp)
                return;

            solverList.Text = "　　　　　　　";

            ydraw.setWindowSize(canvas.ActualWidth, canvas.ActualHeight);
            ydraw.setViewArea(0, 0, canvas.ActualWidth, canvas.ActualHeight);
            ydraw.setWorldWindow(0, 0, mWidth, mHeight);
            if (mTextSize == 0)
                mTextSize = ydraw.getTextSize();
            ydraw.clear();

            //  盤の大きさと位置
            mHaba = mWidth * mBoardSizeRatio / mBoardSize;
            mOx = (mWidth * (1 - mBoardSizeRatio)) / 2f;
            mOy = (mWidth * (1 - mBoardSizeRatio)) / 2f;
            //  盤をグラフィックボタンで作成
            if (mCount == 0) {
                ydraw.GButtonClear();
                for (int x = 0; x < mBoardSize; x++) {
                    for (int y = 0; y < mBoardSize; y++) {
                        ydraw.GButtonAdd(x * 100 + y, BUTTONTYPE.RECT, new Rect(
                            (float)x * mHaba + mOx, (float)y * mHaba + mOy, mHaba, mHaba));
                        ydraw.GButtonBorderThickness(x * 100 + y, 2f);
                    }
                }
            }

            countMessage(mCount);

            //  盤の表示
            axisTitle();
            ydraw.GButtonDraws();

        }

        /// <summary>
        /// セルの反転表示
        /// 指定されたセルとその上下左右のセルを反転表示する
        /// </summary>
        /// <param name="id"></param>
        private void reversBoard(int id)
        {
            int x, y;
            //  セルが押されたときの反転表示
            ydraw.GButtonDwonReversId(id);
            //  下のセルの反転表示
            x = id / 100;
            y = id % 100 - 1;
            if (0 <= x && x < mBoardSize && 0 <= y && y < mBoardSize)
                ydraw.GButtonDwonReversId(x * 100 + y);
            //  上のセルの反転表示
            x = id / 100;
            y = id % 100 + 1;
            if (0 <= x && x < mBoardSize && 0 <= y && y < mBoardSize)
                ydraw.GButtonDwonReversId(x * 100 + y);
            //  左のセルの反転表示
            x = id / 100 - 1;
            y = id % 100;
            if (0 <= x && x < mBoardSize && 0 <= y && y < mBoardSize)
                ydraw.GButtonDwonReversId(x * 100 + y);
            //  右のセルを反転表示
            x = id / 100 + 1;
            y = id % 100;
            if (0 <= x && x < mBoardSize && 0 <= y && y < mBoardSize)
                ydraw.GButtonDwonReversId(x * 100 + y);

            ydraw.GButtonDraws();
        }

        /// <summary>
        /// 行列番号の表示
        /// </summary>
        private void axisTitle()
        {
            ydraw.setTextSize(mTextSize * 4f);
            for (int x = 0; x < mBoardSize; x++) {
                ydraw.drawText("" + x, (float)x * mHaba + mOx + mHaba / 2f, mOy - (mTextSize * 6f), 0,
                    HorizontalAlignment.Center, VerticalAlignment.Top);
                ydraw.drawText("" + x, new Point(mOx - (mTextSize * 2f), (float)x * mHaba + mOy + mHaba / 2f), 0,
                    HorizontalAlignment.Right, VerticalAlignment.Center);
            }
        }

        /// <summary>
        /// 操作回数の表示(盤の下側)
        /// </summary>
        /// <param name="n"></param>
        private void countMessage(int n)
        {
            float x = mWidth / 2;
            float y = mOy + mHaba * mBoardSize;
            ydraw.setTextSize(mTextSize * 4f);
            ydraw.setTextOverWrite(false);
            ydraw.drawText("回数 : " + n, new Point(x, y), 0,
                HorizontalAlignment.Center, VerticalAlignment.Top);
            ydraw.setTextOverWrite(true);
        }

        /// <summary>
        /// 完成メッセージの表示(盤の中央)
        /// </summary>
        /// <param name="text"></param>
        private void completeMessage(String text)
        {
            ydraw.setTextSize(mTextSize * 8f);
            ydraw.setTextColor(Brushes.Red);
            ydraw.drawText(text, new Point(mWidth / 2, mHeight / 2), 0,
                HorizontalAlignment.Center, VerticalAlignment.Center);
            ydraw.setTextColor(Brushes.Black);
        }


        /// <summary>
        /// 盤の問題の解法と手順の表示(1)
        /// 幅優先探索による最短を探索するが盤が大きくなると時間がかかる
        /// </summary>
        private void solver()
        {
            byte[,] board;
            board = getBoard();
            string msg = "解法手順\n";

            AllWhiteSolver allWhiteSolver = new AllWhiteSolver(board);
            if (!allWhiteSolver.Solver()) {
                msg += "解法できず\n"
                    + allWhiteSolver.getCount() + "\n"
                    + allWhiteSolver.getErrorMsg();
            } else {
                msg += "探索数: " + allWhiteSolver.getCount() + "\n";
                msg += "探索;レベル: " + allWhiteSolver.getLevel() + "\n";
                msg += "No[行,列]\n";
                List<int> result = allWhiteSolver.getSolverResult();
                for (int i = 0; i < result.Count - 1; i++) {
                    int loc = result[i];
                    msg += (i + 1) + " [" + loc / mBoardSize + " , " + loc % mBoardSize + "]\n";
                }
            }

            solverList.Text = msg;
        }

        /// <summary>
        /// 盤の問題の解法と手順の表示(2)
        /// 評価点(反転の数)を用いた A*探索よる解法
        /// 必ずしも最適解ではないが比較的早く解が見つかる
        /// </summary>
        private void solver2()
        {
            byte[,] board;
            board = getBoard();
            string msg = "解法手順\n";

            AllWhiteSolver2 allWhiteSolver = new AllWhiteSolver2(board);
            if (allWhiteSolver.Solver()) {
                msg += "探索数:\n" + allWhiteSolver.getCount() + "\n";
                List<int[]> result = allWhiteSolver.getResultList();
                for (int i = result.Count - 2; 0 <= i; i--) {
                    msg += result.Count - i - 1 + ":[" + result[i][0] + "," + result[i][1] + "]\n";
                }
                solverList.Text = msg;
            } else {
                msg += "解法できず\n"
                    + allWhiteSolver.getCount() + "\n"
                    + allWhiteSolver.getErrorMsg();
            }
        }

    }
}
