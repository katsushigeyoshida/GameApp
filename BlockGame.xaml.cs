using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using WpfLib;

namespace GameApp
{
    /// <summary>
    /// BlockGame.xaml の相互作用ロジック
    /// </summary>
    public partial class BlockGame : Window
    {
        private double mWindowWidth;                            //  ウィンドウの高さ
        private double mWindowHeight;                           //  ウィンドウ幅
        private double mPrevWindowWidth;                        //  変更前のウィンドウ幅
        private WindowState mWindowState = WindowState.Normal;  //  ウィンドウの状態(最大化/最小化)

        private double mWidth;                          //	描画領域の幅
        private double mHeight;                         //	描画領域の高さ
        private double mAspect = 0.75f;                 //  描画領域縦横比
        private double mAreaRate;                       //  画面サイズに対する描画比率
        private double mTextSize;                       //  文字サイズ

        private double WIDTH = 640;                     //  描画領域
        private double HEIGHT = 480;
        private Brush[] mColortable = { Brushes.Red, Brushes.Yellow, Brushes.Green};

        private double mBr = 10;                        //  ボールの半径
        private double mPaddlew = 96;                   //  パドルの幅
        private double mPaddleh = 16;                   //  パドルの高さ
        private double mBlockw = 54;                    //  ブロックの幅
        private double mBlockh = 24;                    //  ブロックの高さ
        private Rect mBlocksBack;                       //  ブロックの全体領域
        private bool mGameover = false;
        private int mWinCount = 0;

        private double mBallx;                          //  ボールのX座標
        private double mBally;                          //  ボールのY座標
        private double mBx1;                            //  ボールの速度
        private double mBy1;                            //  ボールの速度
        private Rect mPaddle;                           //  パドルの座標
        private Rect mPrePaddle;                        //  パドルの前回位置
        private List<Rect> mBlocks;                     //  ブロックの座標
        private double mBullAccell = 1.015;             //  ボールがブロックにあたった時の速度アップの割合

        private double mButtonRad;                      //  グラフィックボタンの半径
        private double mLButtonX;                       //  左ボタンのx座標
        private double mLButtonY;                       //  左ボタンのy座標
        private double mRButtonX;                       //  右ボタンのx座標
        private double mRButtonY;                       //  右ボタンのy座標
        private double mRetryButtonX;
        private double mRetryButtonY;
        private enum BUTTONTOUCH { NON = 0, LEFT =1, RIGHT=2, PAUSE=3, START=4, END=5 };
        private Brush mBackColor = Brushes.Blue;        //  背景色

        private int mTimerInteval = 8;                //  タイマーインターバル(m秒)
        private double mBallDx = 1;
        private double mBallDy = -1;

        private DispatcherTimer dispatcherTimer;        // タイマーオブジェクト
        private YGButton ydraw;                         //  グラフィックライブラリ
        private Random mRandom;

        public BlockGame()
        {
            InitializeComponent();

            mWindowWidth = this.Width;
            mWindowHeight = this.Height;
            mPrevWindowWidth = mWindowWidth;

            WindowFormLoad();       //  Windowの位置とサイズを復元

            ydraw = new YGButton(CvBoard);
            mRandom = new Random();

            //  タイマーインスタンスの作成
            dispatcherTimer = new DispatcherTimer(DispatcherPriority.Normal);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0,
                mTimerInteval / 1000, mTimerInteval % 1000);    //  日,時,分,秒,m秒
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            InitScreen();
            InitParameter();
            drawGame();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            WindowFormSave();       //  ウィンドの位置と大きさを保存
        }

        private void Window_LayoutUpdated(object sender, EventArgs e)
        {
            if (this.WindowState != mWindowState &&
                this.WindowState == WindowState.Maximized) {
                //  ウィンドウの最大化時
                mWindowWidth = System.Windows.SystemParameters.WorkArea.Width;
                mWindowHeight = System.Windows.SystemParameters.WorkArea.Height;
            } else if (this.WindowState != mWindowState ||
                mWindowWidth != this.Width ||
                mWindowHeight != this.Height) {
                //  ウィンドウサイズが変わった時
                mWindowWidth = this.Width;
                mWindowHeight = this.Height;
            } else {
                //  ウィンドウサイズが変わらない時は何もしない
                mWindowState = this.WindowState;
                return;
            }
            mWindowState = this.WindowState;
            //  ウィンドウの大きさに合わせてコントロールの幅を変更する
            double dx = mWindowWidth - mPrevWindowWidth;
            mPrevWindowWidth = mWindowWidth;
            //  表示の更新
            InitScreen();
            InitParameter();
            drawGame();
        }

        /// <summary>
        /// Windowの状態を前回の状態にする
        /// </summary>
        private void WindowFormLoad()
        {
            //  前回のWindowの位置とサイズを復元する(登録項目をPropeties.settingsに登録して使用する)
            Properties.Settings.Default.Reload();
            if (Properties.Settings.Default.BlockGameWindowWidth < 100 || 
                Properties.Settings.Default.BlockGameWindowHeight < 100 ||
                SystemParameters.WorkArea.Height < Properties.Settings.Default.BlockGameWindowHeight) {
                Properties.Settings.Default.BlockGameWindowWidth = mWindowWidth;
                Properties.Settings.Default.BlockGameWindowHeight = mWindowHeight;
            } else {
                this.Top = Properties.Settings.Default.BlockGameWindowTop;
                this.Left = Properties.Settings.Default.BlockGameWindowLeft;
                this.Width = Properties.Settings.Default.BlockGameWindowWidth;
                this.Height = Properties.Settings.Default.BlockGameWindowHeight;
            }
        }

        /// <summary>
        /// Window状態を保存する
        /// </summary>
        private void WindowFormSave()
        {
            //  Windowの位置とサイズを保存(登録項目をPropeties.settingsに登録して使用する)
            Properties.Settings.Default.BlockGameWindowTop = this.Top;
            Properties.Settings.Default.BlockGameWindowLeft = this.Left;
            Properties.Settings.Default.BlockGameWindowWidth = this.Width;
            Properties.Settings.Default.BlockGameWindowHeight = this.Height;
            Properties.Settings.Default.Save();
        }

        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            drawGame();
            if (mGameover || mBlocks.Count == 0)
                dispatcherTimer.Stop();
            //throw new NotImplementedException();
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if(e.Key == System.Windows.Input.Key.Left) {
                opreationCheck(BUTTONTOUCH.LEFT);
            } else if (e.Key == System.Windows.Input.Key.Right) {
                opreationCheck(BUTTONTOUCH.RIGHT);
            } else if (e.Key == System.Windows.Input.Key.S) {
                opreationCheck(BUTTONTOUCH.START);
            } else if (e.Key == System.Windows.Input.Key.E) {
                opreationCheck(BUTTONTOUCH.END);
            } else if (e.Key == System.Windows.Input.Key.P) {
                opreationCheck(BUTTONTOUCH.PAUSE);
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Button bt = (Button)e.Source;
            if (bt == BtHelp) {
                HelpView help = new HelpView();
                help.mHelpText = HelpText.mBlockGameHelp;
                help.Show();
            } else if (bt == BtStart) {
                opreationCheck(BUTTONTOUCH.START);
            } else if (bt == BtEnd) {
                opreationCheck(BUTTONTOUCH.END);
            } else if (bt == BtLeft) {
                opreationCheck(BUTTONTOUCH.LEFT);
            } else if (bt == BtRight) {
                opreationCheck(BUTTONTOUCH.RIGHT);
            }
        }

        private void CvBoard_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed) {
                Point sp = e.GetPosition(this);         //  マウスの位置
                //  上部と左部の分だけマウスの位置を調節する
                //sp.X -= solverList.ActualWidth;
                //sp.Y -= menuBar.ActualHeight;
                Point pt = ydraw.cnvScreen2World(sp);   //  マウスの位置を論理座標に変換
                int id = ydraw.GButtonDownId(pt);       //  マウスの位置からセルのIDを求める
                System.Diagnostics.Debug.WriteLine("ID: {0} X {1} Y {2}", id, sp.X, sp.Y);
                if (id == 1) {          //  左ボタン
                    opreationCheck(BUTTONTOUCH.LEFT);
                } else if (id == 2) {   //  右ボタン
                    opreationCheck(BUTTONTOUCH.RIGHT);
                } else if (id == 3) {   //  再開ボタン
                    opreationCheck(BUTTONTOUCH.PAUSE);
                }
            }
        }

        /// <summary>
        /// 操作キーのチェック
        /// </summary>
        /// <param name="buttonTouch"></param>
        private void opreationCheck(BUTTONTOUCH buttonTouch)
        {
            if (buttonTouch == BUTTONTOUCH.LEFT) {
                mPaddle.X -= mPaddlew / 2;
            } else if (buttonTouch == BUTTONTOUCH.RIGHT) {
                mPaddle.X += mPaddlew / 2;
            } else if (buttonTouch == BUTTONTOUCH.PAUSE) {
                if (dispatcherTimer.IsEnabled) {
                    dispatcherTimer.Stop();
                    ydraw.GButtonTitle((int)BUTTONTOUCH.PAUSE, "再開");
                } else {
                    dispatcherTimer.Start();
                    ydraw.GButtonTitle((int)BUTTONTOUCH.PAUSE, "停止");
                }
            } else if (buttonTouch == BUTTONTOUCH.START) {
                dispatcherTimer.Start();
                ydraw.GButtonTitle((int)BUTTONTOUCH.PAUSE, "停止");
                InitParameter();
            } else if (buttonTouch == BUTTONTOUCH.END) {
                dispatcherTimer.Stop();
                ydraw.GButtonTitle((int)BUTTONTOUCH.PAUSE, "再開");
                InitParameter();
            } else {
                return;
            }
            //  パドルが領域を越えた時に元に戻す
            if (mPaddle.Left <= 0) {
                mPaddle.X = 0;
            } else if (WIDTH <= mPaddle.Right) {
                mPaddle.X = WIDTH - mPaddle.Width;
            }
            drawGame();
        }

        /// <summary>
        /// Windowの初期化
        /// Windowエリアの設定
        /// </summary>
        private void InitScreen()
        {
            //  Windowエリアの設定
            mWidth = CvBoard.ActualWidth;
            mHeight = CvBoard.ActualHeight;
            ydraw.setWindowSize(mWidth, mHeight);
            ydraw.setViewArea(0, 0, mWidth, mHeight);
            ydraw.setWorldWindow(0, 0, mWidth, mHeight);
            //  マージンの追加
            double xmargine = Math.Abs(ydraw.screen2worldXlength(10));
            double ymargine = Math.Abs(ydraw.screen2worldYlength(10));
            ydraw.setWorldWindow(-xmargine, -ymargine, mWidth+xmargine, mHeight+ymargine);
            //  テキストサイズ取得
            if (mTextSize == 0)
                mTextSize = ydraw.getTextSize();
            //  画面クリアし背景色と枠設定
            ydraw.clear();
            ydraw.setColor(Brushes.Black);
            ydraw.drawRectangle(new Point(0, 0), new Point(mWidth, mHeight), 0);
        }

        /// <summary>
        /// 各種パラメータの初期化
        /// </summary>
        private void InitParameter()
        {
            //  表示エリアの縦横比
            mAreaRate = mWidth / WIDTH;
            //  表示有効範囲設定
            WIDTH = mWidth;
            HEIGHT = WIDTH * mAspect;

            //  パドルの大きさ
            mPaddlew *= mAreaRate;
            mPaddleh *= mAreaRate;
            //  パドルの位置
            double x = WIDTH / 2;
            double y = HEIGHT * 9 / 10 - mPaddleh * 3f * mWinCount;
            mPaddle = new Rect(x - (mPaddlew / 2), y - (mPaddleh / 2), x + (mPaddlew / 2), y + (mPaddleh / 2));
            mPrePaddle = new Rect();
            mPrePaddle = mPaddle;

            //  ブロックの大きさ
            mBlockw *= mAreaRate;
            mBlockh *= mAreaRate;
            //  ブロックと壁との隙間
            double mBlockMargin = (WIDTH - (mBlockw + 4) * 10) / 2;
            //  ブロックの位置
            if (mBlocks == null) {
                mBlocks = new List<Rect>();
            } else {
                mBlocks.Clear();
            }
            for (int i = 0; i < 50; i++) {
                x = (i % 10) * (mBlockw + 4) + mBlockMargin;
                y = (int)(i / 10) * (mBlockh + 4) + mBlockMargin;
                mBlocks.Add(new Rect(x, y, mBlockw, mBlockh));
            }
            //  ブロック全体の領域
            mBlocksBack = new Rect(new Point(mBlocks[0].Left, mBlocks[0].Top),
                    new Point(mBlocks[mBlocks.Count - 1].Right, mBlocks[mBlocks.Count - 1].Bottom));

            //  ボールの大きさ
            mBr *= mAreaRate;

            //  操作ボタンの位置と大きさ
            //mButtonRad = mWidth / 12;
            //mLButtonX = mButtonRad * 3 - mButtonRad / 2;
            //mLButtonY = mHeight - mButtonRad * 1.5 - mButtonRad / 2;
            //mRButtonX = mWidth - mButtonRad * 3 - mButtonRad / 2;
            //mRButtonY = mHeight - mButtonRad * 1.5 - mButtonRad / 2;
            //mRetryButtonX = mWidth / 2 - mButtonRad / 2;
            //mRetryButtonY = mHeight - mButtonRad * 1.5 - mButtonRad / 2;
            //  操作ボタン位置の設定
            //ydraw.GButtonClear();
            //ydraw.GButtonAdd((int)BUTTONTOUCH.LEFT, BUTTONTYPE.CIRCLE,
            //    new Rect(mLButtonX, mLButtonY, mButtonRad, mButtonRad));
            //ydraw.GButtonAdd((int)BUTTONTOUCH.RIGHT, BUTTONTYPE.CIRCLE,
            //    new Rect(mRButtonX, mRButtonY, mButtonRad, mButtonRad));
            //ydraw.GButtonAdd((int)BUTTONTOUCH.PAUSE, BUTTONTYPE.CIRCLE,
            //    new Rect(mRetryButtonX, mRetryButtonY, mButtonRad, mButtonRad));
            //ydraw.GButtonTitleColor((int)BUTTONTOUCH.LEFT, Brushes.Black);
            //ydraw.GButtonTitleColor((int)BUTTONTOUCH.RIGHT, Brushes.Black);
            //ydraw.GButtonTitleColor((int)BUTTONTOUCH.PAUSE, Brushes.Black);
            //ydraw.GButtonTitle((int)BUTTONTOUCH.LEFT, "左");
            //ydraw.GButtonTitle((int)BUTTONTOUCH.RIGHT, "右");
            //ydraw.GButtonTitle((int)BUTTONTOUCH.PAUSE, "開始");

            //  パドルの位置
            x = WIDTH / 2;
            y = HEIGHT * 9 / 10 - mPaddleh * 3 * mWinCount;
            mPaddle = new Rect(new Point(x - (mPaddlew / 2), y - (mPaddleh / 2)), new Point(x + (mPaddlew / 2), y + (mPaddleh / 2)));
            mPrePaddle.X = mPaddle.Left;

            //  ボールの位置
            mBallx = WIDTH / 2;
            mBally = (mPaddle.Top + mBlocks[mBlocks.Count - 1].Bottom) / 2;

            //  ボールの移動量
            mBx1 = mBallDx * mAreaRate * (1d + (double)mRandom.Next(50) / 100d);
            mBy1 = mBallDy * mAreaRate;

            mGameover = false;
        }

        /// <summary>
        /// ゲームの実行
        /// </summary>
        private void drawGame()
        {
            ballMoveCalc();
            ScreenClear();
            drawBlocks();
            drawPaddle();
            drawBall();
            statusCheck();
        }

        /// <summary>
        /// 画面クリア
        /// </summary>
        private void ScreenClear()
        {
            ydraw.clear();
            //  初期描画
            ydraw.setFillColor(mBackColor);
            ydraw.drawRectangle(new Point(0, 0), new Point(mWidth, mHeight), 0);
            //  操作ボタン表示
            ydraw.GButtonDraws();
        }

        /// <summary>
        /// ブロックの表示設定
        /// </summary>
        private void drawBlocks()
        {
            if (mBlocks == null)
                return;

            ydraw.setFillColor(mBackColor);
            //ydraw.drawRectangle(mBlocksBack, 0);
            for (int i = 0; i < mBlocks.Count; i++) {
                ydraw.setFillColor(mColortable[(int)(mBlocks[i].Top / 28) % 3]);
                ydraw.drawRectangle(mBlocks[i], 0);
            }
        }

        /// <summary>
        /// パドルの表示
        /// </summary>
        private void drawPaddle()
        {
            ydraw.setFillColor(Brushes.White);
            ydraw.drawRectangle(mPaddle, 0);
            mPaddle.X = mPaddle.Left;
        }

        /// <summary>
        /// ボールの表示
        /// </summary>
        private void drawBall()
        {
            ydraw.setFillColor(Brushes.White);
            ydraw.drawCircle(mBallx, mBally, mBr);
        }

        /// <summary>
        /// ボール位置とブロック、パドルとの関係を計算
        /// </summary>
        private void ballMoveCalc()
        {
            double x = mBallx + mBx1;
            double y = mBally + mBy1;
            //  画面枠に対してのボールの反射
            if (x < mBr || (WIDTH - mBr) < x)
                mBx1 = -mBx1;
            if (y < mBr)
                mBy1 = -mBy1;
            if (HEIGHT < y)
                mGameover = true;

            //  パドルの中心とのボールとの距離
            double dx = mPaddle.X + mPaddle.Width / 2 - x;
            double dy = mPaddle.Y + mPaddle.Height / 2 - y;
            //  パドルに対してのボールの反射
            if (dy == 0)
                dy = 1f;
            if (Math.Abs(dx) < (mPaddlew / 2 + mBr) && Math.Abs(dy) < (mPaddleh / 2 + mBr)) {
                //  パドルの上下か左右の判定
                if (Math.Abs(dx / dy) > (mPaddlew / mPaddleh)) {
                    //  左右にあたる
                    mBx1 = -mBx1;
                    mBallx = (mPaddle.X + mPaddle.Width / 2) * Math.Sign(dx) * (mPaddlew / 2 + mBr);
                } else {
                    //  上下にあたる
                    mBy1 = -mBy1;
                    mBally = (mPaddle.Y + mPaddle.Height / 2) - Math.Sign(dy) * (mPaddleh / 2 + mBr);
                }
            }

            //  ブロックに対してのボールの反射
            for (int i = 0; i < mBlocks.Count; i++) {
                dx = (mBlocks[i].Left + mBlocks[i].Width / 2) - x;
                dy = (mBlocks[i].Top + mBlocks[i].Height / 2) - y;
                if (dy == 0)
                    dy = 1;
                //  ボールがブロックにあたった時
                if (Math.Abs(dx) < (mBlockw / 2 + mBr) && Math.Abs(dy) < (mBlockh / 2 + mBr)) {
                    //  ブロックの上下か左右化の判定
                    if (Math.Abs(dx / dy) > (mBlockw / mBlockh)) {
                        //  左右にあたる
                        mBx1 = -mBx1;
                        mBallx = (mBlocks[i].Left + mBlocks[i].Width / 2) - Math.Sign(dx) * (mBlockw / 2 + mBr);
                    } else {
                        //  上下にあたる
                        mBy1 = -mBy1;
                        mBally = (mBlocks[i].Top + mBlocks[i].Height / 2) - Math.Sign(dy) * (mBlockh / 2 + mBr);
                    }
                    mBlocks.RemoveAt(i);            //  ブロックを減らす
                    mBx1 *= mBullAccell;            //  ボールの速度アップ
                    mBy1 *= mBullAccell;
                    break;
                }
            }

            mBallx += mBx1;
            mBally += mBy1;
        }

        /// <summary>
        /// 状態の確認
        /// </summary>
        private void statusCheck()
        {
            if (mGameover) {
                dispatcherTimer.Stop();
                completeMessage("残念でした", 4);
            } else if (mBlocks.Count == 0) {
                dispatcherTimer.Stop();
                completeMessage("よくできました", 4);
            }
        }

        /// <summary>
        /// アプリの中央にメッセージを出す
        /// </summary>
        /// <param name="text">メッセージ</param>
        /// <param name="rate">大きさ比率</param>
        private void completeMessage(String text, double rate)
        {
            ydraw.setTextSize(mTextSize * rate);
            ydraw.setTextColor(Brushes.Red);
            ydraw.drawText(text, new Point(mWidth / 2, mHeight / 2), 0,
                HorizontalAlignment.Center, VerticalAlignment.Center);
            ydraw.setTextColor(Brushes.Black);
        }
    }
}
