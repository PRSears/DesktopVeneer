using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using System.IO;
using System.Security.Permissions;
using Extender.Drawing;
using System.Timers;

namespace DesktopVeneer
{
    [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
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

        public int ScreenCount
        {
            get
            {
                return Screen.AllScreens.Count();
            }
        }

        public double RefreshDelay
        {
            get;
            set;
        }

        public Overlord()
        {
            Watcher             = new FileSystemWatcher();
            FileChanged         = new DateTime();
            DesktopBackground   = new Wall();
            RefreshDelay        = 1000;
            InitWatchers();
        }

        public void RequestRefresh()
        {
            if (!RefreshVeneers())
                RefreshTimer.Enabled = true;
        }

        private bool RefreshVeneers()
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

        public void BeginWatch()
        {
            Watcher.EnableRaisingEvents = true;
            BuildVeneers(); 
        }

        public void EndWatch()
        {
            Watcher.EnableRaisingEvents = false;
        }

        protected void InitWatchers()
        {
            this.Watcher        = new FileSystemWatcher();
            this.Watcher.Path   = Directory.GetParent(this.DesktopBackground.DesktopBackgroundPath)
                                           .FullName;

            this.Watcher.NotifyFilter =
                NotifyFilters.LastAccess |
                NotifyFilters.LastWrite  |
                NotifyFilters.Size;
            Watcher.Changed += Watcher_OnChange;

            this.RefreshTimer = new System.Timers.Timer(RefreshDelay);
            this.RefreshTimer.Elapsed += RequestRefresh;
            this.RefreshTimer.Enabled = false;
        }

        private void RequestRefresh(object sender, ElapsedEventArgs e)
        {
            this.RequestRefresh();
        }

        protected void Watcher_OnChange(object sender, FileSystemEventArgs e)
        {
            this.RequestRefresh();
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
