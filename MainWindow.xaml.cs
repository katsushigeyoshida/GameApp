using System.Windows;
using System.Windows.Input;

namespace GameApp
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        private double mWindowWidth;                            //  ウィンドウの高さ
        private double mWindowHeight;                           //  ウィンドウ幅
        private double mPrevWindowWidth;                        //  変更前のウィンドウ幅
        private string[] mProgramTitle = {                      //  プログラムタイトルリスト
            "パズル「白にしろ」","パズル「15ゲーム」","パズル「数独」",
            "ルービックキューブ","ライフゲーム", "ブロック崩し","テトリス",
            "マインスィーパ",
        };

        public MainWindow()
        {
            InitializeComponent();

            mWindowWidth = GameApp.Width;
            mWindowHeight = GameApp.Height;
            mPrevWindowWidth = mWindowWidth;

            WindowFormLoad();

            ProgramList.Items.Clear();
            foreach (string name in mProgramTitle)
                ProgramList.Items.Add(name);
        }

        private void ProgramList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Window programDlg = null;
            switch (ProgramList.SelectedIndex) {
                case 0: programDlg = new AllWhite(); break;
                case 1: programDlg = new Slide15Game(); break;
                case 2: programDlg = new Sudoku(); break;
                case 3: programDlg = new RubikCube(); break;
                case 4: programDlg = new LifeGame(); break;
                case 5: programDlg = new BlockGame(); break;
                case 6: programDlg = new Tetris(); break;
                case 7: programDlg = new MineSweeper(); break;
            }
            if (programDlg != null)
                programDlg.Show();
            //programDlg.ShowDialog();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
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
            if (Properties.Settings.Default.GameAppWindowWidth < 100 || Properties.Settings.Default.GameAppWindowHeight < 100 ||
                System.Windows.SystemParameters.WorkArea.Height < Properties.Settings.Default.GameAppWindowHeight) {
                Properties.Settings.Default.GameAppWindowWidth = mWindowWidth;
                Properties.Settings.Default.GameAppWindowHeight = mWindowHeight;
            } else {
                GameApp.Top = Properties.Settings.Default.GameAppWindowTop;
                GameApp.Left = Properties.Settings.Default.GameAppWindowLeft;
                GameApp.Width = Properties.Settings.Default.GameAppWindowWidth;
                GameApp.Height = Properties.Settings.Default.GameAppWindowHeight;
            }
        }

        /// <summary>
        /// Window状態を保存する
        /// </summary>
        private void WindowFormSave()
        {
            //  Windowの位置とサイズを保存(登録項目をPropeties.settingsに登録して使用する)
            Properties.Settings.Default.GameAppWindowTop = GameApp.Top;
            Properties.Settings.Default.GameAppWindowLeft = GameApp.Left;
            Properties.Settings.Default.GameAppWindowWidth = GameApp.Width;
            Properties.Settings.Default.GameAppWindowHeight = GameApp.Height;
            Properties.Settings.Default.Save();
        }
    }
}
