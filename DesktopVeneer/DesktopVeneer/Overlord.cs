using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using System.IO;
using System.Security.Permissions;
using System.Timers;
using Extender.Drawing;
using Extender.Debugging;

namespace DesktopVeneer
{
    /// <remarks>
    /// Helper class to track when changes to the background have been made, 
    /// and update the forms.
    /// </remarks>
    public class Overlord
    {
        protected FileSystemWatcher  Watcher;
        protected Veneer[]           Veneers;
        protected DateTime       FileChanged;

        protected System.Timers.Timer RefreshTimer;

        protected Wall DesktopBackground
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the current number of connected and enabled screens from
        /// Screen.AllScreens.
        /// </summary>
        public int ScreenCount
        {
            get
            {
                return Screen.AllScreens.Count();
            }
        }

        /// <summary>
        /// Minimum time in milliseconds to wait between calls to refresh veneers.
        /// In theory this cuts down on flicker.
        /// </summary>
        public double RefreshDelay
        {
            get;
            set;
        }

        /// <summary>
        /// Minimum time in milliseconds between events raised by the 'Watcher' 
        /// FileSystemWatcher.
        /// </summary>
        public double MinFilewatcherDelay
        {
            get;
            set;
        }
        //
        // TODO It would be a better idea to have the first WatcherEvent start a timer
        //      (like how RequestRefresh works) and keep trying to do a refresh until 
        //      TimeSinceFileChEvent > MinFilewatcherDelay.

        /// <summary>
        /// Time to wait between a detected file change and executing the required
        /// action.
        /// </summary>
        protected int FileChDelay
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the total milliseconds elapsed since the last event raised by the
        /// 'Watcher' FileSystemWatcher.
        /// </summary>
        public double TimeSinceFileChEvent
        {
            get
            {
                return (DateTime.Now - FileChanged).TotalMilliseconds;
            }
        }

        public Overlord()
        {
            Watcher             = new FileSystemWatcher();
            FileChanged         = new DateTime();
            DesktopBackground   = new Wall();
            MinFilewatcherDelay = 800;
            RefreshDelay        = 1000;
            FileChDelay         = 2500;
            InitWatchers();
        }

        /// <summary>
        /// Attemps to refresh and update all veneers (desktop overlays).
        /// If the attempt fails a timer is started, and another refresh 
        /// is attempted on each interval until it is successful.
        /// </summary>
        public void RequestRefresh()
        {
            if (!RefreshVeneers())
                RefreshTimer.Enabled = true;
        }

        protected bool RefreshVeneers()
        {
            // If a display has been added or removed we want to re-build
            // the Veneers array from scratch.
            if(Veneers.Length != ScreenCount)
            {
                BuildVeneers();
                return false;
            }

            if (DesktopBackground.Reload())
            {
                for (int i = 0; i < Veneers.Length; i++)
                {
                    Veneers[i].DisposeImages();
                    Veneers[i].WallpaperSlice = DesktopBackground.SliceFor(i);
                    Veneers[i].InvokeReFill();
                }
            }
            else
            {
                foreach (Veneer v in Veneers)
                    v.InvalidateBackground();
                return false;
            }

            RefreshTimer.Enabled = false;
            return true;
        }

        protected void BuildVeneers()
        {
            DesktopBackground.Reload();            
            DestroyVeneers(); // Make sure any old forms get closed

            Veneers = new Veneer[ScreenCount];
            for(int i = 0; i < ScreenCount; i++)
            {
                Veneers[i]  = new Veneer(i, DesktopBackground.SliceFor(i));
                Veneers[i].Show();
            }
        }

        /// <summary>
        /// Starts the FileSystemWatch to monitor for background changes, and immediately 
        /// (re)initializes all veneers.
        /// </summary>
        public void BeginWatch()
        {
            Watcher.EnableRaisingEvents = true;
            BuildVeneers(); 
        }

        /// <summary>
        /// Stops the FileSystemWatch responsible for monitoring the desktop background.
        /// </summary>
        /// <param name="destroyVeneers">
        /// If true EndWatch will attempt to close all veneer windows immediately after
        /// disabling the FileSystemWatcher.
        /// </param>
        public void EndWatch(bool destroyVeneers)
        {
            Watcher.EnableRaisingEvents = false;
            if (destroyVeneers)
                DestroyVeneers();
        }

        protected void InitWatchers()
        {
            this.Watcher        = new FileSystemWatcher();
            this.Watcher.Path   = Directory.GetParent(this.DesktopBackground.DesktopBackgroundPath)
                                           .FullName;

            this.Watcher.NotifyFilter = NotifyFilters.LastWrite;
            Watcher.Changed += Watcher_OnChange;

            this.RefreshTimer = new System.Timers.Timer(RefreshDelay);
            this.RefreshTimer.Elapsed += RefreshTimer_Elapsed;
            this.RefreshTimer.Enabled = false;
        }

        private void RefreshTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            this.RequestRefresh();
        }

        protected void Watcher_OnChange(object sender, FileSystemEventArgs e)
        {
            Debug.WriteMessage(": " + DateTime.Now.Millisecond + "ms", "watcher event");

            // Don't request refresh if a request has already been made within
            // ('MinFilewatcherDelay') milliseconds.
            if (TimeSinceFileChEvent > MinFilewatcherDelay)
            {
                // Hide the veneers while we wait for windows to transition between backgrounds
                foreach (Veneer v in this.Veneers) v.InvokeHide();
                System.Threading.Thread.Sleep(FileChDelay);
                // If we don't wait here then Wall will try to load the TranscodedWallpaper
                // while Windows is still writing to it, causing all kinds of fun problems.

                this.RequestRefresh();
                this.FileChanged = DateTime.Now;
                Debug.WriteMessage("Action triggered", "watcher event");
            }
        }

        protected void DestroyVeneers()
        {
            if(Veneers != null)
            foreach(Veneer v in Veneers)
            {
                if (v != null)
                    v.Close();
            }
        }
    }
}
