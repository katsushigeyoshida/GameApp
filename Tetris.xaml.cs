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
    /// Tetris.xaml の相互作用ロジック
    /// </summary>
    public partial class Tetris : Window
    {
        private double mWindowWidth;                            //  ウィンドウの高さ
        private double mWindowHeight;                           //  ウィンドウ幅
        private double mPrevWindowWidth;                        //  変更前のウィンドウ幅
        private double mPrevWindowHeight;                       //  変更前のウィンドウ高さ
        private WindowState mWindowState = WindowState.Normal;  //  ウィンドウの状態(最大化/最小化)

        private double mWidth;                          //	描画領域の幅
        private double mHeight;                         //	描画領域の高さ
        private double mTextSize;

        private Point mO = new Point(0, 0);             //  盤の原点
        private int mYoko = 10;                         //  盤の大きさ
        private int mTate = 20;
        private double mHaba;                           //  １個の大きさ
        private Block mBlock;
        private int mEraseCount = 0;
        private List<Square> mBlockOnField = new List<Square>();

        private int mTimerInteval = 300;                //  タイマーインターバル(m秒)

        private DispatcherTimer dispatcherTimer;        // タイマーオブジェクト
        private YWorldShapes ydraw;                     //  グラフィックライブラリ
        private YLib mYlib = new YLib();                //  単なるライブラリ

        public Tetris()
        {
            InitializeComponent();

            mWindowWidth = this.Width;
            mWindowHeight = this.Height;
            mPrevWindowWidth = mWindowWidth;
            mPrevWindowHeight = mWindowHeight;

            ydraw = new YGButton(CvTetris);

            WindowFormLoad();       //  Windowの位置とサイズを復元

            //  タイマーインスタンスの作成
            dispatcherTimer = new DispatcherTimer(DispatcherPriority.Normal);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0,
                mTimerInteval / 1000, mTimerInteval % 1000);    //  日,時,分,秒,m秒
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);

        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            InitScreen();
            InitField();
            drawField();
            InitMessage();
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
            double dy = mWindowHeight - mPrevWindowHeight;
            BdTetris.Width += dx;
            BdTetris.Height += dy;
            SlWaitTime.Width += dx;
            mPrevWindowWidth = mWindowWidth;
            mPrevWindowHeight = mWindowHeight;

            //  表示の更新
            InitScreen();
            if (mBlock != null)
                mBlock.upDate(mHaba, mO);
            drawField();
        }

        /// <summary>
        /// Windowの状態を前回の状態にする
        /// </summary>
        private void WindowFormLoad()
        {
            //  前回のWindowの位置とサイズを復元する(登録項目をPropeties.settingsに登録して使用する)
            Properties.Settings.Default.Reload();
            if (Properties.Settings.Default.TetrisWindowWidth < 100 || Properties.Settings.Default.TetrisWindowHeight < 100 ||
                System.Windows.SystemParameters.WorkArea.Height < Properties.Settings.Default.TetrisWindowHeight) {
                Properties.Settings.Default.TetrisWindowWidth = mWindowWidth;
                Properties.Settings.Default.TetrisWindowHeight = mWindowHeight;
            } else {
                this.Top = Properties.Settings.Default.TetrisWindowTop;
                this.Left = Properties.Settings.Default.TetrisWindowLeft;
                this.Width = Properties.Settings.Default.TetrisWindowWidth;
                this.Height = Properties.Settings.Default.TetrisWindowHeight;
            }
        }

        /// <summary>
        /// Window状態を保存する
        /// </summary>
        private void WindowFormSave()
        {
            //  Windowの位置とサイズを保存(登録項目をPropeties.settingsに登録して使用する)
            Properties.Settings.Default.TetrisWindowTop = this.Top;
            Properties.Settings.Default.TetrisWindowLeft = this.Left;
            Properties.Settings.Default.TetrisWindowWidth = this.Width;
            Properties.Settings.Default.TetrisWindowHeight = this.Height;
            Properties.Settings.Default.Save();
        }

        /// <summary>
        /// キー操作
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("KeyDown: " + e.Key.ToString());
            if (e.Key == Key.Left) {            //  LeftMove
                if (chkYokoBlockPos(-1))
                    mBlock.mPosition.X--;
            } else if (e.Key == Key.Right) {    //  RightMove
                if (chkYokoBlockPos(+1))
                    mBlock.mPosition.X++;
            } else if (e.Key == Key.R) {        //  Rotate
                mBlock.rotate();
            } else if (e.Key == Key.M) {        //  Mirror
                mBlock.mirror();
            } else if (e.Key == Key.S) {        //  STOP/START
                StartStop();
            } else if (e.Key == Key.Escape) {   //  Reset
                dispatcherTimer.Stop();
                InitField();
                mBlock = null;
                drawField();
            } else if (e.Key == Key.PageDown) { //  Slow
                SlWaitTime.Value += 50;
            } else if (e.Key == Key.PageUp) {   //  Quick
                SlWaitTime.Value -= 50;
            } else if (e.Key == Key.H) {        //  Help
                HelpView help = new HelpView();
                help.mHelpText = HelpText.mTetrisHelp;
                help.Show();
            }
        }


        /// <summary>
        /// 矢印キー操作
        /// 画面上にButtonを追加すると矢印キーやタブキーがkeyDownでは
        /// 取得できなくなるのでPreviewKeyDownで取得する
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("Windows: " + e.Key.ToString());
            if (e.Key == Key.Left) {           //  LeftMove
                if (chkYokoBlockPos(-1))
                    mBlock.mPosition.X--;
            } else if (e.Key == Key.Right) {   //  RightMove
                if (chkYokoBlockPos(+1))
                    mBlock.mPosition.X++;
            }
        }

        /// <summary>
        /// [開始]ボタン操作
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtStart_Click(object sender, RoutedEventArgs e)
        {
            StartStop();
        }

        /// <summary>
        /// [?]ボタン操作
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtHelp_Click(object sender, RoutedEventArgs e)
        {
            HelpView help = new HelpView();
            help.mHelpText = HelpText.mTetrisHelp;
            help.Show();
        }

        /// <summary>
        /// タイマー操作
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            //  インターバル時間の取得設定
            mTimerInteval = (int)SlWaitTime.Value;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0,
                mTimerInteval / 1000, mTimerInteval % 1000); //  日,時,分,秒,m秒
            //  ブロックの落下
            mBlock.mPosition.Y++;
            if (chkBlockPos()) {
                //  表示更新
                drawField();
            } else {
                //  下限に到達
                if (0 < mBlock.mPosition.Y) {
                    //  ブロックをフィールドに追加
                    mBlock.mPosition.Y--;
                    addSquare(mBlock);
                    if (eraseBlock()) {
                        mEraseCount++;
                    }
                    drawField();
                    //  次のブロックをセット
                    mBlock = new Block(ydraw, mHaba, mO);
                } else {
                    //  終了
                    dispatcherTimer.Stop();
                    EndMessage();
                }
            }
            //throw new NotImplementedException();
        }

        /// <summary>
        /// 初期表示メッセージ
        /// </summary>
        private void InitMessage()
        {
            ydraw.setTextColor(Brushes.Red);
            ydraw.setTextSize(15);
            ydraw.drawWText(
                "キー操作 S:開始/停止, ←:左移動, →:右移動, R:回転, M:左右反転",
                new Point(mO.X + mHaba * mYoko / 2, mO.Y),
                0, HorizontalAlignment.Center, VerticalAlignment.Bottom);
            ydraw.drawWText(
                "ESC:リセット, PgUp:早い, PgDown:遅い, H:ヘルプ",
                new Point(mO.X + mHaba * mYoko / 2, mO.Y + 20),
                0, HorizontalAlignment.Center, VerticalAlignment.Bottom);
        }

        /// <summary>
        /// 行削除の点数表示
        /// </summary>
        private void CountMessage()
        {
            ydraw.setTextColor(Brushes.Red);
            ydraw.setTextSize(17);
            ydraw.drawWText(mEraseCount + " 点",
                new Point(mO.X + mHaba * mYoko / 2, mO.Y + mHaba * mTate),
                0, HorizontalAlignment.Center, VerticalAlignment.Top);
        }

        /// <summary>
        /// 終了時のメッセージ表示
        /// </summary>
        private void EndMessage()
        {
            ydraw.setTextColor(Brushes.Red);
            ydraw.setTextSize(50);
            ydraw.drawWText("ご苦労さん",
                new Point(mO.X + mHaba * mYoko / 2, mO.Y + mHaba * mTate / 2),
                0, HorizontalAlignment.Center, VerticalAlignment.Bottom);
            ydraw.drawWText(mEraseCount + " 点",
                new Point(mO.X + mHaba * mYoko / 2, mO.Y + mHaba * mTate / 2),
                0, HorizontalAlignment.Center, VerticalAlignment.Top);
        }

        /// <summary>
        /// ゲームの開始と停止
        /// </summary>
        private void StartStop()
        {
            if (dispatcherTimer.IsEnabled) {
                //  ゲーム停止
                dispatcherTimer.Stop();
                BtStart.Content = "開始";
            } else {
                //  ゲーム開始
                if (mBlock == null)
                    mBlock = new Block(ydraw, mHaba, mO);
                if (mBlock.mPosition.Y <= 0) {
                    //  初期状態から開始
                    InitField();
                    drawField();
                }
                dispatcherTimer.Start();
                BtStart.Content = "停止";
            }
        }

        /// <summary>
        /// 画面の初期化
        /// </summary>
        private void InitScreen()
        {
            mWidth = CvTetris.ActualWidth;
            mHeight = CvTetris.ActualHeight;
            ydraw.setWindowSize(CvTetris.ActualWidth, CvTetris.ActualHeight);
            ydraw.setViewArea(0, 0, CvTetris.ActualWidth, CvTetris.ActualHeight);
            ydraw.setWorldWindow(0, 0, mWidth, mHeight);
            if (mTextSize == 0)
                mTextSize = ydraw.getTextSize();
            ydraw.clear();

            //  盤のメッシュの幅
            if (mWidth / mYoko < mHeight / mTate)
                mHaba = mWidth / mYoko;
            else
                mHaba = (mHeight - mTextSize * 2f) / mTate;
            //  盤の原点
            mO.X = (mWidth - mHaba * mYoko) / 2f;
            mO.Y = (mHeight - mHaba * mTate) / 2f;

            SlWaitTime.Maximum = 700;
            SlWaitTime.Minimum = 10;
            SlWaitTime.Value = mTimerInteval;
            SlWaitTime.LargeChange = 10;
        }

        /// <summary>
        /// フィールドの初期化
        /// </summary>
        private void InitField()
        {
            //  盤をグラフィックボタンで作成
            ydraw.clear();
            //mBlock = new Block(ydraw, mHaba, mO);
            mBlockOnField.Clear();
            mEraseCount = 0;
        }

        //  フィールドの表示
        private void drawField()
        {
            //  フィールドクリア
            ydraw.clear();
            //  ブロックの表示
            if (mBlock != null)
                mBlock.draw();
            //  四角の表示
            drawSquareList();
            //  フィールドの格子表示
            ydraw.setColor(Brushes.Black);
            for (int i = 0; i <= mYoko; i++)
                ydraw.drawWLine(new Point(mO.X + mHaba * i, mO.Y), new Point(mO.X + mHaba * i, mO.Y + mHaba * mTate));
            for (int i = 0; i <= mTate; i++)
                ydraw.drawWLine(new Point(mO.X, mO.Y + mHaba * i), new Point(mO.X + mHaba * mYoko, mO.Y + mHaba * i));
            //  削除行数表示
            if (0 < mEraseCount) {
                CountMessage();
            }
        }

        /// <summary>
        /// ブロックの横方向移動確認
        /// </summary>
        /// <param name="offset">位置の横オフセット</param>
        /// <returns>移動可否</returns>
        private bool chkYokoBlockPos(int offset)
        {
            for (int i = 0; i < mBlock.getBlockCount(); i++) {
                //  フィールド内の四角との重なりチェック
                foreach (Square sq in mBlockOnField) {
                    if (sq.mPosition.X == (mBlock.getBlockPos(i).X + offset) && sq.mPosition.Y == mBlock.getBlockPos(i).Y)
                        return false;
                }
            }
            //  フィールドの横方向はみだしチェック
            if ((mBlock.mPosition.X + offset) < 0 || mYoko < (mBlock.mPosition.X + mBlock.mSize.Width + offset))
                return false;
            return true;
        }

        /// <summary>
        /// ブロックの落下限度をチェック
        /// </summary>
        /// <returns>落下可否</returns>
        private bool chkBlockPos()
        {
            for (int i =0; i <mBlock.getBlockCount(); i++) {
                //  フィールド内の四角との重なりチェック
                foreach (Square sq in mBlockOnField) {
                    if (sq.mPosition.X == mBlock.getBlockPos(i).X && sq.mPosition.Y == mBlock.getBlockPos(i).Y)
                        return false;
                }
                //  下限値のチェック
                if (mTate <= mBlock.getBlockPos(i).Y)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// 一行に並んだ四角の削除とそれより上の四角を一つ落下させる
        /// </summary>
        /// <returns>行削除あり</returns>
        private bool eraseBlock()
        {
            int[] squareCount = new int[mTate]; //  行ごとの四角の数
            int eraseLine = -1;                 //  削除した行数
            //  行ごとの四角の数を求める
            foreach ( Square sq in mBlockOnField) {
                if (0 <= sq.mPosition.Y) {
                    squareCount[sq.mPosition.Y]++;
                    if (mYoko <= squareCount[sq.mPosition.Y]) {
                        eraseLine = sq.mPosition.Y;
                        break;
                    }
                }
            }
            //  四角の数が一行分ある行の四角を削除
            for (int i = mBlockOnField.Count - 1; i >= 0; i--) {
                if (mBlockOnField[i].mPosition.Y == eraseLine) {
                    mBlockOnField.RemoveAt(i);
                }
            }
            //  削除行よりも上の四角を一つ下に移動
            for (int i = 0; i < mBlockOnField.Count; i++) {
                if (mBlockOnField[i].mPosition.Y < eraseLine) {
                    mBlockOnField[i].mPosition.Y++;
                }
            }
            if (eraseLine < 0)
                return false;   //  削除行なし
            else
                return true;    //  削除行あり
        }

        /// <summary>
        /// リストの四角を表示
        /// </summary>
        private void drawSquareList()
        {
            foreach (Square sq in mBlockOnField) {
                sq.draw(ydraw);
            }
        }

        /// <summary>
        /// 四角をリストに追加
        /// </summary>
        /// <param name="block"></param>
        private void addSquare(Block block)
        {
            for (int i = 0; i < block.getBlockCount(); i++) {
                mBlockOnField.Add(new Square(block, i));
            }
        }
    }

    class Square
    {
        private Brush mBlockColor;                  //  ブロックの色
        private double mBlockSize;                  //  ブロックを構成する四角の大きさ
        private Point mStartPos;                    //  ブロックの表示開始座標
        public PointI mPosition;                    //  ブロックの基準位置

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="pos">四角の位置</param>
        /// <param name="blockSize">四角の大きさ</param>
        /// <param name="color">色</param>
        /// <param name="startPos">基準座標</param>
        public Square(PointI pos, double blockSize, Brush color, Point startPos)
        {
            mPosition = pos;
            mBlockSize = blockSize;
            mBlockColor = color;
            mStartPos = startPos;
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="block">ブロックデータ</param>
        /// <param name="n">ブロックを構成する四角の位置</param>
        public Square(Block block, int n)
        {
            mPosition = block.getBlockPos(n);
            mBlockSize = block.mBlockSize;
            mBlockColor = block.mBlockColor;
            mStartPos = block.mStartPos;
        }

        /// <summary>
        /// 四角の表示
        /// </summary>
        /// <param name="ydraw"></param>
        public void draw(YWorldShapes ydraw)
        {
            ydraw.setFillColor(mBlockColor);
            Point sp = new Point(mStartPos.X + mPosition.X * mBlockSize,
                            mStartPos.Y + mPosition.Y * mBlockSize);
            Point ep = new Point(sp.X + mBlockSize, sp.Y + mBlockSize);
            ydraw.drawWRectangle(sp, ep, 0);
        }
    }


    class Block
    {
        //  ブロックの初期データ
        private PointI[,] mBlock = {
            { new PointI(0,0), new PointI(1,0), new PointI(2,0),  new PointI(3,0)},
            { new PointI(0,0), new PointI(1,0), new PointI(2,0),  new PointI(0,-1)},
            { new PointI(0,0), new PointI(1,0), new PointI(2,0),  new PointI(1,-1)},
            { new PointI(0,0), new PointI(1,0), new PointI(0,-1), new PointI(1,-1)},
            { new PointI(0,0), new PointI(1,0), new PointI(1,-1), new PointI(2,-1)},
        };
        private int mCurBlockNo = 0;                //  ブロックの種類
        public Brush mBlockColor;                   //  ブロックの色
        public Point mStartPos;                     //  ブロックの表示開始座標(基準座標)
        public double mBlockSize;                   //  ブロックを構成する四角の大きさ
        public PointI mPosition = new PointI(3, -1);//  ブロックの基準位置
        public SizeI mSize;                         //  ブロック全体の大きさ(幅と高さ)

        Random mRandom = new Random();
        YWorldShapes mYdraw;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="ydraw"></param>
        /// <param name="blockSize">ブロック内の四角の大きさ</param>
        /// <param name="startPos">開始位置座標</param>
        public Block(YWorldShapes ydraw, double blockSize, Point startPos)
        {
            mYdraw = ydraw;
            mBlockSize = blockSize;
            mStartPos = startPos;
            mBlockColor = mYdraw.getColor15(mRandom.Next(15));
            mCurBlockNo = mRandom.Next(mBlock.GetLength(0));
            mSize = getSize();
        }

        public void upDate(double blockSize, Point startPos)
        {
            mBlockSize = blockSize;
            mStartPos = startPos;
        }

        /// <summary>
        /// ブロックの表示
        /// </summary>
        public void draw()
        {
            mYdraw.setFillColor(mBlockColor);
            for (int i = 0; i < mBlock.GetLength(1); i++) {
                Point sp = new Point(mStartPos.X + (mPosition.X + mBlock[mCurBlockNo, i].X) * mBlockSize,
                                mStartPos.Y + (mPosition.Y + mBlock[mCurBlockNo, i].Y) * mBlockSize);
                Point ep = new Point(sp.X + mBlockSize, sp.Y + mBlockSize);
                //System.Diagnostics.Debug.WriteLine(sp.ToString() + " , " + ep.ToString());
                mYdraw.drawWRectangle(sp, ep, 0);
            }
        }

        /// <summary>
        /// ブロックの構成数を求める
        /// </summary>
        /// <returns>構成数</returns>
        public int getBlockCount()
        {
            return mBlock.GetLength(1);
        }

        /// <summary>
        /// ブロックを構成する四角の位置を求める
        /// </summary>
        /// <param name="n">構成する四角のNo</param>
        /// <returns>位置座標</returns>
        public PointI getBlockPos(int n)
        {
            return new PointI(mPosition.X + mBlock[mCurBlockNo, n].X, mPosition.Y + mBlock[mCurBlockNo, n].Y);
        }

        /// <summary>
        /// ブロックの全体サイズを求める
        /// </summary>
        /// <returns>サイズ(幅､高さ)</returns>
        public SizeI getSize()
        {
            SizeI size = new SizeI(0, 0);
            int h = 0;
            for (int i = 0; i < mBlock.GetLength(1); i++) {
                size.Width = Math.Max(size.Width, Math.Abs(mBlock[mCurBlockNo, i].X));
                size.Height = Math.Max(size.Height, Math.Abs(mBlock[mCurBlockNo, i].Y));
            }
            size.Width++;
            size.Height++;
            return size;
        }

        /// <summary>
        /// ブロックを都計方向に90度回転
        /// </summary>
        public void rotate()
        {
            int minx = 0, maxy = 0;
            //  時計回りに90度回転
            for (int i = 0; i < mBlock.GetLength(1); i++) {
                int tx = mBlock[mCurBlockNo, i].X;
                int ty = mBlock[mCurBlockNo, i].Y;
                mBlock[mCurBlockNo, i].X = -ty;
                mBlock[mCurBlockNo, i].Y =  tx;
                if (i == 0) {
                    minx = -ty;
                    maxy =  tx;
                } else {
                    minx = Math.Min(minx, -ty);
                    maxy = Math.Max(maxy,  tx);
                }
            }
            //  原点を(0,0)に移動
            for (int i = 0; i < mBlock.GetLength(1); i++) {
                mBlock[mCurBlockNo, i].X -= minx;
                mBlock[mCurBlockNo, i].Y -= maxy;
            }
            //  ブロックの大きさを求める
            mSize = getSize();
        }

        /// <summary>
        /// ブロックを縦軸に対して反転
        /// </summary>
        public void mirror()
        {
            int minx = 0, maxy = 0;
            //  Y軸で反転
            for (int i = 0; i < mBlock.GetLength(1); i++) {
                int tx = mBlock[mCurBlockNo, i].X;
                int ty = mBlock[mCurBlockNo, i].Y;
                mBlock[mCurBlockNo, i].X = -tx;
                mBlock[mCurBlockNo, i].Y = ty;
                if (i == 0) {
                    minx = -tx;
                    maxy = ty;
                } else {
                    minx = Math.Min(minx, -tx);
                    maxy = Math.Max(maxy, ty);
                }
            }
            //  原点を(0,0)に移動
            for (int i = 0; i < mBlock.GetLength(1); i++) {
                mBlock[mCurBlockNo, i].X -= minx;
                mBlock[mCurBlockNo, i].Y -= maxy;
            }
            //  ブロックの大きさを求める
            mSize = getSize();
        }
    }
}
