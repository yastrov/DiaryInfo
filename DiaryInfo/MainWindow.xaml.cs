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
using System.Windows.Threading;

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
        private DispatcherTimer myTimer = new DispatcherTimer();
        private Icon defaultIcon = DiaryInfo.Properties.Resources.MainIcon;
        private Icon attentionIcon = DiaryInfo.Properties.Resources.InfoIcon;
        private static string DefaultTrayTitle = "DiaryInfo";
        private static string CANT_DECODE_RESPONSE = "Can't decode response from remote server.";
        private Boolean isAuthenticate = false;

        public MainWindow()
        {
            InitializeComponent();
#if DEBUG
            System.Windows.MessageBox.Show("DEBUG MODE!", DefaultTrayTitle, MessageBoxButton.OK, MessageBoxImage.Information);
#endif
            /*Attention!
            It may be only here, because you have problem with Hide(), Task and Black Wndow! */
            createTrayIcon();
            myTimer.Tick += new EventHandler(TimerEventProcessor);
            SaveCookiesCheckBox.IsChecked = settings.SaveCookiesToDisk;
            client.Timeout = settings.TimeoutForWebRequest;
            usernameTextBox.Focus();
            AuthorLabel.Content = Helper.MyStringJoin("Author: Yuri Astrov ", "Version: ", AssemblyInfoHelper.AssemblyVersion);
            if (this.client.LoadCookies())
            {
                isAuthenticate = true;
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
                if ((e as System.Windows.Forms.MouseEventArgs).Button == System.Windows.Forms.MouseButtons.Left)
                {
                    MethodInfo mi = typeof(NotifyIcon).GetMethod("ShowContextMenu", BindingFlags.Instance | BindingFlags.NonPublic);
                    mi.Invoke(trayIcon, null);
                }
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
                trayIcon.ShowBalloonTip(settings.TimeoutForTrayIconBaloon);
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
                trayIcon.ShowBalloonTip(settings.TimeoutForTrayIconBaloon);
            }
        }
        #endregion
        #region IconEventProcessor
        public void OnReadFavoriteClick(object sender, EventArgs e)
        {
            SetDefaultIcon();
            Helper.OpenUrlInBrowserOrShowException("http://diary.ru/?favorite");
        }

        public void OnReadUmailsClick(object sender, EventArgs e)
        {
            SetDefaultIcon();
            Helper.OpenUrlInBrowserOrShowException("http://www.diary.ru/u-mail/");
        }

        public void OnReadCommentsClick(object sender, EventArgs e)
        {
            SetDefaultIcon();
            Helper.OpenUrlInBrowserOrShowException("http://www.diary.ru/");
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
            bool flag = SaveCookiesCheckBox.IsChecked ?? false;
            if (flag && isAuthenticate)
            {
                if (!this.client.SaveCookies())
                    System.Windows.MessageBox.Show("Failed to save cookies!", MainWindow.DefaultTrayTitle, MessageBoxButton.OK, MessageBoxImage.Error); ;
            }
            else
            {
                removeCookies();
            }
            base.OnClosing(e);
            trayIcon.Visible = false;
        }

        private async void OkButtonClick(object sender, EventArgs e)
        {
            isAuthenticate = false;
            this.Hide();
            try
            {
                await this.client.AuthSecureAsync(usernameTextBox.Text, passTextBox.SecurePassword);
                isAuthenticate = true;
                await DoRequestAsync();
                myTimer.Interval = GetTimeFromTimeoutComboBox();
                myTimer.Start();
            }
            catch (WebException ex)
            {
                var s = Helper.MyStringJoin("Application is disabled. You can Authorize from menu in system tray, or exit from application.",
                    "\n", ex.Message);
                System.Windows.MessageBox.Show(s);
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

        private void removeCookies()
        {
            if (System.IO.File.Exists(DiaryRuClient.CookiesFileName))
                System.IO.File.Delete(DiaryRuClient.CookiesFileName);
        }

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
                        if (data.Error.Contains("Для гостя"))
                        {
                            isAuthenticate = false;
                            removeCookies();
                            this.OnAuthClick(null, null);
                        }
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
        Dictionary<string, TimeSpan> comboboxData = new Dictionary<string, TimeSpan>();

        private void timeoutComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            comboboxData.Add("1 minute", new TimeSpan(0, 1, 0));
            comboboxData.Add("5 minute", new TimeSpan(0, 5, 0));
            comboboxData.Add("10 minute", new TimeSpan(0, 10, 0));
            comboboxData.Add("15 minute", new TimeSpan(0, 15, 0));
            comboboxData.Add("20 minute", new TimeSpan(0, 20, 0));
            comboboxData.Add("25 minute", new TimeSpan(0, 25, 0));
            comboboxData.Add("30 minute", new TimeSpan(0, 30, 0));
            comboboxData.Add("35 minute", new TimeSpan(0, 35, 0));
            comboboxData.Add("40 minute", new TimeSpan(0, 40, 0));
            comboboxData.Add("45 minute", new TimeSpan(0, 45, 0));
            comboboxData.Add("1 hour", new TimeSpan(1, 0, 0));
            timeoutComboBox.ItemsSource = comboboxData;
            SetTimeoutComboboxByValue(settings.TimerForRequest);
        }

        private TimeSpan GetTimeFromTimeoutComboBox()
        {
            KeyValuePair<string, TimeSpan> item = (KeyValuePair<string, TimeSpan>)timeoutComboBox.SelectedItem;
            settings.TimerForRequest = item.Value;
            return item.Value;
        }

        private void SetTimeoutComboboxByValue(TimeSpan value)
        {
            int i = -1;
            foreach (KeyValuePair<string, TimeSpan> val in comboboxData)
            {
                i++;
                if (val.Value == value)
                {
                    break;
                }
            }
            timeoutComboBox.SelectedIndex = i;
        }
        #endregion
    }
}