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

        protected System.Timers.Timer FileChangeTimer;

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
        /// Time (in milliseconds) to wait after a FileChange event before the file
        /// can be considered inactive.
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
            MinFilewatcherDelay = 2200;
            InitWatchers();
        }

        /// <summary>
        /// Attemps to refresh and update all veneers (desktop overlays).
        /// If the attempt fails a timer is started, and another refresh 
        /// is attempted on each interval until it is successful.
        /// </summary>
        public void RequestRefresh()
        {
            int attempts = 0;
            while (!RefreshVeneers())
            {
                if (attempts++ > 10) return;
                System.Threading.Thread.Sleep(1000); 
                //
                // THOUGHT not sure if blocking here is a good idea.
            }
        }

        protected bool RefreshVeneers()
        {
            // If a display has been added or removed we want to re-build
            // the Veneers array from scratch.
            if(Veneers.Length != ScreenCount)
            {
                BuildVeneers();
                DesktopBackground.FreeImages();
                return true;
            }

            if (DesktopBackground.Reload())
            {
                for (int i = 0; i < Veneers.Length; i++)
                {
                    Veneers[i].DisposeImages();
                    Veneers[i].WallpaperSlice = DesktopBackground.SliceFor(i);
                    Veneers[i].InvokeReFill();
                }
                DesktopBackground.FreeImages();
                return true;
            }
            else
            {
                foreach (Veneer v in Veneers)
                    v.InvalidateBackground();
                return false;
            }
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
            Watcher.Changed          += Watcher_OnChange;


            FileChangeTimer          = new System.Timers.Timer(MinFilewatcherDelay);
            FileChangeTimer.Elapsed += FileChangeTimer_Elapsed;
            FileChangeTimer.Enabled  = false;
        }

        protected void FileChangeTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (TimeSinceFileChEvent < MinFilewatcherDelay)
                return;


            Debug.WriteMessage("Action triggered", "watcher event");
            this.RequestRefresh();
            FileChangeTimer.Enabled = false;
        }

        protected void Watcher_OnChange(object sender, FileSystemEventArgs e)
        {
            Debug.WriteMessage(": " + DateTime.Now.Millisecond + "ms", "watcher event");

            foreach (Veneer v in this.Veneers) v.InvokeHide();

            this.FileChanged = DateTime.Now;
            FileChangeTimer.Enabled = true;
        }

        protected void Watcher_OnChange_obs(object sender, FileSystemEventArgs e)
        {
            Debug.WriteMessage(": " + DateTime.Now.Millisecond + "ms", "watcher event");

            // Don't request refresh if a request has already been made within
            // ('MinFilewatcherDelay') milliseconds.
            if (TimeSinceFileChEvent > MinFilewatcherDelay)
            {
                // Hide the veneers while we wait for windows to transition between backgrounds
                foreach (Veneer v in this.Veneers) v.InvokeHide();
                //System.Threading.Thread.Sleep(FileChDelay);
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
