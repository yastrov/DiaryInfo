using System;
using System.Diagnostics;
using System.Windows;

namespace DiaryInfo
{
    /// <summary>
    /// Interaction logic for AboutWindow.xaml
    /// </summary>
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();
            this.productNameLabel.Content = AssemblyInfoHelper.AssemblyProduct;
            this.versionLabel.Content = AssemblyInfoHelper.AssemblyVersion;
            this.copyrightLabel.Content = AssemblyInfoHelper.AssemblyCopyright.Replace("Copyright ", String.Empty);
            this.companyLabel.Content = AssemblyInfoHelper.AssemblyCompany;
            try
            {
                this.Owner = Application.Current.MainWindow;
            }
            catch (Exception)
            {//We have no Show() Main Window before.
                ;
            }
        }
        #region comment for self-teach in future
        /*Only for example in future
        static Hyperlink CreateHyperLink(string linkURL, string linkName=null)
        {
            Hyperlink link = new Hyperlink();
            link.IsEnabled = true;
            if (linkName == null)
                link.Inlines.Add(linkURL);
            else
                link.Inlines.Add(linkName);
            link.NavigateUri = new Uri(linkURL);
            link.RequestNavigate += (sender, args) => Process.Start(args.Uri.ToString());
            return link;
        }

        FlowDocument CreateDescription()
        {
            FlowDocument fd = new FlowDocument(); 
            fd.Blocks.Clear();
            Paragraph p = new Paragraph();
            p.Inlines.Add("This is simple desktop application for notification about new messages on ");
            p.Inlines.Add(CreateHyperLink("http://diary.ru"));
            p.Inlines.Add(" web service.");
            fd.Blocks.Add(p);

            p = new Paragraph();
            p.Margin = new Thickness(0);
            p.Inlines.Add("If you have unread messages, you see green icon in system tray and popup ballon tip.");
            p.Inlines.Add(Environment.NewLine);
            p.Inlines.Add("If you have no unread messages, you see red tray icon only.");
            fd.Blocks.Add(p);

            p = new Paragraph();
            p.Inlines.Add("Homepage for project:");
            p.Inlines.Add(Environment.NewLine);
            p.Inlines.Add(CreateHyperLink("https://github.com/yastrov/DiaryInfo"));
            p.Inlines.Add(Environment.NewLine);
            p.Inlines.Add("You can see new versions at:");
            p.Inlines.Add(Environment.NewLine);
            p.Inlines.Add(CreateHyperLink("https://github.com/yastrov/DiaryInfo/releases"));
            fd.Blocks.Add(p);

            fd.Blocks.Add(new Paragraph(new Run("Written by Yuri Astrov")));
            return fd;
        }
        */
        #endregion
        private void CloseButtonClick(object sender, RoutedEventArgs e)
        {
            this.Close();
            Application.Current.MainWindow.Activate();
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Helper.OpenUrlInBrowserOrShowException(e.Uri.AbsoluteUri);
        }

        private void checkVersionButtonClick(object sender, RoutedEventArgs e)
        {
            checkNewVersion();
        }

        /// <summary>
        /// Check new Version aviable on GitHub, show MessageBox if yes.
        /// </summary>
        private void checkNewVersion()
        {
            var checker = new NewVersionChecker();
            try
            {
                checker.HasNewVersionAsync().ContinueWith(r =>
                {
                    if (r.Result)
                    {
                        var msr = MessageBox.Show("New version aviable! Open in browser?", "DiaryInfo", MessageBoxButton.YesNo, MessageBoxImage.Information);
                        if (msr == MessageBoxResult.Yes)
                        {
                            try
                            {
                                Process.Start(NewVersionChecker.BrowserUrl);
                            }
                            catch (Exception ex)
                            {
                                System.Windows.MessageBox.Show(ex.Message, this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message, this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
