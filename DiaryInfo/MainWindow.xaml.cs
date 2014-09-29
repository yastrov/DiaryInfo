using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Forms;
using System.Drawing;
using System.Reflection;
using System.Net;

namespace DiaryInfo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private NotifyIcon trayIcon = null;
        private System.Windows.Forms.ContextMenu trayMenu = null;
        private DiaryRuClient client = null;
        private Timer myTimer = null;
        private Icon defaultIcon = null;//DiaryInfo.Properties.Resources.Icon1;
        private Icon attentionIcon = null;
        private const int BALOON_TIP_SHOW_DELAY = 3 * 1000;
        private const int SECUNDS_FROM_MILI_MULTIPLIER = 1000;
        private static string DefaultTrayTitle = "DiaryInfo";

        public MainWindow()
        {
            InitializeComponent();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            createTrayIcon();
            myTimer = new Timer();
            myTimer.Tick += new EventHandler(TimerEventProcessor);
            client = new DiaryRuClient();
            usernameTextBox.Focus();

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Author: Yuri Astrov").Append("Version: ").Append(typeof(MainWindow).Assembly.GetName().Version.ToString());
            AuthorLabel.Content = sb.ToString();
        }

        #region TrayIcon
        private void createTrayIcon()
        {
            trayMenu = new System.Windows.Forms.ContextMenu();
            trayMenu.MenuItems.Add("Read Favorites", OnReadFavoriteClick);
            trayMenu.MenuItems.Add("Read Umails", OnReadUmailsClick);
            trayMenu.MenuItems.Add("Read Discuss", OnReadCommentsClick);
            trayMenu.MenuItems.Add("Read Comments", OnReadCommentsClick);
            trayMenu.MenuItems.Add("-");
            trayMenu.MenuItems.Add("Check manually", OnCheckManuallyClick);
            trayMenu.MenuItems.Add("Authorize", OnAuthClick);
            trayMenu.MenuItems.Add("About", OnAboutClick);
            trayMenu.MenuItems.Add("Exit", OnExitClick);

            trayIcon = new NotifyIcon();
            trayIcon.Text = DefaultTrayTitle;
            trayIcon.Click += delegate(object sender, EventArgs e)
            {
                /*if ((e as System.Windows.Forms.MouseEventArgs).Button == System.Windows.Forms.MouseButtons.Left)
                {
                    MethodInfo mi = typeof(NotifyIcon).GetMethod("ShowContextMenu", BindingFlags.Instance | BindingFlags.NonPublic);
                    mi.Invoke(trayIcon, null);
                }
                else
                {*/
                    MethodInfo mi = typeof(NotifyIcon).GetMethod("ShowContextMenu", BindingFlags.Instance | BindingFlags.NonPublic);
                    mi.Invoke(trayIcon, null);
                    Activate();
                //}
            };
            // Add menu to tray icon and show it.
            trayIcon.ContextMenu = trayMenu;
            trayIcon.Visible = true;

            Font font = new Font("Helvetica", 22);
            defaultIcon = CreateIconFromText("D", font, System.Drawing.Color.Red);
            attentionIcon = CreateIconFromText("D", font, System.Drawing.Color.Green);
            trayIcon.Icon = defaultIcon;
        }

        protected static Icon CreateIconFromText(string text, Font font, System.Drawing.Color color)
        {
            Bitmap bitmap = new Bitmap(32, 32);//, System.Drawing.Imaging.PixelFormat.Max);
            System.Drawing.Brush brush = new SolidBrush(color);
            Graphics graphics = Graphics.FromImage(bitmap);
            graphics.DrawString(text, font, brush, 0, 0);
            IntPtr hIcon = bitmap.GetHicon();
            return System.Drawing.Icon.FromHandle(hIcon);
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
        #endregion
        #region IconEventProcessor
        public void OnReadFavoriteClick(object sender, EventArgs e)
        {
            trayIcon.Text = DefaultTrayTitle;
            Process.Start("http://diary.ru/?favorite");
        }

        public void OnReadUmailsClick(object sender, EventArgs e)
        {
            trayIcon.Text = DefaultTrayTitle;
            Process.Start("http://www.diary.ru/u-mail/");
        }

        public void OnReadCommentsClick(object sender, EventArgs e)
        {
            trayIcon.Text = DefaultTrayTitle;
            Process.Start("http://www.diary.ru/");
        }

        public async void OnCheckManuallyClick(object sender, EventArgs e)
        {
            await DoRequestAsync();
        }

        public void OnAuthClick(object sender, EventArgs e)
        {
            myTimer.Stop();
            Show();
            usernameTextBox.Focus();
        }

        private void OnExitClick(object sender, EventArgs e)
        {
            if (myTimer != null) myTimer.Stop();
            trayIcon.Visible = false;
            Activate();
            this.Close();
        }
        private void OnAboutClick(object sender, EventArgs e)
        {
            new AboutBox1().Show();
        }
        #endregion
        #region EventProcessor
        private async void TimerEventProcessor(object sender, EventArgs e)
        {
            await DoRequestAsync();
        }
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);
            trayIcon.Visible = false;
        }

        private async void OkButtonClick(object sender, EventArgs e)
        {
            try
            {
                await this.client.AuthAsync(usernameTextBox.Text, passTextBox.Password);
                await DoRequestAsync();
                if (timeoutTextBox.Text.Equals("300"))
                    myTimer.Interval = 300000;
                else
                    myTimer.Interval = Convert.ToInt32(timeoutTextBox.Text) * SECUNDS_FROM_MILI_MULTIPLIER;
                myTimer.Start();
                this.Hide();
            }
            catch (WebException ex) {
                StringBuilder sb = new StringBuilder();
                sb.Append("Application is disabled. You can Authorize from menu in system tray, or exit from application.")
                .Append("\n").Append(ex.Message);
                System.Windows.MessageBox.Show(sb.ToString());
            }  
        }
        private void ExitButtonClick(object sender, EventArgs e)
        {
            if (myTimer != null) myTimer.Stop();
            Close();
        }

        private void TimerButtonClick(object sender, EventArgs e)
        {
            myTimer.Interval = Convert.ToInt32(timeoutTextBox.Text) * SECUNDS_FROM_MILI_MULTIPLIER;
        }
        #endregion

        /// <summary>
        /// Do request with DiaryRu Client
        /// </summary>
        private async Task DoRequestAsync()
        {
            try
            {
                DiaryRuInfo data = await client.GetInfoAsync();
                if (data == null)
                {
                    SetDefaultIcon();
                    trayIcon.Text = "Can't decode response from remote server.";
                    System.Windows.Forms.MessageBox.Show("Can't decode response from remote server.");
                    return;
                }
                string sdata = data.ToString();
                if (sdata != trayIcon.Text)
                {
                    if (data.HasError())
                        System.Windows.Forms.MessageBox.Show(sdata);
                    trayIcon.Text = sdata;
                    if (data.IsEmpty())
                    {
                        SetDefaultIcon();
                    }
                    else
                    {
                        trayIcon.BalloonTipText = sdata;
                        trayIcon.ShowBalloonTip(BALOON_TIP_SHOW_DELAY);
                        SetAttentionIcon();
                    }
                }
            }
            catch (WebException e)
            {
                System.Windows.MessageBox.Show(e.Message);
                SetDefaultIcon();
                // String length must be < 64
                trayIcon.Text = "Exception: Can't receive response from remote server.";
            }
            catch (Exception e)
            {
                System.Windows.MessageBox.Show(e.ToString());
                myTimer.Stop();
                this.Close();
            }
        }
    }
}
