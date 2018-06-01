using log4net;
using LOL.AcceptQueue.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LOL.AcceptQueue
{
    public class MonitorApplicationContext : ApplicationContext
    {
        private NotifyIcon _TrayIcon;
        private MonitorService _MonitorService;
        MenuItem _OnOffMenuItem;
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            _OnOffMenuItem?.Dispose();
            _TrayIcon?.Dispose();

            if(_MonitorService != null)
            {
                _MonitorService.MonitorStatusHasChanged -= _MonitorService_MonitorStatusHasChanged;
            }
            _MonitorService?.Dispose();

            _MonitorService = null;
            _OnOffMenuItem = null;
            _TrayIcon = null;
        }

        public MonitorApplicationContext()
        {
            // Initialize Tray Icon
            _OnOffMenuItem = new MenuItem("On/Off", TurnOnOff);
            _TrayIcon = new NotifyIcon()
            {
                Icon = Resources.Accept,
                ContextMenu = new ContextMenu(new MenuItem[]
                                             {
                                                 _OnOffMenuItem,
                                                 new MenuItem("Exit", Exit),
                                             }),
                Visible = true
            };

            try
            {
                _MonitorService = new MonitorService();
                _MonitorService.MonitorStatusHasChanged += _MonitorService_MonitorStatusHasChanged;
                _MonitorService.Initialize();
            }
            catch (Exception exception)
            {
                log.Error("Start", exception);
            }
        }

        private void _MonitorService_MonitorStatusHasChanged(object sender, bool currentlyMonitoring)
        {
            _OnOffMenuItem.Checked = currentlyMonitoring;
        }

        void TurnOnOff(object sender, EventArgs e)
        {
            // Hide tray icon, otherwise it will remain shown until user mouses over it
            var menuItem = sender as MenuItem;
            if (menuItem.Checked)
            {
                _MonitorService.UserEnabledOrDisabled = false;
            }
            else
            {
                _MonitorService.UserEnabledOrDisabled = true;
            }
        }

        void Exit(object sender, EventArgs e)
        {
            // Hide tray icon, otherwise it will remain shown until user mouses over it
            _TrayIcon.Visible = false;
            Application.Exit();
        }
    }

}
