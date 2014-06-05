using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using System.IO;
using Extender.Drawing;

namespace DesktopVeneer
{
    static class Program
    {
        // TODO Add system tray icon to control pause/exit/restart/etc

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Overlord overlord = new Overlord();
            overlord.BeginWatch();

            Application.Run();
        }
    }
}
