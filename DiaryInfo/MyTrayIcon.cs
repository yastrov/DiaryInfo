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
    public delegate void OptionsHandlerDelegate(string user, string password, int timeout);

    class MyTrayIcon : Form
    {
        private NotifyIcon  trayIcon;
        private ContextMenu trayMenu;
        private DiaryRuClient client = null;
        private Timer myTimer = null;
        private Icon defaultIcon = DiaryInfo.Properties.Resources.Icon1;
        private Icon attentionIcon = DiaryInfo.Properties.Resources.Icon2;
        private const int BALOON_TIP_SHOW_DELAY = 4 * 1000;
        public static string DefaultTrayTitle = "DiaryInfo";
        private static string CANT_DECODE_RESPONSE = "Can't decode response from remote server.";
        private bool authFormShowing = false;

        #region TimerRegion
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
        #endregion
        #region DoRequestAsync
        /// <summary>
        /// Do request with DiaryRu Client
        /// </summary>
        private async Task DoRequestAsync() 
        {
            try {
                DiaryRuInfo data = await client.GetInfoAsync();
                if (data == null)
                {
                    SetDefaultIcon(CANT_DECODE_RESPONSE);
                    MessageBox.Show(CANT_DECODE_RESPONSE, MyTrayIcon.DefaultTrayTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                string sdata = data.ToString();
                if (sdata != trayIcon.Text)
                {
                    if (data.HasError())
                    {
                        MessageBox.Show(sdata, MyTrayIcon.DefaultTrayTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        SetDefaultIcon(sdata);
                        return;
                    }
                    if (data.IsEmpty())
                    {
                        SetDefaultIcon(sdata);
                    }
                    else
                    {
                        SetAttentionIcon(sdata, sdata);
                    }
                }
            }
            catch (WebException e) {
                MessageBox.Show(e.Message, MyTrayIcon.DefaultTrayTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                string message = e.Message;
                if (message.Length > 63)
                    message = message.Substring(0, 63);
                SetDefaultIcon(message);
                // String length must be < 64
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString(), MyTrayIcon.DefaultTrayTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                myTimer.Stop();
                this.Close();
            }
        }
        #endregion
        #region IconRegion
        /// <summary>
        /// Set default tray Icon
        /// </summary>
        /// <param name="Text">Tray Icon Text</param>
        /// <param name="BaloonTipText">Baloon Text</param>
        public void SetDefaultIcon(string Text = null, string BaloonTipText = null)
        {
            trayIcon.Icon = defaultIcon;
            if (Text != null)
                trayIcon.Text = Text;
            else trayIcon.Text = DefaultTrayTitle;
            if (BaloonTipText != null)
            {
                trayIcon.BalloonTipText = BaloonTipText;
                trayIcon.ShowBalloonTip(BALOON_TIP_SHOW_DELAY);
            }
        }

        /// <summary>
        /// Set attention tray icon
        /// </summary>
        /// <param name="Text">Tray Icon Text</param>
        /// <param name="BaloonTipText">Baloon Text</param>
        public void SetAttentionIcon(string Text = null, string BaloonTipText = null)
        {
            trayIcon.Icon = attentionIcon;
            if (Text != null)
                trayIcon.Text = Text;
            else trayIcon.Text = DefaultTrayTitle;
            if (BaloonTipText != null)
            {
                trayIcon.BalloonTipText = BaloonTipText;
                trayIcon.ShowBalloonTip(BALOON_TIP_SHOW_DELAY);
            }
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
            trayIcon.MouseClick += new MouseEventHandler(trayIconMouseClick);
            // Add menu to tray icon and show it.
            trayIcon.ContextMenu = trayMenu;
            trayIcon.Visible = true;
            myTimer.Tick += new EventHandler(TimerEventProcessor);
            SetDefaultIcon();
        }
        #endregion

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
        #region EventHandlers
        /// <summary>
        /// Authorize ContextMenu event handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnAuth(object sender, EventArgs e)
        {
            if (authFormShowing)
                return;
            myTimer.Stop();
            AuthForm form = new AuthForm(new OptionsHandlerDelegate(ReceiveAuthData));
            form.StartPosition = FormStartPosition.CenterScreen;
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
        private async void ReceiveAuthData(string user, string password, int timeout) {
            authFormShowing = false;
            try
            {
                await this.client.AuthAsync(user, password);
                await DoRequestAsync();
                StartTimer(timeout);
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
            SetDefaultIcon();
            try
            {
                Process.Start("http://www.diary.ru/u-mail/");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// Read Comments ContextMenu event handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnReadComments(object sender, EventArgs e)
        {
            SetDefaultIcon();
            try
            {
                Process.Start("http://www.diary.ru/");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// Read Favorite ContextMenu event handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnReadFavorite(object sender, EventArgs e)
        {
            SetDefaultIcon();
            try
            {
                Process.Start("http://diary.ru/?favorite");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void OnAboutClick(object sender, EventArgs e)
        {
            new AboutWindow().Show();
        }
        #endregion

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
