using System;
using System.Windows;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;
using Extender.Drawing;
using Extender.Debugging;

namespace DesktopVeneer
{
    [Obsolete]
    public static class BitmapResize
    {
        #region Moved to Extender library
        //    /// <summary>
    //    /// Creates a copy of this Bitmap and resizes it.
    //    /// Does not presevce aspect ratio, or performs any translations.
    //    /// </summary>
    //    /// <param name="newSize">The target size to scale the new Bitmap to.</param>
    //    /// <returns>Resized copy of this Bitmap.</returns>
    //    public static Bitmap Resize(this Bitmap image, Size newSize)
    //    {
    //        if (image.Size.Equals(newSize))
    //            return image;

    //        return new Bitmap(image, newSize);
    //    }

    //    /// <summary>
    //    /// Creates a copy of this Bitmap and resizes it.
    //    /// Preserves the original aspect ratio while resizing and cropping
    //    /// to fill the target size.
    //    /// </summary>
    //    /// <param name="targetSize">
    //    /// The target size to scale the new bitmap to.
    //    /// The resulting bitmap will fill the entire area of targetSize.
    //    /// </param>
    //    /// <returns>Resized copy of this Bitmap</returns>
    //    public static Bitmap ResizeFill(this Bitmap image, Size targetSize)
    //    {
    //        if (image.Size.Equals(targetSize))
    //            return image;

    //        Size scaleFitWidth  = BitmapResize.FitWidthSize(image.Size, targetSize);
    //        Size scaleFitHeight = BitmapResize.FitHeightSize(image.Size, targetSize);

    //        Bitmap scaled;

    //        //
    //        // Scale to...
    //        // Fit width
    //        if (targetSize.Width > targetSize.Height)
    //        {
    //            // Make sure the scaled dimensions are tall enough
    //            if (!(scaleFitWidth.Height < targetSize.Height))
    //                scaled = new Bitmap(image, scaleFitWidth);
    //            else
    //                scaled = new Bitmap(image, scaleFitHeight);
    //        }
    //        // Fit height
    //        else
    //        {
    //            // Make sure the scaled dimensions are wide enough
    //            if (!(scaleFitHeight.Width < targetSize.Width))
    //                scaled = new Bitmap(image, scaleFitHeight);
    //            else
    //                scaled = new Bitmap(image, scaleFitWidth);
    //        }

    //        //
    //        // Crop to fit targetSize if neccessary
    //        if (scaled.Size.Equals(targetSize))
    //            return scaled;
    //        else
    //            return scaled.CropCenteredFill(targetSize);
    //    }

    //    /// <summary>
    //    /// Creates a copy of this Bitmap, cropped and centered to fill as much of
    //    /// targetDimensions as possible.
    //    /// </summary>
    //    /// <param name="image"></param>
    //    /// <param name="targetDimensions"></param>
    //    /// <returns></returns>
    //    public static Bitmap CropCenteredFill(this Bitmap image, Size targetDimensions)
    //    {
    //        return image.Clone
    //            (
    //                GetCropBoundsCentered(image, targetDimensions),
    //                image.PixelFormat
    //            );
    //    }

    //    private static Rectangle GetCropBoundsCentered(Rectangle imageBounds, Rectangle targetBounds)
    //    {
    //        if (imageBounds.CompareAreaTo(targetBounds) < 0)
    //            throw new ArgumentOutOfRangeException("imageBounds is smaller than the targetBounds.");
    //        else if (imageBounds.CompareAreaTo(targetBounds) == 0)
    //            return imageBounds;

    //        Offset offset = new Offset(imageBounds, targetBounds);

    //        return new Rectangle
    //            (
    //                imageBounds.X + offset.HalfX,
    //                imageBounds.Y + offset.HalfY,
    //                targetBounds.Width,
    //                targetBounds.Height
    //            );
    //    }

    //    private static Rectangle GetCropBoundsCentered(Bitmap image, Size targetSize)
    //    {
    //        return GetCropBoundsCentered
    //            (
    //                new Rectangle(0, 0, image.Width, image.Height),
    //                new Rectangle(0, 0, targetSize.Width, targetSize.Height)
    //            );
    //    }

    //    private static Size FitWidthSize(Size imageSize, Size targetSize)
    //    {
    //        float multiplier = ((float)targetSize.Width) / ((float)imageSize.Width);

    //        return new Size
    //            (
    //                targetSize.Width,
    //                (int)(Math.Round(imageSize.Height * multiplier))
    //            );
    //    }

    //    private static Size FitHeightSize(Size imageSize, Size targetSize)
    //    {
    //        float multiplier = ((float)targetSize.Height) / ((float)imageSize.Height);

    //        return new Size
    //            (
    //                (int)Math.Round(imageSize.Width * multiplier),
    //                targetSize.Height
    //            );
        //    }
        #endregion

        public static void Test_Harness()
        {
            Rectangle a = new Rectangle(0, 0, 8525, 1606);
            Rectangle b = new Rectangle(0, 0, 6528, 1606);

            Rectangle c = (new Bitmap(a.Width, a.Height)).GetCenteredCropBounds(b.Size);

            Console.WriteLine(String.Format
                (
                    "Rect a: {0}\nRect b: {1}\n\n > Crop  : {2}",
                    a.ToDebugString(),
                    b.ToDebugString(),
                    c.ToDebugString()
                ));
        }
    }

    #region Extensions & Helpers [Deprecated]
    /*
    public static class RectangleCenter
    {
        public static Point GetCenter(this Rectangle rect)
        {
            return new Point
                    (
                        (int)Math.Round(rect.Width / 2d),
                        (int)Math.Round(rect.Height / 2d)
                    );
        }

        public static int CompareAreaTo(this Rectangle a, Rectangle b)
        {
            return a.Size.CompareAreaTo(b.Size);
        }
    }

    public static class SizeComparator
    {
        public static int CompareAreaTo(this Size a, Size b)
        {
            int a_area = a.Width * a.Height;
            int b_area = b.Width * b.Height;

            if (a_area > b_area)
                return 1;
            else if (a_area == b_area)
                return 0;
            else
                return -1;
        }
    }

    public static class PointOffset
    {
        public static Offset GetOffset(this Point a, Point b)
        {
            return new Offset
                (
                    a.X - b.X,
                    a.Y - b.Y
                );
        }
    }

    public class Offset
    {
        public int X
        {
            get;
            set;
        }

        public int Y
        {
            get;
            set;
        }

        public int HalfX
        {
            get
            {
                return (int)Math.Round(this.X / 2f);
            }
        }

        public int HalfY
        {
            get
            {
                return (int)Math.Round(this.Y / 2f);
            }
        }

        public Offset(int x, int y)
        {
            this.X = x;
            this.Y = y;
        }

        public Offset(Point a, Point b)
        {
            this.X = a.GetOffset(b).X;
            this.Y = a.GetOffset(b).Y;
        }

        public Offset(Size a, Size b)
        {
            this.X = a.Width - b.Width;
            this.Y = a.Height - b.Height;
        }

        public Offset(Rectangle a, Rectangle b)
        {
            this.X = a.Right - b.Right;
            this.Y = a.Bottom - b.Bottom;
        }
    }
     */
    #endregion
}
