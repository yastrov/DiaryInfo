﻿using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
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
        private Icon defaultIcon = DiaryInfo.Properties.Resources.MainIcon;
        private Icon attentionIcon = DiaryInfo.Properties.Resources.InfoIcon;
        private const int BALOON_TIP_SHOW_DELAY = 2 * 1000;
        private const int SECUNDS_FROM_MILI_MULTIPLIER = 1000;
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
            client = new DiaryRuClient();
            usernameTextBox.Focus();
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Author: Yuri Astrov").Append("Version: ").Append(typeof(MainWindow).Assembly.GetName().Version.ToString());
            AuthorLabel.Content = sb.ToString();
            if (this.client.LoadCookies())
            {
                Hide();
                myTimer.Interval = 300 * SECUNDS_FROM_MILI_MULTIPLIER;
                Task _task = DoRequestAsync();
                myTimer.Start();
            }
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
                if (timeoutTextBox.Text.Equals("300"))
                    myTimer.Interval = 300000;
                else
                {
                    try
                    {
                        int timeout = 0;
                        timeout = Convert.ToInt32(timeoutTextBox.Text);
                        if (timeout < 0) {
                            System.Windows.MessageBox.Show("Timeout value must be bigger than 0!", MainWindow.DefaultTrayTitle, MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                        else
                            myTimer.Interval = timeout * SECUNDS_FROM_MILI_MULTIPLIER;
                    }
                    catch (FormatException)
                    {
                        this.Show();
                        System.Windows.MessageBox.Show("Timeout value is not valid number!", MainWindow.DefaultTrayTitle, MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    catch (OverflowException)
                    {
                        this.Show();
                        System.Windows.MessageBox.Show("Timeout value is very big!", MainWindow.DefaultTrayTitle, MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
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
            try
            {
                int timeout = 0;
                timeout = Convert.ToInt32(timeoutTextBox.Text);
                if (timeout < 0)
                {
                    System.Windows.MessageBox.Show("Timeout value must be bigger than 0!", MainWindow.DefaultTrayTitle, MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                else
                    myTimer.Interval = timeout * SECUNDS_FROM_MILI_MULTIPLIER;
            }
            catch (FormatException)
            {
                System.Windows.MessageBox.Show("Timeout value is not valid number!", MainWindow.DefaultTrayTitle, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (OverflowException)
            {
                System.Windows.MessageBox.Show("Timeout value is very big!", MainWindow.DefaultTrayTitle, MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
    }
}
