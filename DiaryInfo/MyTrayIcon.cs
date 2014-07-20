using System;
using System.Windows.Forms;
using System.Drawing;
using System.Diagnostics;
using System.Reflection;

namespace DiaryInfo
{
    public delegate void OptionsHandlerDelegate(string user, string password, string timeout);

    class MyTrayIcon : Form
    {
        private NotifyIcon  trayIcon;
        private ContextMenu trayMenu;
        private DiaryRuClient client = new DiaryRuClient();
        private Timer myTimer = new Timer();
        private Icon defaultIcon = Resource.Icon1;
        private Icon attentionIcon = Resource.Icon2;

        /// <summary>
        /// Event Handler for Timer
        /// </summary>
        /// <param name="myObject"></param>
        /// <param name="myEventArgs"></param>
        private void TimerEventProcessor(Object myObject, EventArgs myEventArgs) {
            myTimer.Stop();
            DoRequest();
            myTimer.Enabled = true;
        }

        /// <summary>
        /// Start Timer with interval
        /// </summary>
        /// <param name="secunds">time interval in secunds</param>
        private void StartTimer(int secunds)
        {
            myTimer.Interval = secunds * 1000;
            myTimer.Start();
        }

        /// <summary>
        /// Do request with DiaryRu Client
        /// </summary>
        private void DoRequest() 
        {
            try
            {
                DiaryRuInfo data = this.client.GetInfo();
                string sdata = data.ToString();
                if (data.HasError())
                {
                    MessageBox.Show(sdata);
                }
                trayIcon.BalloonTipText = sdata;
                trayIcon.ShowBalloonTip(5 * 1000);
                trayIcon.Text = sdata;
                if (data.IsEmpty())
                {
                    SetDefaultIcon();
                }
                else
                {
                    SetAttentionIcon();
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
                SetDefaultIcon();
            }
        }

        /// <summary>
        /// Set default tray Icon
        /// </summary>
        public void SetDefaultIcon()
        {
            trayIcon.Icon = defaultIcon;
        }

        /// <summary>
        /// Set attention tray icon
        /// </summary>
        public void SetAttentionIcon()
        {
            trayIcon.Icon = attentionIcon;
        }

        /// <summary>
        /// Create NotifyIcon for tray and context menu for it.
        /// Also load Icons.
        /// </summary>
        public void CreateIconAndMenu()
        {
            // Create a simple tray menu with only one item.
            trayMenu = new ContextMenu();
            trayMenu.MenuItems.Add("Read Favorites", OnReadFavorite);
            trayMenu.MenuItems.Add("Read Umails", OnReadUmails);
            trayMenu.MenuItems.Add("Read Discuss", OnReadComments);
            trayMenu.MenuItems.Add("Read Comments", OnReadComments);
            trayMenu.MenuItems.Add("-");
            trayMenu.MenuItems.Add("Check manually", OnCheckManually);
            trayMenu.MenuItems.Add("Authorize", OnAuth);
            trayMenu.MenuItems.Add("Exit", OnExit);
            // Create a tray icon. In this example we use a
            // standard system icon for simplicity, but you
            // can of course use your own custom icon too.
            trayIcon = new NotifyIcon();
            trayIcon.Text = "DiaryInfo";
            trayIcon.MouseClick += new MouseEventHandler(trayIconMouseClick);
            trayIcon.MouseUp += new MouseEventHandler(trayIconMouseUpClick);
            // Add menu to tray icon and show it.
            trayIcon.ContextMenu = trayMenu;
            trayIcon.Visible = true;
            myTimer.Tick += new EventHandler(TimerEventProcessor);
            SetDefaultIcon();
        }

        public MyTrayIcon()
        {
            CreateIconAndMenu();
        }
 
        protected override void OnLoad(EventArgs e)
        {
            Visible       = false; // Hide form window.
            ShowInTaskbar = false; // Remove from taskbar.
            base.OnLoad(e);
            OnAuth(null, null);
        }
 
        private void OnExit(object sender, EventArgs e)
        {
            myTimer.Stop();
            this.Close();
        }

        /// <summary>
        /// Authorize ContextMenu event handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnAuth(object sender, EventArgs e)
        {
            myTimer.Stop();
            AuthForm form = new AuthForm(new OptionsHandlerDelegate(ReceiveAuthData));
            form.Show();
        }

        /// <summary>
        /// Chack manually ContextMenu event handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnCheckManually(object sender, EventArgs e) {
            myTimer.Stop();
            DoRequest();
            myTimer.Start();
        }
        
        /// <summary>
        /// Received auth data from other form event handler. Auth for DiaryRu, start Timer.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="password"></param>
        /// <param name="timeout"></param>
        private void ReceiveAuthData(string user, string password, string timeout) {
            this.client.Auth(user, password);
            DoRequest();
            StartTimer(Convert.ToInt32(timeout));
        }

        /// <summary>
        /// NotifyIcon Mouse Up Event Handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void trayIconMouseUpClick(object sender, MouseEventArgs e)
        {
            ;
        }

        /// <summary>
        /// NotifyIcon Mouse One Click Event Handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void trayIconMouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                MethodInfo mi = typeof(NotifyIcon).GetMethod("ShowContextMenu", BindingFlags.Instance | BindingFlags.NonPublic);
                mi.Invoke(trayIcon, null);
            }
        }

        /// <summary>
        /// Read Umails ContextMenu event handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnReadUmails(object sender, EventArgs e)
        {
            Process.Start("http://www.diary.ru/u-mail/");
        }

        /// <summary>
        /// Read Comments ContextMenu event handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnReadComments(object sender, EventArgs e)
        {
            Process.Start("http://www.diary.ru/");
        }

        /// <summary>
        /// Read Favorite ContextMenu event handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnReadFavorite(object sender, EventArgs e)
        {
            Process.Start("http://diary.ru/?favorite");
        }
        
        protected override void Dispose(bool isDisposing)
        {
            if (isDisposing)
            {
                // Release the icon resource.
                trayIcon.Dispose();
                myTimer.Dispose();
            }
 
            base.Dispose(isDisposing);
        }
    }
}
