using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Documents;

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
            this.productNameLabel.Content = AssemblyProduct;
            this.versionLabel.Content = AssemblyVersion;
            this.copyrightLabel.Content = AssemblyCopyright;
            this.companyLabel.Content = AssemblyCompany;
            
           this.Owner = Application.Current.MainWindow;
        }

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

        #region Assembly Attribute Accessors

        public string AssemblyTitle
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyTitleAttribute), false);
                if (attributes.Length > 0)
                {
                    AssemblyTitleAttribute titleAttribute = (AssemblyTitleAttribute)attributes[0];
                    if (titleAttribute.Title != "")
                    {
                        return titleAttribute.Title;
                    }
                }
                return System.IO.Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().CodeBase);
            }
        }

        public string AssemblyVersion
        {
            get
            {
                return Assembly.GetExecutingAssembly().GetName().Version.ToString();
            }
        }

        public string AssemblyDescription
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyDescriptionAttribute), false);
                if (attributes.Length == 0)
                {
                    return "";
                }
                return ((AssemblyDescriptionAttribute)attributes[0]).Description;
            }
        }

        public string AssemblyProduct
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyProductAttribute), false);
                if (attributes.Length == 0)
                {
                    return "";
                }
                return ((AssemblyProductAttribute)attributes[0]).Product;
            }
        }

        public string AssemblyCopyright
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false);
                if (attributes.Length == 0)
                {
                    return "";
                }
                return ((AssemblyCopyrightAttribute)attributes[0]).Copyright;
            }
        }

        public string AssemblyCompany
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyCompanyAttribute), false);
                if (attributes.Length == 0)
                {
                    return "";
                }
                return ((AssemblyCompanyAttribute)attributes[0]).Company;
            }
        }
        #endregion

        private void CloseButtonClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            try
            {
                Process.Start(e.Uri.AbsoluteUri);
            }
            catch(Exception ex) 
            {
                System.Windows.MessageBox.Show(ex.Message, "DiaryInfo", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
