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
    /// WallDisplayWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class WallDisplayWindow : Window
    {
        public static int[] initialDepth;
        

        public bool IsClosed = false;
        public CeilingWindow ceilingWindow { get; set; }

        public List<FloatObject> Objects { get; set; }

        public bool IsThreadAborted = false;

        public WallDisplayWindow()
        {
            InitializeComponent();

            init();


        }

        private void init()
        {

            loopFlag = true;
            objectGeneratorThread = new Thread(new ThreadStart(looping));
            objectGeneratorThread.Start();
            this.Loaded += (s, e) => { DiscoverKinectSensor();};
            this.Unloaded += (s, e) => { 
                this.Kinect = null;
                loopFlag = false;
            };

            Objects = new List<FloatObject>();

            KinectSensor.KinectSensors.StatusChanged += KinectSensors_StatusChanged;
            this.KinectDevice = KinectSensor.KinectSensors.FirstOrDefault(x => x.Status == KinectStatus.Connected);
        }

        public double windowHeight;
        private void Window_SizeChanged_1(object sender, SizeChangedEventArgs e)
        {
            windowHeight = Height;
            WallDisplay.Height = Height;

            WallDisplay.Width = Width;
        }

        private void Window_MouseDoubleClick_1(object sender, MouseButtonEventArgs e)
        {
            FloatObject obj = new FloatObject(ceilingWindow, this, e.GetPosition(this).X);
            Objects.Add(obj);
        }

        private void Window_Closing_1(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Ceiling Window 닫기
            FloatObject.loopFlag = false;
            ceilingWindow.IsClosed = true;
            loopFlag = false;
            if (!IsClosed) ceilingWindow.Close();
            //this.Close();
        }

        private void Window_StateChanged_1(object sender, EventArgs e)
        {
            Window w = sender as Window;

            if (w.WindowState.ToString().Equals(WindowState.Maximized.ToString()))
            {
                MessageBox.Show(WindowState.Maximized.ToString() + ", " + Height.ToString());
                WallDisplay.Height = Height;
            }
        }


        SettingWindow settingWindow;
        private void Window_KeyUp_1(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.S:
                    settingWindow = new SettingWindow(ceilingWindow, this);
                    settingWindow.ShowDialog();
                    break;
            }
        }
    }
}
