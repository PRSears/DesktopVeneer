using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Drawing.Imaging;
using System.Drawing;
using System.Windows.Forms;
using Extender.Drawing;

namespace DesktopVeneer
{
    // TODO Implement IDisposable?
    // TOOD Switch to caching temp images/slices in Themes folder to cut memory useage.
    public class Wall
    {
        protected static string SystemBackgroundPath = @"Microsoft\Windows\Themes\TranscodedWallpaper";

        private string _DesktopBackgroundPath = string.Empty;
        public string DesktopBackgroundPath
        {
            get
            {
                if (_DesktopBackgroundPath.Equals(string.Empty))
                {
                    _DesktopBackgroundPath = Path.Combine
                        (
                            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                            SystemBackgroundPath
                        );
                }
                
                return _DesktopBackgroundPath;
            }
            private set
            {
                _DesktopBackgroundPath = value;
            }
        }

        public Bitmap TranscodedWallpaper
        {
            get;
            protected set;
        }

        private Bitmap _ScaledWallpaper;
        public Bitmap ScaledWallpaper
        {
            get
            {
                if (_ScaledWallpaper == null)
                    _ScaledWallpaper = TranscodedWallpaper.ResizeFill(AbsoluteScreenSize);
                return _ScaledWallpaper;
            }
            protected set
            {
                _ScaledWallpaper = TranscodedWallpaper.ResizeFill(AbsoluteScreenSize);
            }
        }

        public Size AbsoluteScreenSize
        {
            get
            {
                int sumWidth = Screen.AllScreens
                                .Select
                                (
                                    screen => screen.Bounds.Width
                                )
                                .Aggregate
                                (
                                    (w1, w2) => w1 + w2
                                );

                int sumHeight = Screen.AllScreens
                                .Select
                                (
                                    screen => screen.Bounds.Height
                                )
                                .Aggregate
                                (
                                    (w1, w2) => w1 + w2
                                );

                if(sumWidth > sumHeight)
                {
                    return new Size
                        (
                            sumWidth,
                            Screen.AllScreens.Max(s => s.Bounds.Bottom)
                        );
                }
                else
                {
                    return new Size
                        (
                            Screen.AllScreens.Max(s => s.Bounds.Right),
                            sumHeight
                        );
                }

            }
        }

        public Wall()
        {
            // hack to force the value to generate for the first time.
            this.DesktopBackgroundPath  = this.DesktopBackgroundPath; 
            this.Reload();
        }

        // TODO Write system to invalidate image when background changes
        //      Force Bitmaps to reload/regen

        /// <summary>
        /// Loads the image at %AppData%\TranscodedWallpaper, stores it in this.TranscodedWallpaper, 
        /// and returns the same Image.
        /// Called automatically by constructor.
        /// Do not call if you don't need to update the Image stored in this.TranscodedWallpaper.
        /// </summary>
        public bool Reload()
        {
            this.FreeImages();

            try
            {
                this.TranscodedWallpaper = Bitmaps.FromFile(this.DesktopBackgroundPath);
            }
            catch(IOException e)
            {
                Console.WriteLine(Extender.Exceptions.ExceptionTools.CreateExceptionText(e, true));
                return false;
            }

            this.ScaledWallpaper = ScaledWallpaper; // forces a re-scale with new TranscodedWallpaper
            return true;
        }

        /// <summary>
        /// Gets the portion of TranscodedWallpaper which is shown on the Screen at index 'forIndex'.
        /// </summary>
        public Bitmap SliceFor(int forIndex)
        {
            #region deprecated
            //Bitmap ScaledImage = (NeedsScaling()) ? 
            //    new Bitmap(this.TranscodedWallpaper, CalculateScaleSize()) :
            //    new Bitmap(this.TranscodedWallpaper);

            //Console.WriteLine("\nSlicing wallpaper: " + this.DesktopBackgroundPath);
            //DEBUG_WriteRect("Full image bounds:", new Rectangle(0, 0, this.TranscodedWallpaper.Width, this.TranscodedWallpaper.Height));
            //DEBUG_WriteRect("Scaled bounds: ", new Rectangle(0, 0, ScaledImage.Width, ScaledImage.Height));
            //DEBUG_WriteRect(" > Cropping at", ConvertToAbsoluteBounds(Screen.AllScreens[forIndex].Bounds));

            //DEBUG_WriteRect("\n Screen" + forIndex + " bounds: ", Screen.AllScreens[forIndex].Bounds);

            //return (ScaledImage).Clone
            //    (
            //        ConvertToAbsoluteBounds(Screen.AllScreens[forIndex].Bounds), 
            //        this.TranscodedWallpaper.PixelFormat
            //    );
            #endregion

            return ScaledWallpaper.Clone
                (
                    ConvertToAbsoluteBounds(Screen.AllScreens[forIndex].Bounds),
                    this.ScaledWallpaper.PixelFormat
                );
        }

        protected void FreeImages()
        {
            if (TranscodedWallpaper != null)
            {
                this.TranscodedWallpaper.Dispose();
                this.TranscodedWallpaper = null;
            }

            if (_ScaledWallpaper != null)
            {
                this._ScaledWallpaper.Dispose();
                this._ScaledWallpaper = null;
            }
        }

        protected bool NeedsScaling()
        {
            return 
                (
                    (this.TranscodedWallpaper.Width != AbsoluteScreenSize.Width) ||
                    (this.TranscodedWallpaper.Height != AbsoluteScreenSize.Height)
                );
        }

        protected Size CalculateScaleSize()
        {
            if (!NeedsScaling())
                return this.TranscodedWallpaper.Size;

            Size scaleFitWidth  = FitWidth(TranscodedWallpaper.Size, AbsoluteScreenSize);
            Size scaleFitHeight = FitHeight(TranscodedWallpaper.Size, AbsoluteScreenSize);

            // Fit width
            if(AbsoluteScreenSize.Width > AbsoluteScreenSize.Height)
            {
                // Make sure the scaled dimensions are tall enough
                if (!(scaleFitWidth.Height < AbsoluteScreenSize.Height))
                    return scaleFitWidth;
                else
                    return scaleFitHeight;
            }
            // Fit height
            else
            {
                // Make sure the scaled dimensions are wide enough
                if (!(scaleFitHeight.Width < AbsoluteScreenSize.Width))
                    return scaleFitHeight;
                else
                    return scaleFitWidth;
            }
        }

        protected Size FitWidth(Size imageSize, Size screenSize)
        {
            float multiplier = ((float)screenSize.Width) / ((float)imageSize.Width);

            return new Size
                (
                    screenSize.Width,
                    (int)(Math.Round(imageSize.Height * multiplier))
                );
        }

        protected Size FitHeight(Size imageSize, Size screenSize)
        {
            float multiplier = ((float)screenSize.Height) / ((float)imageSize.Height);

            return new Size
                (
                    (int)Math.Round(imageSize.Width * multiplier),
                    screenSize.Height
                );
        }
        
        public float ScreenAspectRatio
        {
            get
            {
                return AbsoluteScreenSize.Width / AbsoluteScreenSize.Height;
            }
        }

        public float WallpaperAspectRatio
        {
            get
            {
                return TranscodedWallpaper.Size.Width / TranscodedWallpaper.Size.Height;
            }
        }

        protected Rectangle ConvertToAbsoluteBounds(Rectangle relativeBounds)
        {
            Point Origin = new Point
                (
                    Screen.AllScreens.Min(s => s.Bounds.Left),
                    Screen.AllScreens.Min(s => s.Bounds.Top)
                );

            return new Rectangle
                (
                    relativeBounds.X - Origin.X,
                    relativeBounds.Y - Origin.Y,
                    relativeBounds.Width,
                    relativeBounds.Height
                );
        }

        #region Deprecated
        [Obsolete]
        public static void DEBUG_WriteRect(Rectangle rect)
        {
            Console.WriteLine(String.Format
                (
                    "({0}, {1}) width: {2}, height: {3}",
                    rect.X,
                    rect.Y,
                    rect.Width,
                    rect.Height
                ));
        }

        [Obsolete]
        public static void DEBUG_WriteRect(string message, Rectangle rect)
        {
            Console.Write(String.Format("{0} ", message));
            DEBUG_WriteRect(rect);
        }
        [Obsolete]
        public static string DEBUG_RectToString(Rectangle r)
        {
            return String.Format
                (
                    "({0}, {1}) [Width {2}, Height {3}]",
                    r.X, r.Y,
                    r.Width, r.Height
                );
        }
        #endregion

        public static void Test_Harness()
        {
            Wall t = new Wall();

            Console.WriteLine("Image size          : (" + t.TranscodedWallpaper.Size.Width + ", " + t.TranscodedWallpaper.Size.Height + ")");
            Console.WriteLine("Absolute screen size: (" + t.AbsoluteScreenSize.Width + ", " + t.AbsoluteScreenSize.Height + ")");
            Console.WriteLine("Scaled image size   : (" + t.CalculateScaleSize().Width + ", " + t.CalculateScaleSize().Height + ")");

            for(int i = 0; i < Screen.AllScreens.Count(); i++)
            {
                Bitmap slice = t.SliceFor(i);
                slice.Save("slice" + i + ".bmp");
                slice.Dispose();
            }

            //Console.WriteLine("Slice size          : (" + slice1.Width + ", " + slice1.Height + ")");
        }
    }
}
