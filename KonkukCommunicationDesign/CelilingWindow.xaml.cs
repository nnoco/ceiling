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

namespace KonkukCommunicationDesign
{
    /// <summary>
    /// CelilingWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class CeilingWindow : Window
    {
        #region Member Variables
        public static int[] initialDepth;
        private KinectSensor _KinectDevice;
        private WriteableBitmap _RawDepthImage;
        private Int32Rect _RawDepthImageRect;
        private int _RawDepthImageStride;
        public short[] _DepthImagePixelData;
        private int _TotalFrames;
        private DateTime _StartFrameTime;
        public DepthImageFrame _LastDepthFrame;
        public byte[][] backgroundColor;
        public double maxDepthX;
        public double maxDepthY;
        public WriteableBitmap _ColorImageBitmap;
        private Int32Rect _ColorImageBitmapRect;
        private int _ColorImageStride;

        #endregion Member Variables

        public static double hallX;
        public static double hallY;
        public static int maxDepthIndex;

        public static bool isValidHall = false;

        Thread calculatingThread;
        bool calculatingThreadFlag = false;

        public static int closerDepth = -1;
        public static int closerDepthIndex = -1;



        public bool IsClosed = false;
        public WallDisplayWindow wallDisplayWindow { get; set; }
        public CeilingWindow()
        {
            InitializeComponent();
            //calculatingThread = new Thread(new ThreadStart(calculate));
            //calculatingThreadFlag = true;
            //calculatingThread.Start();

            System.Drawing.Bitmap background = Properties.Resources.sky_taken_in_cancun_jan_2011;

            backgroundColor = new byte[background.Width * background.Height][];
            int length = background.Width * background.Height;
            for (int i = 0; i < background.Height; i++ )
            {
                for (int j = 0; j < background.Width; j++)
                {
                    backgroundColor[i * background.Width + j] = new byte[]{
                        background.GetPixel(j, i).B,
                        background.GetPixel(j, i).B,
                        background.GetPixel(j, i).R
                    };
                }
            }
            

            KinectSensor.KinectSensors.StatusChanged += KinectSensors_StatusChanged;
            //int kinectCount = KinectSensor.KinectSensors.Count;
            this.KinectDevice = KinectSensor.KinectSensors.LastOrDefault(x => x.Status == KinectStatus.Connected);
            


            //MessageBox.Show(KinectSensor.KinectSensors.Count.ToString());

            //CeilingDisplay.MouseLeftButtonUp += DepthImage_MouseLeftButtonUp;
        }

        private void KinectSensors_StatusChanged(object sender, StatusChangedEventArgs e)
        {
            switch (e.Status)
            {
                case KinectStatus.Initializing:
                case KinectStatus.Connected:
                case KinectStatus.NotPowered:
                case KinectStatus.NotReady:
                case KinectStatus.DeviceNotGenuine:
                    this.KinectDevice = e.Sensor;
                    break;
                case KinectStatus.Disconnected:
                    //TODO: Give the user feedback to plug-in a Kinect device.                    
                    this.KinectDevice = null;
                    break;
                default:
                    //TODO: Show an error state
                    break;
            }
        }

        private void KinectDevice_DepthFrameReady(object sender, DepthImageFrameReadyEventArgs e)
        {
            if (this._LastDepthFrame != null)
            {
                this._LastDepthFrame.Dispose();
                this._LastDepthFrame = null;
            }

            this._LastDepthFrame = e.OpenDepthImageFrame();

            if (this._LastDepthFrame != null)
            {
                this._LastDepthFrame.CopyPixelDataTo(this._DepthImagePixelData);

                
                CreateColorDepthImage(this._LastDepthFrame, this._DepthImagePixelData);


                this._RawDepthImage.WritePixels(this._RawDepthImageRect, this._DepthImagePixelData, this._RawDepthImageStride, 0);
            }
        }

        public int frameWidth;
        public int frameHeight;
        private void CreateColorDepthImage(DepthImageFrame depthFrame, short[] pixelData)
        {
            int depth;
            int recogCount = 0;
            int centerX = 0;
            int centerY = 0;
            int loThreshold = 1220;
            int hiThreshold = 3048;
            int bytesPerPixel = 4;
            byte[] rgb = new byte[3];
            frameWidth = _LastDepthFrame.Width;
            frameHeight = _LastDepthFrame.Height;
            byte[] enhPixelData = new byte[depthFrame.Width * depthFrame.Height * bytesPerPixel];

            // 현재 프레임의 최대 Depthh
            int currentFrameCloser = Int32.MaxValue;
            int currentFrameCloserIndex = -1;
            int imageIndex;
            for (int i = 0, j = 0; i < pixelData.Length; i++, j += bytesPerPixel)
            {
                depth = pixelData[i] >> DepthImageFrame.PlayerIndexBitmaskWidth;
                
                if (depth < loThreshold || depth > hiThreshold)
                {
                    enhPixelData[j] = 0x00;
                    enhPixelData[j + 1] = 0x00;
                    enhPixelData[j + 2] = 0x00;
                }
                else
                {

                    enhPixelData[j] = (byte)(0x0);  //Blue
                    enhPixelData[j + 1] = (byte)(0x00);  //Green
                    enhPixelData[j + 2] = (byte)(0x0);  //Red

                    if (initialDepth != null)
                    {
                        if (depth > 500)
                        {
                            
                            // 부스 방향에서 천을 위로 누를 경우 가까워지므로 Min값을 택함
                            
                        }
                        if ( Utils.isChangedDepthOnCeiling(depth, i, initialDepth))
                        {
                            if (currentFrameCloser >= depth)
                            {
                                currentFrameCloser = depth;
                                currentFrameCloserIndex = i;
                            }
                            // 카운트 증가
                            recogCount++;

                            // 평균 더하기
                            centerX += i % frameWidth;
                            centerY += i / frameWidth;

                            // 이미지 색상 덮어쓰기

                            // i == j / bytePerPixel
                            // | centerX | frameWidth-centerX | frameWidth
                            // 
                            //imageIndex = i - centerX;
                            // imageIndex += frameWidth - 1 - centerX;
                            // imageIndex *= bytesPerPixel;
                            //imageIndex = ((pixelData.Length-1) - (imageIndex % frameWidth)) * bytesPerPixel;
                            enhPixelData[j] = backgroundColor[i][0];
                            enhPixelData[j + 1] = backgroundColor[i][1];
                            enhPixelData[j + 2] = backgroundColor[i][2];
                        }
                    }
                }
                
            }

            /*if (currentFrameCloser != Int32.MaxValue)
            {
                closerDepth = currentFrameCloser;
                closerDepthIndex = currentFrameCloserIndex;
            }
            */
            log.Content = string.Format("{0}, {1}", closerDepth, closerDepthIndex);

            if (recogCount > Preferences.ValidHallCount)
            {
                hallX = centerX / recogCount;
                hallY = centerY / recogCount;

                // 확대된 좌표로 변환
                hallX = hallX / depthFrame.Width * Width;
                hallY = hallY / depthFrame.Height * Height * 1.2;

                // 좌우 반전
                hallX = Width - hallX;
                // 상하 반전
                hallY = Height - hallY;

                isValidHall = true;

            }
            else
            {
                isValidHall = false;
            }
            CeilingDisplay.Source = BitmapSource.Create(depthFrame.Width, depthFrame.Height, 96, 96, PixelFormats.Bgr32, null, enhPixelData, depthFrame.Width * bytesPerPixel);
        }

       

        private void InitializeRawDepthImage(DepthImageStream depthStream)
        {
            if (depthStream == null)
            {
                this._RawDepthImage = null;
                this._RawDepthImageRect = new Int32Rect();
                this._RawDepthImageStride = 0;
                this._DepthImagePixelData = null;
            }
            else
            {
                this._RawDepthImage = new WriteableBitmap(depthStream.FrameWidth, depthStream.FrameHeight, 96, 96, PixelFormats.Gray16, null);
                this._RawDepthImageRect = new Int32Rect(0, 0, depthStream.FrameWidth, depthStream.FrameHeight);
                this._RawDepthImageStride = depthStream.FrameBytesPerPixel * depthStream.FrameWidth;
                this._DepthImagePixelData = new short[depthStream.FramePixelDataLength];
            }

            this.CeilingDisplay.Source = this._RawDepthImage;
        }

        public void ConvertHslToRgb(double hue, double saturation, double lightness, byte[] rgb)
        {
            double red = 0.0;
            double green = 0.0;
            double blue = 0.0;
            hue = hue % 360.0;
            saturation = saturation / 100.0;
            lightness = lightness / 100.0;

            if (saturation == 0.0)
            {
                red = lightness;
                green = lightness;
                blue = lightness;
            }
            else
            {
                double huePrime = hue / 60.0;
                int x = (int)huePrime;
                double xPrime = huePrime - (double)x;
                double L0 = lightness * (1.0 - saturation);
                double L1 = lightness * (1.0 - (saturation * xPrime));
                double L2 = lightness * (1.0 - (saturation * (1.0 - xPrime)));

                switch (x)
                {
                    case 0:
                        red = lightness;
                        green = L2;
                        blue = L0;
                        break;
                    case 1:
                        red = L1;
                        green = lightness;
                        blue = L0;
                        break;
                    case 2:
                        red = L0;
                        green = lightness;
                        blue = L2;
                        break;
                    case 3:
                        red = L0;
                        green = L1;
                        blue = lightness;
                        break;
                    case 4:
                        red = L2;
                        green = L0;
                        blue = lightness;
                        break;
                    case 5:
                        red = lightness;
                        green = L0;
                        blue = L1;
                        break;
                }
            }

            rgb[0] = (byte)(255.0 * red);
            rgb[1] = (byte)(255.0 * green);
            rgb[2] = (byte)(255.0 * blue);
        }

        

        private void DepthImage_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Point p = e.GetPosition(CeilingDisplay);

            // 늘어난 좌표에 대한 변환
            int originalWidth = this._LastDepthFrame.Width;
            int originalHeight = this._LastDepthFrame.Height;

            p.X = (p.X / Width) * originalWidth;
            p.Y = (p.Y / Height) * originalHeight;

            if (this._DepthImagePixelData != null && this._DepthImagePixelData.Length > 0)
            {
                int width = this._LastDepthFrame.Width;
                int pixelIndex = (int)(p.X + ((int)p.Y * width));
                int depth = this._DepthImagePixelData[pixelIndex] >> DepthImageFrame.PlayerIndexBitmaskWidth;
                int depthInches = (int)(depth * 0.0393700787);
                int depthFt = depthInches / 12;
                depthInches = depthInches % 12;

                //PixelDepth.Text = string.Format("{0}mm ~ {1}'{2}\"", depth, depthFt, depthInches);
                MessageBox.Show(string.Format("{0}, {1}", _LastDepthFrame.Width, _LastDepthFrame.Height));
            }
        }

        private void Window_KeyUp_1(object sender, KeyEventArgs e)
        {
            shortCutDispatch(e);
        }

        private void Window_Closing_1(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Wall Display Window 닫기
            wallDisplayWindow.IsClosed = true;

            if (!IsClosed) wallDisplayWindow.Close();
        }

       

        #region Properties
        public KinectSensor KinectDevice
        {
            get { return this._KinectDevice; }
            set
            {
                if (this._KinectDevice != value)
                {
                    //Uninitialize
                    if (this._KinectDevice != null)
                    {
                        this._KinectDevice.Stop();
                        this._KinectDevice.DepthFrameReady -= KinectDevice_DepthFrameReady;
                        
                        this._KinectDevice.DepthStream.Disable();
                        
                        InitializeRawDepthImage(null);

                        this.CeilingDisplay.Source = null;
                        this._RawDepthImage = null;
                    }

                    this._KinectDevice = value;

                    //Initialize
                    if (this._KinectDevice != null)
                    {
                        if (this._KinectDevice.Status == KinectStatus.Connected)
                        {
                            this._KinectDevice.DepthStream.Enable();
                            InitializeRawDepthImage(this._KinectDevice.DepthStream);
                            this._KinectDevice.DepthFrameReady += KinectDevice_DepthFrameReady;

                            //this._KinectDevice.ColorStream.Enable();
                            //InitializeKinectSensor(this._KinectDevice);
                            //this._KinectDevice.ColorFrameReady += Kinect_ColorFrameReady;
                            this._KinectDevice.Start();

                            this._StartFrameTime = DateTime.Now;
                        }
                    }
                }
            }
        }

        void _KinectDevice_ColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        {
            
        }



       
        #endregion Properties

        public double windowWidth;
        public double windowHeight;
       

        private void Window_SizeChanged_1(object sender, SizeChangedEventArgs e)
        {
            CeilingDisplay.Width = Width;
            CeilingDisplay.Height = Height;
            this.windowWidth = Width;
            this.windowHeight = Height;
        }

        private void Window_StateChanged_1(object sender, EventArgs e)
        {
            Window w = sender as Window;

            if (w.WindowState.ToString().Equals(WindowState.Maximized.ToString()))
            {
                MessageBox.Show(WindowState.Maximized.ToString() + ", " + Height.ToString());
                CeilingDisplay.Height = Height;
                CeilingDisplay.Width = Width;
            }
        }

        private void Window_MouseLeftButtonUp_1(object sender, MouseButtonEventArgs e)
        {
            if (initialDepth == null) return;

            Point p = e.GetPosition(CeilingDisplay);

            // 늘어난 좌표에 대한 변환
            int originalWidth = this._LastDepthFrame.Width;
            int originalHeight = this._LastDepthFrame.Height;

            p.X = (p.X / Width) * originalWidth;
            p.Y = (p.Y / Height) * originalHeight;

            if (this._DepthImagePixelData != null && this._DepthImagePixelData.Length > 0)
            {
                string isEqual = "같음";
                int width = this._LastDepthFrame.Width;
                int pixelIndex = (int)(p.X + ((int)p.Y * width));
                int depth = this._DepthImagePixelData[pixelIndex] >> DepthImageFrame.PlayerIndexBitmaskWidth;

                if (Utils.isChangedDepthOnCeiling(depth, pixelIndex, initialDepth))
                {
                    isEqual = "다름";
                }
                int depthInches = (int)(depth * 0.0393700787);
                int depthFt = depthInches / 12;
                depthInches = depthInches % 12;

                //MessageBox.Show(string.Format("{3},  {0}mm ~ {1}'{2}\"", depth, depthFt, depthInches, isEqual));
            }
        }

        private void Window_MouseLeftButtonDown_1(object sender, MouseButtonEventArgs e)
        {

            hallX = e.GetPosition(CanvasForObject).X;
            hallY = e.GetPosition(CanvasForObject).Y;
            isValidHall = true;

        }

        private void Window_MouseMove_1(object sender, MouseEventArgs e)
        {
            hallX = e.GetPosition(CanvasForObject).X;
            hallY = e.GetPosition(CanvasForObject).Y;
        }

        private void Window_MouseLeftButtonUp_2(object sender, MouseButtonEventArgs e)
        {
            isValidHall = false;
        }

    }
}
