using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KonkukCommunicationDesign
{
    class Preferences
    {
        public static float ObjectWidth = 200;
        public static float ObjectHeight = 200;

        public static int FPS = 12;
        public static float Distance = 0;

        public static double Accelation = -500.0;

        public static double ConflictAccelation = -700;

        public static double ScaleDiff = 0.02;

        public static double RecognizingDepth = 100; //100;
        public static int WallRecognizingDepth = 300;

        public static string CeilingBackgroundPath = "";
        public static int WallRecogCountBase = 40000;
        public static int GenerationGap = 2000;

        public static int ValidHallCount = 10000;

        public static int MaxObjectCount = 30;
        
    }
}
