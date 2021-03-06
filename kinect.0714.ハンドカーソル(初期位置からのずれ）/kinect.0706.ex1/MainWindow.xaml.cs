﻿using System;
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

        // 表示
        WriteableBitmap depthImage;
        ushort[] depthBuffer;
        byte[] depthBitmapBuffer;
        Int32Rect depthRect;
        int depthStride;

        Point depthPoint;
        const int R = 20;


        //マウス操作の関数宣言
        [DllImport("USER32.dll",CallingConvention=CallingConvention.StdCall)]
        static extern void SetCursorPos(int X, int Y);

        [DllImport("USER32.dll", CallingConvention = CallingConvention.StdCall)]
        static extern void mouse_event(int dFlags, int dx, int dy, int cButtons, int dwExtraInfo);

        private const int MOUSEEVENTF_LEFTDOWN = 0x2;
        private const int MOUSEEVENTF_LEFTUP = 0x4;


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

            if (kinect != null)
            {
                kinect.Close();
                kinect = null;
            }
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

        /*
        private void UpdateDepthValue()
        {
            CanvasPoint.Children.Clear();

            // クリックしたポイントを表示する
            var ellipse = new Ellipse()
            {
                Width = R,
                Height = R,
                StrokeThickness = R / 4,
                Stroke = Brushes.Red,
            };
            Canvas.SetLeft(ellipse, depthPoint.X - (R / 2));
            Canvas.SetTop(ellipse, depthPoint.Y - (R / 2));
            CanvasPoint.Children.Add(ellipse);

            // クリックしたポイントのインデックスを計算する
            int depthindex = (int)((depthPoint.Y * depthFrameDesc.Width) + depthPoint.X);

            // クリックしたポイントの距離を表示する
            var text = new TextBlock()
            {
                Text = string.Format("{0}mm", depthBuffer[depthindex]),
                FontSize = 20,
                Foreground = Brushes.Green,
            };
            Canvas.SetLeft(text, depthPoint.X);
            Canvas.SetTop(text, depthPoint.Y - R);
            CanvasPoint.Children.Add(text);
        }

        
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            depthPoint = e.GetPosition(this);
        }
        */

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

        //座標格納
        float x1, x2, y1, y2;
        float handx, handy;
        int startpoint = 0;
        int nowx, nowy;


        // ボディの表示
        private void DrawBodyFrame()
        {
            CanvasBody.Children.Clear();

            foreach (var body in bodies.Where(b => b.IsTracked))
            {
                foreach (var joint in body.Joints)
                {

                    // 手の位置が追跡状態
                    if (joint.Value.TrackingState == TrackingState.Tracked)
                    {
                        //左手
                        if (joint.Value.JointType == JointType.HandLeft)
                        {
                            if (startpoint == 0)
                            {
                                DrawEllipse(joint.Value, 10, Brushes.Green);

                                //3次元座標を取得
                                CameraSpacePoint hand = new CameraSpacePoint();
                                hand = joint.Value.Position;

                                //2次元座標に変換、表示
                                var handpoint = kinect.CoordinateMapper.MapCameraPointToDepthSpace(joint.Value.Position);

                                handx = handpoint.X;
                                handy = handpoint.Y;

                                startpoint = 1;

                            }

                            //3次元座標を取得
                            CameraSpacePoint h = new CameraSpacePoint();
                            h = joint.Value.Position;

                            //2次元座標に変換、表示
                            var hpoint = kinect.CoordinateMapper.MapCameraPointToDepthSpace(joint.Value.Position);

                            xc.Content = "x=" + hpoint.X;
                            yc.Content = "y=" + hpoint.Y;

                            //起点となる位置
                            Drawstart(10, Brushes.Yellow, (int)handx, (int)handy);

                            DrawEllipse(joint.Value, 10, Brushes.Green);

                            DrawHandState(body.Joints[JointType.HandLeft],
                               body.HandLeftConfidence, body.HandLeftState);

                            //ベクトルを求める
                            float bex = handx - hpoint.X;
                            float bey = handy - hpoint.Y;
                            xz.Content = "x=" + bex;
                            yz.Content = "y=" + bey;

                            //Jointを線で結ぶ
                            DrawLine((int)handx, (int)handy, (int)hpoint.X, (int)hpoint.Y);

                            //現在のカーソル位置取得
                            nowx = System.Windows.Forms.Cursor.Position.X;
                            nowy = System.Windows.Forms.Cursor.Position.Y;

                            //カーソル位置の再定義
                            System.Windows.Forms.Cursor.Position =
                                new System.Drawing.Point(nowx - (int)bex / 5, nowy - (int)bey / 5);


                        }





                        /*
                        //左ひじ
                        if (joint.Value.JointType == JointType.ElbowLeft)
                        {
                            DrawEllipse(joint.Value, 10, Brushes.Blue);

                            //3次元座標を取得
                            CameraSpacePoint h = new CameraSpacePoint();
                            h = joint.Value.Position;

                            //2次元座標に変換、表示
                            var hpoint = kinect.CoordinateMapper.MapCameraPointToDepthSpace(joint.Value.Position);

                            xc.Content = "x=" + hpoint.X;
                            yc.Content = "y=" + hpoint.Y;

                            //座標を反転
                            x1 = hpoint.X;
                            y1 = -hpoint.Y;
                           
                        }

                        //左手首
                        if (joint.Value.JointType == JointType.WristLeft)
                        {
                            DrawEllipse(joint.Value, 10, Brushes.Red);

                            //3次元座標を取得
                            CameraSpacePoint w = new CameraSpacePoint();
                            w = joint.Value.Position;

                            //2次元座標に変換、表示
                            var wpoint = kinect.CoordinateMapper.MapCameraPointToDepthSpace(joint.Value.Position);

                            xd.Content = "x=" + wpoint.X;
                            yd.Content = "y=" + wpoint.Y;

                            //座標を反転
                            x2 = wpoint.X;
                            y2 = -wpoint.Y;


                            //System.Windows.Forms.Cursor.Position = new System.Drawing.Point((int)colorpoint.X, (int)colorpoint.Y);
                            
                        }

                        //ベクトルを求める
                        float bex= x2 - x1;
                        float bey= y2 - y1;
                        xz.Content = "x=" + bex;
                        yz.Content = "y=" + bey;

                        //ベクトルの大きさ
                        double size = Math.Sqrt(bex * bex + bey * bey);

                        if (clickstate == 1)
                        {
                            //右上
                            if ((x2 - x1) > 0 && (y2 - y1) > 0)
                            {
                                //Check(20, Brushes.Yellow);

                                //Jointを線で結ぶ
                                DrawLine((int)x1, -(int)y1, (int)x2, -(int)y2);

                                int curX = System.Windows.Forms.Cursor.Position.X;
                                int curY = System.Windows.Forms.Cursor.Position.Y;

                                if (size > 80)
                                {
                                    System.Windows.Forms.Cursor.Position
                                   = new System.Drawing.Point(curX + 1, curY - 1);
                                }

                            }
                            //左上
                            if ((x2 - x1) < 0 && (y2 - y1) > 0)
                            {
                                //Check(20, Brushes.Yellow);

                                //Jointを線で結ぶ
                                DrawLine((int)x1, -(int)y1, (int)x2, -(int)y2);

                                int curX = System.Windows.Forms.Cursor.Position.X;
                                int curY = System.Windows.Forms.Cursor.Position.Y;

                                if (size > 80)
                                {
                                    System.Windows.Forms.Cursor.Position
                                   = new System.Drawing.Point(curX + 1, curY - 1);
                                }

                            }
                            //右下
                            if ((x2 - x1) > 0 && (y2 - y1) < 0)
                            {
                                //Check(20, Brushes.Yellow);

                                //Jointを線で結ぶ
                                DrawLine((int)x1, -(int)y1, (int)x2, -(int)y2);

                                int curX = System.Windows.Forms.Cursor.Position.X;
                                int curY = System.Windows.Forms.Cursor.Position.Y;

                                if (size > 80)
                                {
                                    System.Windows.Forms.Cursor.Position
                                   = new System.Drawing.Point(curX + 1, curY + 1);
                                }

                            }
                            //左下
                            if ((x2 - x1) < 0 && (y2 - y1) < 0)
                            {
                                //Check(20, Brushes.Yellow);

                                //Jointを線で結ぶ
                                DrawLine((int)x1, -(int)y1, (int)x2, -(int)y2);

                                int curX = System.Windows.Forms.Cursor.Position.X;
                                int curY = System.Windows.Forms.Cursor.Position.Y;

                                if (size > 80)
                                {
                                    System.Windows.Forms.Cursor.Position
                                   = new System.Drawing.Point(curX - 1, curY + 1);
                                }

                            }
                        }
                        */

                        /*
                        // 右手を追跡していたら、手の状態を表示する
                        else if (joint.Value.JointType == JointType.HandRight)
                        {
                            DrawEllipse(joint.Value, 10, Brushes.Green);

                            //DrawHandState(body.Joints[JointType.HandRight],
                                //body.HandRightConfidence, body.HandRightState);


                            
                            //Depthの表示
                            CameraSpacePoint i = new CameraSpacePoint();
                            i = joint.Value.Position;
                            z.Content = "z=" + i.Z;

                            
                            //クリック操作
                            int clickstate = 0;

                            if (i.Z < 0.7)
                            {
                                mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
                                mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);

                                clickstate = 1;
                            }
                            if (i.Z > 0.7)
                            {
                                if (clickstate == 1)
                                {
                                    clickstate = 0;
                                }
                            }
                            
                            
                        }
                        */
                    }

                    // 手の位置が推測状態
                    else if (joint.Value.TrackingState == TrackingState.Inferred)
                    {
                        //DrawEllipse(joint.Value, 10, Brushes.Yellow);
                    }
                }
            }

        }

        //クリック状態を表す変数
        public int clickstate = 0;

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
                DrawEllipse(joint, 40, new SolidColorBrush(new Color()
                {
                    R = 255,
                    G = 255,
                    A = 128
                }));

                //初期状態に戻す
                if (clickstate == 1)
                {
                    clickstate = 0;
                }

            }
            // チョキのような感じ
            else if (handState == HandState.Lasso)
            {
                DrawEllipse(joint, 40, new SolidColorBrush(new Color()
                {
                    R = 255,
                    B = 255,
                    A = 128
                }));
            }
            // 手が閉じている(グー)
            else if (handState == HandState.Closed)
            {
                DrawEllipse(joint, 40, new SolidColorBrush(new Color()
                {
                    G = 255,
                    B = 255,
                    A = 128
                }));

                //クリック操作
                if (clickstate == 0)
                {
                    //mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
                   // mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);

                    clickstate = 1;
                }

                //初期位置設定
                if (startpoint == 1)
                {
                    startpoint = 0;
                }

            }
        }

        private void Drawstart(int R, Brush brush, int x, int y)
        {
            var ellipse = new Ellipse()
            {
                Width = R,
                Height = R,
                Fill = brush,
            };

            // Depth座標系で円を配置する
            Canvas.SetLeft(ellipse, x - (R / 2));
            Canvas.SetTop(ellipse, y - (R / 2));

            CanvasBody.Children.Add(ellipse);
        }

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
            Canvas.SetLeft(ellipse, point.X - (R / 2));
            Canvas.SetTop(ellipse, point.Y - (R / 2));

            CanvasBody.Children.Add(ellipse);
        }


        private void Check(int R, Brush brush)
        {
            var ellipse = new Ellipse()
            {
                Width = R,
                Height = R,
                Fill = brush,
            };

            // 円を配置する
            Canvas.SetLeft(ellipse, 200);
            Canvas.SetTop(ellipse, 200);

            CanvasBody.Children.Add(ellipse);
        }

        //線を描く
        private void DrawLine(int a, int b, int c, int d)
        {
            CanvasPoint.Children.Clear();

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

            CanvasPoint.Children.Add(line);
        }

        //ポイントを表示
        private void DrawCursor(int R, int x, int y)
        {
            var ellipse = new Ellipse()
            {
                Width = R,
                Height = R,
                StrokeThickness = R / 4,
                Stroke = Brushes.Red,
            };
            Canvas.SetLeft(ellipse, x);
            Canvas.SetTop(ellipse, y);
            CanvasPoint.Children.Add(ellipse);

            var text = new TextBlock()
            {
                Text = string.Format("Cursor"),
                FontSize = 20,
                Foreground = Brushes.Green,
            };
            Canvas.SetLeft(text, x+R);
            Canvas.SetTop(text, y+R);
            CanvasPoint.Children.Add(text);
        }

    }
}
