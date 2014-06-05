using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.Timers;
using System.Runtime.InteropServices;
using System.IO;
using Extender.Exceptions;
using Extender.Debugging;

namespace DesktopVeneer
{
    //
    // TODO Re-write:
    //      Have Wall class save the cropped & scaled background to 
    //      %AppData%\Microsoft\Windows\Themes\Facade\. Generate all 
    //      slices for currently connected screens, saving them to the 
    //      same folder. Dispose of Wall object. (Perhaps a static class
    //      would be best suited for this purpose?)
    //
    // TODO Remove all timers from this class, let overlord handle it.
    // 

    public partial class Veneer : Form
    {
        public int ScreenIndex
        {
            get;
            set;
        }

        public Image WallpaperSlice
        {
            get;
            set;
        }
        protected static Bitmap DefaultBackground = Veneer.DefaultBackground_Load();

        private DateTime LastPaint;
        private System.Timers.Timer startupDelayTimer;
        private System.Timers.Timer reshowDelayTimer;
        private bool lapsed = false;
        public double InitialDelay
        {
            get;
            set;
        }

        public double ReshowInterval
        {
            get;
            set;
        }

        public double PaintDelay
        {
            get;
            set;
        }

        #region for debug...
        [Obsolete]
        public Veneer():this(0, null)
        {
        }
        #endregion

        public Veneer(int screenIndex, Image wallpaperSlice):this(screenIndex, wallpaperSlice, 1600, 1600, 0)
        {
        }
        
        public Veneer
            (int screenIndex, Image wallpaperSlice, double initialDelay, double reshowInterval, double paintDelay)
        {
            LastPaint           = new DateTime();
            InitialDelay        = initialDelay;
            ReshowInterval      = reshowInterval;
            PaintDelay          = paintDelay;
            ScreenIndex         = screenIndex;
            WallpaperSlice      = wallpaperSlice;

            InitializeComponent();
            InitializeComponent_extended();
            Subscribe();
            ReFill();
        }

        protected void InitializeComponent_extended()
        {
            this.BackColor = Color.LimeGreen;
            this.TransparencyKey = Color.LimeGreen;
        }

        private void Subscribe()
        {
            //startupDelayTimer = new System.Timers.Timer(InitialDelay);
            //startupDelayTimer.Elapsed += startupDelayTimer_Elapsed;
            //startupDelayTimer.Enabled = true;

            reshowDelayTimer = new System.Timers.Timer(ReshowInterval);
            reshowDelayTimer.Elapsed += reshowDelayTimer_Elapsed;
            reshowDelayTimer.Enabled = false;

            this.Enter          += Form1_Enter;
            this.Shown          += Form1_Shown;
            this.Activated      += Form1_Activated;
            this.HandleCreated  += Form1_HandleCreated;
            this.ParentChanged  += Veneer_ParentChanged;
            this.VisibleChanged += Form1_VisibleChanged;
        }

        void reshowDelayTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if(this.WallpaperSlice != null)
            {
                this.InvokeReshow();
            }
        }

        void startupDelayTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            //this.startupDelayTimer.Enabled = false;
            //this.lapsed = true;
            this.Invalidate();
        }


        public void Build(int screenIndex, Image wallpaperSlice)
        {
            this.ScreenIndex = screenIndex;
            this.WallpaperSlice = wallpaperSlice;
        }

        protected static Bitmap DefaultBackground_Load()
        {
            System.Reflection.Assembly exeAsembly = System.Reflection.Assembly.GetExecutingAssembly();
            Stream imageStream = exeAsembly.GetManifestResourceStream("DesktopFacade.default.bmp");
            return new Bitmap(imageStream);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            //if(!lapsed)
            //{
            //    this.BackgroundImage = Veneer.DefaultBackground;
            //    this.OnPaintBackground(e);
            //    return;
            //}

            if (TimeSinceLastPaint.TotalMilliseconds < PaintDelay)
                return;

            if(this.BackgroundImage != this.WallpaperSlice)
            {
                if (this.WallpaperSlice == null)
                    this.WallpaperSlice = Veneer.DefaultBackground;

                this.BackgroundImage = this.WallpaperSlice;
            }

            try
            {
                base.OnPaintBackground(e);
            }
            #region catch
            catch
            {
                // Don't sweat it if the paint fucks up
                Debug.WriteMessage
                    (
                        string.Format("Veneer{0} encountered an exception while painting.", ScreenIndex),
                        "warn"
                    );
            }
            #endregion
        }

        protected void EnableDoubleBuffering()
        {
           // Set the value of the double-buffering style bits to true. 
           this.SetStyle(ControlStyles.DoubleBuffer | 
              ControlStyles.UserPaint | 
              ControlStyles.AllPaintingInWmPaint,
              true);
           this.UpdateStyles();
        }

        protected TimeSpan TimeSinceLastPaint
        {
            get
            {
                return (DateTime.Now - LastPaint);
            }
        }
            

        public void InvokeHide()
        {
            try
            {
                Invoke(new MethodInvoker(MakeInvisible));
            }
            catch(Exception e)
            {
                Console.WriteLine(ExceptionTools.CreateExceptionText(e, false));
            }
        }

        public void InvokeReshow()
        {
            try
            {
                Invoke(new MethodInvoker(MakeVisible));
            }
            catch(Exception e)
            {
                Console.WriteLine(ExceptionTools.CreateExceptionText(e, false));
            }
        }

        private void MakeInvisible()
        {
            this.Visible = false;
            this.reshowDelayTimer.Enabled = true;
        }

        private void MakeVisible()
        {
            this.reshowDelayTimer.Enabled = false;
            this.Invalidate();
            this.Visible = true;
        }

        public void InvokeReFill()
        {
            try
            {
                Invoke(new MethodInvoker(ReFill));
            }
            catch(Exception e)
            {
                Console.WriteLine(ExceptionTools.CreateExceptionText(e, false));
            }
        }

        private void ReFill()
        {
            if (WallpaperSlice == null || WallpaperSlice == Veneer.DefaultBackground)
                return;

            //this.WindowState = FormWindowState.Normal;
            this.Location = new Point
                (
                    Screen.AllScreens[this.ScreenIndex].Bounds.X,
                    Screen.AllScreens[this.ScreenIndex].Bounds.Y
                );
            this.Maximize();
            // THOUGHT Changing window state might cause flickering.
            //         I'm not sure if the location gets properly 
            //         updated if the window is maximized, however.

            this.BackgroundImage = this.WallpaperSlice;
            this.BackgroundImageLayout = ImageLayout.Center;
        }

        public void Maximize()
        {
            if(!this.WindowState.Equals(FormWindowState.Maximized))
                this.WindowState = FormWindowState.Maximized;
        }

        /// <summary>
        /// Disposes the current background, and automatically hides the form until 
        /// a valid image is assigned to this.WallpaperSlice.
        /// </summary>
        public void InvalidateBackground()
        {
            try
            {
                Invoke(new MethodInvoker(_InvalidateBackground));
            }
            catch (Exception e)
            {
                Console.WriteLine(ExceptionTools.CreateExceptionText(e, false));
            }
        }

        private void _InvalidateBackground()
        {
            this.InvokeHide();
            this.DisposeImages();
            this.WallpaperSlice = Veneer.DefaultBackground;
        }

        public void DisposeImages()
        {
            if((this.WallpaperSlice != null) || (this.WallpaperSlice == Veneer.DefaultBackground))
                this.WallpaperSlice.Dispose();
        }

        void Veneer_ParentChanged(object sender, EventArgs e)
        {
            this.InvokeFall();
        }

        void Form1_Shown(object sender, EventArgs e)
        {
            this.InvokeFall();
        }

        void Form1_Enter(object sender, EventArgs e)
        {
            this.InvokeFall();
        }

        void Form1_VisibleChanged(object sender, EventArgs e)
        {
            this.InvokeFall();
        }

        void Form1_MouseEnter(object sender, EventArgs e)
        {
            this.InvokeFall();
        }

        void Form1_Activated(object sender, EventArgs e)
        {
            this.InvokeFall();
        }

        void Form1_Paint(object sender, PaintEventArgs e)
        {
            this.InvokeFall();
        }

        void Form1_HandleCreated(object sender, EventArgs e)
        {
            this.InvokeFall();
        }

        public void InvokeFall()
        {
            try
            {
                Invoke(new MethodInvoker(SendDown));
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }        

        private void SendDown()
        {
            this.SendToBack();
        }


        [DllImport("user32", CharSet = CharSet.Auto, SetLastError = true, ExactSpelling = true)]
        protected static extern IntPtr GetWindow(IntPtr hwnd, int wFlag);

        protected int GetZOrder()
        {
            int z = 0;
            for (IntPtr h = this.Handle; h != IntPtr.Zero; h = GetWindow(h, 3)) z++;

            return z;
        }

        public enum GWL
        {
            ExStyle = -20
        }

        public enum WS_EX
        {
            Transparent = 0x20,
            Layered = 0x80000
        }

        public enum LWA
        {
            ColorKey = 0x1,
            Alpha = 0x2
        }

        [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
        public static extern int GetWIndowLong(IntPtr hWnd, GWL nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
        public static extern int SetWindowLong(IntPtr hWnd, GWL nIndex, int dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetLayeredWindowAttributes")]
        public static extern bool SetLayeredWindowAttributes(IntPtr hWnd, int crKey, byte alpha, LWA dwFlags);

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            int wl = GetWIndowLong(this.Handle, GWL.ExStyle);
            wl = wl | 0x80000 | 0x20;

            SetWindowLong(this.Handle, GWL.ExStyle, wl);
        }
    }
}
