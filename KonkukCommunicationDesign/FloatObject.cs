using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Drawing;
using System.IO;
using System.Windows.Media.Effects;

namespace KonkukCommunicationDesign
{
    public class FloatObject
    {
        public static bool loopFlag = true;

        public static int objectCount = 0;

        delegate void Task();

        public static readonly Random RANDOM = new Random((int)DateTime.Now.Ticks);
        public double Scale = 1;
        public Point3D Position { get; set; }

        CeilingWindow ceiling;
        WallDisplayWindow wall;
        Canvas ceilingCanvas;
        Canvas wallCanvas;

        public Thread thread;
        Boolean upFlag = false;
        System.Windows.Controls.Image wallObject;
        System.Windows.Controls.Image ceilingObject;
        public static Random random = new Random((int)System.DateTime.Now.Ticks);

        double upVelocity;

        TransformGroup transformGroup;
        RotateTransform rotateTransform;
        ScaleTransform scaleTransform;

        double degree;

        public FloatObject(CeilingWindow ceiling, WallDisplayWindow wall, double x)
        {
            this.ceiling = ceiling;
            this.wall = wall;
            this.ceilingCanvas = ceiling.CanvasForObject;
            this.wallCanvas = wall.CanvasForObject;

            initMotions();
            init(x);

            objectCount++;
            
        }
        int imageIndex;

        Point3D p0;
        Point3D cp;
        Point3D p;
        double t0, t;
        long tGap, lastT;
        double scale = 0;

        double vy;

        double opacity = 1; // 투명도
        double dx, dz;
        double div = 100;
        bool isOpen = false;

        
        Task pendulumMotion;
        Task sinXMotion;
        Task upwardMotion;
        Task createMotion;
        Task freeFallMotion;
        Task moveToHallMotion;
        Task opacityMotion;
        Task sinYMotion;

        int state = 0;
        int lastState;

        double targetX;
        double targetY;
        double distHallX;
        double distHallY;
        double hallGX;
        double hallGY;


        public const int STATE_CREATING = 1;
        public const int STATE_UPWARD = 2;
        public const int STATE_FREE_FALL = 3;
        public const int STATE_MOVE_TO_HALL = 4;

        int count;

        double destCeilingHeight;

        void init(double x)
        {
            // 값 초기화
            destCeilingHeight = RANDOM.NextDouble() * (ceiling.Height - Preferences.ObjectHeight);
            p0 = new Point3D(x, 150, ceiling.Height + 150);
            p = new Point3D(p0.x, p0.y, p0.z);
            t0 = System.DateTime.Now.Ticks;

            // 객체 초기화
            transformGroup = new TransformGroup();
            rotateTransform = new RotateTransform(0);
            scaleTransform = new ScaleTransform(scale, scale, 0, Preferences.ObjectHeight / 2);

            transformGroup.Children.Add(rotateTransform);
            transformGroup.Children.Add(scaleTransform);

            // 말풍선 이미지 인덱스 초기화
            imageIndex = random.Next(bubbles.Length);

            createAtWall();
            createAtCeiling();


            // 애니메이션 스레드 동작 시작
            state = STATE_CREATING;
            thread = new Thread(new ThreadStart(animate));
            thread.Start();
        }

        void initMotions()
        {
            pendulumMotion = () =>
            {
                degree = Math.Sin(t / 30) * 30;
            };

            sinXMotion = () =>
            {
                dx = -Math.Sin(t / 30) * 50;
                p.x = p0.x + dx;
            };

               
            createMotion = () => {
                scale = Math.Abs(Math.Sin(t / 100));
            };

            upwardMotion = () =>
            {
                if (wallObject != null) p.y = p0.y + 1.0 / 2 * Preferences.Accelation * t * t / 10000;
                if (!CeilingWindow.isValidHall && ceilingObject != null) p.z = p0.z + 1.0 / 2 * Preferences.Accelation * t * t / 10000;

            };

            freeFallMotion = () =>
            {
                scale = Math.Abs(Math.Sin(t / 70)) * 0.7 + 0.3;
            };

            opacityMotion = () =>
            {
                opacity = Math.Abs(Math.Sin(t / 70)) + 0.1;
            };

            sinYMotion = () =>
            {
                dz = -Math.Cos(t / 30) * 50;
                p.z = destCeilingHeight + dz;
            };

            moveToHallMotion = () =>
            {
                double left = p.x; // Canvas.GetLeft(ceilingObject);
                double top = p.z; // Canvas.GetTop(ceilingObject);

                if (isOpen && count == -1)
                {
                    distHallX = (targetX - left) / div;
                    distHallY = (targetY - top) / div;
                    count = 0;
                }
                p.x = left + distHallX;
                p.z = top + distHallY;
                p.y = p.z - wall.windowHeight;

                p0.x = p.x;
                p0.z = p.z;
                p0.y = p.y;
                
            };
        }

        void createAtWall()
        {
            // 벽면 객체 생성
            wallObject = new System.Windows.Controls.Image();
            wallObject.RenderTransformOrigin = new System.Windows.Point(0.5, 0);
            wallObject.Source = bubbleFrames[imageIndex];
            wallObject.Width = Preferences.ObjectWidth;
            wallObject.Height = Preferences.ObjectHeight;
            wallObject.RenderTransform = transformGroup;

            // 벽면 객체 위치 설정
            setLocation(wallObject, p0.x, p0.y);

            // 캔버스에 추가
            wallCanvas.Children.Add(wallObject);
        }

        void createAtCeiling()
        {
            // 벽면 객체 생성
            ceilingObject = new System.Windows.Controls.Image();
            ceilingObject.RenderTransformOrigin = new System.Windows.Point(0.5, 0);
            ceilingObject.Source = bubbleFrames[imageIndex];
            ceilingObject.Width = Preferences.ObjectWidth;
            ceilingObject.Height = Preferences.ObjectHeight;
            ceilingObject.RenderTransform = transformGroup;

            // 벽면 객체 위치 설정
            setLocation(ceilingObject, p.x, p.z);

            // 캔버스에 추가
            ceilingCanvas.Children.Add(ceilingObject);
        }


        public void animate()
        {
            Task uiTask = () =>
            {
                // Scale
                scaleTransform.ScaleX = scale;
                scaleTransform.ScaleY = scale;

                // Rotate
                rotateTransform.Angle = degree;

                if (wallObject != null)
                {
                    // 위치 이동
                    setLocation(wallObject, p.x, p.y);

                    // Transform 셋
                    wallObject.RenderTransform = transformGroup;

                    // 투명도
                    wallObject.Opacity = opacity; 
                }

                if (ceilingObject != null)
                {
                    // 위치 이동
                    setLocation(ceilingObject, p.x, p.z);

                    // Transform
                    ceilingObject.RenderTransform = transformGroup;

                    // 투명도
                    ceilingObject.Opacity = opacity;
                }

                wall.log.Content = t.ToString() + " y:" + p.y.ToString();
            };
            while (loopFlag)
            {
                t = ((System.DateTime.Now.Ticks - t0) / 100000); // -tGap; // 현재 시간 측정

                if (!isOpen && CeilingWindow.isValidHall)
                {
                    targetX = CeilingWindow.hallX - (Preferences.ObjectWidth / 2);
                    targetY = CeilingWindow.hallY - (Preferences.ObjectHeight / 2);
                    count = -1;
                    isOpen = true;
                }
                else if (!CeilingWindow.isValidHall)
                {
                    isOpen = false;
                    count = -1;
                }

                switch (state)
                {
                    case STATE_CREATING:
                        createMotion.Invoke();

                        if (t > 170)
                        {
                            state = STATE_UPWARD;
                            t0 = System.DateTime.Now.Ticks;
                        }
                        break;

                    case STATE_UPWARD:
                        upwardMotion.Invoke();
                        pendulumMotion.Invoke();
                        sinXMotion.Invoke();

                        if (p.y + Preferences.ObjectHeight <= 0)
                        {
                            wallCanvas.Dispatcher.Invoke(() =>
                            {
                                wallCanvas.Children.Remove(wallObject);
                                wallObject = null;
                            });
                        }

                        if (p.z <= destCeilingHeight)
                        {
                            state = STATE_FREE_FALL;
                            //t = System.DateTime.Now.Ticks;
                        }

                        if (CeilingWindow.isValidHall)
                        {
                            lastState = state;
                            state = STATE_MOVE_TO_HALL;
                            lastT = System.DateTime.Now.Ticks;
                            //targetX = CeilingWindow.hallX - (ceilingObject.Width / 2);
                            //targetY = CeilingWindow.hallY - (ceilingObject.Height / 2);
                        }

                        break;

                    case STATE_FREE_FALL:
                        pendulumMotion.Invoke();
                        sinXMotion.Invoke();
                        sinYMotion.Invoke();
                        freeFallMotion.Invoke();
                        opacityMotion.Invoke();

                        if (CeilingWindow.isValidHall)
                        {
                            lastState = state;
                            state = STATE_MOVE_TO_HALL;
                            lastT = System.DateTime.Now.Ticks;
                            //targetX = CeilingWindow.hallX - (ceilingObject.Width / 2);
                            //targetY = CeilingWindow.hallY - (ceilingObject.Height / 2);
                        }
                        break;

                    case STATE_MOVE_TO_HALL:
                        if (!CeilingWindow.isValidHall)
                        {
                            //tGap += (System.DateTime.Now.Ticks - lastT) / 100000; // ((System.DateTime.Now.Ticks - t0) / 100000) - lastT;
                            //t0 += tGap;
                            state = lastState;
                            //p0.x = p.x;
                            //p0.z = p.z;
                        }

                        pendulumMotion.Invoke();
                        moveToHallMotion.Invoke();
                        upwardMotion();

                        if (p.y + Preferences.ObjectHeight <= 0)
                        {
                            wallCanvas.Dispatcher.Invoke(() =>
                            {
                                wallCanvas.Children.Remove(wallObject);
                                wallObject = null;
                            });
                        }
                            

                        if (count == div)
                        {
                            ceiling.Dispatcher.Invoke(() =>
                            {
                                ceiling.CanvasForObject.Children.Remove(ceilingObject);
                            });

                            objectCount--;
                            
                            return;
                        }

                        count++;
                        break;
                }

                ceiling.Dispatcher.Invoke(uiTask);



                Thread.Sleep(Preferences.FPS);
            }
        }

        private void setLocation(System.Windows.Controls.Image wallObject, double x, double y)
        {
            Canvas.SetLeft(wallObject, x);
            Canvas.SetTop(wallObject, y);
        }

        bool increase = true;
        

        void up()
        {
            double distHallX = 0;
            double distHallY = 0;
            double targetX = 0, targetY = 0;
            bool isOpen = false;
            int count = -1;
            int div = 100;
            double x = 0;
            double y = 0;
            double diff = 0;

            bool creating = true;
            int creatingTimer = 0;
            try
            {
            
                for (int i = 0; true; i++)
                {
                    if (!isOpen &&  CeilingWindow.isValidHall)
                    {
                        targetX = ceiling.windowWidth - (CeilingWindow.hallX / ceiling.frameWidth * ceiling.windowWidth) - (Preferences.ObjectWidth/2);
                        targetY = (CeilingWindow.hallY / ceiling.frameHeight * ceiling.windowHeight) - (Preferences.ObjectWidth / 2);
                        count = -1;
                        isOpen = true;
                    }
                    else if(!CeilingWindow.isValidHall)
                    {
                        isOpen = false;
                        count = -1;
                    }

                    wallCanvas.Dispatcher.Invoke(() =>
                    {
                        if (wallObject != null)
                        {
                            if (creating)
                            {
                                Thickness margin = wallObject.Margin;

                                wallObject.Width += 1;
                                wallObject.Height += 1;

                                margin.Left -= 0.5;
                                margin.Top -= 0.5;
                                wallObject.Margin = margin;

                                creatingTimer++;

                                if (creatingTimer == 190)
                                {
                                    creating = false;
                                }

                                
                            }
                            else
                            {
                                Thickness margin = wallObject.Margin;
                                margin.Top -= 2;

                                rotateTransform.Angle += 0.1;
                                wallObject.RenderTransform = rotateTransform;
                                wallObject.Margin = margin;
                            }

                            // wall.log.Content = margin.Top.ToString();
                        }


                        

                        
                        if (ceilingObject != null)
                        {
                            /*if (CeilingWindow.initialDepth != null && CeilingWindow.closerDepth != Int32.MaxValue)
                            {
                                if (Utils.isChangedDepthOnCeiling(CeilingWindow.closerDepth,
                                    CeilingWindow.closerDepthIndex,
                                    CeilingWindow.initialDepth))
                                {
                                    Thickness margin = ceilingEllipse.Margin;


                                    if (CeilingWindow.closerDepth != Int32.MaxValue && !isOpen && diff > 200)
                                    {
                                        isOpen = true;

                                        // 현재 좌표
                                        distHallX = (x - margin.Left) / 150;
                                        distHallY = (y - margin.Top) / 150;
                                        
                                    }
                                    
                                    if (isOpen && diff > 200)
                                    {
                                        // 이동
                                        margin.Left += distHallX;
                                        margin.Top += distHallY;
                                        count++;
                                        ceilingEllipse.Margin = margin;
                                        if (count == 150)
                                        {
                                            ceiling.CanvasForObject.Children.Remove(ceilingEllipse);
                                            return;
                                        }
                                    }
                                    else
                                    {
                                        isOpen = false;
                                    }


                                    

                                    

                                    //일단 방향 1씩 이동

                                    //MarkerIndex.Content = string.Format("{0}, {1}, depth {2}", (int)x, (int)y, currentFrameCloser);
                                    //Canvas.SetLeft(DepthMarker, x);
                                    //Canvas.SetTop(DepthMarker, y);
                                }
                            } */

                            // 홀로 들어갈 때
                            if (CeilingWindow.isValidHall)
                            {
                                double left = Canvas.GetLeft(ceilingObject);
                                double top = Canvas.GetTop(ceilingObject);

                                if (isOpen && count == -1)
                                {
                                    distHallX = (targetX - left) / div;
                                    distHallY = (targetY - top) / div;
                                    count = 0;
                                }

                                Canvas.SetLeft(ceilingObject, left + distHallX);
                                Canvas.SetTop(ceilingObject, top + distHallY);

                                Position.x = Canvas.GetLeft(ceilingObject);
                                Position.z = Canvas.GetTop(ceilingObject);

                                if (count == div)
                                {
                                    ceiling.CanvasForObject.Children.Remove(ceilingObject);
                                    return;
                                }
                                count++;

                                


                            }

                            ///
                            /// 천장에서 상하 운동 하는 경우
                            ///
                            else
                            {
                                isOpen = false;

                                if (Position.z > Canvas.GetTop(ceilingObject))
                                {
                                    if (increase)
                                    {
                                        Scale += Preferences.ScaleDiff;

                                        if (Scale > 1.8)
                                        {
                                            increase = false;
                                        }
                                    }
                                    else
                                    {
                                        Scale -= Preferences.ScaleDiff;

                                        if (Scale < 1)
                                        {
                                            increase = true;
                                        }
                                    }

                                    ceilingObject.Width = Preferences.ObjectWidth * Scale;
                                    ceilingObject.Height = Preferences.ObjectHeight * Scale;

                                    //ceilingEllipse.Effect = new BlurEffect();
                                    ceilingObject.Opacity = (Scale - 0.7) / 2;

                                    double width = ceilingObject.Width;
                                    double height = ceilingObject.Height;

                                    // 위치 조정
                                    Canvas.SetLeft(ceilingObject, Position.x + (Preferences.ObjectWidth - width) / 2);
                                    Canvas.SetTop(ceilingObject, Position.z + (Preferences.ObjectHeight - height) / 2);
                                    // 스케일 조정
                                    //ceilingEllipse.Tran
                                }
                                else
                                {
                                    Canvas.SetTop(ceilingObject, Canvas.GetTop(ceilingObject)- 2);
                                }
                            }
                        
                        }

                        if (wallObject != null && wallObject.Margin.Top + Preferences.ObjectHeight < 0)
                        {
                            wallObject = null;
                        }

                        if (!upFlag && wallObject.Margin.Top < 0)
                        {
                            Thickness margin = wallObject.Margin;
                            ceilingObject = new System.Windows.Controls.Image();
                            //ceilingEllipse.Fill = new SolidColorBrush(Colors.White);
                            ceilingObject.Source = bubbleFrames[imageIndex];
                            ceilingObject.Width = Preferences.ObjectWidth;
                            ceilingObject.Height = Preferences.ObjectHeight;

                            ceiling.CanvasForObject.Children.Add(ceilingObject);

                            Thickness wallMargin = wallObject.Margin;

                            Canvas.SetLeft(ceilingObject, wallMargin.Left / wall.Width * ceiling.Width);
                            Canvas.SetTop(ceilingObject, ceiling.Height - Preferences.Distance);

                            Position.x = Canvas.GetLeft(ceilingObject);
                            upFlag = true;
                        }
                    });

                    Thread.Sleep(Preferences.FPS);
                }

            }
            catch (TaskCanceledException e)
            {
               
            }
        }

        public static Bitmap[] bubbles = {
                                             Properties.Resources.bubbles_01,
                                             Properties.Resources.bubbles_02,
                                             Properties.Resources.bubbles_03,
                                             Properties.Resources.bubbles_04,
                                             Properties.Resources.bubbles_05,
                                         };

        public static BitmapFrame[] bubbleFrames = null;
    }

}
