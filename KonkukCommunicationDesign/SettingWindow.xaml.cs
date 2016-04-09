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
using System.Threading;
using Microsoft.Kinect;
using System.Drawing;

namespace KonkukCommunicationDesign
{
    /// <summary>
    /// SettingWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class SettingWindow : Window
    {
        public CeilingWindow cw;
        public WallDisplayWindow ww;


        System.Windows.Point origin;
        ScaleTransform scaleTransform;

        public static bool isOpened;

        public SettingWindow(CeilingWindow cw, WallDisplayWindow ww)
        {
            InitializeComponent();

            this.cw = cw;
            this.ww = ww;

            origin = cw.CeilingDisplay.RenderTransformOrigin;
            lblOriginY.Text = origin.Y.ToString();
            scaleTransform = cw.CeilingDisplay.RenderTransform as ScaleTransform;
            lblScaleY.Text = scaleTransform.ScaleY.ToString();

            sldCeilingRecogDist.Value = Preferences.RecognizingDepth;
            sldWallRecogDist.Value = Preferences.WallRecognizingDepth;

            this.Loaded += (s, e) => {
                sliderObjectCount.Value = Preferences.MaxObjectCount;
            };
            this.Closing += (s, e) => {
                
            };
        }


        private void Slider_ValueChanged_1(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            valueFPS.Content = ((int)sldFPS.Value).ToString();

            Preferences.FPS = (int)sldFPS.Value;
        }

        private void btnInitCeilingDepth_Click_1(object sender, RoutedEventArgs e)
        {
            int[] initialDepth = new int[cw._LastDepthFrame.PixelDataLength];
            int width = cw._LastDepthFrame.Width;
            for (int i = 0 ; i < initialDepth.Length ; i++) {
                initialDepth[i] = cw._DepthImagePixelData[i] >> DepthImageFrame.PlayerIndexBitmaskWidth;
            }

            CeilingWindow.initialDepth = initialDepth;

            MessageBox.Show("천장 깊이가 초기화 되었습니다.");
        }

        private void sldCeilingRecogDist_ValueChanged_1(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            int dist = (int)sldCeilingRecogDist.Value;
            lblCeilingRecogDist.Content = dist.ToString() + "mm 부터 인식";
            Preferences.RecognizingDepth = dist;
        }

        private void sldWallRecogDist_ValueChanged_1(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            int dist = (int)sldWallRecogDist.Value;
            lblWallRecogDist.Content = dist.ToString() + "mm 부터 인식";
            Preferences.WallRecognizingDepth = dist;
        }

        private void btnInitWallDepth_Click(object sender, RoutedEventArgs e)
        {
            int[] initialDepth = new int[ww._LastDepthFrame.PixelDataLength];
            int width = ww._LastDepthFrame.Width;
            for (int i = 0; i < initialDepth.Length; i++)
            {
                initialDepth[i] = ww._DepthImagePixelData[i] >> DepthImageFrame.PlayerIndexBitmaskWidth;
            }

            WallDisplayWindow.initialDepth = initialDepth;

            MessageBox.Show("벽면 깊이가 초기화 되었습니다.");
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            Close();
        }

        bool IsElevationOutstanding;
        private void sliderAngleOnWall_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Slider slider = sender as Slider;
            if (IsElevationOutstanding)
            {
                slider.Value = ww.Kinect.ElevationAngle;
                return;
            }
            int angle = (int)(sender as Slider).Value;

            Thread thread = new Thread(new ThreadStart(() =>
            {
                try
                {
                    IsElevationOutstanding = true;
                    ww.Kinect.ElevationAngle = angle;

                    lblWallAngle.Dispatcher.Invoke(() =>
                    {
                        lblWallAngle.Content = angle.ToString();
                        sliderAngleOnWall.Value = angle;
                    });

                    //(sender as Slider).Value = angle;
                }
                // 모터가 움직이고 있는 중에 값을 바꾸려고 하면 오류가 남.
                catch (InvalidOperationException ex)
                {

                }

                IsElevationOutstanding = false;
            }));
            thread.IsBackground = false;
            thread.Start();
        }

        bool IsElevationOutstanding2;
        

        private void btnMoveToPrimary_Click_1(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.Screen primary = System.Windows.Forms.Screen.AllScreens.FirstOrDefault();
            System.Windows.Forms.Screen secondary = System.Windows.Forms.Screen.AllScreens.LastOrDefault();
            double paramWidth = System.Windows.SystemParameters.PrimaryScreenWidth;
            double rate = paramWidth / primary.Bounds.Width;

            ww.Width = secondary.Bounds.Width * rate;
            ww.Height = secondary.Bounds.Height * rate;
            ww.Left = secondary.Bounds.X * rate;
            ww.Top = secondary.Bounds.Y * rate;

            cw.Width = primary.Bounds.Width * rate;
            cw.Height = primary.Bounds.Height * rate;
            cw.Left = primary.Bounds.X * rate;
            cw.Top = primary.Bounds.Y * rate;
            
        }

        private void btnMoveToSecondary_Click_1(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.Screen primary = System.Windows.Forms.Screen.AllScreens.FirstOrDefault();
            System.Windows.Forms.Screen secondary = System.Windows.Forms.Screen.AllScreens.LastOrDefault();
            double paramWidth = System.Windows.SystemParameters.PrimaryScreenWidth;
            double rate = paramWidth / primary.Bounds.Width;
            
            cw.Width = secondary.Bounds.Width * rate;
            cw.Height = secondary.Bounds.Height * rate;
            cw.Left = secondary.Bounds.X * rate;
            cw.Top = secondary.Bounds.Y * rate;

            ww.Width = primary.Bounds.Width * rate;
            ww.Height = primary.Bounds.Height * rate;
            ww.Left = primary.Bounds.X * rate;
            ww.Top = primary.Bounds.Y * rate;

        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            double originY = Double.Parse(lblOriginY.Text);
            double scaleY = Double.Parse(lblScaleY.Text);

            origin.Y = originY;
            scaleTransform.ScaleY = scaleY;

            cw.CeilingDisplay.RenderTransformOrigin = origin;
            cw.CeilingDisplay.RenderTransform = scaleTransform;

        }

        private void sliderObjectCount_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Slider slider = sender as Slider;
            int value = (int)slider.Value;
            if (Preferences.MaxObjectCount != value)
            {
                lblObjectCount.Content = value.ToString();
                
                Preferences.MaxObjectCount = value;
            }
        }
    }
}
