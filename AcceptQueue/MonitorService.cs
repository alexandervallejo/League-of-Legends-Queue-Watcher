using log4net;
using System;
using System.Linq;
using System.Reflection;
using System.Timers;
using System.Drawing;
using System.Drawing.Imaging;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using System.IO;
using LOL.AcceptQueue.Properties;

namespace LOL.AcceptQueue
{
    public class MonitorService
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private bool _CurrentlyMonitoring = false;
        private bool _UserEnabledOrDisabled = true;
        System.Timers.Timer _MonitorTimer;
        Process _Paint, _LoLProcess;
        bool _LeagueWasRunning = false;


        public event EventHandler<bool> MonitorStatusHasChanged;

        #region Properties
        public bool UserEnabledOrDisabled
        {
            get { return _UserEnabledOrDisabled; }

            set
            {
                _UserEnabledOrDisabled = value;
                CurrentlyMonitoring = value;
            }
        }

        private bool CurrentlyMonitoring
        {
            get { return _CurrentlyMonitoring; }

            set
            {
                if (_CurrentlyMonitoring != value)
                {
                    _CurrentlyMonitoring = value;
                    if (_CurrentlyMonitoring)
                    {
                        _MonitorTimer.Interval = Settings.Default.IntervalToCheckForQueueSec * 1000;
                    }
                    else
                    {
                        //NOTE: Do not check as often if the monitoring has been turned off.
                        _MonitorTimer.Interval = Settings.Default.IntervalToCheckOutOfGameSec * 1000;
                    }
                    OnMonitorStatusHasChanged(_CurrentlyMonitoring);
                }
            }
        }
        #endregion

        #region Constructor/Initialization
        public MonitorService()
        {

        }

        public MonitorService(string[] args)
        {

        }

        public void Dispose()
        {
            try
            {
                _Paint?.Kill();
                _MonitorTimer.Elapsed -= _MonitorTimer_Elapsed;
                _MonitorTimer.Stop();

                _MonitorTimer = null;
                _Paint = null;
                _LoLProcess = null;
            }
            catch (Exception exception)
            {
                log.Error("Dispose", exception);
            }
        }

        public void Initialize()
        {
            this._MonitorTimer = new System.Timers.Timer();
            this._MonitorTimer.Interval = Settings.Default.IntervalToCheckForQueueSec * 1000;
            this._MonitorTimer.Elapsed += _MonitorTimer_Elapsed;
            this._MonitorTimer.AutoReset = false;
            this.CurrentlyMonitoring = true;
            this.MontiorFunctions();
        }
        #endregion

        #region Methods
        private async void MontiorFunctions()
        {
            try
            {
                Process[] processes = Process.GetProcesses().Where(x => x.ProcessName.Contains("League") && x.MainWindowHandle != IntPtr.Zero).ToArray();
                var processCount = processes.Count();
                var currentlyPlayingGame = processes.Any(x => x.ProcessName.Contains("League of Legends"));
                if (processCount > 0 &&
                   _LeagueWasRunning == false)
                {
                    log.Info("MontiorFunctions League of legends is now running.");
                    CurrentlyMonitoring = true;
                    _LeagueWasRunning = true;
                }
                else if (_UserEnabledOrDisabled == false ||
                         _CurrentlyMonitoring == false ||
                         processCount == 0 ||
                        currentlyPlayingGame)
                {
                    if(processCount == 0)
                    {
                        _LeagueWasRunning = false;
                    }

                    if (processCount > 0 && _UserEnabledOrDisabled == true &&
                        currentlyPlayingGame == false && _CurrentlyMonitoring == false)
                    {
                        //NOTE: If league is running, user did not disable the control,
                        //      we are not playing the game, and  CurrentlyMonitoring is off,
                        //      turn back on currently monitoring, because we have just finished the game.
                        CurrentlyMonitoring = true;
                    }
                    else
                    {
                        CurrentlyMonitoring = false;
                        return;
                    }
                }
                else
                {
                    CurrentlyMonitoring = true;
                }

                _LoLProcess = processes.FirstOrDefault(x => (x.ProcessName.Contains("LeagueClientUxRender") || x.ProcessName.Contains("LeagueClient")));
                if (_LoLProcess == null)
                {
                    log.Warn("MontiorFunctions was unable to find a non null main window in its league of legends processes.");
                    return;
                }
                IntPtr ptr = _LoLProcess.MainWindowHandle;
                //NOTE: The print window method does not work correctly when the applicaiton is minimized.
                if (Settings.Default.AllowMinimizingOfLeague == false)
                {
                    WindowFunctions.ShowWindow(_LoLProcess.MainWindowHandle, WindowFunctions.SW_SHOWNORMAL);
                }
                var image = PrintWindow(ptr);
                log.Debug($"MontiorFunctions - Read Image.");
                var screenCaptureIron = new Tesseract.TesseractEngine(@"./tessdata", "eng").Process(image);
                var screenCaptureIronText = screenCaptureIron.GetText();
                log.Debug($"MontiorFunctions - Result From Image = {screenCaptureIronText}");
                var expectedString = "ACCEPT";
                if (screenCaptureIronText.ToUpper().Contains(expectedString))
                {
                    WindowFunctions.SetForegroundWindow(_LoLProcess.MainWindowHandle);
                    await Task.Delay(500);

                    WindowFunctions.RECT LeagueRect;
                    WindowFunctions.GetWindowRect(ptr, out LeagueRect);
                    var right = (int)(Properties.Settings.Default.AcceptLocationX * (double)(LeagueRect.Left + LeagueRect.Right));
                    var bottom = (int)(Properties.Settings.Default.AcceptLocationY * (double)(LeagueRect.Top + LeagueRect.Bottom));
                    log.Debug($"LeftMouseClick center = {bottom} bottom = {right}");
                    WindowFunctions.LeftMouseClick(right, bottom);
                }
            }
            catch (Exception exception)
            {
                log.Error("MontiorFunctions", exception);
            }
            finally
            {
                this._MonitorTimer.Start();
            }
        }

        public Bitmap PrintWindow(IntPtr hwnd)
        {
            log.Info($"PrintWindow IntPtr = {hwnd.ToInt32()}");
            WindowFunctions.RECT rc;
            WindowFunctions.GetWindowRect(hwnd, out rc);
            Bitmap src = new Bitmap(rc.Width, rc.Height, PixelFormat.Format32bppArgb);
            Graphics gfxBmp = Graphics.FromImage(src);
            IntPtr hdcBitmap = gfxBmp.GetHdc();
            WindowFunctions.PrintWindow(hwnd, hdcBitmap, 0);
            gfxBmp.ReleaseHdc(hdcBitmap);
            gfxBmp.Dispose();
            //NOTE: To make the image smaller and less OCR needed take a smaller picture.
            var croppedWidth = (int)(rc.Width * .3);
            var croppedHeight = (int)(rc.Height * .3);
            return CroppedImage(src, croppedWidth, croppedHeight);
        }

        private Bitmap CroppedImage(Image image, int targetWidth, int targetHeight)
        {
            //crop the image from the specified location and size
            var startX = image.Width / 4;   //Starts from 25% from the left.
            var startY = image.Height / 2;  //Sarts from 50% from the top.
            var sourceRectangle = new Rectangle(startX, startY, targetWidth, targetHeight);

            //the future size of the image
            var bitmap = new Bitmap(targetWidth, targetHeight);

            //fill-in the whole bitmap
            var destinationRectangle = new Rectangle(0, 0, bitmap.Width, bitmap.Height);

            //generate the new image
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.DrawImage(image, destinationRectangle, sourceRectangle, GraphicsUnit.Pixel);
                graphics.Dispose();
            }

            if (Properties.Settings.Default.ShowWhatAppSees &&
                _Paint == null)
            {
                try
                {
                    log.Error("CroppedImage attempting to save and open an image.");
                    bitmap.Save("SavedImage.bmp", ImageFormat.Jpeg);
                    _Paint = new Process();
                    var paintPath = @"C:\Windows\system32\mspaint.exe";
                    _Paint.StartInfo.FileName = paintPath;
                    _Paint.StartInfo.Arguments = "SavedImage.bmp";
                    _Paint.Start();
                }
                catch (Exception ex)
                {
                    log.Error("CroppedImage attempted to save and open an image.", ex);
                }
            }

            return bitmap;
        }

        #endregion

        #region Events
        public void OnMonitorStatusHasChanged(bool monitorStopped)
        {
            MonitorStatusHasChanged?.Invoke(this, monitorStopped);
        }

        private void _MonitorTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            this.MontiorFunctions();
        }
        #endregion
    }
}

