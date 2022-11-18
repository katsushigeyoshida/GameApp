using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using WpfLib;

namespace GameApp
{
    /// <summary>
    /// LifeGame.xaml の相互作用ロジック
    /// </summary>
    public partial class LifeGame : Window
    {
        private double mWindowWidth;                            //  ウィンドウの高さ
        private double mWindowHeight;                           //  ウィンドウ幅
        private double mPrevWindowWidth;                        //  変更前のウィンドウ幅
        private WindowState mWindowState = WindowState.Normal;  //  ウィンドウの状態(最大化/最小化)

        private double mWidth;                          //	描画領域の幅
        private double mHeight;                         //	描画領域の高さ
        private double mTextSize;

        private double mOx = 0;                         //  盤の原点
        private double mOy = 0;
        private int mYoko = 50;                         //  盤の大きさ
        private int mTate = 72;
        private double mHaba;                           //  １個の大きさ
        private int mPatternCellX = 11;                 //  パターン作成の一辺のセルの数
        private int mPatternCellY = 11;                 //  パターン作成の一辺のセルの数
        private double  mPatternHaba;                   //  パターン作成のセルの幅

        private int mLoopCount = 0;
        private int mGenerateMax = 1000;                //  最大世代数
        private bool[,] mDisp;                          //  表示
        private int[,] mGen;                            //  世代数
        private Dictionary<string, int[]> mPatterns;    //  初期パターン
        private string mPatternName = "ペントミノ";
        private int mTimerInteval = 500;                //  タイマーインターバル(m秒)

        private string mAppFolder;
        private string mPatternFailePath;
        private string mPatternFileName = "LifeGamePattern.csv";

        private DispatcherTimer dispatcherTimer;        // タイマーオブジェクト
        private YGButton ydraw;                         //  グラフィックライブラリ
        private YLib mYlib = new YLib();                //  単なるライブラリ

        public LifeGame()
        {
            InitializeComponent();

            mWindowWidth = this.Width;
            mWindowHeight = this.Height;
            mPrevWindowWidth = mWindowWidth;

            WindowFormLoad();       //  Windowの位置とサイズを復元

            ydraw = new YGButton(CvPattern);
            mAppFolder = mYlib.getAppFolderPath();
            mPatternFailePath = Path.Combine(mAppFolder, mPatternFileName);

            //  初期パターン登録
            mPatterns = new Dictionary<string, int[]>();
            mPatterns.Add("ペントミノ", new int[] { 0, 0, -1, 0, 1, 0, 0, -1, -1, 1 });
            mPatterns.Add("棒状", new int[] { 0, -5, 0, -4, 0, -3, 0, -1, 0, 0, 0, 1, 0, 2, 0, 3, 0, 4, 0, 5 });
            mPatterns.Add("Ｌ形", new int[] { 0, -5, 0, -4, 0, -3, 0, -2, 0, -1, 0, 0, 0, 1, 0, 2, 1, 2, 2, 2 });
            mPatterns.Add("どんぐり", new int[] { -3, 1, -2, -1, -2, 1, 0, 0, 1, 1, 2, 1, 3, 1 });
            mPatterns.Add("ダイハード", new int[] { -4, 0, -3, 0, -3, 1, 1, 1, 2, -1, 2, 1, 3, 1 });

            loadPattern(mPatternFailePath);
            patternNameSet();

            //  タイマーインスタンスの作成
            dispatcherTimer = new DispatcherTimer(DispatcherPriority.Normal);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0,
                mTimerInteval / 1000, mTimerInteval % 1000);    //  日,時,分,秒,m秒
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);

            InitBord();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            InitScreen();
            drawCells();
            drawBoard();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            savePattern(mPatternFailePath);
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
            CbPattern.Width += dx;
            mPrevWindowWidth = mWindowWidth;
            //  表示の更新
            InitScreen();
            if (BtRegist.Content.ToString().CompareTo("作成") == 0) {
                drawCells();
            } else {
                createPattern();
            }
            drawBoard();
        }

        /// <summary>
        /// Windowの状態を前回の状態にする
        /// </summary>
        private void WindowFormLoad()
        {
            //  前回のWindowの位置とサイズを復元する(登録項目をPropeties.settingsに登録して使用する)
            Properties.Settings.Default.Reload();
            if (Properties.Settings.Default.LifeGameWindowWidth < 100 || Properties.Settings.Default.LifeGameWindowHeight < 100 ||
                System.Windows.SystemParameters.WorkArea.Height < Properties.Settings.Default.LifeGameWindowHeight) {
                Properties.Settings.Default.LifeGameWindowWidth = mWindowWidth;
                Properties.Settings.Default.LifeGameWindowHeight = mWindowHeight;
            } else {
                this.Top = Properties.Settings.Default.LifeGameWindowTop;
                this.Left = Properties.Settings.Default.LifeGameWindowLeft;
                this.Width = Properties.Settings.Default.LifeGameWindowWidth;
                this.Height = Properties.Settings.Default.LifeGameWindowHeight;
            }
        }

        /// <summary>
        /// Window状態を保存する
        /// </summary>
        private void WindowFormSave()
        {
            //  Windowの位置とサイズを保存(登録項目をPropeties.settingsに登録して使用する)
            Properties.Settings.Default.LifeGameWindowTop = this.Top;
            Properties.Settings.Default.LifeGameWindowLeft = this.Left;
            Properties.Settings.Default.LifeGameWindowWidth = this.Width;
            Properties.Settings.Default.LifeGameWindowHeight = this.Height;
            Properties.Settings.Default.Save();
        }

        private void Btn_Click(object sender, RoutedEventArgs e)
        {
            Button bt = (Button)e.Source;
            if (bt.Name.CompareTo("BtStart") == 0) {
                InitBord();
                mLoopCount = 0;
                dispatcherTimer.Start();
            } else if (bt.Name.CompareTo("BtEnd") == 0) {
                dispatcherTimer.Stop();
            } else if (bt.Name.CompareTo("BtPouse") == 0) {
                dispatcherTimer.Stop();
            } else if (bt.Name.CompareTo("BtRestart") == 0) {
                dispatcherTimer.Start();
            } else if (bt.Name.CompareTo("BtRegist") == 0) {
                if (bt.Content.ToString().CompareTo("作成")==0) {
                    createPatternStart();
                } else if (bt.Content.ToString().CompareTo("登録") == 0) {
                    createPatternEnd();
                }
            } else if (bt.Name.CompareTo("BtDelete") == 0) {
                mPatterns.Remove(mPatternName);
                patternNameSet();
            }

        }

        /// <summary>
        /// 初期パターンの変更
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CbPattern_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dispatcherTimer != null && 
                BtRegist.Content.ToString().CompareTo("登録") != 0 &&
                0 <= CbPattern.SelectedIndex) {
                dispatcherTimer.Stop();
                mPatternName = CbPattern.Items[CbPattern.SelectedIndex].ToString();
                InitBord();
                drawCells();
                drawBoard();
            }
        }

        /// <summary>
        /// マウスクリックによるセル反転(pattern作成時)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CvPattern_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Point sp = e.GetPosition(this);         //  マウスの位置(上部と左部の分だけマウスの位置を調節する)
                                                    //  sp.X -= solverList.ActualWidth;
            sp.Y -= menuBar.ActualHeight;
            Point pt = ydraw.cnvScreen2World(sp);   //  マウスの位置を論理座標に変換
            if (BtRegist.Content.ToString().CompareTo("登録") == 0) {
                int id = ydraw.GButtonDownId(pt);   //  マウスの位置からセルのIDを求める
                ydraw.GButtonDwonReversId(id);      //  セルの反転
                ydraw.GButtonDraws();               //  再表示
            }
        }

        /// <summary>
        /// タイマーによる更新処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            //  インターバル時間の取得設定
            mTimerInteval = (int)SlWaitTime.Value;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0,
                mTimerInteval / 1000, mTimerInteval % 1000); //  日,時,分,秒,m秒

            //  世代数の表示
            mLoopCount++;
            LbGeneration.Content = mLoopCount.ToString();

            //  世代更新
            drawSlowly();
            if (drawCells() <= 0 || mGenerateMax < mLoopCount)
                dispatcherTimer.Stop();
            //  更新データの表示
            drawBoard();
            //throw new NotImplementedException();
        }

        /// <summary>
        /// 初期パターンの作成開始
        /// </summary>
        private void createPatternStart()
        {
            BtRegist.Content = "登録";
            dispatcherTimer.Stop();
            InitScreen();
            createPattern();
            drawBoard();
            CbPattern.IsEditable = true;
            CbPattern.Text = "";
        }

        /// <summary>
        /// 初期パターンの作成終了
        /// </summary>
        private void createPatternEnd()
        {
            if (completePattern()) {
                patternNameSet();
                CbPattern.IsEditable = false;
                BtRegist.Content = "作成";
                InitScreen();
                drawCells();
                drawBoard();
            }
        }

        /// <summary>
        /// 画面の初期化
        /// </summary>
        private void InitScreen()
        {
            mWidth =  CvPattern.ActualWidth;
            mHeight = CvPattern.ActualHeight;
            ydraw.setWindowSize(CvPattern.ActualWidth, CvPattern.ActualHeight);
            ydraw.setViewArea(0, 0, CvPattern.ActualWidth, CvPattern.ActualHeight);
            ydraw.setWorldWindow(0, 0, mWidth, mHeight);
            if (mTextSize == 0)
                mTextSize = ydraw.getTextSize();
            ydraw.clear();

            SlWaitTime.Maximum = 1000;
            SlWaitTime.Minimum = 10;
            SlWaitTime.Value = mTimerInteval;
            SlWaitTime.LargeChange = 10;

            //  盤のメッシュの幅
            if (mWidth / mYoko < mHeight / mTate)
                mHaba = mWidth / mYoko;
            else
                mHaba = (mHeight - mTextSize * 2f) / mTate;
            //  盤の原点
            mOx = (mWidth - mHaba * mYoko) / 2f;
            mOy = (mHeight - mHaba * mTate) / 2f;
            //  パターン作成のメッシュの幅
            mPatternHaba = Math.Min(mWidth, mHeight) / (mPatternCellX + 1);
        }

        /// <summary>
        /// 盤データを初期化(初期パターンを設定)
        /// </summary>
        private void InitBord()
        {
            mDisp = new bool[mYoko + 2, mTate + 2];
            mGen = new int[mYoko + 2, mTate + 2];           //  世代数

            //  盤の初期化
            for (int i = 0; i < mYoko; i++) {
                for (int j = 0; j < mTate; j++) {
                    mDisp[i,j] = false;
                    mGen[i,j] = 0;
                }
            }
            //  初期パターンの取得
            int[] pattern = mPatterns[mPatternName];
            for (int i = 0; i < pattern.Length; i += 2) {
                mDisp[mYoko / 2 + pattern[i],mTate / 2 + pattern[i + 1]] = true;
            }
        }

        /// <summary>
        /// 盤データの表示データを設定
        /// </summary>
        /// <returns></returns>
        private int drawCells()
        {
            //  盤をグラフィックボタンで作成
            ydraw.clear();
            ydraw.GButtonClear();

            int count = 0;
            ydraw.setColor(Brushes.Black);
            ydraw.setFillColor(Brushes.White);
            ydraw.drawWRectangle(new Point(mOx, mOy), new Point(mOx + mHaba * mYoko, mOy + mHaba * mTate), 0);

            for (int i = 0; i < mYoko; i++) {
                for (int j = 0; j < mTate; j++) {
                    if (mDisp[i,j]) {
                        ydraw.GButtonAdd(getId(i, j), BUTTONTYPE.RECT,
                            new Rect(mOx + mHaba * i, mOy + mHaba * j, mHaba, mHaba));
                        ydraw.GButtonBorderThickness(getId(i, j), 0.8f);
                        ydraw.GButtonBackColor(getId(i, j), Brushes.Red);
                        count++;
                    }
                }
            }
            return count;
        }

        /// <summary>
        /// 世代更新をおこなう
        /// </summary>
        private void drawSlowly()
        {
            //  生命体のあるセルの周りのセルの世代を一つ上げる
            for (int i = 1; i <= mYoko; i++) {
                for (int j = 1; j <= mTate; j++) {
                    if (mDisp[i,j]) {
                        mGen[i - 1,j - 1]++; mGen[i - 1,j]++; mGen[i - 1,j + 1]++;
                        mGen[i,    j - 1]++; mGen[i,j + 1]++;
                        mGen[i + 1,j - 1]++; mGen[i + 1,j]++; mGen[i + 1,j + 1]++;
                    }
                }
            }
            //  世代が３だったら表示し、世代をクリアする
            for (int i = 0; i <= mYoko + 1; i++) {
                for (int j = 0; j <= mTate + 1; j++) {
                    if (mGen[i,j] != 2)
                        mDisp[i,j] = (mGen[i,j] == 3);
                    mGen[i,j] = 0;
                }
            }
        }


        /// <summary>
        /// 盤面を表示する
        /// </summary>
        public void drawBoard()
        {
            if (mHaba <= 0)
                return;
            //  背景色白で盤の表示
            ydraw.backColor(Brushes.White);
            ydraw.GButtonDraws();
            //  3x3の区切り線の表示
            //drawSmallBoard();
        }

        /// <summary>
        /// 行(y)列(x)からボタンのIDを求める
        /// </summary>
        /// <param name="x">列番豪</param>
        /// <param name="y">行番号</param>
        /// <returns>ID</returns>
        private int getId(int x, int y)
        {
            return x * 100 + y;
        }

        /// <summary>
        /// 初期パターンをコンボボックスに設定
        /// </summary>
        private void patternNameSet()
        {
            CbPattern.Items.Clear();
            foreach (string key in mPatterns.Keys)
                CbPattern.Items.Add(key);
            CbPattern.SelectedIndex = 0;
        }


        /// <summary>
        /// 初期パターンの作成
        /// </summary>
        private void createPattern()
        {
            //  盤面クリア
            ydraw.clear();
            ydraw.GButtonClear();
            //  格子盤作成
            for (int x = 0; x < mPatternCellX; x++) {
                for (int y = 0; y < mPatternCellY; y++) {
                    ydraw.GButtonAdd(getId(x, y), BUTTONTYPE.RECT,
                        new Rect(x * mPatternHaba + mPatternHaba / 2f, y * mPatternHaba + mPatternHaba / 2,
                            mPatternHaba, mPatternHaba));
                }
            }
        }

        /// <summary>
        /// 盤状態から初期パターンの完成形を取得
        /// </summary>
        /// <returns></returns>
        public bool completePattern()
        {
            //  ボタンダウン設定値の取り出し
            List<int> patternList = new List<int>();
            for (int x = 0; x < mPatternCellX; x++) {
                for (int y = 0; y < mPatternCellY; y++) {
                    if (ydraw.GButtonDownGet(getId(x, y)) == true) {
                        patternList.Add(x - mPatternCellX / 2);
                        patternList.Add(y - mPatternCellY / 2);
                    }
                }
            }
            if (patternList.Count <= 0)
                return true;
            //  パターンの登録
            int[] pattern = new int[patternList.Count];
            for (int i = 0; i < pattern.Length; i++)
                pattern[i] = patternList[i];
            if (0 < CbPattern.Text.Length) {
                if (mPatterns.ContainsKey(CbPattern.Text))
                    mPatterns[CbPattern.Text] = pattern;
                else
                    mPatterns.Add(CbPattern.Text, pattern);
                return true;
            } else {
                MessageBox.Show("パターン名が設定されていません");
                return false;
            }
        }

        /// <summary>
        /// 初期パターンを保存する
        /// </summary>
        /// <param name="path"></param>
        private void savePattern(string path)
        {
            string[] title = new string[] { "タイトル", "データ" };
            List<string[]> patterns = new List<string[]>();
            foreach (KeyValuePair<string, int[]> keyValue in mPatterns) {
                string[] data = new string[keyValue.Value.Length + 1];
                data[0] = keyValue.Key;
                for (int i = 0; i < keyValue.Value.Length; i++)
                    data[i + 1] = keyValue.Value[i].ToString();
                patterns.Add(data);
            }
            mYlib.saveCsvData(path, title, patterns);
        }

        /// <summary>
        /// 初期パターンを読み込む
        /// </summary>
        /// <param name="path"></param>
        private void loadPattern(string path)
        {
            if (!File.Exists(path))
                return;
            List<string[]>  patterns = mYlib.loadCsvData(path);
            foreach (string[] val in patterns) {
                string key = val[0];
                if (key.CompareTo("タイトル") == 0)
                    continue;
                int[] data = new int[val.Length - 1];
                for (int i = 1; i < val.Length; i++)
                    data[i - 1] = int.Parse(val[i]);
                if (mPatterns.ContainsKey(key)) {
                    mPatterns[key] = data;
                } else {
                    mPatterns.Add(key, data);
                }
            }
        }
    }
}
