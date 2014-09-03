using System;
using System.Windows.Forms;
using System.Drawing;
using System.Diagnostics;
using System.Reflection;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DiaryInfo
{
    public delegate void OptionsHandlerDelegate(string user, string password, string timeout);

    class MyTrayIcon : Form
    {
        private NotifyIcon  trayIcon;
        private ContextMenu trayMenu;
        private DiaryRuClient client = null;
        private Timer myTimer = null;
        private Icon defaultIcon = Resource.Icon1;
        private Icon attentionIcon = Resource.Icon2;
        private const int BALOON_TIP_SHOW_DELAY = 5 * 1000;

        /// <summary>
        /// Event Handler for Timer
        /// </summary>
        /// <param name="myObject"></param>
        /// <param name="myEventArgs"></param>
        private async void TimerEventProcessor(Object myObject, EventArgs myEventArgs) {
            myTimer.Stop();
            await DoRequestAsync();
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
        private async Task DoRequestAsync() 
        {
            try {
                DiaryRuInfo data = await client.GetInfoAsync();
                if (data == null) {
                    SetDefaultIcon();
                    trayIcon.Text = "Can't decode response from remote server.";
                    MessageBox.Show("Can't decode response from remote server.");
                    return;
                }
                string sdata = data.ToString();
                if (data.HasError())
                    MessageBox.Show(sdata);
                trayIcon.Text = sdata;
                if (data.IsEmpty()) {
                    SetDefaultIcon();
                }
                else {
                    trayIcon.BalloonTipText = sdata;
                    trayIcon.ShowBalloonTip(BALOON_TIP_SHOW_DELAY);
                    SetAttentionIcon();
                }
            }
            catch (WebException e) {
                MessageBox.Show(e.Message);
                SetDefaultIcon();
                trayIcon.Text = e.Message;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
                myTimer.Stop();
                this.Close();
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
            trayMenu.MenuItems.Add("About", OnAboutClick);
            trayMenu.MenuItems.Add("Exit", OnExit);
            // Create a tray icon. In this example we use a
            // standard system icon for simplicity, but you
            // can of course use your own custom icon too.
            trayIcon = new NotifyIcon();
            trayIcon.Text = "DiaryInfo";
            trayIcon.MouseClick += new MouseEventHandler(trayIconMouseClick);
            // Add menu to tray icon and show it.
            trayIcon.ContextMenu = trayMenu;
            trayIcon.Visible = true;
            myTimer.Tick += new EventHandler(TimerEventProcessor);
            SetDefaultIcon();
        }

        public MyTrayIcon()
        {
            ;
        }
 
        protected override void OnLoad(EventArgs e)
        {
            Visible       = false; // Hide form window.
            ShowInTaskbar = false; // Remove from taskbar.
            client = new DiaryRuClient();
            myTimer = new Timer();
            base.OnLoad(e);
            CreateIconAndMenu();
            OnAuth(null, null);
        }
 
        private void OnExit(object sender, EventArgs e)
        {
            if(myTimer != null) myTimer.Stop();
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
        private async void OnCheckManually(object sender, EventArgs e) {
            myTimer.Stop();
            await DoRequestAsync();
            myTimer.Start();
        }
        
        /// <summary>
        /// Received auth data from other form event handler. Auth for DiaryRu, start Timer.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="password"></param>
        /// <param name="timeout"></param>
        private async void ReceiveAuthData(string user, string password, string timeout) {
            try
            {
                await this.client.AuthAsync(user, password);
                await DoRequestAsync();
                StartTimer(Convert.ToInt32(timeout));
            }
            catch (WebException e) {
                StringBuilder sb = new StringBuilder();
                sb.Append("Application is disabled. You can Authorize from menu in system tray, or exit from application.")
                .Append("\n").Append(e.Message);
                MessageBox.Show(sb.ToString());
            }  
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

        private void OnAboutClick(object sender, EventArgs e)
        {
            new AboutWindow().Show();
        }

        protected override void Dispose(bool isDisposing)
        {
            if (isDisposing)
            {
                if (myTimer != null) myTimer.Dispose();
                if(defaultIcon != null) defaultIcon.Dispose();
                if(attentionIcon != null) attentionIcon.Dispose();
                if(trayMenu != null) trayMenu.Dispose();
                if(trayIcon != null) trayIcon.Dispose();
            }
            base.Dispose(isDisposing);
        }
    }
}
