using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using WpfLib;

namespace GameApp
{
    /// <summary>
    /// Slide15Game.xaml の相互作用ロジック
    /// </summary>
    public partial class Slide15Game : Window
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
        private double mTextSize = 20;          //  文字の大きさ
        private int mCount = 0;                 //  実施回数
        private String[] mNumber = {
            "*","1","2","3","4","5","6","7","8","9",
            "10","11","12","13","14","15","16","17","18","19",
            "20","21","22","23","24","25","26","27","28","29",
        };
        private bool mDisp = false;
        private Random mRandom;
        private int mCreateBoardCount = 0;      //  盤の問題作成のための繰り返し数
        private int mCreatePreId;

        public Slide15Game()
        {
            InitializeComponent();

            mWindowWidth = WindowForm.Width;
            mWindowHeight = WindowForm.Height;

            WindowFormLoad();

            ydraw = new YGButton(canvas);
            mRandom = new Random();

            //  タイマーインスタンスの作成
            dispatcherTimer = new DispatcherTimer(DispatcherPriority.Normal);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 100); //  日,時,分,秒,m秒
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
            solverList.Text = "　　　　　";
        }

        private void WindowForm_Loaded(object sender, RoutedEventArgs e)
        {
            mWindowWidth = WindowForm.Width;
            mWindowHeight = WindowForm.Height;

            //  盤の初期表示
            mDisp = true;
            mCount = 0;
            drawBoard();
        }

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
            } else
                return;

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
            if (Properties.Settings.Default.SlideGameWindowWidth < 100 || Properties.Settings.Default.SlideGameWindowHeight < 100 ||
                System.Windows.SystemParameters.WorkArea.Height < Properties.Settings.Default.SlideGameWindowHeight) {
                Properties.Settings.Default.SlideGameWindowWidth = mWindowWidth;
                Properties.Settings.Default.SlideGameWindowHeight = mWindowHeight;
            } else {
                WindowForm.Top = Properties.Settings.Default.SlideGameWindowTop;
                WindowForm.Left = Properties.Settings.Default.SlideGameWindowLeft;
                WindowForm.Width = Properties.Settings.Default.SlideGameWindowWidth;
                WindowForm.Height = Properties.Settings.Default.SlideGameWindowHeight;
            }
        }

        /// <summary>
        /// Window状態を保存する
        /// </summary>
        private void WindowFormSave()
        {
            //  Windowの位置とサイズを保存(登録項目をPropeties.settingsに登録して使用する)
            Properties.Settings.Default.SlideGameWindowTop = WindowForm.Top;
            Properties.Settings.Default.SlideGameWindowLeft = WindowForm.Left;
            Properties.Settings.Default.SlideGameWindowWidth = WindowForm.Width;
            Properties.Settings.Default.SlideGameWindowHeight = WindowForm.Height;
            Properties.Settings.Default.Save();
        }

        /// <summary>
        /// 盤サイズの変更
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
        /// [問題作成]ボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void creatBtn_Click(object sender, RoutedEventArgs e)
        {
            createProblem();
        }

        /// <summary>
        /// [解法]ボタン 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Solver_Click(object sender, RoutedEventArgs e)
        {
            if (3 < mBoardSize) {
                if (4 < mBoardSize)
                    //if (MessageBox.Show("解法に時間がかかります(数十分以上)、多分メモリ不足で解けない場合が多いと思います\n" +
                    //    "それでも実行しますか?", "警告", MessageBoxButton.OKCancel) == MessageBoxResult.Cancel)
                    //    return;
                    MessageBox.Show("盤データが大きすぎて解法を実行することができません");
                else
                    solver2();
            } else {
                solver();
            }
        }

        /// <summary>
        /// [?]ボタン ヘルプを表示
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void help_Click(object sender, RoutedEventArgs e)
        {
            HelpView help = new HelpView();
            help.mHelpText = HelpText.mSlideGameHelp;
            help.Show();
        }

        /// <summary>
        /// 盤上のマウス操作
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
                int id = ydraw.GButtonDownId(pt);       //  マウスの位置からセルのIDを求める
                if (0 <= id) {
                    if (!chkBlank(id)) {
                        //  ブランクボタンでなければ前後左右のブランクボタンをチェックする
                        int id2 = getBlankId(id);
                        //  前後左右にブランクボタンがあればブランクボタンと入れ替えをする
                        if (0 <= id2) {
                            swapTitle(id, id2);
                            countMessage(mCount++);
                            if (completeChk())
                                completeMessage("完成");
                            else
                                completeMessage("　　");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 問題作成をアニメーション表示するためのタイマー処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            createProblemBoard();
            //throw new NotImplementedException();
        }

        /// <summary>
        /// 指定したIDに移動できるIDのリストを取得
        /// </summary>
        /// <param name="id">ID</param>
        /// <returns>IDの配列リスト(移動できない場所は-1を設定)</returns>
        private int[] getNextPosition(int id)
        {
            int x = id / 100;
            int y = id % 100;
            int[] nextId = { -1, -1, -1, -1 };
            int i = 0;
            if (0 < x)
                nextId[i++] = (x - 1) * 100 + y;
            if (x < mBoardSize - 1)
                nextId[i++] = (x + 1) * 100 + y;
            if (0 < y)
                nextId[i++] = x * 100 + y - 1;
            if (y < mBoardSize - 1)
                nextId[i++] = x * 100 + y + 1;
            return nextId;
        }

        /// <summary>
        /// 動作をシミュレーションして問題を作成する
        /// </summary>
        private void createProblem()
        {
            mCreatePreId = -1;
            mCount = 0;
            drawBoard();

            //  問題の作成のための操作回数を設定
            mCreateBoardCount = (int)(mRandom.NextDouble() * Math.Pow(mBoardSize, 3.0) + 3); //  操作回数
            mCount++;

            dispatcherTimer.Start();    //  タイマースタート
        }

        /// <summary>
        /// タイマーイベントで一回分の動作をシミュレートする
        /// </summary>
        private void createProblemBoard()
        {
            int id = ydraw.GButtonTitleIdGet("*");  //  ブランクの位置
            int[] nextIds = getNextPosition(id);    //  ブランクの位置から移動できるブロックの位置のリストを求める
            int nextId;
            //  移動できるブロックからランダムに一つ選択する
            do {
                nextId = nextIds[(int)(mRandom.NextDouble() * nextIds.Length)];
            } while (nextId == mCreatePreId || nextId < 0);
            swapTitle(id, nextId);
            mCreatePreId = id;

            if (mCreateBoardCount-- <= 0) {
                dispatcherTimer.Stop();     //  タイマー処理終了
            }
        }

        /// <summary>
        /// 盤が完成したどうかを判定する
        /// </summary>
        /// <returns>判定</returns>
        private bool completeChk()
        {
            int i = 1;
            for (int y = 0; y < mBoardSize; y++) {
                for (int x = 0; x < mBoardSize; x++) {
                    int id = x * 100 + y;
                    if (i < mBoardSize * mBoardSize) {
                        if (ydraw.GButtonTitleGet(id).CompareTo(mNumber[i++]) != 0)
                            return false;
                    }
                }
            }
            return true;
        }


        /// <summary>
        /// セルのタイトルを入れ替える
        /// </summary>
        /// <param name="id1">ID</param>
        /// <param name="id2">ID</param>
        private void swapTitle(int id1, int id2)
        {
            string txt1 = ydraw.GButtonTitleGet(id1);
            string txt2 = ydraw.GButtonTitleGet(id2);
            ydraw.GButtonTitle(id1, txt2);
            ydraw.GButtonTitle(id2, txt1);

            ydraw.GButtonDraws();
        }


        /// <summary>
        /// 指定のセルの上下左右にブランクセル[*]がないかをチェック
        /// </summary>
        /// <param name="id">指定セル</param>
        /// <returns>ブランクセルのID(-1はブランクセルなし)</returns>
        private int getBlankId(int id)
        {
            int[] rel = { -1, 1, -100, 100 };       //  上下左右のセルの相対位置
            for (int i = 0; i < rel.Length; i++) {
                int id2 = id + rel[i];
                int x = id2 / 100;
                int y = id2 % 100;
                if (0 <= x && x < mBoardSize)
                    if (0 <= y && y < mBoardSize) {
                        if (chkBlank(id2)) {
                            return id2;             //  ブランクセルのID
                        }
                    }
            }
            return -1;      //  ブランクセルなし
        }

        /// <summary>
        /// 指定のセルがブランク[*]かどうかを確認する
        /// </summary>
        /// <param name="id">ID</param>
        /// <returns>true [*]</returns>
        private bool chkBlank(int id)
        {
            if (ydraw.GButtonTitleGet(id).CompareTo("*") == 0)
                return true;
            else
                return false;
        }

        /// <summary>
        /// 盤の状態を取得する(ブランク[*]は[0]に変換)
        /// </summary>
        /// <returns>盤の状態</returns>
        public sbyte[] getBoard()
        {
            sbyte[] board = new sbyte[mBoardSize * mBoardSize];
            int i = 0;
            for (int y = 0; y < mBoardSize; y++) {
                for (int x = 0; x < mBoardSize; x++) {
                    int id = x * 100 + y;
                    string title = ydraw.GButtonTitleGet(id);
                    if (title.CompareTo("*") == 0) {
                        board[i++] = 0;
                    } else {
                        board[i++] = sbyte.Parse(ydraw.GButtonTitleGet(id));
                    }
                }
            }
            return board;
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
            ydraw.drawWText("回数 : " + n, new Point(x, y), 0,
                HorizontalAlignment.Center, VerticalAlignment.Top);
            ydraw.setTextOverWrite(true);
        }

        /// <summary>
        /// 完成メッセージの表示(盤の中央)
        /// </summary>
        /// <param name="text">メッセージ</param>
        private void completeMessage(string text)
        {
            ydraw.setTextSize(mTextSize * 6f);
            ydraw.setTextColor(Brushes.Red);
            ydraw.drawWText(text, new Point(mWidth / 2, mOy), 0,
                HorizontalAlignment.Center, VerticalAlignment.Bottom);
            ydraw.setTextColor(Brushes.Black);
        }

        /// <summary>
        /// 盤の作成と状態の表示
        /// </summary>
        private void drawBoard()
        {
            if (!mDisp)
                return;

            solverList.Text = "　　　　　";

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
                        int id = x * 100 + y;
                        ydraw.GButtonAdd(id, BUTTONTYPE.RECT,
                            new Rect((float)x * mHaba + mOx, (float)y * mHaba + mOy, mHaba, mHaba));
                        ydraw.GButtonBorderThickness(id, 2f);
                        ydraw.GButtonTitleColor(x * 100 + y, Brushes.Black);
                        ydraw.GButtonTitleRatio(id, 0.8);
                        if (x == mBoardSize - 1 && y == mBoardSize - 1)
                            ydraw.GButtonTitle(id, mNumber[0]);    //  [*]を表示
                        else
                            ydraw.GButtonTitle(id, mNumber[y * mBoardSize + x + 1]);   //  数値を表示
                    }
                }
            }
            countMessage(mCount);
            //  盤の表示
            ydraw.GButtonDraws();
        }

        /// <summary>
        /// 解法手順の表示 (幅優先探索)
        /// </summary>
        private void solver()
        {
            sbyte[] board = getBoard();
            //  問題パターン
            int boardSize = (int)Math.Sqrt(board.Length);
            string txt = "盤の状態\n";
            for (int row = 0; row < boardSize; row++) {
                for (int col = 0; col < boardSize; col++) {
                    txt += board[row * boardSize + col] + " ";
                }
                txt += "\n";
            }
            solverList.Text = txt;
            txt = "解法\n";
            SlideBoardSolver boardSolver = new SlideBoardSolver(board);
            bool result = boardSolver.solver();
            if (!result) {
                txt += "計算不可\n" + boardSolver.mErrorMsg;
            } else {
                txt += "探索数: " + boardSolver.getCount() + "\n手順数: " + boardSolver.getLevel() + "\n";
                if (boardSolver.getStat() == SlideBoard.Status.COMPLETE) {
                    txt += "回答手順\n";
                    List<int> resultNo = boardSolver.getResult();
                    for (int i = resultNo.Count - 2; 0 <= i; i--) {
                        txt += "" + (resultNo.Count - i - 1) + ": [" + resultNo[i] + "] \n";
                    }
                } else if (boardSolver.getStat() == SlideBoard.Status.UNCOMPLETE) {
                    txt += "解答不可";
                } else {
                    txt += "解答未完成(打ち切り)\n" + boardSolver.mErrorMsg;
                }
            }
            solverList.Text = txt;
        }

        /// <summary>
        /// 解法手順の表示 (A*探索)
        /// </summary>
        private void solver2()
        {
            sbyte[] board = getBoard();
            //  問題パターン
            int boardSize = (int)Math.Sqrt(board.Length);
            string txt = "盤の状態\n";
            for (int row = 0; row < boardSize; row++) {
                for (int col = 0; col < boardSize; col++) {
                    txt += board[row * boardSize + col] + " ";
                }
                txt += "\n";
            }
            solverList.Text = txt;
            txt = "解法\n";
            SlideBoardSolver2 boardSolver = new SlideBoardSolver2(board);
            bool result = boardSolver.Solver();
            if (!result) {
                txt += "計算不可\n" + boardSolver.mErrorMsg;
            } else {
                if (boardSolver.mStat == SlideBoardSolver2.Status.COMPLETE) {
                    txt += "探索数: \n " + boardSolver.getCount() + "\n";
                    //txt += "手順数: " + boardSolver.getLevel() + "\n";
                    txt += "回答手順\n";
                    List<int> resultNo = boardSolver.getResultList();
                    for (int i = resultNo.Count - 2; 0 <= i; i--) {
                        txt += "" + (resultNo.Count - i - 1) + ": [" + resultNo[i] + "] \n";
                    }
                } else if (boardSolver.mStat == SlideBoardSolver2.Status.UNCOMPLETE) {
                    txt += "解答不可\n";
                } else {
                    txt += "解答未完成(打ち切り)\n" + boardSolver.mErrorMsg;
                }
            }
            solverList.Text = txt;
        }
    }
}
