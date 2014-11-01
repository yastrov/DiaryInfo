using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Drawing;
using System.Reflection;
using System.Net;
using System.Collections.Generic;

namespace DiaryInfo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private DiaryInfoSettings settings = new DiaryInfoSettings();
        private NotifyIcon trayIcon = null;
        private System.Windows.Forms.ContextMenu trayMenu = null;
        private DiaryRuClient client = new DiaryRuClient();
        private Timer myTimer = null;
        private Icon defaultIcon = DiaryInfo.Properties.Resources.MainIcon;
        private Icon attentionIcon = DiaryInfo.Properties.Resources.InfoIcon;
        private const int BALOON_TIP_SHOW_DELAY = 2 * 1000;
        private static string DefaultTrayTitle = "DiaryInfo";
        private static string CANT_DECODE_RESPONSE = "Can't decode response from remote server.";

        public MainWindow()
        {
            InitializeComponent();
            /*Attention!
            It may be only here, because you have problem with Hide(), Task and Black Wndow! */
            createTrayIcon();
            myTimer = new Timer();
            myTimer.Tick += new EventHandler(TimerEventProcessor);
            SaveCookiesCheckBox.IsChecked = settings.SaveCookiesToDisk;
            client.Timeout = settings.TimeoutForWebRequest;
            usernameTextBox.Focus();
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Author: Yuri Astrov").Append("Version: ").Append(typeof(MainWindow).Assembly.GetName().Version.ToString());
            AuthorLabel.Content = sb.ToString();
            if (this.client.LoadCookies())
            {
                Hide();
                myTimer.Interval = settings.TimerForRequest;
                Task _task = DoRequestAsync();
                myTimer.Start();
            }
            else
                this.Focus();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
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
                    //Activate();
                //}
            };
            // Add menu to tray icon and show it.
            trayIcon.ContextMenu = trayMenu;
            trayIcon.Visible = true;
            SetDefaultIcon();
        }

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
        #endregion
        #region IconEventProcessor
        public void OnReadFavoriteClick(object sender, EventArgs e)
        {
            SetDefaultIcon();
            try
            {
                Process.Start("http://diary.ru/?favorite");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message, MainWindow.DefaultTrayTitle, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void OnReadUmailsClick(object sender, EventArgs e)
        {
            SetDefaultIcon();
            try
            {
                Process.Start("http://www.diary.ru/u-mail/");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message, MainWindow.DefaultTrayTitle, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void OnReadCommentsClick(object sender, EventArgs e)
        {
            SetDefaultIcon();
            try
            {
                Process.Start("http://www.diary.ru/");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message, MainWindow.DefaultTrayTitle, MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
            new AboutWindow().Show();
        }
        #endregion
        #region EventProcessor
        private async void TimerEventProcessor(object sender, EventArgs e)
        {
            await DoRequestAsync();
        }
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            settings.SaveCookiesToDisk = (bool)SaveCookiesCheckBox.IsChecked;
            settings.Save();
            if (SaveCookiesCheckBox.IsChecked == true)
            {
                if (!this.client.SaveCookies())
                    ;
                //System.Windows.MessageBox.Show("Failed to save cookies!", MainWindow.DefaultTrayTitle, MessageBoxButton.OK, MessageBoxImage.Error); ;
            }
            else
            {
                if (System.IO.File.Exists(DiaryRuClient.CookiesFileName))
                    System.IO.File.Delete(DiaryRuClient.CookiesFileName);
            }
            base.OnClosing(e);
            trayIcon.Visible = false;
        }

        private async void OkButtonClick(object sender, EventArgs e)
        {
            this.Hide();
            try
            {
                await this.client.AuthSecureAsync(usernameTextBox.Text, passTextBox.SecurePassword);
                await DoRequestAsync();
                myTimer.Interval = GetTimeFromTimeoutComboBox();
                myTimer.Start();
            }
            catch (WebException ex) {
                StringBuilder sb = new StringBuilder();
                sb.Append("Application is disabled. You can Authorize from menu in system tray, or exit from application.")
                .Append("\n").Append(ex.Message);
                System.Windows.MessageBox.Show(sb.ToString());
                this.Show();
            }  
        }
        private void ExitButtonClick(object sender, EventArgs e)
        {
            if (myTimer != null) myTimer.Stop();
            Close();
        }

        private void TimerButtonClick(object sender, EventArgs e)
        {
            myTimer.Interval = GetTimeFromTimeoutComboBox();
            myTimer.Start();
        }
        #endregion

        #region Do request to service
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
                    SetDefaultIcon(CANT_DECODE_RESPONSE);
                    System.Windows.MessageBox.Show(CANT_DECODE_RESPONSE, MainWindow.DefaultTrayTitle, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                string sdata = data.ToString();
                if (sdata != trayIcon.Text)
                {
                    if (data.HasError())
                    {
                        System.Windows.MessageBox.Show(sdata, MainWindow.DefaultTrayTitle, MessageBoxButton.OK, MessageBoxImage.Warning);
                        SetDefaultIcon(sdata);
                        this.OnAuthClick(null, null);
                    }
                    else
                    {
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
            }
            catch (WebException e)
            {
                System.Windows.MessageBox.Show(e.Message, MainWindow.DefaultTrayTitle, MessageBoxButton.OK, MessageBoxImage.Warning);
                string message = e.Message;
                if (message.Length > 63)
                    message = message.Substring(0, 63);
                SetDefaultIcon(message);
                // String length must be < 64
            }
            catch (Exception e)
            {
                System.Windows.MessageBox.Show(e.ToString(), MainWindow.DefaultTrayTitle, MessageBoxButton.OK, MessageBoxImage.Warning);
                myTimer.Stop();
                this.Close();
            }
        }
        #endregion

        #region Combobox
        Dictionary<string, int> comboboxData = new Dictionary<string, int>();

        private void timeoutComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            comboboxData.Add("1 minute",    60000);
            comboboxData.Add("5 minute",   300000);
            comboboxData.Add("10 minute",  600000);
            comboboxData.Add("15 minute",  800000);
            comboboxData.Add("20 minute", 1100000);
            comboboxData.Add("25 minute", 1400000);
            comboboxData.Add("30 minute", 1700000);
            comboboxData.Add("35 minute", 2000000);
            comboboxData.Add("40 minute", 2300000);
            comboboxData.Add("45 minute", 2600000);
            comboboxData.Add("1 hour",    3400000);
            timeoutComboBox.ItemsSource = comboboxData;
            SetTimeoutComboboxByValue(settings.TimerForRequest);
        }

        private int GetTimeFromTimeoutComboBox()
        {
            KeyValuePair<string, int> item = (KeyValuePair<string, int>)timeoutComboBox.SelectedItem;
            settings.TimerForRequest = item.Value;
            return item.Value;
        }

        private void SetTimeoutComboboxByValue(int value)
        {
            if (value == 300000)
            {
                timeoutComboBox.SelectedIndex = 1;
                return;
            }
            int i = -1;
            foreach(KeyValuePair<string, int> val in comboboxData)
            { 
                i++;
                if(val.Value == value)
                {
                    break;
                }
            }
            timeoutComboBox.SelectedIndex = i;
        }
        #endregion
    }
}