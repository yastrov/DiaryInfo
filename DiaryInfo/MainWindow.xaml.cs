﻿using System;
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
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace DiaryInfo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
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

        #region string fileds for DataBind
        // Field for binding to Login TextBox
        private string _loginName;
        public string LoginName
        {
            get { return _loginName; }
            set
            {
                _loginName = value;
                NotifyPropertyChanged("LoginName");
            }
        }
        public string VersionString
        {
            get
            {
                return Helper.MyStringJoin("Author: Yuri Astrov ", "Version: ", AssemblyInfoHelper.AssemblyVersion);
            }
            set { ; }
        }
        #endregion
        #region Cookies checkbox saved fields
        private bool _isCookiesSavedFlag;
        public bool IsCookiesSavedFlag
        {
            get { return _isCookiesSavedFlag; }
            set
            {
                _isCookiesSavedFlag = value;
                NotifyPropertyChanged("IsCookiesSavedFlag");
            }
        }
        #endregion

        public MainWindow()
        {
            InitializeComponent();
#if DEBUG
            System.Windows.MessageBox.Show("DEBUG MODE!", DefaultTrayTitle, MessageBoxButton.OK, MessageBoxImage.Information);
#endif
            UpgradeSettingsIfNeed();
            /*Attention!
            It may be only here, because you have problem with Hide(), Task and Black Wndow! */
            createTrayIcon();
            CreateTimeSpanCollection();
            LoginName = settings.UserName;
            myTimer.Tick += new EventHandler(TimerEventProcessor);
            IsCookiesSavedFlag = settings.SaveCookiesToDisk;
            client.Timeout = settings.TimeoutForWebRequest;
            usernameTextBox.Focus();
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
            ProcessSettingsWhileProgrammClosing();
            base.OnClosing(e);
            trayIcon.Visible = false;
        }

        private async void OkButtonClick(object sender, EventArgs e)
        {
            isAuthenticate = false;
            this.Hide();
            try
            {
                await this.client.AuthSecureAsync(LoginName.Trim(), passTextBox.SecurePassword);
                isAuthenticate = true;
                await DoRequestAsync();
                myTimer.Interval = CurrentTimeSpan.Interval;
                settings.TimerForRequest = CurrentTimeSpan.Interval;
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
            myTimer.Interval = CurrentTimeSpan.Interval;
            settings.TimerForRequest = CurrentTimeSpan.Interval;
            myTimer.Start();
        }
        #endregion

        #region Closing Helpers (Cookies Settings)
        /// <summary>
        /// Process settings while Programm Closing
        /// </summary>
        private void ProcessSettingsWhileProgrammClosing()
        {
            settings.TimerForRequest = CurrentTimeSpan.Interval;
            settings.SaveCookiesToDisk = IsCookiesSavedFlag;
            //settings.SaveCookiesToDisk = SaveCookiesCheckBox.IsChecked ?? false;
            settings.UserName = LoginName;
            settings.Save();
            if (IsCookiesSavedFlag && isAuthenticate)
            {
                if (!this.client.SaveCookies())
                    System.Windows.MessageBox.Show("Failed to save cookies!", MainWindow.DefaultTrayTitle, MessageBoxButton.OK, MessageBoxImage.Error); ;
            }
            else
            {
                removeCookies();
            }
        }
        private void removeCookies()
        {
            if (System.IO.File.Exists(DiaryRuClient.CookiesFileName))
                System.IO.File.Delete(DiaryRuClient.CookiesFileName);
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
        private ObservableCollection<TimeSpanViewModel> _timeSpanCollection = null;
        public ObservableCollection<TimeSpanViewModel> TimeSpanCollection
        {
            get { return _timeSpanCollection; }
            set
            {
                _timeSpanCollection = value;
                NotifyPropertyChanged("TimeSpanCollection");
            }
        }

        private TimeSpanViewModel _currentTimeSpan;
        public TimeSpanViewModel CurrentTimeSpan
        {
            get { return _currentTimeSpan; }
            set
            {
                _currentTimeSpan = value;
                NotifyPropertyChanged("CurrentTimeSpan");
            }
        }
        private void CreateTimeSpanCollection()
        {
            ObservableCollection<TimeSpanViewModel> tsc = new ObservableCollection<TimeSpanViewModel>();
            tsc.Add(new TimeSpanViewModel("1 minute", new TimeSpan(0, 1, 0)));
            tsc.Add(new TimeSpanViewModel("5 minute", new TimeSpan(0, 5, 0)));
            tsc.Add(new TimeSpanViewModel("10 minute", new TimeSpan(0, 10, 0)));
            tsc.Add(new TimeSpanViewModel("15 minute", new TimeSpan(0, 15, 0)));
            tsc.Add(new TimeSpanViewModel("20 minute", new TimeSpan(0, 20, 0)));
            tsc.Add(new TimeSpanViewModel("25 minute", new TimeSpan(0, 25, 0)));
            tsc.Add(new TimeSpanViewModel("30 minute", new TimeSpan(0, 30, 0)));
            tsc.Add(new TimeSpanViewModel("35 minute", new TimeSpan(0, 35, 0)));
            tsc.Add(new TimeSpanViewModel("40 minute", new TimeSpan(0, 40, 0)));
            tsc.Add(new TimeSpanViewModel("45 minute", new TimeSpan(0, 45, 0)));
            tsc.Add(new TimeSpanViewModel("1 hour", new TimeSpan(1, 0, 0)));

            foreach (var item in tsc)
            {
                if (item.CompareTo(settings.TimerForRequest) == 0)
                {
                    CurrentTimeSpan = item;
                    TimeSpanCollection = tsc;
                    break;
                }
            }
        }
        #endregion
        
        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected void NotifyPropertyChanged(String propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion

        #region settings
        /// <summary>
        /// Upgrade settings from previous version, if that need.
        /// </summary>
        private void UpgradeSettingsIfNeed()
        {
            if (settings.UpgradeRequired)
            {
                try
                {
                    settings.Upgrade();
                }
                catch (System.Configuration.ConfigurationException ex)
                {
                    /*It's not bad, we really don't need to do in this case.
                     * May previous config does not exist.*/
                    ;
                }
                finally
                {
                    settings.UpgradeRequired = false;
                }
            }
        }
        #endregion
    }
}