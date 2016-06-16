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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Kinect;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Windows.Threading;
using System.Collections;



namespace kinect._0706.ex1
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        // Kinect SDK
        KinectSensor kinect;

        BodyFrameReader bodyFrameReader;
        Body[] bodies;

        DepthFrameReader depthFrameReader;
        FrameDescription depthFrameDesc;

        ColorFrameReader colorFrameReader;
        FrameDescription colorFrameDesc;

        ColorImageFormat colorFormat = ColorImageFormat.Bgra;

        // WPF
        WriteableBitmap colorBitmap;
        byte[] colorBuffer;
        int colorStride;
        Int32Rect colorRect;

        // 表示
        WriteableBitmap depthImage;
        ushort[] depthBuffer;
        byte[] depthBitmapBuffer;
        Int32Rect depthRect;
        int depthStride;

        Point depthPoint;
        const int R = 20;

        DispatcherTimer timer, timer2, timer3;


        public MainWindow()
        {
            InitializeComponent();

        }


        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Kinectを開く
                kinect = KinectSensor.GetDefault();
                kinect.Open();

                // 表示のためのデータを作成
                depthFrameDesc = kinect.DepthFrameSource.FrameDescription;

                // 表示のためのビットマップに必要なものを作成
                depthImage = new WriteableBitmap(
                    depthFrameDesc.Width, depthFrameDesc.Height,
                    96, 96, PixelFormats.Gray8, null);
                depthBuffer = new ushort[depthFrameDesc.LengthInPixels];
                depthBitmapBuffer = new byte[depthFrameDesc.LengthInPixels];
                depthRect = new Int32Rect(0, 0,
                                        depthFrameDesc.Width, depthFrameDesc.Height);
                depthStride = (int)(depthFrameDesc.Width);

                ImageDepth.Source = depthImage;

                // 初期の位置表示座標(中心点)
                depthPoint = new Point(depthFrameDesc.Width / 2,
                                        depthFrameDesc.Height / 2);

                // Depthリーダーを開く
                depthFrameReader = kinect.DepthFrameSource.OpenReader();
                depthFrameReader.FrameArrived += depthFrameReader_FrameArrived;

                // Bodyを入れる配列を作る
                bodies = new Body[kinect.BodyFrameSource.BodyCount];

                // ボディーリーダーを開く
                bodyFrameReader = kinect.BodyFrameSource.OpenReader();
                bodyFrameReader.FrameArrived += bodyFrameReader_FrameArrived;

                // カラー画像の情報を作成する(BGRAフォーマット)
                colorFrameDesc = kinect.ColorFrameSource.CreateFrameDescription(
                                                        colorFormat);

                // カラーリーダーを開く
                colorFrameReader = kinect.ColorFrameSource.OpenReader();
                colorFrameReader.FrameArrived += colorFrameReader_FrameArrived;

                // カラー用のビットマップを作成する
                colorBitmap = new WriteableBitmap(
                                    colorFrameDesc.Width, colorFrameDesc.Height,
                                    96, 96, PixelFormats.Bgra32, null);
                colorStride = colorFrameDesc.Width * (int)colorFrameDesc.BytesPerPixel;
                colorRect = new Int32Rect(0, 0,
                                    colorFrameDesc.Width, colorFrameDesc.Height);
                colorBuffer = new byte[colorStride * colorFrameDesc.Height];
                ImageColor.Source = colorBitmap;

                //タイマー設定
                timer = new DispatcherTimer();
                timer.Tick += timer_Tick;
                timer.Interval = new TimeSpan(0, 0, 1);

                timer2 = new DispatcherTimer();
                timer2.Tick += timer2_Tick;
                timer2.Interval = new TimeSpan(0, 0, 0, 0, 500);

                timer3 = new DispatcherTimer();
                timer3.Tick += timer3_Tick;
                timer3.Interval = new TimeSpan(0, 0, 1);

            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.Message);
                Close();
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (depthFrameReader != null)
            {
                depthFrameReader.Dispose();
                depthFrameReader = null;
            }

            if (bodyFrameReader != null)
            {
                bodyFrameReader.Dispose();
                bodyFrameReader = null;
            }

            if (colorFrameReader != null)
            {
                colorFrameReader.Dispose();
                colorFrameReader = null;
            }

            if (kinect != null)
            {
                kinect.Close();
                kinect = null;
            }
        }

        void colorFrameReader_FrameArrived(object sender, ColorFrameArrivedEventArgs e)
        {
            UpdateColorFrame(e);
            DrawColorFrame();
        }

        private void UpdateColorFrame(ColorFrameArrivedEventArgs e)
        {
            // カラーフレームを取得する
            using (var colorFrame = e.FrameReference.AcquireFrame())
            {
                if (colorFrame == null)
                {
                    return;
                }

                // BGRAデータを取得する
                colorFrame.CopyConvertedFrameDataToArray(
                                            colorBuffer, colorFormat);
            }
        }

        private void DrawColorFrame()
        {
            // ビットマップにする
            colorBitmap.WritePixels(colorRect, colorBuffer,
                                            colorStride, 0);
        }

        void depthFrameReader_FrameArrived(object sender, DepthFrameArrivedEventArgs e)
        {
            UpdateDepthFrame(e);
            DrawDepthFrame();
        }

        // Depthフレームの更新
        private void UpdateDepthFrame(DepthFrameArrivedEventArgs e)
        {
            using (var depthFrame = e.FrameReference.AcquireFrame())
            {
                if (depthFrame == null)
                {
                    return;
                }

                // Depthデータを取得する
                depthFrame.CopyFrameDataToArray(depthBuffer);
            }
        }

        // Depthフレームの表示
        private void DrawDepthFrame()
        {
            // 距離情報の表示を更新する
            //UpdateDepthValue();

            // 0-8000のデータを255ごとに折り返すようにする(見やすく)
            for (int i = 0; i < depthBuffer.Length; i++)
            {
                depthBitmapBuffer[i] = (byte)(depthBuffer[i] % 255);
            }

            depthImage.WritePixels(depthRect, depthBitmapBuffer, depthStride, 0);
        }       

        void bodyFrameReader_FrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            UpdateBodyFrame(e);
            DrawBodyFrame();
        }

        // ボディの更新
        private void UpdateBodyFrame(BodyFrameArrivedEventArgs e)
        {
            using (var bodyFrame = e.FrameReference.AcquireFrame())
            {
                if (bodyFrame == null)
                {
                    return;
                }

                // ボディデータを取得する
                bodyFrame.GetAndRefreshBodyData(bodies);
            }
        }

        //右手の座標
        float handx, handy;
        //頭と左手のy座標
        float heady, Lhandy=1080;
        //ベクトル（カーソルの移動方向）
        float bex, bey;
        //フラッグ変数群
        int startpoint = 0;  //ベクトルの起点となる位置を定める
        int cursorcheck = 0;  //手形状でカーソルを操作
        ArrayList shoot = new ArrayList();  //
        ArrayList flag = new ArrayList();  //ターゲットの出現
        int a = 0;  //配列のはじめの要素をつくる
        int onhead = 0;  //ゲームスタート
        int timeover = 0;  //制限時間終了
        int startset = 0; //初期設定
        //配列番号
        int s = 0;
        //カーソルの現在位置
        int nowx,nowy;
        //カーソルの直前位置
        int ax, ay;
        //カーソルの制御スピード
        int sp;
        //ターゲットの位置
        int Tx,Ty;
        //ターゲットの大きさ
        int tsize;
        //ポインターの大きさ
        int psize;
        //制限時間
        int time;
        //得点
        int getpoint = 0;
        //中心位置
        int Ctx, Cty;
        //プレイ画面サイズ
        int height = 730, width = 1020;




        // ボディの表示
        private void DrawBodyFrame()
        {
            if (startset == 0)
            {
                //カーソルの移動スピード調整
                sp = Convert.ToInt32(speed.Text);

                psize = Convert.ToInt32(ptsize.Text);

                tsize = Convert.ToInt32(tgsize.Text);

                time = Convert.ToInt32(Time.Text);

                startset = 1;

            }

            CanvasBody.Children.Clear();

            foreach (var body in bodies.Where(b => b.IsTracked))
            {
                foreach (var joint in body.Joints)
                {

                    // 手の位置が追跡状態
                    if (joint.Value.TrackingState == TrackingState.Tracked)
                    {
                        //頭
                        if (joint.Value.JointType == JointType.Head)
                        {
                            var headpoint = kinect.CoordinateMapper.MapCameraPointToColorSpace(joint.Value.Position);

                            heady = headpoint.Y;
                            //yz.Content = "heady=" + heady;

                            //DrawEllipse(joint.Value, 10, Brushes.Green);
                        }

                        //右手
                        if (joint.Value.JointType == JointType.HandRight)
                        {

                            if (startpoint == 0 || startpoint == 1)
                            {
                                //2次元座標に変換、表示
                                var handpoint = kinect.CoordinateMapper.MapCameraPointToColorSpace(joint.Value.Position);

                                handx = handpoint.X;
                                handy = handpoint.Y;

                                if (startpoint == 0)
                                {
                                    Ctx = width / 2;
                                    Cty = height / 2;

                                    nowx = width / 2;
                                    nowy = height / 2;
                                }

                                startpoint = 2;
                            }

                            //制限時間内であれば
                            if (timeover == 0)
                            {
                                //ターゲットを配置
                                RandomTarget();
                            }


                            //2次元座標に変換
                            var hpoint = kinect.CoordinateMapper.MapCameraPointToColorSpace(joint.Value.Position);

                            //起点となる位置
                            Drawstart(50, Brushes.Yellow, Ctx, Cty);

                            //手の位置
                            DrawControl(joint.Value, 20, Brushes.Green);

                            //手形状
                            DrawHandState(body.Joints[JointType.HandRight],
                               body.HandRightConfidence, body.HandRightState);

                            //ベクトルを求める
                            bex = handx - hpoint.X;
                            bey = handy - hpoint.Y;

                            //ベクトルを線で表す
                            DrawLine(Ctx, Cty, Ctx - (int)bex, Cty - (int)bey);

                            //手形状によるカーソル制御
                            CursorCheck();

                            //現在位置の更新
                            nowx = ax - (int)bex / sp;
                            nowy = ay - (int)bey / sp;

                            //ポインタの移動範囲の制御
                            Area(nowx, nowy,psize);

                            //ポインタを描く
                            DrawCursor(psize, nowx, nowy);

                            //現在位置を次の起点位置へ書き換え
                            ax = nowx;
                            ay = nowy;

                            //クリック判定
                            HandClick(body.Joints[JointType.HandRight],
                                body.HandRightConfidence, body.HandRightState, Tx, Ty, tsize);

                            //得点表示
                            score.Content = getpoint + "点";


                        }
                      
                        // 左手
                        if (joint.Value.JointType == JointType.HandLeft)
                        {
                            var Lhandpoint = kinect.CoordinateMapper.MapCameraPointToColorSpace(joint.Value.Position);

                            Lhandy = Lhandpoint.Y;
                            //xz.Content = "Lhandy=" + Lhandy;

                            //DrawEllipse(joint.Value, 10, Brushes.Green);                          

                        }
                        
                    }

                    // 手の位置が推測状態
                    else if (joint.Value.TrackingState == TrackingState.Inferred)
                    {
                        //DrawEllipse(joint.Value, 10, Brushes.Yellow);
                    }

                }

            }

            //プレイエリアの枠
            AreaLine(width, height);

            //タイマーのon/off
            if (time >= 0)
            {
                TimerControl();
            }

        }

        //手の形
        private void DrawHandState(Joint joint,
            TrackingConfidence trackingConfidence, HandState handState)
        {
            // 手の追跡信頼性が高い
            if (trackingConfidence != TrackingConfidence.High)
            {
                return;
            }

            // 手が開いている(パー)
            if (handState == HandState.Open)
            {
                DrawControl(joint, 40, new SolidColorBrush(new Color()
                {
                    R = 255,
                    G = 255,
                    A = 128
                }));

            }
            // チョキのような感じ
            else if (handState == HandState.Lasso)
            {
                DrawControl(joint, 40, new SolidColorBrush(new Color()
                {
                    R = 255,
                    B = 255,
                    A = 128
                }));

                //初期位置設定
                if (startpoint == 1)
                {
                    startpoint = 0;
                }

                //初期位置設定
                if (cursorcheck == 1)
                {
                    cursorcheck = 2;
                }

            }
            // 手が閉じている(グー)
            else if (handState == HandState.Closed)
            {
                DrawControl(joint, 40, new SolidColorBrush(new Color()
                {
                    G = 255,
                    B = 255,
                    A = 128
                }));

                //初期位置設定
                if (startpoint == 2)
                {
                    startpoint = 1;
                }

                //初期位置設定
                if (cursorcheck == 1)
                {
                    cursorcheck = 0;
                }

            }
        }

        //クリック
        private void HandClick(Joint joint,
            TrackingConfidence trackingConfidence, HandState handState, int x, int y, int N)
        {
            // 手の追跡信頼性が高い
            if (trackingConfidence != TrackingConfidence.High)
            {
                return;
            }

            // 手が閉じている(グー)
            if (handState == HandState.Closed)
            {
                if ((x < nowx + psize / 2 && nowx + psize / 2 < x + N) && (y < nowy + psize / 2 && nowy + psize / 2 < y + N))
                {
                    //タイマーが動いていれば点数加算
                    if (timer.IsEnabled == true)
                    {
                        getpoint++;
                    }

                    shoot.Add(1);
                    flag.Add(1);
                    s++;

                    Drawburn(tsize, x, y);

                    CanvasTarget.Children.Clear();
                    //this.TargetImage.Visibility = Visibility.Hidden;
                }

                else
                {
                    //CannotClick(20, nowx, nowy);
                }

            }
        }

        //関節表示
        private void DrawEllipse(Joint joint, int R, Brush brush)
        {
            var ellipse = new Ellipse()
            {
                Width = R,
                Height = R,
                Fill = brush,
            };

            // カメラ座標系をDepth座標系に変換する
            var point = kinect.CoordinateMapper.MapCameraPointToDepthSpace(joint.Position);
            if ((point.X < 0) || (point.Y < 0))
            {
                return;
            }

            // Depth座標系で円を配置する
            Canvas.SetLeft(ellipse, point.X);
            Canvas.SetTop(ellipse, point.Y);
            CanvasBody.Children.Add(ellipse);

        }

        //コントローラ表示
        private void DrawControl(Joint joint, int R, Brush brush)
        {
            var ellipse = new Ellipse()
            {
                Width = R,
                Height = R,
                Fill = brush,
            };

            Canvas.SetLeft(ellipse, Ctx - (int)bex - R / 2);
            Canvas.SetTop(ellipse, Cty - (int)bey - R / 2);
            CanvasBody.Children.Add(ellipse);

        }

        //初期位置表示
        private void Drawstart(int R, Brush brush, int x, int y)
        {

            Line line = new Line()
            {
                //始点
                X1 = x - R / 2,
                Y1 = y,
                //終点
                X2 = x + R / 2,
                Y2 = y,
                //色
                Stroke = new SolidColorBrush(Colors.Red),
                //太さ
                StrokeThickness = 2,
            };
            CanvasBody.Children.Add(line);

            Line line2 = new Line()
            {
                //始点
                X1 = x,
                Y1 = y - R / 2,
                //終点
                X2 = x,
                Y2 = y + R / 2,
                //色
                Stroke = new SolidColorBrush(Colors.Red),
                //太さ
                StrokeThickness = 2,
            };
            CanvasBody.Children.Add(line2);

            /*
            var ellipse = new Ellipse()
            {
                Width = R,
                Height = R,
                Fill = brush,
            };

            // Depth座標系で円を配置する
            //Canvas.SetLeft(ellipse, x - (R / 2));
            //Canvas.SetTop(ellipse, y - (R / 2));
            Canvas.SetLeft(ellipse, 500 - (R / 2));
            Canvas.SetTop(ellipse,  500 - (R / 2));

            CanvasBody.Children.Add(ellipse);
            */
        }

        //線を描く
        private void DrawLine(int a, int b, int c, int d)
        {
            CanvasLine.Children.Clear();

            Style lineStyle = this.FindResource("GridLineStyle") as Style;

            Line line = new Line()
            {
                //始点
                X1 = a,
                Y1 = b,
                //終点
                X2 = c,
                Y2 = d,
                //色
                Stroke = new SolidColorBrush(Colors.Red),
                //太さ
                StrokeThickness = 5,
            };

            CanvasLine.Children.Add(line);
        }

        //ターゲットの位置決め
        private void RandomTarget()
        {
            int seed = Environment.TickCount;
            Random rnd = new Random(seed++);

            if (a == 0)
            {
                shoot.Add(1);
                flag.Add(1);
                a = 1;
            }

            else if((int)shoot[s] == 1)
            {

                if ((int)flag[s] == 1)
                {
                    Tx = rnd.Next(width - tsize);
                    Ty = rnd.Next(height - tsize);

                    //targetの表示
                    //DrawTarget(tsize, Tx, Ty);
                    DrawTargetImage(tsize, Tx, Ty);
                    flag[s] = 0;
                }

            }

        }

        //ポインタを表示
        private void DrawCursor(int R, int x, int y)
        {
            CanvasPoint.Children.Clear();

            var ellipse = new Ellipse()
            {
                Width = R,
                Height = R,
                StrokeThickness = 2,
                Stroke = Brushes.Red,
            };
            Canvas.SetLeft(ellipse, x);
            Canvas.SetTop(ellipse, y);
            CanvasPoint.Children.Add(ellipse);

            Line line = new Line()
            {
                //始点
                X1 = x + R / 2,
                Y1 = y,
                //終点
                X2 = x + R / 2,
                Y2 = y + R,
                //色
                Stroke = new SolidColorBrush(Colors.Red),
                //太さ
                StrokeThickness = 2,
            };
            CanvasPoint.Children.Add(line);

            Line line2 = new Line()
            {
                //始点
                X1 = x,
                Y1 = y + R / 2,
                //終点
                X2 = x + R,
                Y2 = y + R / 2,
                //色
                Stroke = new SolidColorBrush(Colors.Red),
                //太さ
                StrokeThickness = 2,
            };
            CanvasPoint.Children.Add(line2);

            /*
            var text = new TextBlock()
            {
                Text = string.Format("Cursor"),
                FontSize = 20,
                Foreground = Brushes.Green,
            };
            Canvas.SetLeft(text, x+R);
            Canvas.SetTop(text, y+R);
            CanvasPoint.Children.Add(text);
            */
        }

        //クリックできない時
        private void CannotClick(int R, int x, int y)
        {

            var text = new TextBlock()
            {
                Text = string.Format("×"),
                FontSize = 30,
                Foreground = Brushes.Blue,
            };
            Canvas.SetLeft(text, x+R);
            Canvas.SetTop(text, y+R);
            CanvasPoint.Children.Add(text);
            
        }

        //ターゲットの表示
        private void DrawTarget(int R, int x, int y)
        {
            //長方形
            Rectangle rect = new Rectangle()
            {
                Width=R,
                Height=R,
                Fill = Brushes.Yellow,

            };

            // Depth座標系で円を配置する
            Canvas.SetLeft(rect, x);
            Canvas.SetTop(rect, y);
            CanvasTarget.Children.Add(rect);

        }

        //ターゲットの画像表示
        private void DrawTargetImage(int R, int x, int y)
        {
            Canvas.SetLeft(TargetImage, x);
            Canvas.SetTop(TargetImage, y);
            this.TargetImage.Height = R;
            this.TargetImage.Width = R;
            this.TargetImage.Visibility = Visibility.Visible;

            var text = new TextBlock()
            {
                Text = string.Format("ここだ！"),
                FontSize = tsize / 5 * 2,
                Foreground = Brushes.Black,
            };
            Canvas.SetLeft(text, x);
            Canvas.SetTop(text, y + R);
            CanvasTarget.Children.Add(text);

            CanvasTarget.Children.Add(TargetImage);

        }

        //ターゲット爆破
        private void Drawburn(int R, int x, int y)
        {

            if (timer2.IsEnabled == true)
            {
                timer2.Stop();
                CanvasBurn.Children.Clear();
            }

            Canvas.SetLeft(TargetImage2, x);
            Canvas.SetTop(TargetImage2, y);
            this.TargetImage2.Height = R;
            this.TargetImage2.Width = R;
            this.TargetImage2.Visibility = Visibility.Visible;

            CanvasBurn.Children.Add(TargetImage2);

            /*
            Line line = new Line()
            {
                //始点
                X1 = x + R / 2,
                Y1 = y,
                //終点
                X2 = x + R / 2,
                Y2 = y + R,
                //色
                Stroke = new SolidColorBrush(Colors.Red),
                //太さ
                StrokeThickness = 2,
            };
            CanvasBurn.Children.Add(line);

            Line line2 = new Line()
            {
                //始点
                X1 = x,
                Y1 = y + R / 2,
                //終点
                X2 = x + R,
                Y2 = y + R / 2,
                //色
                Stroke = new SolidColorBrush(Colors.Red),
                //太さ
                StrokeThickness = 2,
            };
            CanvasBurn.Children.Add(line2);
            */

            timer2.Start();

        }

        //手形状によるカーソル操作
        private void CursorCheck()
        {
            if (cursorcheck == 0)
            {
                DrawCursor(20, nowx, nowy);
                ax = nowx;
                ay = nowy;

                cursorcheck = 1;

            }

            if (cursorcheck == 2)
            {
                DrawCursor(20, Ctx, Cty);
                ax = Ctx;
                ay = Cty;

                cursorcheck = 1;

            }
        }

        //エリア指定
        private void Area(int x, int y,int R)
        {
            if (x + R > width)
            {
                nowx = width - R;
                ax = width - R;
            }
            else if (x < 0)
            {
                nowx = 0;
                ay = 0;
            }

            if (y + R > height)
            {
                nowy = height - R;
                ax = height - R;

            }
            else if (y < 0)
            {
                nowy = 0;
                ay = 0;
            }
        }

        //エリア枠
        private void AreaLine(int x, int y)
        {

            Line line = new Line()
            {
                //始点
                X1 = 0,
                Y1 = y,
                //終点
                X2 = x,
                Y2 = y,
                //色
                Stroke = new SolidColorBrush(Colors.Black),
                //太さ
                StrokeThickness = 3,
            };
            CanvasBody.Children.Add(line);

            Line line2 = new Line()
            {
                //始点
                X1 = x,
                Y1 = 0,
                //終点
                X2 = x,
                Y2 = y,
                //色
                Stroke = new SolidColorBrush(Colors.Black),
                //太さ
                StrokeThickness = 3,
            };
            CanvasBody.Children.Add(line2);

            Line line3 = new Line()
            {
                //始点
                X1 = 0,
                Y1 = 0,
                //終点
                X2 = x,
                Y2 = 0,
                //色
                Stroke = new SolidColorBrush(Colors.Black),
                //太さ
                StrokeThickness = 3,
            };
            CanvasBody.Children.Add(line3);

            Line line4 = new Line()
            {
                //始点
                X1 = 0,
                Y1 = 0,
                //終点
                X2 = 0,
                Y2 = y,
                //色
                Stroke = new SolidColorBrush(Colors.Black),
                //太さ
                StrokeThickness = 3,
            };
            CanvasBody.Children.Add(line4);

        }

        //タイマーのon/off
        private void TimerControl()
        {

            if (onhead == 0)
            {

                if (Lhandy < heady)
                {
                    timer.Start();
                    timer3.Start();

                    startmessarge.Visibility = Visibility.Hidden;
                    finish.Content = "スタート！";
                    finish.Visibility = Visibility.Visible;

                    onhead = 1;
                }

            }

            else
            {
                if (time == 0)
                {
                    timer.Stop();

                    CanvasTarget.Children.Clear();

                    finish.Content = "君は小五郎を" + getpoint + "人眠らせた！";
                    finish.Visibility = Visibility.Visible;

                }
                
            }


        }

        //タイマー処理
        void timer_Tick(object sender, EventArgs e)
        {
            time--;
            //this.textBlock1.Text = DateTime.Now.ToString("HH:mm:ss");
            this.textBlock1.Text = ("あと" + time + "秒");

        }

        //タイマー2処理
        void timer2_Tick(object sender, EventArgs e)
        {
            timer2.Stop();
            CanvasBurn.Children.Clear();
            
        }

        //タイマー3処理
        void timer3_Tick(object sender, EventArgs e)
        {
            timer3.Stop();
            finish.Visibility = Visibility.Hidden;
        }

        //RESETボタン
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            if (timer.IsEnabled == false)
            {
                time = 10;
                getpoint = 0;
                timeover = 0;
                onhead = 0;
                startset = 0;

                shoot.Add(1);
                flag.Add(1);
                s++;
                CanvasTarget.Children.Clear();

                finish.Visibility = Visibility.Hidden;
                startmessarge.Visibility = Visibility.Visible;

            }

        }

    }
}
