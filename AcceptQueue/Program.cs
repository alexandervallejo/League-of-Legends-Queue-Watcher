using log4net;
using log4net.Config;
using System;
using System.Collections.Generic;
using System.Configuration.Install;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

[assembly: log4net.Config.XmlConfigurator(ConfigFile = "Log.config", Watch = true)]

namespace LOL.AcceptQueue
{
    static class Program
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public const string ServiceName = "League Of Legends Accept Queue";

        static Mutex mutex = new Mutex(true, "{8F6F0AC4-B9A1-45fd-A8CF-72F04E6BDE8F}");
        [STAThread]
        static void Main()
        {
            if (mutex.WaitOne(TimeSpan.Zero, true))
            {
                XmlConfigurator.Configure();
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MonitorApplicationContext());
                mutex.ReleaseMutex();
            }
            //else
            //{
            //    MessageBox.Show(ServiceName + "is already running.");
            //}
        }
    }
}
