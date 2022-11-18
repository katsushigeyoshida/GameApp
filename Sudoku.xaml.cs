using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;         //  参照の追加でアセンブリから追加する
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using WpfLib;

namespace GameApp
{
    /// <summary>
    /// Sudoku.xaml の相互作用ロジック
    /// </summary>
    public partial class Sudoku : Window
    {
        private double mWindowWidth;                            //  ウィンドウの高さ
        private double mWindowHeight;                           //  ウィンドウ幅
        private double mPrevWindowWidth;                        //  変更前のウィンドウ幅
        private WindowState mWindowState = WindowState.Normal;  //  ウィンドウの状態(最大化/最小化)

        //  システムメニューに追加(https://ameblo.jp/kani-tarou/entry-10240156672.html)
        [DllImport("user32.dll")]
        private static extern IntPtr GetSystemMenu(IntPtr hWnd, int bRevert);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int AppendMenu(IntPtr hMenu, int Flagsw, int IDNewItem, string lpNewItem);
        private HwndSource hwndSource = null;
        private const int WM_SYSCOMMAND = 0x112;
        private const int MF_SEPARATOR = 0x0800;
        private const int FIXPOS_COLOR_MENU = 100;              //  変更不可セルの背景色の設定
        private const int FIXPOS_PATTERN_INPORT_MENU = 200;     //  問題パターンの追加取込み
        private const int FIXPOS_PATTERN_EXPORT_MENU = 300;     //  問題パターンのエキスポート

        private string mAppFolder;                              //  アプリケーションのフォルダ
        private string mSaveFileName = "SudokuData";            //  問題パターンファイル名
        private string mSvaeBoardName = "SudokuBoard";          //  一時保存データ
        private string mSaveFilePath;                           //  保存ファイルのフルパス
        private string mSaveBoardPath;
        private Dictionary<String, String> mDataList = new Dictionary<String, String>();    //  問題パターンリスト

        private double mTextSize = 0;                           //  文字の大きさ
        private int mWidth = 1000;                              //  Viewの論理サイズ
        private int mHeight = 1000;                             //  Viewの論理サイズ
        private int mBoardSize = 9;                             //  盤の数
        private float mBoardSizeRatio = 0.9f;                   //  画面に対する盤の大きさの比
        private float mHaba = 0;                                //  盤のマスの大きさ
        private float mOx, mOy;                                 //  盤の原点
        private HashSet<int> mFixPos = new HashSet<int>();      //  変更不可のセル(問題パターン)
        private bool mFixPosMode = false;                       //  問題パターンが設定されている状態
        private int mCurId = -1;                                //  変更されたセルのID
        private bool mSupportMode = false;                      //  解法補助ボタン表示モード
        private bool mSupport2Mode = false;                     //  解法補助表示モード
        private int mBlockId = 100;                             //  区切り線のためのブロックのID
        private int mSupportBtId = 200;                         //  解法の補助ボタンのID
        private int mNumberInBtId = 300;                        //  数値入力ボードのID
        private bool mNumberInBtDisp = false;
        private string mTempBoard;
        private int mFixPosColor = 0x1e7d7d7d;                  //  変更不可セルの背景色
        private int mCompleteColor = 0x1e00ffff;                //  完成セルの背景色
        private int mDuplicateColor = 0x1eff0000;               //  重複セルの背景色
        private Brush mPassColor = Brushes.Blue;                //  候補数値の色


        YGButton ydraw;                         //  グラフィックライブラリ
        YLib mYlib = new YLib();                //  単なるライブラリ

        public Sudoku()
        {
            InitializeComponent();

            mWindowWidth = WindowForm.Width;
            mWindowHeight = WindowForm.Height;
            mPrevWindowWidth = mWindowWidth;

            WindowFormLoad();

            ydraw = new YGButton(canvas);

            //  実行ファイルのフォルダを取得しワークフォルダとする
            mAppFolder = System.AppDomain.CurrentDomain.BaseDirectory;
            //  問題パターンファイル名の設定
            mSaveFilePath = mAppFolder + "/" + mSaveFileName + ".csv";
            mSaveBoardPath = mAppFolder + "/" + mSvaeBoardName + ".csv";
            //  問題パターンの取り込み
            loadPatternData(mDataList, mSaveFilePath, false);

            mTempBoard = "";
            mFixPos.Clear();
        }

        private void WindowForm_Loaded(object sender, RoutedEventArgs e)
        {
            mWindowWidth = WindowForm.Width;
            mWindowHeight = WindowForm.Height;

            // dシステムメニューに設定メニューを追加
            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            IntPtr menu = GetSystemMenu(hwnd, 0);
            AppendMenu(menu, MF_SEPARATOR, 0, null);
            AppendMenu(menu, 0, FIXPOS_COLOR_MENU, "変更不可セルの色設定");
            AppendMenu(menu, 0, FIXPOS_PATTERN_INPORT_MENU, "問題パターンのインポート");
            AppendMenu(menu, 0, FIXPOS_PATTERN_EXPORT_MENU, "問題パターンのエキスポート");

            //  盤の初期化
            initScreen();
            patternSetComboBox();
        }

        /// <summary>
        /// システムメニューに追加するためのフック設定
        /// </summary>
        /// <param name="e"></param>
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            // フックを追加
            hwndSource = PresentationSource.FromVisual(this) as HwndSource;
            if (hwndSource != null) {
                hwndSource.AddHook(new HwndSourceHook(this.hwndSourceHook));
            }
        }

        /// <summary>
        /// システムメニューに追加したメニューの処理
        /// </summary>
        /// <param name="hwnd"></param>
        /// <param name="msg"></param>
        /// <param name="wParam"></param>
        /// <param name="lParam"></param>
        /// <param name="handled"></param>
        /// <returns></returns>
        private IntPtr hwndSourceHook(IntPtr hwnd, int msg,
           IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_SYSCOMMAND) {
                if (wParam.ToInt32() == FIXPOS_COLOR_MENU) {
                    //MessageBox.Show("メニューテスト");
                    System.Windows.Forms.ColorDialog cd = new System.Windows.Forms.ColorDialog();
                    cd.AllowFullOpen = true;
                    cd.SolidColorOnly = false;
                    cd.CustomColors = new int[] {
                        0xf4f4f4, 0xdfdfdf, 0xcfcfcf, 0xbfbfbf, 0xffff80,0x95ff95 };
                    System.Windows.Forms.DialogResult result = cd.ShowDialog();
                    if (result == System.Windows.Forms.DialogResult.OK) {
                        mFixPosColor = cd.Color.ToArgb();
                        setFixPosColor();
                        drawBoard();
                    }
                } else if (wParam.ToInt32() == FIXPOS_PATTERN_INPORT_MENU) {
                    //  問題パターンのインポート
                    fileSelectLoad();
                } else if (wParam.ToInt32() == FIXPOS_PATTERN_EXPORT_MENU) {
                    //  問題パターンのエキスポート
                    fileSaveAS();
                }
            }
            return IntPtr.Zero;
        }

        private void WindowForm_LayoutUpdated(object sender, EventArgs e)
        {
            //  最大化時の処理
            if (this.WindowState != mWindowState &&
                this.WindowState == WindowState.Maximized) {
                mWindowWidth = System.Windows.SystemParameters.WorkArea.Width;
                mWindowHeight = System.Windows.SystemParameters.WorkArea.Height;

                drawBoard();    //  盤の再表示
            } else if (this.WindowState != mWindowState ||
                mWindowWidth != WindowForm.Width ||
                mWindowHeight != WindowForm.Height) {
                mWindowWidth = WindowForm.Width;
                mWindowHeight = WindowForm.Height;

                drawBoard();    //  盤の再表示
            }
            mWindowState = this.WindowState;

            //  ウィンドウの大きさに合わせてコントロールの幅を変更する
            double dx = mWindowWidth - mPrevWindowWidth;
            patternCb.Width += dx;
            mPrevWindowWidth = mWindowWidth;
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
            if (Properties.Settings.Default.SudokuWindowWidth < 100 || Properties.Settings.Default.SudokuWindowHeight < 100 ||
                System.Windows.SystemParameters.WorkArea.Height < Properties.Settings.Default.SudokuWindowHeight) {
                Properties.Settings.Default.SudokuWindowWidth = mWindowWidth;
                Properties.Settings.Default.SudokuWindowHeight = mWindowHeight;
            } else {
                WindowForm.Top = Properties.Settings.Default.SudokuWindowTop;
                WindowForm.Left = Properties.Settings.Default.SudokuWindowLeft;
                WindowForm.Width = Properties.Settings.Default.SudokuWindowWidth;
                WindowForm.Height = Properties.Settings.Default.SudokuWindowHeight;
            }
            mFixPosColor = Properties.Settings.Default.FixPosColor;     //  変更不可セルの背景色
        }

        /// <summary>
        /// Window状態を保存する
        /// </summary>
        private void WindowFormSave()
        {
            //  Windowの位置とサイズを保存(登録項目をPropeties.settingsに登録して使用する)
            Properties.Settings.Default.SudokuWindowTop = WindowForm.Top;
            Properties.Settings.Default.SudokuWindowLeft = WindowForm.Left;
            Properties.Settings.Default.SudokuWindowWidth = WindowForm.Width;
            Properties.Settings.Default.SudokuWindowHeight = WindowForm.Height;
            Properties.Settings.Default.FixPosColor = mFixPosColor;             //  変更不可セルの背景色
            Properties.Settings.Default.Save();
        }

        /// <summary>
        /// 登録パターン名の入力と選択コンボボックス
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void patternCb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (0 <= patternCb.SelectedIndex)
                setPatternBoard(mDataList[patternCb.Items[patternCb.SelectedIndex].ToString()]);
        }

        /// <summary>
        /// [登録]ボタン 盤の状態を問題パターンとして登録する
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void registBtn_Click(object sender, RoutedEventArgs e)
        {
            InputBox dlg = new InputBox();
            dlg.Title = "タイトル登録";
            string patternTitle = DateTime.Now.ToString("yyyyMMdd-HHmm");
            dlg.mEditText = patternTitle;
            var result = dlg.ShowDialog();
            if (result == true) {
                string pattern = getCurBoard();
                mDataList.Add(dlg.mEditText, getCurBoard());    //  データリストに問題パターンを追加
                savePatternData(mDataList, mSaveFilePath);      //  問題パターンをファイルに保存
                patternSetComboBox(dlg.mEditText);              //  コンボボックスに登録
                setPatternBoard(pattern);
            }
        }

        /// <summary>
        /// [削除]ボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void deleteBtn_Click(object sender, RoutedEventArgs e)
        {
            deletePattern();
        }

        /// <summary>
        /// [保存]ボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void saveBtn_Click(object sender, RoutedEventArgs e)
        {
            saveData();
        }

        /// <summary>
        /// [復元]ボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void restorBtn_Click(object sender, RoutedEventArgs e)
        {
            loadData();
        }

        /// <summary>
        /// [クリア]ボタン セルをクリアする
        /// セルが一度選択されていればそのセルのみクリア
        /// 選択されていなければ入力したセルをすべてクリア
        /// 入力セルがなければ問題パターンのセルも含めてすべてクリアする
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void clearBtn_Click(object sender, RoutedEventArgs e)
        {
            if (0 <= mCurId)
                curCellClear();
            else
                boardClear();
        }

        /// <summary>
        /// [解答]ボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void solverBtn_Click(object sender, RoutedEventArgs e)
        {
            executeSolver();
        }

        /// <summary>
        /// [問題作成]ボタン 問題を作成する
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void creatBtn_Click(object sender, RoutedEventArgs e)
        {
            CreatSudokuProblem creatProblem = new CreatSudokuProblem();
            int[] creatBoard = creatProblem.getCreatProblem(55, 200);
            string buf = "";
            for (int i = 0; i < creatBoard.Length; i++)
                buf += creatBoard[i].ToString();
            setPatternBoard(buf);
            bottomMessage("空白数: " + creatProblem.mMaxCount
                + "(" + creatProblem.mRepeatCount + ")", 0.8f);
        }

        /// <summary>
        /// [?]ボタン ヘルプ表示
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void helpBtn_Click(object sender, RoutedEventArgs e)
        {
            HelpView help = new HelpView();
            help.mHelpText = HelpText.mSudokuHelp;
            help.Show();
        }

        /// <summary>
        /// 補助機能の表示/非表示を設定
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SupportCb_Click(object sender, RoutedEventArgs e)
        {
            if (SupportCb.IsChecked == true)
                setSupportBordVisible(true);
            else
                setSupportBordVisible(false);
            drawBoard();
        }

        /// <summary>
        /// 補助機能2の表示/非表示を設定
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SupportCb2_Click(object sender, RoutedEventArgs e)
        {
            if (SupportCb2.IsChecked == true) {
                mSupport2Mode = true;
                boardPassCheck();
            } else {
                mSupport2Mode = false;
                boardPassCheckClear();
            }
            drawBoard();
        }

        /// <summary>
        /// [マウスボタン]操作
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Point sp = e.GetPosition(this);         //  マウスの位置
                                                    //  上部と左部の分だけマウスの位置を調節する
                                                    //sp.X -= solverList.ActualWidth;
            sp.Y -= menuBar.ActualHeight;
            Point pt = ydraw.cnvScreen2World(sp);   //  マウスの位置を論理座標に変換
            int id = ydraw.GButtonDownId(pt);       //  マウスの位置からセルのIDを求める

            if (mSupportBtId <= id && id < mNumberInBtId) {
                //  補助ボタン操作
                drawBoard();                        //  盤を表示する
                supportDraw(id);                    //  使用できないところに線を引く
            } else if (0 <= id && !mFixPos.Contains(id)) {
                //  盤上のボタン操作
                if (e.RightButton == System.Windows.Input.MouseButtonState.Pressed) {   //  マウス右ボタン
                    if (id < mSupportBtId) {
                        //  セルをクリアする
                        mCurId = id;
                        setIdNum(mCurId, 0);        //  数値入力ボードの選択数値を設定
                    }
                } else {
                    //  数値を入力
                    if (mNumberInBtId <= id) {
                        setIdNum(mCurId, int.Parse(ydraw.GButtonTitleGet(id)));   //  数値入力ボードの選択数値を設定
                    } else {
                        mCurId = id;
                    }
                    numberInputBord(pt);    //  数値入力ボード表示/非表示
                }

                boradColorClear();          //  背景色の設定をクリア
                bool b = boardCheck();      //  完成チェック(行、列、ブロックの完成は背景色を変える)
                boardDuplicateCheck();      //  数値の重複があれば背景色を変える
                setFixPosColor();           //  問題の入っている数値ボタンの色を設定
                if (mSupport2Mode)
                    boardPassCheck();
                drawBoard();                //  盤を表示する
                if (b) {
                    //  完成時のメッセージ表示
                    SupportCb.IsChecked = false;
                    setSupportBordVisible(false);
                    drawBoard();                //  盤を表示する
                    bottomMessage("完成");
                } else if (mFixPosMode)
                    passCheckMessage(id);
            } else {
                mNumberInBtDisp = true;
                numberInputBord(pt);        //  数値入力ボード表示
                drawBoard();                //  盤を表示する
            }
        }

        /// <summary>
        /// パターン削除
        /// </summary>
        private void deletePattern()
        {
            String pattern = patternCb.Text;
            if (mDataList.ContainsKey(pattern))
                mDataList.Remove(pattern);
            patternSetComboBox();                           //  削除した内容でコンボボックスに再登録
            savePatternData(mDataList, mSaveFilePath);
        }


        /// <summary>
        /// Solverで求めた解を盤に反映する
        /// </summary>
        /// <param name="board">解答</param>
        /// <param name="count">回答に要した操作回数</param>
        public void setSolveData(int[] board, int count)
        {
            for (int x = 0; x < 9; x++) {
                for (int y = 0; y < 9; y++) {
                    if (!mFixPos.Contains(getId(x, y)))
                        ydraw.GButtonTitle(getId(x, y), board[y * 9 + x] == 0 ? "" : board[y * 9 + x].ToString());
                }
            }
            if (boardCheck()) {
                setFixPosColor();
                drawBoard();
                bottomMessage("解法　操作回数：" + count, 1.0f);
            }
        }


        /// <summary>
        /// 盤の入力値をすべてクリアする
        /// 入力値がない時は盤を初期化する
        /// </summary>
        public void boardClear()
        {
            if (0 < getInputdataCount()) {
                //  入力したセルだけをクリアする
                for (int x = 0; x < 9; x++) {
                    for (int y = 0; y < 9; y++) {
                        int id = getId(x, y);
                        if (!mFixPos.Contains(id)) {
                            ydraw.GButtonTitle(id, "");
                            ydraw.GButtonBackColor(id, Brushes.White);
                        }
                    }
                }
                drawBoard();                //  盤を再表示
            } else {
                //  すべたのセルをクリアし初期化する
                initBoard();
                setFixPosClear();
            }
        }

        /// <summary>
        /// 値を設定したセルの数を求める
        /// </summary>
        /// <returns>セルの数</returns>
        private int getInputdataCount()
        {
            int n = 0;
            for (int x = 0; x < 9; x++) {
                for (int y = 0; y < 9; y++) {
                    int id = getId(x, y);
                    if (!mFixPos.Contains(id))
                        if (0 < getButtonNo(id))
                            n++;
                }
            }
            return n;
        }

        /// <summary>
        /// 現在選択されているセルをクリアする
        /// </summary>
        public void curCellClear()
        {
            if (0 <= mCurId && !mFixPos.Contains(mCurId))
                ydraw.GButtonTitle(mCurId, "");
            mCurId = -1;
            drawBoard();                //  盤を再表示
        }

        /// <summary>
        /// 盤文字列データを盤面に表示させる(問題のパターン設定)
        /// 盤文字列データは"0123456...789"などの数値文字列で表す
        /// </summary>
        /// <param name="board">盤文字列データ</param>
        public void setPatternBoard(String board)
        {
            boradColorClear();
            setBoardData(board, false);
            setFixPos();
            setFixPosColor();
            drawBoard();
        }


        /// <summary>
        /// 問題パターンは変更できないように固定するIDを登録する
        /// </summary>
        public void setFixPos()
        {
            mFixPos.Clear();
            for (int x = 0; x < 9; x++) {
                for (int y = 0; y < 9; y++) {
                    int n = getButtonNo(x, y);
                    if (0 < n) {
                        mFixPos.Add(getId(x, y));
                    }
                }
            }
            mFixPosMode = true;
        }

        /// <summary>
        /// 固定位置の背景色をライトグレイに設定する
        /// </summary>
        public void setFixPosColor()
        {
            foreach (int id in mFixPos) {
                ydraw.GButtonBackColor(id, mYlib.getInt2Color(mFixPosColor));
            }
        }

        /// <summary>
        /// 固定セル(問題パターン)を解除する
        /// </summary>
        public void setFixPosClear()
        {
            mFixPos.Clear();
            mFixPosMode = false;
        }

        /// <summary>
        /// 文字列による盤データを盤面に表示させる(データの復元)
        /// セルの確定があるため、同じ問題パターンに適用
        /// </summary>
        /// <param name="board"></param>
        public void setCurBoard(String board)
        {
            boradColorClear();
            setBoardData(board, true);
            setFixPosColor();
            drawBoard();
        }

        /// <summary>
        /// 盤の状態(表示データ)を盤文字列データに変換する
        /// 空白は'0'にする
        /// </summary>
        /// <returns>盤の文字列データ</returns>
        public String getCurBoard()
        {
            String board = "";
            for (int y = 0; y < mBoardSize; y++) {
                for (int x = 0; x < mBoardSize; x++) {
                    int n = getButtonNo(x, y);
                    board += 9 < n ? "0" : n.ToString();
                }
            }
            return board;
        }


        /// <summary>
        /// 指定値のセルに入れられる数値の候補を表示する
        /// 指定値の行、列、ブロックで使用されていない数値の一覧
        /// </summary>
        /// <param name="id">ID</param>
        public void passCheckMessage(int id)
        {
            String msg = "候補値" + passCheck(id);
            topMessage(msg);
        }

        /// <summary>
        /// IDで指定されたセルの行、列、ブロックで使用していない数値を求める
        /// </summary>
        /// <param name="id">ID</param>
        /// <returns>数値文字列</returns>
        private string passCheck(int id)
        {
            bool[] check = new bool[10];
            int ox = getXId(id);
            int oy = getYId(id);
            if (9 < ox || 9 < oy)
                return "";
            int x, y;
            for (x = 0; x < 9; x++)
                if (x != ox)
                    check[getButtonNo(x, oy)] = true;
            for (y = 0; y < 9; y++)
                if (y != oy)
                    check[getButtonNo(ox, y)] = true;
            x = ox / 3 * 3;
            y = oy / 3 * 3;
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                    if ((i + x) != ox && (j + y) != oy)
                        check[getButtonNo(i + x, j + y)] = true;

            string msg = "";
            for (int i = 1; i < 10; i++)
                if (!check[i])
                    msg += " " + i.ToString();
            return msg;
        }

        /// <summary>
        /// メッセージを盤の上部に表示
        /// </summary>
        /// <param name="text">表示文字列</param>
        public void topMessage(String text)
        {
            ydraw.setTextSize(mOy * 0.7f);
            ydraw.setTextColor(Brushes.Red);
            ydraw.drawWText(text, new Point(mWidth / 2, mOy), 0,
                            System.Windows.HorizontalAlignment.Center, VerticalAlignment.Bottom);
            ydraw.setTextColor(Brushes.Black);
        }

        /// <summary>
        /// 下部のメッセージ表示
        /// </summary>
        /// <param name="text">表示文字列</param>
        public void bottomMessage(String text)
        {
            bottomMessage(text, 2.0f);
        }

        /// <summary>
        /// メッセージを盤の下部に表示
        /// </summary>
        /// <param name="text"></param>
        public void bottomMessage(String text, float sizeRatio)
        {
            ydraw.setTextSize(mOy * sizeRatio);
            ydraw.setTextColor(Brushes.Red);
            ydraw.drawWText(text, new Point(mWidth / 2, mOy + mHaba * 9), 0,
                            System.Windows.HorizontalAlignment.Center, VerticalAlignment.Top);
            ydraw.setTextColor(Brushes.Black);
        }

        /// <summary>
        /// 盤の完成を確認する
        /// 列単位、行単位、ブロック単位で完成したところは背景をシアンに設定する
        /// </summary>
        /// <returns>成否</returns>
        private bool boardCheck()
        {
            bool result = true;
            for (int x = 0; x < 9; x++) {
                if (rowCheck(x))
                    rowColorSet(x);
                else
                    result = false;
            }
            for (int y = 0; y < 9; y++) {
                if (columnCheck(y))
                    columnColorSet(y);
                else
                    result = false;
            }
            for (int x = 0; x < 9; x += 3) {
                for (int y = 0; y < 9; y += 3) {
                    if (blockCheck(x, y))
                        blockColorSet(x, y);
                    else
                        result = false;
                }
            }
            return result;
        }

        /// <summary>
        /// 列範囲で完成しているかを確認
        /// </summary>
        /// <param name="x">列位置</param>
        /// <returns>成否</returns>
        private bool rowCheck(int x)
        {
            bool[] data = new bool[10];
            for (int y = 0; y < 9; y++) {
                int n = getButtonNo(x, y);
                if (n == 0)
                    return false;
                if (data[n])
                    return false;
                else
                    data[n] = true;
            }
            return true;
        }

        /// <summary>
        /// 行単位で完成しているかを確認
        /// </summary>
        /// <param name="y">行位置</param>
        /// <returns>成否</returns>
        private bool columnCheck(int y)
        {
            bool[] data = new bool[10];
            for (int x = 0; x < 9; x++) {
                int n = getButtonNo(x, y);
                if (n == 0)
                    return false;
                if (data[n])
                    return false;
                else
                    data[n] = true;
            }
            return true;
        }

        /// <summary>
        /// 3x3 のブロック範囲で完成しているかを確認
        /// </summary>
        /// <param name="x">列位置</param>
        /// <param name="y">行位置</param>
        /// <returns>成否</returns>
        private bool blockCheck(int x, int y)
        {
            bool[] data = new bool[10];
            x = x / 3 * 3;
            y = y / 3 * 3;
            for (int i = 0; i < 3; i++) {
                for (int j = 0; j < 3; j++) {
                    int n = getButtonNo(x + i, y + j);
                    if (n == 0)
                        return false;
                    if (data[n])
                        return false;
                    else
                        data[n] = true;
                }
            }
            return true;
        }


        /// <summary>
        /// 列単位で背景色をシアンにする
        /// </summary>
        /// <param name="x">列位置</param>
        private void rowColorSet(int x)
        {
            for (int y = 0; y < 9; y++) {
                ydraw.GButtonBackColor(getId(x, y), mYlib.getInt2Color(mCompleteColor));
            }
        }

        /// <summary>
        /// 行単位で背景色をシアンにする
        /// </summary>
        /// <param name="y">行位置</param>
        private void columnColorSet(int y)
        {
            for (int x = 0; x < 9; x++) {
                ydraw.GButtonBackColor(getId(x, y), mYlib.getInt2Color(mCompleteColor));
            }
        }

        /// <summary>
        /// 3x3ブロック単位で背景色をシアンにする
        /// </summary>
        /// <param name="x">列位置</param>
        /// <param name="y">行位置</param>
        private void blockColorSet(int x, int y)
        {
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++) {
                    ydraw.GButtonBackColor(getId(x + i, y + j), mYlib.getInt2Color(mCompleteColor));
                }
        }

        /// <summary>
        /// 盤上の空白セルに候補数値を入れる
        /// </summary>
        private void boardPassCheck()
        {
            for (int row = 0; row < 9; row++) {
                for (int col = 0; col < 9; col++) {
                    int id = getId(col, row);
                    int n = getButtonNo(id);
                    if (n < 1 || 9 < n) {
                        string pass = passCheck(id);
                        if (1 < pass.Length)
                            pass = pass.Substring(0, pass.Length / 2) + "\n" + pass.Substring(pass.Length / 2);
                        ydraw.GButtonTitleColor(id, mPassColor);
                        ydraw.GButtonTitle(id, pass);
                    }
                }
            }
        }

        /// <summary>
        /// 候補数値が入ったセルをクリアする
        /// </summary>
        private void boardPassCheckClear()
        {
            for (int row = 0; row < 9; row++) {
                for (int col = 0; col < 9; col++) {
                    int id = getId(col, row);
                    int n = getButtonNo(id);
                    if (n < 1 || 9 < n) {
                        ydraw.GButtonTitleColor(id, Brushes.Black);
                        ydraw.GButtonTitle(id, "");
                    }
                }
            }
        }

        /// <summary>
        /// 盤上での数値の重複チェックを行う
        /// 重複していればそのセルの背景色を薄い赤にする
        /// </summary>
        /// <returns></returns>
        private bool boardDuplicateCheck()
        {
            bool result = true;
            for (int x = 0; x < 9; x++) {
                rowDuplicateCheck(x);
            }
            for (int y = 0; y < 9; y++) {
                columnDuplicateCheck(y);
            }
            for (int x = 0; x < 9; x += 3) {
                for (int y = 0; y < 9; y += 3) {
                    blockDuplicateCheck(x, y);
                }
            }
            return result;
        }

        /// <summary>
        /// 列の中に同じ数値があるかをチェックする
        /// あれば背景色を薄い赤にする
        /// </summary>
        /// <param name="x">列</param>
        private void rowDuplicateCheck(int x)
        {
            int[] data = new int[10];
            for (int y = 0; y < 9; y++) {
                int n = getButtonNo(x, y);
                if (0 < n)
                    data[n]++;
            }
            for (int y = 0; y < 9; y++) {
                int n = getButtonNo(x, y);
                if (1 < data[n])
                    ydraw.GButtonBackColor(getId(x, y), mYlib.getInt2Color(mDuplicateColor));
            }
        }

        /// <summary>
        /// 行の中に同じ数値があるかをチェック
        /// あれば背景色を薄い赤にする
        /// </summary>
        /// <param name="y">行</param>
        private void columnDuplicateCheck(int y)
        {
            int[] data = new int[10];
            for (int x = 0; x < 9; x++) {
                int n = getButtonNo(x, y);
                if (0 < n)
                    data[n]++;
            }
            for (int x = 0; x < 9; x++) {
                int n = getButtonNo(x, y);
                if (1 < data[n])
                    ydraw.GButtonBackColor(getId(x, y), mYlib.getInt2Color(mDuplicateColor));
            }
        }

        /// <summary>
        /// ブロックの中に同じ数値があるかをチェック
        /// あれば背景色を薄い赤にする
        /// </summary>
        /// <param name="x">行</param>
        /// <param name="y">列</param>
        private void blockDuplicateCheck(int x, int y)
        {
            int[] data = new int[10];
            x = x / 3 * 3;
            y = y / 3 * 3;
            for (int i = 0; i < 3; i++) {
                for (int j = 0; j < 3; j++) {
                    int n = getButtonNo(x + i, y + j);
                    if (0 < n)
                        data[n]++;
                }
            }
            for (int i = 0; i < 3; i++) {
                for (int j = 0; j < 3; j++) {
                    int n = getButtonNo(x + i, y + j);
                    if (1 < data[n])
                        ydraw.GButtonBackColor(getId(x + i, y + j), mYlib.getInt2Color(mDuplicateColor));
                }
            }
        }

        /// <summary>
        /// 盤の背景色をクリア(白)する
        /// </summary>
        public void boradColorClear()
        {
            for (int x = 0; x < 9; x++)
                for (int y = 0; y < 9; y++) {
                    ydraw.GButtonBackColor(getId(x, y), Brushes.White);
                    ydraw.GButtonTitleColor(getId(x, y), Brushes.Black);
                }
        }

        /// <summary>
        /// 盤に数値データを設定する
        /// 盤データ(文字列: 01230056...90)
        /// 固定数値セルにデータを書き込まない(問題として設定されている場所)
        /// </summary>
        /// <param name="board">盤データ</param>
        /// <param name="fixpos">固定値書き込み可否</param>
        private void setBoardData(String board, bool fixpos)
        {
            int n = 0;
            for (int y = 0; y < mBoardSize; y++) {
                for (int x = 0; x < mBoardSize; x++) {
                    if (!fixpos || !mFixPos.Contains(getId(x, y)))
                        ydraw.GButtonTitle(getId(x, y), board[n] == '0' ? "" : board[n].ToString());
                    n++;
                }
            }
        }

        /// <summary>
        /// ボタンのタイトルの数字をインクリメントする
        /// "0"の場合は空白とする
        /// </summary>
        /// <param name="id">ID</param>
        private void incId(int id)
        {
            int no = getButtonNo(id);
            if (0 <= no && no < 9)
                no++;
            else
                no = 0;
            ydraw.GButtonTitle(id, 0 < no ? no.ToString() : "");
        }

        /// <summary>
        /// ボタンのタイトルの数字をデクリメントする
        /// "0"の場合は空白とする
        /// </summary>
        /// <param name="id"></param>
        private void decId(int id)
        {
            int no = getButtonNo(id);
            if (0 < no && no <= 9)
                no--;
            else
                no = 9;
            ydraw.GButtonTitle(id, 0 < no ? no.ToString() : "");
        }

        /// <summary>
        /// ボタンのタイトルに数値を設定
        /// </summary>
        /// <param name="id">ID</param>
        /// <param name="num">表示する数値</param>
        private void setIdNum(int id, int num)
        {
            ydraw.GButtonTitle(id, 0 < num ? num.ToString() : "");
        }

        /// <summary>
        /// x,yで指定された位置のセルの数値を取得(空白は0)
        /// </summary>
        /// <param name="x">列位置</param>
        /// <param name="y">行位置</param>
        /// <returns>数値</returns>
        private int getButtonNo(int x, int y)
        {
            return getButtonNo(getId(x, y));
        }


        /// <summary>
        /// ID で指定された位置のセルの数値を取得(数値以外は0)
        /// </summary>
        /// <param name="id">セルのID</param>
        /// <returns>数値</returns>
        private int getButtonNo(int id)
        {
            int n;
            string no = ydraw.GButtonTitleGet(id);
            if (0 <= no.IndexOf(' '))
                return 0;
            if (int.TryParse(no, out n))
                return n;
            else
                return 0;
        }

        /// <summary>
        /// 行(y)列(x)からボタンのIDを求める
        /// </summary>
        /// <param name="x">列番豪</param>
        /// <param name="y">行番号</param>
        /// <returns>ID</returns>
        private int getId(int x, int y)
        {
            return x * 10 + y;
        }

        /// <summary>
        /// IDから列番号を取得
        /// </summary>
        /// <param name="id">ID</param>
        /// <returns>列番号</returns>
        private int getXId(int id)
        {
            return id / 10;
        }

        /// <summary>
        /// IDから行番号を取得
        /// </summary>
        /// <param name="id">ID</param>
        /// <returns>行番号</returns>
        private int getYId(int id)
        {
            return id % 10;
        }

        /// <summary>
        /// 入力用数値ボタンの表示(円形配置)
        /// </summary>
        /// <param name="mp">表示の基準位置(中心座標)</param>
        private void numberInputBord(Point mp)
        {
            if (mNumberInBtDisp) {
                ydraw.GButtonRemove(mNumberInBtId);
                mNumberInBtDisp = false;
            } else {
                //string[] titles = { "","1", "2", "3", "4", "5", "6", "7", "8", "9" };
                //float width = mHaba * 1.5f;
                //Point sp = new Point(mp.X - width, mp.Y - width);
                //Point ep = new Point(mp.X + width, mp.Y + width);
                //Rect rect = new Rect(sp, ep);
                //ydraw.GButtonGroupAdd(mNumberInBtId, BUTTONTYPE.GROUPCIRCLE, rect, titles.Length, 0, titles);
                string[] titles = { "7", "8", "9", "4", "5", "6", "1", "2", "3" };
                float width = mHaba;
                Point sp = new Point(mp.X - width, mp.Y - width);
                Point ep = new Point(mp.X + width, mp.Y + width);
                Rect rect = new Rect(sp, ep);
                ydraw.GButtonGroupAdd(mNumberInBtId, BUTTONTYPE.GROUPRECT, rect, 3, 3, titles);
                mNumberInBtDisp = true;
            }
        }

        /// <summary>
        /// 解法の補助ボタンが押されたときに指定された数値で設定できない場所に取り消し線を入れて
        /// 設定できる場所をわかるようにする。ブロックまたは行、列で一か所しかない場合は確定となる
        /// </summary>
        /// <param name="id">ID</param>
        private void supportDraw(int id)
        {
            float startx = mOx + mHaba / 3f;
            float endx = mOx + mHaba * mBoardSize - mHaba / 3f;
            float starty = mOy + mHaba / 3f;
            float endy = mOy + mHaba * mBoardSize - mHaba / 3f;
            int n = id - mSupportBtId;

            ydraw.setColor(Brushes.Blue);
            ydraw.setThickness(1f);

            for (int x = 0; x < 9; x++) {
                for (int y = 0; y < 9; y++) {
                    if (n == getButtonNo(x, y)) {
                        ydraw.drawWLine(new Point(startx, mOy + mHaba / 2f + mHaba * y), new Point(endx, mOy + mHaba / 2f + mHaba * y));
                        ydraw.drawWLine(new Point(mOx + mHaba / 2f + mHaba * x, starty), new Point(mOx + mHaba / 2f + mHaba * x, endy));
                        int blockX = x / 3 * 3;
                        int blockY = y / 3 * 3;
                        for (int i = 0; i < 3; i++)
                            ydraw.drawWLine(new Point(mOx + mHaba / 3f + mHaba * blockX, mOy + mHaba / 2f + mHaba * (blockY + i)),
                                        new Point(mOx - mHaba / 3f + mHaba * (blockX + 3), mOy + mHaba / 2f + mHaba * (blockY + i)));
                    }
                }
            }
        }

        /// <summary>
        /// 解法のための補助ボタンの表示
        /// </summary>
        private void supportBord()
        {
            float ox = mOx;
            float oy = mOy + mHaba * mBoardSize + mHaba * 0.7f;
            for (int i = 1; i <= 9; i++) {
                ydraw.GButtonAdd(mSupportBtId + i, BUTTONTYPE.RECT,
                    new Rect((float)ox + mHaba * (i - 1), (float)oy, mHaba, mHaba));
                ydraw.GButtonBorderThickness(mSupportBtId + i, 1f);
                ydraw.GButtonTitleColor(mSupportBtId + i, Brushes.Black);
                ydraw.GButtonTitle(mSupportBtId + i, i.ToString());
            }
        }

        /// <summary>
        /// 解法補助の数値ボタンの表示/非表示設定
        /// </summary>
        /// <param name="visible">表示/非表示</param>
        private void setSupportBordVisible(bool visible)
        {
            for (int i = 1; i <= 9; i++) {
                ydraw.GButtonVisible(mSupportBtId + i, visible);
                ydraw.GButtonEnabled(mSupportBtId + i, visible);
            }
        }


        /// <summary>
        /// 3x3 の盤に区切り線を入れる
        /// </summary>
        private void drawSmallBoard()
        {
            ydraw.setColor(Brushes.Black);
            ydraw.setThickness(3f);
            for (int x = 0; x <= mBoardSize / 3; x++) {
                ydraw.drawWLine(new Point(mOx + mHaba * 3f * x, mOy), new Point(mOx + mHaba * 3f * x, mOy + mHaba * 9f));
            }
            for (int y = 0; y <= mBoardSize / 3; y++) {
                ydraw.drawWLine(new Point(mOx, mOy + mHaba * 3f * y), new Point(mOx + mHaba * 9f, mOy + mHaba * 3f * y));
            }
        }

        /// <summary>
        /// 論理座標の設定と画面クリア
        /// </summary>
        private void windowSet()
        {
            ydraw.setWindowSize(canvas.ActualWidth, canvas.ActualHeight);
            ydraw.setViewArea(0, 0, canvas.ActualWidth, canvas.ActualHeight);
            ydraw.setWorldWindow(0, 0, mWidth, mHeight);
            if (mTextSize == 0)
                mTextSize = ydraw.getTextSize();
            ydraw.clear();
        }

        /// <summary>
        /// 盤面を表示する
        /// </summary>
        public void drawBoard()
        {
            if (mHaba <= 0)
                return;

            windowSet();

            //  背景色白で盤の表示
            ydraw.backColor(Brushes.White);
            ydraw.GButtonDraws();
            //  3x3の区切り線の表示
            //drawSmallBoard();
        }

        /// <summary>
        /// 盤の初期化を行う
        /// </summary>
        private void initBoard()
        {
            windowSet();

            //  盤をグラフィックボタンで作成
            ydraw.GButtonClear();

            //  盤の完成状態を作成
            for (int y = 0; y < mBoardSize; y++) {
                for (int x = 0; x < mBoardSize; x++) {
                    ydraw.GButtonAdd(getId(x, y), BUTTONTYPE.RECT,
                        new Rect((float)x * mHaba + mOx, (float)y * mHaba + mOy, mHaba, mHaba));
                    ydraw.GButtonBorderThickness(getId(x, y), 0.8f);
                    ydraw.GButtonTitleColor(getId(x, y), Brushes.Black);
                    ydraw.GButtonTitle(getId(x, y), "");
                }
            }

            //  ブロックの境界線
            for (int y = 0; y < mBoardSize; y += 3) {
                for (int x = 0; x < mBoardSize; x += 3) {
                    int id = mBlockId + getId(x, y);
                    ydraw.GButtonAdd(id, BUTTONTYPE.RECT,
                        new Rect((float)x * mHaba + mOx, (float)y * mHaba + mOy, mHaba * 3, mHaba * 3));
                    ydraw.GButtonBorderThickness(id, 2f);   //  外枠線太さ
                    ydraw.GButtonBackColor(id, null);       //  透過設定
                    ydraw.GButtonEnabled(id, false);        //  内外判定無効化
                }
            }


            //  補助ボタンの設定
            supportBord();
            setSupportBordVisible(mSupportMode);

            //  盤の表示
            drawBoard();
        }

        /// <summary>
        /// 画面と盤のの初期化
        /// </summary>
        private void initScreen()
        {
            //  盤の大きさと位置
            float habaX = mWidth * mBoardSizeRatio / mBoardSize;
            float habaY = mHeight * mBoardSizeRatio / (mBoardSize + 1.5f);
            mHaba = habaX < habaY ? habaX : habaY;

            mOx = ((float)mWidth - (float)mBoardSize * mHaba) / 2f;
            mOy = mHaba / 1.5f;

            //backClear();
            initBoard();

            if (0 < mTempBoard.Length) {
                setBoardData(mTempBoard, false);        //  盤に数値データを設定
                supportBord();
                setSupportBordVisible(mSupportMode);
                setFixPosColor();                       //  固定数値(問題バターン)セルの背景色を設定
                drawBoard();                            //  盤の表示
            }
        }

        /// <summary>
        /// 問題を解いて盤に反映する
        /// </summary>
        private void executeSolver()
        {
            int[] board = new int[81];
            //  問題のパターンをSolverに合わせて変換する
            if (!mDataList.ContainsKey(patternCb.Text))
                return;
            String pattern = mDataList[patternCb.Text];
            for (int i = 0; i < 81; i++)
                board[i] = int.Parse(pattern[i].ToString());

            //  Solverにデータをセットして開放する
            SudokuSolver solver = new SudokuSolver(board);
            solver.preCheck();                  //  解法前に候補データを求める
            if (solver.solver(0)) {             //  解法の実施
                board = solver.getResult();
                setSolveData(board, solver.getCount());
            } else {
                bottomMessage("解答なし 操作回数:" + solver.getCount());
            }
        }

        /// <summary>
        /// 問題パターン名リストをコンボボックスに登録し、Text表示する
        /// </summary>
        /// <param name="patternTitle"></param>
        private void patternSetComboBox(string patternTitle)
        {
            patternSetComboBox();
            int index = patternCb.Items.IndexOf(patternTitle);
            if (0 <= index)
                patternCb.SelectedIndex = index;
        }


        /// <summary>
        /// 問題パターン名リストをコンボボックスに登録
        /// </summary>
        private void patternSetComboBox()
        {
            List<string> dataList = new List<string>();
            foreach (String key in mDataList.Keys)
                dataList.Add(key);
            dataList.Sort((a, b) => b.CompareTo(a));
            patternCb.Items.Clear();
            foreach (String title in dataList)
                patternCb.Items.Add(title);
        }

        /// <summary>
        /// ファイルから問題パターンを読み込む
        /// 追加フラグがtrueの時既存データに追加、falseだと既存データをクリアして追加
        /// </summary>
        /// <param name="dataList">問題パターンリスト</param>
        /// <param name="path">ファイルパス</param>
        /// <param name="add">追加フラグ(true:追加 false:新規追加)</param>
        /// <returns>成否</returns>
        private bool loadPatternData(Dictionary<String, String> dataList, String path, bool add)
        {
            if (System.IO.File.Exists(path)) {
                System.IO.StreamReader dataFile = new System.IO.StreamReader(path);
                if (!add)
                    dataList.Clear();
                String line;
                while ((line = dataFile.ReadLine()) != null) {
                    String[] buf = mYlib.seperateString(line);
                    if (1 < buf.Length) {
                        if (!dataList.ContainsKey(buf[0]))
                            dataList.Add(buf[0], buf[1]);
                    }
                }
                dataFile.Close();
                return true;
            }
            return false;
        }

        /// <summary>
        /// 問題パターンをファイルに書き込む
        /// </summary>
        /// <param name="dataList"></param>
        /// <param name="path"></param>
        private void savePatternData(Dictionary<String, String> dataList, String path)
        {
            //  ファイルに保存
            System.IO.StreamWriter dataFile = new System.IO.StreamWriter(path, false);
            //  計算式リストをStringにバッファリングする
            foreach (KeyValuePair<String, String> entry in dataList) {
                if (entry.Value != null) {                  //  タイトルのみ
                    string buffer = entry.Key + "," + entry.Value;
                    dataFile.WriteLine(buffer);
                }
            }
            dataFile.Close();
        }

        /// <summary>
        /// 盤の状態を復元する
        /// </summary>
        private void loadData()
        {
            Dictionary<string, string> dataList = new Dictionary<string, string>();
            loadPatternData(dataList, mSaveBoardPath, false);
            String patterName = patternCb.Text;
            if (dataList.ContainsKey(patterName)) {
                String boardData = dataList[patterName];
                setCurBoard(boardData);
            } else
                System.Windows.MessageBox.Show("データが存在しません", "警告");
        }

        /// <summary>
        /// 盤面のデータをパターン名で保存する
        /// </summary>
        private void saveData()
        {
            string boardData = getCurBoard();
            string patternName = patternCb.Text;
            if (patternName.Length < 1) {
                System.Windows.MessageBox.Show("パターン名が登録されていません", "警告");
                return;
            }
            Dictionary<string, string> dataList = new Dictionary<string, string>();
            loadPatternData(dataList, mSaveBoardPath, false);
            if (!dataList.ContainsKey(patternName))
                dataList.Add(patternName, boardData);
            else
                dataList[patternName] = boardData;
            savePatternData(dataList, mSaveBoardPath);
        }

        /// <summary>
        /// ファイルを選択して問題パターンを追加
        /// </summary>
        private void fileSelectLoad()
        {
            // ダイアログ(Open)のインスタンスを生成
            var dialog = new OpenFileDialog();
            // ファイルの種類を設定
            dialog.Filter = "CSVファイル (*.csv)|*.csv|全てのファイル (*.*)|*.*";
            // ダイアログを表示する
            DialogResult result = dialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK) {
                loadPatternData(mDataList, dialog.FileName, true);
                patternSetComboBox();                           //  問題パターンをコンボボックスに登録
                savePatternData(mDataList, mSaveFilePath);      //  問題パターンをファイルに保存
            }
        }

        /// <summary>
        /// 問題パターンをファイルにエキスポートする
        /// </summary>
        private void fileSaveAS()
        {
            // ダイアログ(Save)のインスタンスを生成
            var dialog = new SaveFileDialog();
            // ファイルの種類を設定
            dialog.Filter = "CSVファイル (*.csv)|*.csv|全てのファイル (*.*)|*.*";
            // ダイアログを表示する
            DialogResult result = dialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK) {
                savePatternData(mDataList, dialog.FileName);      //  問題パターンをファイルに保存
            }
        }
    }
}
