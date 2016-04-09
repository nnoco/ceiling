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
using System.IO;
using System.Windows.Controls;
using System.Drawing;

using Microsoft.Kinect;
using System.Threading;

namespace KonkukCommunicationDesign
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // 창 크기 최대화
            //Width = System.Windows.SystemParameters.PrimaryScreenWidth;
            //Height = System.Windows.SystemParameters.PrimaryScreenHeight;

            Start();

            /* checkSensorStatus();

            Loaded += (s, e) =>
            {
                KinectSensor.KinectSensors.StatusChanged += KinectSensors_StatusChanged;
            };

            Closing += (s, e) =>
            {
                KinectSensor.KinectSensors.StatusChanged -= KinectSensors_StatusChanged;
                
                uninitPreview(secondSensor);
            };*/
            
        }

        void KinectSensors_StatusChanged(object sender, StatusChangedEventArgs e)
        {
            checkSensorStatus();
        }

        KinectSensor firstSensor = null;
        KinectSensor secondSensor = null;

        private void checkSensorStatus()
        {
            firstSensor = null;
            secondSensor = null;

            switch (KinectSensor.KinectSensors.Count)
            {
                case 0:
                    MessageBox.Show("연결된 키넥트가 없습니다. 애플리케이션 시작을 위해서는 2개의 키넥트 센서가 필요합니다.");
                    break;
                case 1:
                    firstSensor = KinectSensor.KinectSensors.FirstOrDefault();
                    MessageBox.Show("1개의 연결된 키넥트를 확인했습니다. 애플리케이션 시작을 위해서는 2개의 키넥트 센서가 필요합니다.");
                    break;
                case 2:
                default:
                    firstSensor = KinectSensor.KinectSensors[0];
                    secondSensor = KinectSensor.KinectSensors[1];

                    if (secondSensor.Status == KinectStatus.Connected)
                    {
                        initPreview(secondSensor);
                    }
                    break;
            }

            fillStatusAtLable(lblFirstSensorStatus, firstSensor);
            fillStatusAtLable(lblSecondSensorStatus, secondSensor);
        }

        WriteableBitmap bitmap;
        Int32Rect rect;
        int stride;

        private void initPreview(KinectSensor sensor)
        {
            ColorImageStream stream = sensor.ColorStream;

            sensor.ColorStream.Enable();

            bitmap = new WriteableBitmap(stream.FrameWidth, stream.FrameHeight, 96, 96, PixelFormats.Bgr32, null);
            rect = new Int32Rect(0, 0, stream.FrameWidth, stream.FrameHeight);
            stride = stream.FrameWidth * stream.FrameBytesPerPixel;

            imgPreviewCeilingImage.Source = bitmap;

            sliderAngleOnCeiling.IsEnabled = true;
            
            sensor.ColorFrameReady += sensor_ColorFrameReady;
            
            sensor.Start();
            sliderAngleOnCeiling.Value = sensor.ElevationAngle;
        }

        void sensor_ColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        {
            using (ColorImageFrame frame = e.OpenColorImageFrame())
            {
                try
                {
                    byte[] pixelData = new byte[frame.PixelDataLength];
                    frame.CopyPixelDataTo(pixelData);

                    bitmap.WritePixels(rect, pixelData, stride, 0);
                }
                catch (NullReferenceException ex)
                {

                }
            }
        }

        bool isUninitialized = false;
        private void uninitPreview(KinectSensor sensor)
        {
            if (!isUninitialized && sensor != null)
            {
                sensor.Stop();
                sensor.ColorStream.Disable();
                sensor.ColorFrameReady -= sensor_ColorFrameReady;
                //sensor.ColorStream.Disable();

                sensor.Start();

                isUninitialized = true;
                
            }

            lblCeilingAngle.IsEnabled = false;
        }

        private void fillStatusAtLable(Label label, KinectSensor sensor)
        {
            if (null != sensor)
            {
                label.Content = "상태-" + sensor.Status.ToString() + ", 디바이스 연결 ID-" + sensor.DeviceConnectionId;
            }
            else
            {
                label.Content = "키넥트가 연결되지 않았습니다.";
            }
        }

        private void initBubblesBitmap()
        {
            // create BitmapFrame;
            MemoryStream stream;
            BitmapFrame[] bf = new BitmapFrame[FloatObject.bubbles.Length];
            Bitmap bitmap;
            for (int i = 0; i < bf.Length; i++)
            {
                stream = new MemoryStream();
                bitmap = FloatObject.bubbles[i];
                bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                stream.Seek(0, SeekOrigin.Begin);
                bf[i] = BitmapFrame.Create(stream);
            }

            FloatObject.bubbleFrames = bf;
        }

        public void Start()
        {
            initBubblesBitmap();

            KinectSensor.KinectSensors.StatusChanged -= KinectSensors_StatusChanged;
            uninitPreview(secondSensor);

            


            CeilingWindow ceilingWindow = new CeilingWindow();
            WallDisplayWindow wallDisplayWindow = new WallDisplayWindow();
            //ceilingWindow.Owner = this;
            Size screenSize = Utils.getScreenSize();
            //MessageBox.Show(KinectSensor.KinectSensors.Count.ToString());
            ceilingWindow.Top = wallDisplayWindow.Left = ceilingWindow.Left = 0;
            wallDisplayWindow.Top = screenSize.Height / 2;
            ceilingWindow.Width = wallDisplayWindow.Width = screenSize.Width;
            ceilingWindow.Height = wallDisplayWindow.Height = screenSize.Height / 2;
            ceilingWindow.Show();
            wallDisplayWindow.Show();

            wallDisplayWindow.WallDisplay.Width = screenSize.Width;
            wallDisplayWindow.WallDisplay.Height = screenSize.Height / 2;

            ceilingWindow.wallDisplayWindow = wallDisplayWindow;
            wallDisplayWindow.ceilingWindow = ceilingWindow;

            Close();
        }

        private bool checkConditionForStart()
        {
            return (firstSensor != null && secondSensor != null && firstSensor.Status == KinectStatus.Connected && secondSensor.Status == KinectStatus.Connected);
        }


        bool IsElevationOutstanding = false;
        private void sliderAngleOnCeiling_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Slider slider = sender as Slider;
            if (IsElevationOutstanding)
            {
                slider.Value = secondSensor.ElevationAngle;
                return;
            }
            int angle = (int)(sender as Slider).Value;

            Thread thread = new Thread(new ThreadStart(() =>
            {
                try
                {
                    IsElevationOutstanding = true;
                    secondSensor.ElevationAngle = angle;

                    lblCeilingAngle.Dispatcher.Invoke(() =>
                    {
                        lblCeilingAngle.Content = angle.ToString();
                        sliderAngleOnCeiling.Value = angle;
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

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            Start();
            //bool startable = checkConditionForStart();
            
            //if (startable) Start();
            //else MessageBox.Show("시작하려면 두개의 키넥트가 정상적으로 연결되어야 합니다.");
        }
    }
}
