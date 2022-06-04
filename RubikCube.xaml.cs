using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System;
using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Threading;
using WpfLib;
using Button = System.Windows.Controls.Button;
using KeyEventArgs = System.Windows.Forms.KeyEventArgs;

namespace GameApp
{
    /// <summary>
    /// RubikCube.xaml の相互作用ロジック
    /// 
    /// ルービック・キューブのシミュレーションゲーム
    /// 
    /// WindowsFormsHost を使用するには参照で WindowsFormsIntegration を追加しておく
    /// OpenGLを使うために OpenTKとOpenTK.GLControl をNuGetでインストールしておく
    /// 
    /// </summary>
    public partial class RubikCube : Window
    {
        private double mWindowWidth;                    //  ウィンドウの高さ
        private double mWindowHeight;                   //  ウィンドウ幅

        private GLControl glControl;                    //  OpenTK.GLcontrol

        private bool isCameraRotating;                  //  カメラが回転状態かどうか
        private Vector2 current, previous;              //  現在の点、前の点
        private Matrix4 rotate;                         //  回転行列
        private float zoom;                             //  拡大度
        private Color4 mBackColor = Color4.LightGray;   //  背景色
        private int mTiltAngle = 15;                    //  1回の動作で動く角度
        private int mCubes = 3;                         //  1辺の立方体の数
        private float mCubeSize = 1.0f;                 //  個々のキューブの辺の長さ
        private bool mIsCube = false;

        private DispatcherTimer dispatcherTimer;        // タイマーオブジェクト
        private Random mRandom;
        private int mOperationCountMax = 10;
        private int mOperationCount;
        private int mFace = 0;

        private CubeUnit[,,] mCube;                     //  個々の立方体のプロパティ

        public RubikCube()
        {
            InitializeComponent();

            mWindowWidth = WindowForm.Width;
            mWindowHeight = WindowForm.Height;
            WindowFormLoad();

            isCameraRotating = false;
            current = Vector2.Zero;
            previous = Vector2.Zero;
            rotate = Matrix4.Identity;
            zoom = 1.0f;
            levelCb.Items.Add("3");
            levelCb.Items.Add("5");
            levelCb.Items.Add("7");
            levelCb.Items.Add("10");
            levelCb.Items.Add("20");
            levelCb.Items.Add("30");
            levelCb.SelectedIndex = 1;
            mOperationCountMax = int.Parse(levelCb.Text);
            cubeSize.Items.Add("2x2x2");
            cubeSize.Items.Add("3x3x3");
            //cubeSize.Items.Add("4x4x4");
            cubeSize.SelectedIndex = 1;

            initCube();

            //  OpenGLのイベント追加
            glControl = new GLControl();
            glControl.Load += glControl_Load;
            glControl.Paint += glControl_Paint;
            glControl.Resize += glControl_Resize;
            glControl.MouseDown += glControl_MouseDown;
            glControl.MouseUp += glControl_MouseUp;
            glControl.MouseMove += glControl_MosueMove;
            glControl.MouseWheel += glControl_MouseWheel;
            glControl.KeyDown += glControl_KeyDown;
            glRCube.Child = glControl;      //  OpenGLをWindowsに接続

            //  タイマーインスタンスの作成
            dispatcherTimer = new DispatcherTimer(DispatcherPriority.Normal);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 100); //  日,時,分,秒,m秒
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            mRandom = new Random();
        }

        /// <summary>
        /// WindowFormのLoad 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

        }

        /// <summary>
        /// Windowのクローズ処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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
            if (Properties.Settings.Default.RubikCubeWindowWidth < 100 || Properties.Settings.Default.RubikCubeWindowHeight < 100 ||
                System.Windows.SystemParameters.WorkArea.Height < Properties.Settings.Default.RubikCubeWindowHeight) {
                Properties.Settings.Default.RubikCubeWindowWidth = mWindowWidth;
                Properties.Settings.Default.RubikCubeWindowHeight = mWindowHeight;
            } else {
                WindowForm.Top = Properties.Settings.Default.RubikCubeWindowTop;
                WindowForm.Left = Properties.Settings.Default.RubikCubeWindowLeft;
                WindowForm.Width = Properties.Settings.Default.RubikCubeWindowWidth;
                WindowForm.Height = Properties.Settings.Default.RubikCubeWindowHeight;
            }
        }

        /// <summary>
        /// Window状態を保存する
        /// </summary>
        private void WindowFormSave()
        {
            //  Windowの位置とサイズを保存(登録項目をPropeties.settingsに登録して使用する)
            Properties.Settings.Default.RubikCubeWindowTop = WindowForm.Top;
            Properties.Settings.Default.RubikCubeWindowLeft = WindowForm.Left;
            Properties.Settings.Default.RubikCubeWindowWidth = WindowForm.Width;
            Properties.Settings.Default.RubikCubeWindowHeight = WindowForm.Height;
            Properties.Settings.Default.Save();
        }

        /// <summary>
        /// OpenGL 起動時の設定
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void glControl_Load(object sender, EventArgs e)
        {
            GL.Enable(EnableCap.DepthTest);
            //GL.Enable(EnableCap.Lighting);    //  光源の使用

            GL.PointSize(3.0f);     //  点の大きさ
            GL.LineWidth(1.5f);     //  線の太さ

            mIsCube = true;
            //throw new NotImplementedException();
        }

        /// <summary>
        /// OpenGLの描画 都度呼ばれる
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void glControl_Paint(object sender, PaintEventArgs e)
        {
            renderFrame();

            //throw new NotImplementedException();
        }

        /// <summary>
        /// Windowのサイズが変わった時、glControl_Paintも呼ばれる
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void glControl_Resize(object sender, EventArgs e)
        {
            GL.Viewport(glControl.ClientRectangle);

            //throw new NotImplementedException();
        }

        /// <summary>
        /// マウスホイールによるzoom up/down
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void glControl_MouseWheel(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            float delta = (float)e.Delta / 1000f;// - wheelPrevious;
            zoom *= (float)Math.Pow(1.2, delta);
            if (2.0f < zoom)
                zoom = 2.0f;
            if (zoom < 0.5f)
                zoom = 0.5f;

            renderFrame();
            //throw new NotImplementedException();
        }

        /// <summary>
        /// 視点(カメラ)の回転
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void glControl_MosueMove(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (isCameraRotating) {
                previous = current;
                current = new Vector2(e.X, e.Y);
                Vector2 delta = current - previous;
                delta /= (float)Math.Sqrt(glControl.Width * glControl.Width + glControl.Height * glControl.Height);
                float length = delta.Length;
                if (0.0 < length) {
                    float rad = length * MathHelper.Pi;
                    float theta = (float)Math.Sin(rad) / length;
                    Quaternion after = new Quaternion(delta.Y * theta, delta.X * theta, 0.0f, (float)Math.Cos(rad));
                    rotate = rotate * Matrix4.CreateFromQuaternion(after);
                }
                renderFrame();
            }
            //throw new NotImplementedException();
        }

        /// <summary>
        /// マウスダウン 視点(カメラ)の回転開始
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void glControl_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) {
                isCameraRotating = true;
                current = new Vector2(e.X, e.Y);
            }
            //throw new NotImplementedException();
        }

        /// <summary>
        /// マウスアップ 視点(カメラ)の回転終了
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void glControl_MouseUp(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) {
                isCameraRotating = false;
                previous = Vector2.Zero;
            }
            //throw new NotImplementedException();
        }

        /// <summary>
        /// キーボード入力
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void glControl_KeyDown(object sender, KeyEventArgs e)
        {
            int ang = mTiltAngle;
            if (e.KeyData == Keys.Q)            //  角度の反転
                mTiltAngle = -mTiltAngle;
            if (e.KeyData == Keys.L) {          //  左側(left)の回転
                translateCube(0, ang);
            } else if (e.KeyData == Keys.R) {   //  右側(right)の回転
                translateCube(1, ang);
            } else if (e.KeyData == Keys.D) {   //  下側(down)の回転
                translateCube(2, ang);
            } else if (e.KeyData == Keys.U) {   //  上側(up)の回転
                translateCube(3, ang);
            } else if (e.KeyData == Keys.B) {   //  後側(back)の回転
                translateCube(4, ang);
            } else if (e.KeyData == Keys.F) {   //  前側(front)の回転
                translateCube(5, ang);
            } else if (e.KeyData == Keys.Z) {   //  初期状態に戻す
                initCube();
            }
            renderFrame();
            //ythrow new NotImplementedException();
        }

        /// <summary>
        /// キューブの大きさ変更
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cubeSize_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cubeSize.SelectedIndex == 0) {
                mCubes = 2;
                mCubeSize = 2.0f;
                zoom = 0.85f;
            } else if (cubeSize.SelectedIndex == 2) {
                mCubes = 4;
                mCubeSize = 0.8f;
                zoom = 1.0f;
            } else {
                mCubes = 3;
                mCubeSize = 1.0f;
                zoom = 1.0f;
            }
            initCube();
            if (mIsCube)
                renderFrame();
        }

        /// <summary>
        /// ランダム化の回数設定
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void levelCb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (0 < levelCb.Text.Length)
                mOperationCountMax = int.Parse(levelCb.Items[levelCb.SelectedIndex].ToString());
        }

        /// <summary>
        /// [前(F)]ボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void front_Click(object sender, RoutedEventArgs e)
        {
            Button bt = (Button)e.Source;
            int ang = Math.Abs(mTiltAngle);
            if (bt.Name.CompareTo("front2") == 0)
                ang = -ang;
            translateCube(5, ang);
            renderFrame();
        }

        /// <summary>
        ///  [後(B)]ボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void back_Click(object sender, RoutedEventArgs e)
        {
            Button bt = (Button)e.Source;
            int ang = Math.Abs(mTiltAngle);
            if (bt.Name.CompareTo("back2") == 0)
                ang = -ang;
            translateCube(4, ang);
            renderFrame();
        }

        /// <summary>
        /// [上(U)]ボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void up_Click(object sender, RoutedEventArgs e)
        {
            Button bt = (Button)e.Source;
            int ang = Math.Abs(mTiltAngle);
            if (bt.Name.CompareTo("up2") == 0)
                ang = -ang;
            translateCube(3, ang);
            renderFrame();
        }

        /// <summary>
        /// [下(D)]ボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void down_Click(object sender, RoutedEventArgs e)
        {
            Button bt = (Button)e.Source;
            int ang = Math.Abs(mTiltAngle);
            if (bt.Name.CompareTo("down2") == 0)
                ang = -ang;
            translateCube(2, ang);
            renderFrame();
        }

        /// <summary>
        /// [左(L)]ボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void left_Click(object sender, RoutedEventArgs e)
        {
            Button bt = (Button)e.Source;
            int ang = Math.Abs(mTiltAngle);
            if (bt.Name.CompareTo("left2") == 0)
                ang = -ang;
            translateCube(0, ang);
            renderFrame();
        }

        /// <summary>
        /// [右(R]ボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void right_Click(object sender, RoutedEventArgs e)
        {
            Button bt = (Button)e.Source;
            int ang = Math.Abs(mTiltAngle);
            if (bt.Name.CompareTo("right2") == 0)
                ang = -ang;
            translateCube(1, ang);
            renderFrame();
        }

        /// <summary>
        /// [リセット(Z)]ボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void reset_Click(object sender, RoutedEventArgs e)
        {
            initCube();
            renderFrame();
        }

        /// <summary>
        /// [ランダム化]ボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void probrem_Click(object sender, RoutedEventArgs e)
        {
            createProblem();
        }

        /// <summary>
        /// [?]ボタン ヘルプの表示
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void help_Click(object sender, RoutedEventArgs e)
        {
            HelpView help = new HelpView();
            help.mHelpText = HelpText.mRubicCubeHelp;
            help.Show();
        }


        /// <summary>
        /// タイマーTick処理 問題作成中のパターンを表示する
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            createProblemCube();
            //throw new NotImplementedException();
        }

        /// <summary>
        /// 問題パターンの作成
        /// 乱数を使って問題を作成する
        /// </summary>
        public void createProblem()
        {
            initCube();
            mOperationCount = 90 / mTiltAngle * mOperationCountMax;
            dispatcherTimer.Start();    //  タイマースタート
        }

        /// <summary>
        /// タイマー処理によって問題作成パターンをアニメーション表示する
        /// </summary>
        private void createProblemCube()
        {
            if (mOperationCount % (90 / mTiltAngle) == 0)
                mFace = (int)Math.Floor(mRandom.NextDouble() * 6);
            translateCube(mFace, mTiltAngle);
            renderFrame();
            mOperationCount--;

            if (mOperationCount <= 0) {
                dispatcherTimer.Stop();     //  タイマー処理終了
            }
        }


        /// <summary>
        /// Cubeの回転
        /// </summary>
        /// <param name="face">面の種類</param>
        /// <param name="ang">回転角(deg)</param>
        private void translateCube(int face, int ang)
        {
            float pos = (mCubes - 1f) / 2f * mCubeSize;
            for (int x = 0; x < mCubes; x++) {
                for (int y = 0; y < mCubes; y++) {
                    for (int z = 0; z < mCubes; z++) {
                        if (face == 0) {        //  L
                            if (mCube[x, y, z].mTranPosInt.X == -pos) {
                                mCube[x, y, z].setAddAngle(ang, 0, 0);
                            }
                        } else if (face == 1) { //  R
                            if (mCube[x, y, z].mTranPosInt.X == pos) {
                                mCube[x, y, z].setAddAngle(ang, 0, 0);
                            }
                        } else if (face == 2) { //  D
                            if (mCube[x, y, z].mTranPosInt.Y == -pos) {
                                mCube[x, y, z].setAddAngle(0, ang, 0);
                            }
                        } else if (face == 3) { //  U
                            if (mCube[x, y, z].mTranPosInt.Y == pos) {
                                mCube[x, y, z].setAddAngle(0, ang, 0);
                            }
                        } else if (face == 4) { //  B
                            if (mCube[x, y, z].mTranPosInt.Z == -pos) {
                                mCube[x, y, z].setAddAngle(0, 0, ang);
                            }
                        } else if (face == 5) { //  F
                            if (mCube[x, y, z].mTranPosInt.Z == pos) {
                                mCube[x, y, z].setAddAngle(0, 0, ang);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 三次元データ表示
        /// </summary>
        private void renderFrame()
        {
            GL.ClearColor(mBackColor);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            //  視界の設定
            Matrix4 modelView = Matrix4.LookAt(Vector3.UnitZ * 10 / zoom, Vector3.Zero, Vector3.UnitY);
            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadMatrix(ref modelView);
            GL.MultMatrix(ref rotate);
            //  視体積の設定
            Matrix4 projection = Matrix4.CreatePerspectiveFieldOfView(MathHelper.PiOver4 / zoom, (float)this.Width / (float)this.Height, 1.0f, 64.0f);
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadMatrix(ref projection);

            GL.MatrixMode(MatrixMode.Modelview);

            //  Cubeの表示
            for (int x = 0; x < mCubes; x++) {
                for (int y = 0; y < mCubes; y++) {
                    for (int z = 0; z < mCubes; z++) {
                        DrawCube(mCube[x, y, z]);
                    }
                }
            }

            //  軸の表示
            DrawAxis(3f);

            glControl.SwapBuffers();
        }

        /// <summary>
        ///  立方体の移動と回転して表示
        ///  操作した記録をトレースして回転位置を決める
        /// </summary>
        /// <param name="cube">立方体データ</param>
        private void DrawCube(CubeUnit cube)
        {
            GL.PushMatrix();    //  現在の行列をスタックに
            if (0 < cube.mAngList.Count) {
                for (int i = cube.mAngList.Count - 1; 0 <= i; i--) {
                    if (0 != cube.mAngList[i].X)
                        GL.Rotate(cube.mAngList[i].X, Vector3.UnitX);   //  X軸で回転
                    if (0 != cube.mAngList[i].Y)
                        GL.Rotate(cube.mAngList[i].Y, Vector3.UnitY);   //  Y軸で回転
                    if (0 != cube.mAngList[i].Z)
                        GL.Rotate(cube.mAngList[i].Z, Vector3.UnitZ);   //  Z軸で回転
                }
            }
            GL.Translate(cube.mPos.X * Vector3.UnitX);  //  X軸方向に移動
            GL.Translate(cube.mPos.Y * Vector3.UnitY);  //  X軸方向に移動
            GL.Translate(cube.mPos.Z * Vector3.UnitZ);  //  Z軸方向に移動
            DrawCube();     //  立方体を描画

            GL.PopMatrix();
        }

        //private void DrawCube(CubeUnit cube)
        //{
        //    DrawCube(cube.mPos, cube.mAngInt.get());
        //}

        //  立方体の移動と回転して表示
        //  先に回転してから移動する(回転は移動先を原点として回るため)
        private void DrawCube(Vector3 pos, Vector3 angle)
        {
            GL.PushMatrix();                        //  現在の行列をスタックに

            GL.Rotate(angle.X, Vector3.UnitX);      //  X軸で回転
            GL.Rotate(angle.Y, Vector3.UnitY);      //  Y軸で回転
            GL.Rotate(angle.Z, Vector3.UnitZ);      //  Z軸で回転
            GL.Translate(pos.X * Vector3.UnitX);    //  X軸方向に移動
            GL.Translate(pos.Y * Vector3.UnitY);    //  X軸方向に移動
            GL.Translate(pos.Z * Vector3.UnitZ);    //  Z軸方向に移動

            DrawCube();                             //  立方体を描画

            GL.PopMatrix();
        }

        /// <summary>
        /// 立方体の初期化
        /// </summary>
        private void initCube()
        {
            mCube = new CubeUnit[mCubes, mCubes, mCubes];
            for (int x = 0; x < mCubes; x++) {
                for (int y = 0; y < mCubes; y++) {
                    for (int z = 0; z < mCubes; z++) {
                        Vector3 pos = new Vector3(
                            (float)(x - (mCubes - 1f) / 2f) * mCubeSize,
                            (float)(y - (mCubes - 1f) / 2f) * mCubeSize,
                            (float)(z - (mCubes - 1f) / 2f) * mCubeSize);
                        mCube[x, y, z] = new CubeUnit(pos, x * 100 + y * 10 + z);
                    }
                }
            }
        }

        /// <summary>
        /// 回転軸の表示(X軸:赤 Y軸:青 Z軸:黄色)
        /// </summary>
        /// <param name="size">回転軸の大きさ</param>
        private void DrawAxis(float size)
        {
            float[] vertexs = {
                //  x, y, z
                 0f,   0f,   0f,  //x axis start     P0
                 1f,   0f,   0f,                   //P1
                 0.9f, 0.05f,0f,                   //P2
                 0.9f, 0f,   0f,                   //P3
                 0f,   0f,   0f,  //x axis end     //P4
                 0f,   0f,   0f,  //y axis start   //P5
                 0f,   1f,   0f,                   //P6
                -0.05f,0.9f, 0f,                   //P7
                 0f,   0.9f, 0f,                   //P8
                 0f,   0f,   0f,  //y axis end      P9
                 0f,   0f,   0f,  //z axis start    P10
                 0f,   0f,   1f,                  //P11
                -0.05f,0f,   0.9f,                //P12
                 0f,   0f,   0.9f,//z axis end      P13
                 // 文字データ
                 1.05f,0f,   0f,  //char X          P14
                 1.15f,0.12f,0f,                  //P15
                 1.1f, 0.06f,0f,                  //P16
                 1.05f,0.12f,0f,                  //P17
                 1.15f,0f,   0f,                  //P18

                 0.05f,1.05f,0f, //char Y         //P19
                 0.05f,1.12f,0f,                  //P20
                 0f,   1.17f,0f,                  //P21
                 0.05f,1.12f,0f,                  //P22
                 0.1f, 1.17f,0f,                  //P23

                 0.05f,0.12f,1.05f,   //char Z      P24
                 0.1f, 0.12f,1.05f,               //P25
                 0.05f,0f,   1.05f,               //P26
                 0.1f, 0f,   1.05f                //P27
            };
            float width = 2f;
            GL.PushMatrix();                    //現在の行列をスタックに
            GL.Scale(size, size, size);
            polyLine(vertexs, 0, 5, Color.Blue, width);
            polyLine(vertexs, 5, 5, Color.Red, width);
            polyLine(vertexs, 10, 4, Color.Yellow, width);
            polyLine(vertexs, 14, 5, Color.Black, width);
            polyLine(vertexs, 19, 5, Color.Black, width);
            polyLine(vertexs, 24, 4, Color.Black, width);
            GL.PopMatrix();
        }

        /// <summary>
        /// 個々の立方体を描く
        /// </summary>
        private void DrawCube()
        {
            float rate = mCubeSize * 0.46f;
            float edgelen = mCubeSize * 0.5f;

            GL.Begin(PrimitiveType.Quads);

            GL.Color4(Color4.Blue);		//	Right
            GL.Vertex3(edgelen, rate, rate);
            GL.Vertex3(edgelen, -rate, rate);
            GL.Vertex3(edgelen, -rate, -rate);
            GL.Vertex3(edgelen, rate, -rate);

            GL.Color4(Color4.Green);	//	Left
            GL.Vertex3(-edgelen, rate, rate);
            GL.Vertex3(-edgelen, rate, -rate);
            GL.Vertex3(-edgelen, -rate, -rate);
            GL.Vertex3(-edgelen, -rate, rate);

            GL.Color4(Color4.Red);		//	top
            GL.Vertex3(rate, edgelen, rate);
            GL.Vertex3(rate, edgelen, -rate);
            GL.Vertex3(-rate, edgelen, -rate);
            GL.Vertex3(-rate, edgelen, rate);

            GL.Color4(Color4.Orange);	//	down
            GL.Vertex3(rate, -edgelen, rate);
            GL.Vertex3(-rate, -edgelen, rate);
            GL.Vertex3(-rate, -edgelen, -rate);
            GL.Vertex3(rate, -edgelen, -rate);

            GL.Color4(Color4.Yellow);	//	front
            GL.Vertex3(rate, rate, edgelen);
            GL.Vertex3(-rate, rate, edgelen);
            GL.Vertex3(-rate, -rate, edgelen);
            GL.Vertex3(rate, -rate, edgelen);

            GL.Color4(Color4.White);	//	back
            GL.Vertex3(rate, rate, -edgelen);
            GL.Vertex3(rate, -rate, -edgelen);
            GL.Vertex3(-rate, -rate, -edgelen);
            GL.Vertex3(-rate, rate, -edgelen);

            //  内側に黒の面を表示し、辺が黒くなるようにする
            rate = mCubeSize * 0.499f;
            edgelen = mCubeSize * 0.499f;

            GL.Color4(Color4.Black);
            GL.Vertex3(edgelen, rate, rate);
            GL.Vertex3(edgelen, -rate, rate);
            GL.Vertex3(edgelen, -rate, -rate);
            GL.Vertex3(edgelen, rate, -rate);

            GL.Vertex3(-edgelen, rate, rate);
            GL.Vertex3(-edgelen, rate, -rate);
            GL.Vertex3(-edgelen, -rate, -rate);
            GL.Vertex3(-edgelen, -rate, rate);

            GL.Vertex3(rate, edgelen, rate);
            GL.Vertex3(rate, edgelen, -rate);
            GL.Vertex3(-rate, edgelen, -rate);
            GL.Vertex3(-rate, edgelen, rate);

            GL.Vertex3(rate, -edgelen, rate);
            GL.Vertex3(-rate, -edgelen, rate);
            GL.Vertex3(-rate, -edgelen, -rate);
            GL.Vertex3(rate, -edgelen, -rate);

            GL.Vertex3(rate, rate, edgelen);
            GL.Vertex3(-rate, rate, edgelen);
            GL.Vertex3(-rate, -rate, edgelen);
            GL.Vertex3(rate, -rate, edgelen);

            GL.Vertex3(rate, rate, -edgelen);
            GL.Vertex3(rate, -rate, -edgelen);
            GL.Vertex3(-rate, -rate, -edgelen);
            GL.Vertex3(-rate, rate, -edgelen);

            GL.End();
        }

        /// <summary>
        /// 連続線分を描画
        /// </summary>
        /// <param name="array">頂点データ</param>
        /// <param name="start">開始位置(3データづつ)</param>
        /// <param name="size">データサイズ(3データづつ)</param>
        /// <param name="color">色</param>
        /// <param name="width">線の太さ</param>
        private void polyLine(float[] array, int start, int size, Color color, float width)
        {
            GL.Begin(PrimitiveType.LineStrip);
            GL.Color3(color);
            GL.LineWidth(width);
            for (int i = 0; i < size; i++) {
                GL.Vertex3(array[(start + i) * 3], array[(start + i) * 3 + 1], array[(start + i) * 3 + 2]);
            }
            GL.End();
        }

        /// <summary>
        /// 線分の描画
        /// </summary>
        /// <param name="ps">始点</param>
        /// <param name="pe">終点</param>
        /// <param name="color">色</param>
        private void line(Vector3 ps, Vector3 pe, Color color)
        {
            line(ps.X, ps.Y, ps.Z, pe.X, pe.Y, pe.Z, color);
        }

        /// <summary>
        /// 線分の描画
        /// </summary>
        /// <param name="x1">始点X</param>
        /// <param name="y1">始点Y</param>
        /// <param name="z1">始点Z</param>
        /// <param name="x2">終点X</param>
        /// <param name="y2">終点Y</param>
        /// <param name="z2">終点Z</param>
        /// <param name="color">色</param>
        private void line(double x1, double y1, double z1, double x2, double y2, double z2, Color color)
        {
            GL.Begin(PrimitiveType.Lines);
            GL.Color3(color);
            GL.Vertex3(x1, y1, z1);
            GL.Vertex3(x2, y2, z2);
            GL.End();
        }

        /// <summary>
        /// 破線の描画
        /// </summary>
        /// <param name="x1">始点X</param>
        /// <param name="y1">始点Y</param>
        /// <param name="z1">始点Z</param>
        /// <param name="x2">終点X</param>
        /// <param name="y2">終点Y</param>
        /// <param name="z2">終点Z</param>
        private void dot_line(double x1, double y1, double z1, double x2, double y2, double z2)
        {
            GL.Enable(EnableCap.LineStipple);   //  破線パターン
            GL.LineStipple(1, 0xe0e0);          //  LinePattern=0b1110000011100000=0xe0e0   16ビットパターン設定
            GL.Begin(PrimitiveType.LineStrip);
            GL.Vertex3(x1, y1, z1);
            GL.Vertex3(x2, y2, z2);
            GL.End();
            GL.Disable(EnableCap.LineStipple);
        }

    }
}
