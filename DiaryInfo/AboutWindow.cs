using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DiaryInfo
{
    partial class AboutWindow : Form
    {
        public AboutWindow()
        {
            InitializeComponent();
            this.Icon = DiaryInfo.Properties.Resources.Icon1;
            this.Text = String.Format("About {0}", AssemblyTitle);
            this.labelProductName.Text = AssemblyProduct;
            this.labelVersion.Text = String.Format("Version {0}", AssemblyVersion);
            this.labelCopyright.Text = AssemblyCopyright;
            this.labelCompanyName.Text = AssemblyCompany;
            StringBuilder sb = new StringBuilder();
            sb.Append("This is simple desktop application for notification ")
              .Append("about new messages on http://diary.ru web service.")
              .AppendLine(Environment.NewLine)
              .Append("If you have unread messages, you see green icon in ")
              .Append("system tray and popup ballon tip. ")
              .Append("If you have no unread messages, you see red tray icon only.")
              .AppendLine(Environment.NewLine)
              .AppendLine("Homepage for project:")
              .AppendLine("https://github.com/yastrov/DiaryInfo")
              .AppendLine("You can see new versions at:")
              .AppendLine("https://github.com/yastrov/DiaryInfo/releases")
              .AppendLine(Environment.NewLine)
              .AppendLine("Written by Yuri Astrov");
            this.textBoxDescription.Text = sb.ToString().Trim();
            this.logoPictureBox.Image = CreateImageFromText(AssemblyTitle, new Font("Helvetica", 21), System.Drawing.Color.Red);
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

        private void okButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        protected static Image CreateImageFromText(string text, Font font, System.Drawing.Color color)
        {
            Bitmap bitmap = new Bitmap(131, 131);//, System.Drawing.Imaging.PixelFormat.Max);
            System.Drawing.Brush brush = new SolidBrush(color);
            Graphics graphics = Graphics.FromImage(bitmap);
            graphics.DrawString(text, font, brush, 0, 0);
            IntPtr hBitmap = bitmap.GetHbitmap();
            return System.Drawing.Image.FromHbitmap(hBitmap);
        }
    }
}
