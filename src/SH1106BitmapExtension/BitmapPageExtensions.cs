using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace SecretNest.Hardware.IO
{
    public static class BitmapPageExtensions
    {
        public static byte[] ToSH1106Pages(this Bitmap bitmap, int start, int length)
        {
            var width = bitmap.Width;
            var yStart = start * 8;
            var yEndPlusOne = (start + length) * 8;

            var array = new BitArray(width * length * 8);

            int index = 0;
            for (int y = yStart; y < yEndPlusOne; y += 8)
            {
                for (int x = 0; x < width; x++)
                {
                    for (int yOffset = 0; yOffset < 8; yOffset++)
                    {
                        array[index++] = bitmap.GetPixel(x, y + yOffset).IsBlack();
                    }
                }
            }

            var result = new byte[width * length];
            array.CopyTo(result, 0);

            return result;
        }

        static bool IsBlack(this Color color)
        {
            return color.R == 0 && color.G == 0 && color.B == 0;
        }
    }
}
