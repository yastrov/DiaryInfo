using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace DiaryInfo
{
    public static class Helper
    {
        /// <summary>
        /// Open url in browser
        /// </summary>
        /// <param name="url">url</param>
        public static void OpenUrlInBrowserOrShowException(string url)
        {
            try
            {
                Process.Start(url);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message, "DiaryInfo", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// Wrapper over StringBuilder
        /// </summary>
        /// <param name="values"></param>
        /// <returns>complete string</returns>
        public static string MyStringJoin(params string[] values)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < values.Length; i++)
            {
                sb.Append(values[i]);
            }
            return sb.ToString();
        }
    }
}
