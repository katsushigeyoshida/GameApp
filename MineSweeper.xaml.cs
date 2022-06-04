using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WpfLib;

namespace GameApp
{
    /// <summary>
    /// MineSweeper.xaml の相互作用ロジック
    /// </summary>
    public partial class MineSweeper : Window
    {
        private double mWindowWidth;                            //  ウィンドウの高さ
        private double mWindowHeight;                           //  ウィンドウ幅
        private double mPrevWindowWidth;                        //  変更前のウィンドウ幅
        private WindowState mWindowState = WindowState.Normal;  //  ウィンドウの状態(最大化/最小化)

        private double mTextSize = 0;                           //  文字の大きさ
        private int mWidth = 1000;                              //  Viewの論理サイズ
        private int mHeight = 1000;                             //  Viewの論理サイズ
        private float mBoardSizeRatio = 0.95f;                  //  画面に対する盤の大きさの比
        private float mHaba = 0;                                //  盤のマスの大きさ
        private float mOx, mOy;                                 //  盤の原点
        private int mRowCount = 9;                              //  盤の列数
        private int mColCount = 9;                              //  盤の行数
        private int mBombCount = 5;                             //  爆弾の数
        private int[] mBombPos;                                 //  爆弾の位置
        private System.Drawing.Bitmap[] mTileBitmap = {         //  セルのイメージ(爆弾の数)
            Properties.Resources.ontitle,
            Properties.Resources.on1title,
            Properties.Resources.on2title,
            Properties.Resources.on3title,
            Properties.Resources.on4title,
            Properties.Resources.on5title,
            Properties.Resources.on6title,
            Properties.Resources.on7title,
            Properties.Resources.on8title,
        };
        private string[] mRowNumStr =
            { "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15" };
        private string[] mColNumStr = 
            { "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15" };
        private string[] mBombNumStr = 
            { "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15", "16", "17", "18", "19", "20" };

        private YGButton ydraw;                         //  グラフィックライブラリ
        private YLib mYlib = new YLib();                //  単なるライブラリ

        public MineSweeper()
        {
            InitializeComponent();

            mWindowWidth = Width;
            mWindowHeight = Height;
            mPrevWindowWidth = mWindowWidth;

            WindowFormLoad();

            //  行数、列数、地雷数の取得
            mRowCount = Properties.Settings.Default.MineSweeperRow;
            mColCount = Properties.Settings.Default.MineSweeperCol;
            mBombCount = Properties.Settings.Default.MineSweeperBomb;

            //  コンボボックスの設定
            CbColCount.ItemsSource = mColNumStr;
            CbColCount.SelectedIndex = CbColCount.Items.IndexOf(mColCount.ToString());
            CbRowCount.ItemsSource = mRowNumStr;
            CbRowCount.SelectedIndex = CbRowCount.Items.IndexOf(mRowCount.ToString());
            CbBombCount.ItemsSource = mBombNumStr;
            CbBombCount.SelectedIndex = CbBombCount.Items.IndexOf(mBombCount.ToString());

            ydraw = new YGButton(CvBoard);

        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            mWindowWidth = Width;
            mWindowHeight = Height;

            initScreen();
        }

        private void Window_LayoutUpdated(object sender, EventArgs e)
        {
            //  最大化時の処理
            if (this.WindowState != mWindowState &&
                this.WindowState == WindowState.Maximized) {
                mWindowWidth = SystemParameters.WorkArea.Width;
                mWindowHeight = SystemParameters.WorkArea.Height;
            } else if (this.WindowState != mWindowState ||
                mWindowWidth != Width ||
                mWindowHeight != Height) {
                mWindowWidth = Width;
                mWindowHeight = Height;
            } else {
                return;
            }

            drawBoard();    //  盤の再表示
            //  ウィンドウの大きさに合わせてコントロールの幅を変更する
            double dx = mWindowWidth - mPrevWindowWidth;
            //コントロール.Width += dx;

            mWindowState = this.WindowState;
            mPrevWindowWidth = mWindowWidth;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //  行数、列数、地雷数の保存
            Properties.Settings.Default.MineSweeperRow = mRowCount;
            Properties.Settings.Default.MineSweeperCol = mColCount;
            Properties.Settings.Default.MineSweeperBomb = mBombCount;

            WindowFormSave();
        }


        /// <summary>
        /// Windowの状態を前回の状態にする
        /// </summary>
        private void WindowFormLoad()
        {
            //  前回のWindowの位置とサイズを復元する(登録項目をPropeties.settingsに登録して使用する)
            Properties.Settings.Default.Reload();
            if (Properties.Settings.Default.MineSweeperWidth < 100 || Properties.Settings.Default.MineSweeperHeight < 100 ||
                System.Windows.SystemParameters.WorkArea.Height < Properties.Settings.Default.MineSweeperHeight) {
                Properties.Settings.Default.MineSweeperWidth = mWindowWidth;
                Properties.Settings.Default.MineSweeperHeight = mWindowHeight;
            } else {
                Top = Properties.Settings.Default.MineSweeperTop;
                Left = Properties.Settings.Default.MineSweeperLeft;
                Width = Properties.Settings.Default.MineSweeperWidth;
                Height = Properties.Settings.Default.MineSweeperHeight;
            }
        }

        /// <summary>
        /// Window状態を保存する
        /// </summary>
        private void WindowFormSave()
        {
            //  Windowの位置とサイズを保存(登録項目をPropeties.settingsに登録して使用する)
            Properties.Settings.Default.MineSweeperTop = Top;
            Properties.Settings.Default.MineSweeperLeft = Left;
            Properties.Settings.Default.MineSweeperWidth = Width;
            Properties.Settings.Default.MineSweeperHeight = Height;
            Properties.Settings.Default.Save();
        }

        //  [リセット]ボタン
        private void BtReset_Click(object sender, RoutedEventArgs e)
        {
            initBoard();
        }

        /// <summary>
        /// [ヘルプ]ボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtHelp_Click(object sender, RoutedEventArgs e)
        {
            HelpView help = new HelpView();
            help.mHelpText = HelpText.mMineSweeperHelp;
            help.Show();
        }

        //  列数の選択変更
        private void CbColCount_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (0 <= CbColCount.SelectedIndex) {
                mColCount = int.Parse(CbColCount.Items[CbColCount.SelectedIndex].ToString());
                initScreen();
            }
        }

        //  行数の選択変更
        private void CbRowCount_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (0 <= CbRowCount.SelectedIndex) {
                mRowCount = int.Parse(CbRowCount.Items[CbRowCount.SelectedIndex].ToString());
                initScreen();
            }
        }

        //  爆弾の数を選択変更
        private void CbBombCount_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (0 <= CbBombCount.SelectedIndex) {
                mBombCount = int.Parse(CbBombCount.Items[CbBombCount.SelectedIndex].ToString());
                setBomb(mBombCount);
                initBoard();
            }
        }

        /// <summary>
        /// マウスクリック処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CvBoard_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Point sp = e.GetPosition(this);         //  マウスの位置
                                                    //  上部と左部の分だけマウスの位置を調節する
            //sp.X -= solverList.ActualWidth;
            sp.Y -= menuBar.ActualHeight;           //  メニューバー分のオフセット除去
            Point pt = ydraw.cnvScreen2World(sp);   //  マウスの位置を論理座標に変換
            int id = ydraw.GButtonDownId(pt);       //  マウスの位置からセルのIDを求める

            string message = "";
            if (e.LeftButton == MouseButtonState.Pressed) {
                //  左クリック処理
                if (mBombPos[id] == 1) {
                    //  爆弾にあたる
                    ydraw.GButtonResource(id, Properties.Resources.bombtile);
                    message = "残念でした " + mYlib.stopWatchLapTime().ToString(@"mm\:ss");
                    //MessageBox.Show(message);
                    bottomMessage(message, 2.0f);
                } else {
                    //  セーフ
                    ydraw.GButtonResource(id, mTileBitmap[nearBomb(id)]);
                    mBombPos[id] = 2;
                }
            } else if (e.RightButton == MouseButtonState.Pressed) {
                //  右クリックで旗を立てる
                ydraw.GButtonResource(id, Properties.Resources.flagtile);
                mBombPos[id] = 3;
            }
            //  完了確認
            if (!mBombPos.Contains(0)) {
                message = "ご苦労さんでした " + mYlib.stopWatchLapTime().ToString(@"mm\:ss");
                //MessageBox.Show(message);
            }
            drawBoard();
            bottomMessage(message, 1.5f);
        }

        /// <summary>
        /// 指定位置の周辺の爆弾の数を求める
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        private int nearBomb(int pos)
        {
            int nearBombNum = 0;
            for (int i = -1; i < 2; i++) {
                for (int j = -1; j < 2; j++) {
                    int r = pos / mColCount + i;
                    int c = pos % mColCount + j;
                    if (0 <= c && c < mColCount && 0 <= r && r < mRowCount) {
                        int p = r * mColCount + c;
                        if (p != pos && mBombPos[p] == 1)
                            nearBombNum++;
                    }
                }
            }
            return nearBombNum;
        }

        /// <summary>
        /// 爆弾(地雷)の位置を作成
        /// </summary>
        /// <param name="bombNum"></param>
        private void setBomb(int bombNum)
        {
            int totalCellNum = mRowCount * mColCount;
            mBombPos = new int[totalCellNum];
            Random random = new Random();
            for (int i = 0; i < bombNum; i++) {
                int pos = random.Next(totalCellNum);
                if (mBombPos[pos] != 1) {
                    mBombPos[pos] = 1;
                } else {
                    i--;
                }
            }
        }

        /// <summary>
        /// 画面の初期化
        /// </summary>
        private void initScreen()
        {
            //  盤の大きさと位置
            float habaX = mWidth * mBoardSizeRatio / mColCount;
            float habaY = mHeight * mBoardSizeRatio / (mRowCount + 1.3f);
            mHaba = habaX < habaY ? habaX : habaY;

            mOx = ((float)mWidth - (float)mColCount * mHaba) / 2f;
            mOy = mHaba / 1.5f;

            initBoard();
        }

        /// <summary>
        /// 盤の作成
        /// </summary>
        private void initBoard()
        {
            if (!windowSet())
                return;

            //  爆弾の位置を設定
            setBomb(mBombCount);

            //  盤をグラフィックボタンで作成
            ydraw.GButtonClear();
            //  盤の完成状態を作成
            for (int y = 0; y < mRowCount; y++) {
                for (int x = 0; x < mColCount; x++) {
                    ydraw.GButtonAdd(getId(x, y), BUTTONTYPE.RECT,
                        new Rect((float)x * mHaba + mOx, (float)y * mHaba + mOy, mHaba, mHaba));
                    ydraw.GButtonBorderThickness(getId(x, y), 0.8f);
                    ydraw.GButtonResource(getId(x, y), Properties.Resources.offtile);
                }
            }
            //  盤の表示
            ydraw.GButtonDraws();
            //drawBoard();

            //  時間計測開始
            mYlib.stopWatchStartNew();
        }

        /// <summary>
        /// 盤面を表示する
        /// </summary>
        public void drawBoard()
        {
            if (mHaba <= 0)
                return;

            if (!windowSet())
                return;
            ydraw.GButtonDraws();
        }


        /// <summary>
        /// 論理座標の設定と画面クリア
        /// </summary>
        private bool windowSet()
        {
            if (CvBoard.ActualWidth <= 0 || CvBoard.ActualHeight <= 0)
                return false;
            ydraw.setWindowSize(CvBoard.ActualWidth, CvBoard.ActualHeight);
            ydraw.setViewArea(0, 0, CvBoard.ActualWidth, CvBoard.ActualHeight);
            ydraw.setWorldWindow(0, 0, mWidth, mHeight);
            if (mTextSize == 0)
                mTextSize = ydraw.getTextSize();
            ydraw.clear();
            return true;
        }

        /// <summary>
        /// 行(y)列(x)からボタンのIDを求める
        /// </summary>
        /// <param name="x">列番豪</param>
        /// <param name="y">行番号</param>
        /// <returns>ID</returns>
        private int getId(int x, int y)
        {
            return x + y * mColCount;
        }

        /// <summary>
        /// IDから列数を取得
        /// </summary>
        /// <param name="id">ID</param>
        /// <returns>列番号</returns>
        private int getXId(int id)
        {
            return id % mColCount;
        }

        /// <summary>
        /// IDから行数を取得
        /// </summary>
        /// <param name="id">ID</param>
        /// <returns>行数</returns>
        private int getYId(int id)
        {
            return id / mColCount;
        }

        /// <summary>
        /// メッセージを盤の下部に表示
        /// </summary>
        /// <param name="text"></param>
        public void bottomMessage(String text, float sizeRatio)
        {
            double textSize = ydraw.screen2worldYlength(20);
            ydraw.setTextSize(textSize * sizeRatio);
            ydraw.setTextColor(Brushes.Red);
            ydraw.drawText(text, new Point(mWidth / 2, mOy + mHaba * mRowCount), 0,
                            System.Windows.HorizontalAlignment.Center, VerticalAlignment.Top);
            ydraw.setTextColor(Brushes.Black);
        }
    }
}
