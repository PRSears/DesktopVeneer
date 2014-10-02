using Extender.Debugging;
using Extender.Exceptions;
using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace DesktopVeneer
{   
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
        
        private DateTime LastFallTime;

        public Veneer(int screenIndex, Image wallpaperSlice)
        {
            ScreenIndex         = screenIndex;
            WallpaperSlice      = wallpaperSlice;

            InitializeComponent();
            InitializeComponent_extended();
            Subscribe();
            ReFill();
        }

        protected void InitializeComponent_extended()
        {
            this.LastFallTime       = new DateTime();
            this.BackColor          = Color.LimeGreen;
            this.TransparencyKey    = Color.LimeGreen;

            this.Text = String.Format("Veneer {0}", ScreenIndex.ToString("D2"));
        }

        private void Subscribe()
        {
            this.Enter          += Veneer_Enter;
            this.Shown          += Veneer_Shown;
            this.Activated      += Veneer_Activated;
            this.HandleCreated  += Veneer_HandleCreated;
            this.ParentChanged  += Veneer_ParentChanged;
            this.VisibleChanged += Veneer_VisibleChanged;
        }

        public void Build(int screenIndex, Image wallpaperSlice)
        {
            this.ScreenIndex = screenIndex;
            this.WallpaperSlice = wallpaperSlice;
        }

        protected static Bitmap DefaultBackground_Load()
        {
            System.Reflection.Assembly exeAsembly = System.Reflection.Assembly.GetExecutingAssembly();
            Stream imageStream = exeAsembly.GetManifestResourceStream("DesktopVeneer.default.bmp");
            return new Bitmap(imageStream);
        }

        protected Image WallpaperSliceOrDefault()
        {
            return this.WallpaperSlice != null ? this.WallpaperSlice : Veneer.DefaultBackground;
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            if(this.BackgroundImage != this.WallpaperSlice)
                this.BackgroundImage = WallpaperSliceOrDefault();

            #region base.OnPaintBackground(e)...
            try
            {
                base.OnPaintBackground(e);
            }
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
           this.SetStyle(ControlStyles.DoubleBuffer | 
              ControlStyles.UserPaint | 
              ControlStyles.AllPaintingInWmPaint,
              true);
           this.UpdateStyles();
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
        }

        private void MakeVisible()
        {
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

            this.Location = new Point
                (
                    Screen.AllScreens[this.ScreenIndex].Bounds.X,
                    Screen.AllScreens[this.ScreenIndex].Bounds.Y
                );
            this.Maximize();

            this.BackgroundImage = this.WallpaperSlice;
            this.BackgroundImageLayout = ImageLayout.Center;

            if (this.Visible == false)
                this.MakeVisible();

        }

        public void Maximize()
        {
            if(!this.WindowState.Equals(FormWindowState.Maximized))
                this.WindowState = FormWindowState.Maximized;
        }

        /// <summary>
        /// Disposes the current background, and automatically hides the form.
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

        #region SendToBack Event handlers
        void Veneer_ParentChanged(object sender, EventArgs e)
        {
            this.InvokeFall();
        }

        void Veneer_Shown(object sender, EventArgs e)
        {
            this.InvokeFall();
        }

        void Veneer_Enter(object sender, EventArgs e)
        {
            this.InvokeFall();
        }

        void Veneer_VisibleChanged(object sender, EventArgs e)
        {
            this.InvokeFall();
        }

        void Veneer_MouseEnter(object sender, EventArgs e)
        {
            this.InvokeFall();
        }

        void Veneer_Activated(object sender, EventArgs e)
        {
            this.InvokeFall();
        }

        void Veneer_Paint(object sender, PaintEventArgs e)
        {
            this.InvokeFall();
        }

        void Veneer_HandleCreated(object sender, EventArgs e)
        {
            this.InvokeFall();
        }
        #endregion

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
            if((DateTime.Now - LastFallTime).TotalMilliseconds > 5)
                this.SendToBack();
        }
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            SetWindowLong
                (
                    this.Handle, 
                    GWL.ExStyle,
                    GetWindowLong(this.Handle, GWL.ExStyle) | (int)WS_EX.Layered | (int)WS_EX.Transparent
                );
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
        public static extern int GetWindowLong(IntPtr hWnd, GWL nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
        public static extern int SetWindowLong(IntPtr hWnd, GWL nIndex, int dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetLayeredWindowAttributes")]
        public static extern bool SetLayeredWindowAttributes(IntPtr hWnd, int crKey, byte alpha, LWA dwFlags);


        public static void Test_Harness()
        {
            Veneer v = new Veneer(0, Veneer.DefaultBackground);
        }
    }
}
