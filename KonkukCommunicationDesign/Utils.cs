using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace KonkukCommunicationDesign
{
    class Utils
    {
        public static Size getScreenSize()
        {
            return new Size(System.Windows.SystemParameters.PrimaryScreenWidth,
                System.Windows.SystemParameters.PrimaryScreenHeight);
        }

        public static long TickToMillis(long tick)
        {
            return tick / 10000;
        }

        public static bool isChangedDepthOnCeiling(int depth, int index, int[] initialDepth)
        {
            if (initialDepth == null || index == -1) return false;
            return depth != 0  && initialDepth[index]!= 0 && Math.Abs(depth - initialDepth[index]) > Preferences.RecognizingDepth;
        }

        public static bool isChangedDepthOnWall(int depth, int index, int[] initialDepth)
        {
            return depth != 0 && initialDepth[index] != 0 && Math.Abs(depth - initialDepth[index]) > Preferences.WallRecognizingDepth;
        }
        public static void getRgb(Bitmap image, int startX, int startY, int w, int h, int[] rgbArray, int offset, int scansize)
        {

            int PixelWidth = 3;
            PixelFormat PixelFormat = PixelFormat.Format32bppRgb;

            if (image == null) throw new ArgumentException("image");
            if (rgbArray == null) throw new ArgumentNullException("rgbArray");
            if (startX < 0 || startX + w > image.Width) throw new ArgumentOutOfRangeException("startX");
            if (startY < 0 || startY + h > image.Height) throw new ArgumentOutOfRangeException("startY");
            if (w < 0 || w > scansize || w > image.Width) throw new ArgumentOutOfRangeException("w");
            if (h < 0 || (rgbArray.Length < offset + h * scansize) || h > image.Height) throw new ArgumentOutOfRangeException("h");


            BitmapData data = image.LockBits(new Rectangle(startX, startY, w, h), System.Drawing.Imaging.ImageLockMode.ReadOnly, PixelFormat);
            try
            {
                byte[] pixelData = new Byte[data.Stride];
                for (int scanline = 0; scanline < data.Height; scanline++)
                {
                    Marshal.Copy(data.Scan0 + (scanline * data.Stride), pixelData, 0, data.Stride);
                    for (int pixeloffset = 0; pixeloffset < data.Width; pixeloffset++)
                    {
                        // PixelFormat.Format32bppRgb means the data is stored
                        // in memory as BGR. We want RGB, so we must do some 
                        // bit-shuffling.
                        rgbArray[offset + (scanline * scansize) + pixeloffset] =
                            (pixelData[pixeloffset * PixelWidth + 2] << 16) +   // R 
                            (pixelData[pixeloffset * PixelWidth + 1] << 8) +    // G
                            pixelData[pixeloffset * PixelWidth];                // B
                    }
                }
            }
            finally
            {
                image.UnlockBits(data);
            }
        }

       
    }
}
