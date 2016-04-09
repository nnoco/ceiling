using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Kinect;
using System.Windows.Media.Imaging;
using System.Windows;
using System.Windows.Media;


namespace KonkukCommunicationDesign
{
    public partial class WallDisplayWindow
    {
        public WriteableBitmap _ColorImageBitmap;
        private Int32Rect _ColorImageBitmapRect;
        private int _ColorImageStride;
        private KinectSensor _KinectDevice;
        private WriteableBitmap _RawDepthImage;
        private Int32Rect _RawDepthImageRect;
        private int _RawDepthImageStride;
        private int _TotalFrames;
        private DateTime _StartFrameTime;

        public short[] _DepthImagePixelData;
        public DepthImageFrame _LastDepthFrame;

        public Thread objectGeneratorThread;
        bool loopFlag = false;

        public KinectSensor Kinect
        {
            get { return this._Kinect; }

            set
            {
                if (this._Kinect != value)
                {
                    if (this._Kinect != null)
                    {
                        UninitializeKinectSensor(this._Kinect);
                        this._Kinect = null;
                    }

                    if (value != null && value.Status == KinectStatus.Connected)
                    {
                        this._Kinect = value;
                        InitializeKinectSensor(this._Kinect);
                    }
                }
            }
        }

        private KinectSensor _Kinect;
        private double FrameWidth = -1;
        
        private void DiscoverKinectSensor()
        {
            KinectSensor.KinectSensors.StatusChanged += KinectSensors_StatusChanged;

            this.Kinect = KinectSensor.KinectSensors
                .FirstOrDefault(x => x.Status == KinectStatus.Connected);
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
                    if (this.Kinect == null)
                    {
                        this.Kinect = e.Sensor;
                        this.KinectDevice = e.Sensor;

                        
                    }
                    break;

                case KinectStatus.Disconnected:
                    if (this.Kinect == e.Sensor)
                    {
                        loopFlag = false;
                        objectGeneratorThread = null;

                        this.Kinect = null;
                        //this.Kinect = KinectSensor.KinectSensors.FirstOrDefault(x => x.Status == KinectStatus.Connected);
                        this.KinectDevice = null;
                        if (this.Kinect == null)
                        {

                        }
                    }
                    break;
            }
        }

        public void looping()
        {
            while (loopFlag)
            {
                //MessageBox.Show("Create Object");
                if (Preferences.MaxObjectCount > FloatObject.objectCount)
                {
                    createObject();
                }
                Thread.Sleep(Preferences.GenerationGap);
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
                this.FrameWidth = _LastDepthFrame.Width;
                this._LastDepthFrame.CopyPixelDataTo(this._DepthImagePixelData);

                this._RawDepthImage.WritePixels(this._RawDepthImageRect, this._DepthImagePixelData, this._RawDepthImageStride, 0);

                //createObject(_LastDepthFrame);
            }
        }
        int max = 0;
        private void createObject()
        {
            
            if (initialDepth == null || _LastDepthFrame == null || _DepthImagePixelData == null || FrameWidth == -1) return;

            // 인지 거리 이상의 위치에 오브젝트 생성
            short[] pixelDate = _DepthImagePixelData;
            int depth;
            int bytesPerPixel = 4;

            int recogPixelCount = 0;
            int sumOfX = 0;

            for (int i = 0, j = 0; i < pixelDate.Length; i++, j += bytesPerPixel)
            {
                depth = pixelDate[i] >> DepthImageFrame.PlayerIndexBitmaskWidth;

                if (Utils.isChangedDepthOnWall(depth, i, initialDepth))
                {
                    recogPixelCount++;

                    sumOfX += i % (int)FrameWidth;
                }
            }

            if (Preferences.WallRecogCountBase < recogPixelCount)
            {
                try
                {
                    // x 위치의 평균
                    double x = ((double)sumOfX) / recogPixelCount;
                    max = Math.Max(recogPixelCount, max);

                    // 윈도우 좌표로 변환
                    double windowWidth = 640;
                    Dispatcher.Invoke(() =>
                    {
                        windowWidth = Width;
                    });
                    double xOnWindow = ((x / FrameWidth) * windowWidth);

                    log.Dispatcher.Invoke(() =>
                    {
                        log.Content = "count : " + recogPixelCount.ToString() + ", average : " + x + ", max : " + max.ToString();
                    });

                    Dispatcher.Invoke(() => {
                        FloatObject obj = new FloatObject(ceilingWindow, this, xOnWindow);
                        Objects.Add(obj);
                    });
                    
                }
                catch (TaskCanceledException e)
                {

                }
            }
            


        }



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
                            this._KinectDevice.Start();

                            this._StartFrameTime = DateTime.Now;
                        }
                    }
                }
            }
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

        }

        private void InitializeKinectSensor(KinectSensor sensor)
        {
            if (sensor != null)
            {
                ColorImageStream colorStream = sensor.ColorStream;
                sensor.ColorStream.Enable();

                this._ColorImageBitmap = new WriteableBitmap(colorStream.FrameWidth,
                    colorStream.FrameHeight, 96, 96, PixelFormats.Bgr32, null);

                this._ColorImageBitmapRect = new Int32Rect(0, 0, colorStream.FrameWidth,
                    colorStream.FrameHeight);

                this._ColorImageStride = colorStream.FrameWidth * colorStream.FrameBytesPerPixel;
                WallDisplay.Source = this._ColorImageBitmap;

                sensor.ColorFrameReady += Kinect_ColorFrameReady;
                sensor.Start();
            }
        }

        private void UninitializeKinectSensor(KinectSensor sensor)
        {
            if (sensor != null)
            {
                sensor.Stop();
                sensor.ColorFrameReady -= Kinect_ColorFrameReady;
            }
        }

        private void Kinect_ColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        {
            using (ColorImageFrame frame = e.OpenColorImageFrame())
            {
                if (frame != null)
                {
                    byte[] pixelData = new byte[frame.PixelDataLength];
                    frame.CopyPixelDataTo(pixelData);

                    this._ColorImageBitmap.WritePixels(this._ColorImageBitmapRect, pixelData, this._ColorImageStride, 0);

                    
                }
            }
        }
    }
}
