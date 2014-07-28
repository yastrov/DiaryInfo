using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.Net;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Runtime.Serialization;

namespace DiaryInfo
{
    // Classes for JSON parsing.
    [DataContract]
    public class NewComments
    {
        [DataMember(Name = "count")]
        public int Count { get; set; }
    }
    [DataContract]
    public struct Ddiscuss
    {
        [DataMember(Name = "count")]
        public int Count { get; set; }
    }
    [DataContract]
    public struct Umails
    {
        [DataMember(Name = "count")]
        public int Count { get; set; }
    }
    [DataContract]
    public struct UserInfo
    {
        [DataMember(Name = "userid")]
        public string UserId  { get; set; }
        [DataMember(Name = "username")]
        public string UserName  { get; set; }
        [DataMember(Name = "shortname")]
        public string ShortName  { get; set; }
    }
    // Main object for JSON
    [DataContract]
    public class DiaryRuInfo
    {
        [DataMember(Name = "newcomments")]
        public NewComments NewComments { get; set; }
        [DataMember(Name = "discuss")]
        public Ddiscuss Discuss { get; set; }
        [DataMember(Name = "umails")]
        public Umails Umails  { get; set; }
        [DataMember(Name = "userinfo")]
        public UserInfo UserInfo  { get; set; }
        [DataMember(Name = "error")]
        public string Error { get; set; }

        /// <summary>
        /// Test for non-zero incoming messages.
        /// </summary>
        /// <returns>true if non-zero messages. false in other.</returns>
        public bool IsEmpty() {
            if (this.NewComments.Count+
                this.Umails.Count+this.Discuss.Count == 0)
                return true;
            return false;
        }

        /// <summary>
        /// Represent common information as string.
        /// </summary>
        /// <returns>Common information</returns>
        public override string ToString() {
            if (this.Error != null)
                return this.Error;
            StringBuilder str = new StringBuilder();
            str.Append(this.UserInfo.UserName).Append("\n")
                .Append("NewComments: ").Append(this.NewComments.Count)
                .Append("\n").Append("Discuss: ").Append(this.Discuss.Count)
                .Append("\n").Append("Umails: ").Append(this.Umails.Count);
            return str.ToString();
        }

        /// <summary>
        /// Has Error
        /// </summary>
        /// <returns></returns>
        public bool HasError() {
            if (this.Error != null)
                return true;
            return false;
        }
    }

    /// <summary>
    /// Author Yuri Astrov <yuriastrov@gmail.com>
    /// </summary>
    public class DiaryRuClient
    {
        private string _referer;
        private CookieContainer _cookies;
        private static string URL_MAIN = "http://www.diary.ru/";
        private static string URL_LOGIN = "http://pda.diary.ru/login.php";
        private static string URL_INFO = "http://pay.diary.ru/yandex/online.php";

        public DiaryRuClient()
        {
            this._cookies = new CookieContainer();
        }

        /// <summary>
        /// Do Abstract Request.
        /// </summary>
        /// <param name="url">url</param>
        /// <param name="requestMethod">GET or POST or smth</param>
        /// <param name="content">byte array content</param>
        /// <returns>Response object</returns>
        private HttpWebResponse _Request(String url, String requestMethod, byte[] content)
        {
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(url);
            request.ProtocolVersion = new Version(1, 0);
            request.Method = requestMethod;
            if (!String.IsNullOrEmpty(this._referer))
                request.Referer = this._referer;

            request.AllowAutoRedirect = false;
            request.UserAgent = "Mozilla/5.0 (Windows NT 5.1) AppleWebKit/535.11 (KHTML, like Gecko) Chrome/17.0.963.56 Safari/535.11";
            request.Headers.Add(HttpRequestHeader.AcceptCharset, "windows-1251,utf-8;q=0.7,*;q=0.3");
            request.CookieContainer = this._cookies;
            if (content != null)
            {
                request.ContentType = "application/x-www-form-urlencoded; charset=windows-1251";
                request.ContentLength = content.LongLength;
                using (Stream newStream = request.GetRequestStream())
                {
                    newStream.Write(content, 0, content.Length);
                }
            }
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            request = null;
            this._BugFix_CookieDomain(this._cookies);
            return response;
        }
        /// <summary>
        /// Bug fix for Cookie processing in .NET Framework.
        /// </summary>
        /// <param name="cookieContainer"></param>
        private void _BugFix_CookieDomain(CookieContainer cookieContainer)
        {
            System.Collections.Hashtable table = (System.Collections.Hashtable)cookieContainer.GetType().InvokeMember("m_domainTable",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.GetField |
                System.Reflection.BindingFlags.Instance,
                null,
                cookieContainer,
                new object[] { }
            );
            System.Collections.ArrayList keys = new System.Collections.ArrayList(table.Keys);
            foreach (string keyObj in keys)
            {
                string key = (keyObj as string);
                if (key[0] == '.')
                {
                    string newKey = key.Remove(0, 1);
                    table[newKey] = table[keyObj];
                }
            }
        }

        /// <summary>
        /// Encode pair values to byte array.
        /// </summary>
        /// <param name="_formValues"></param>
        /// <returns></returns>
        private static byte[] EncodeValues(Dictionary<string, string> _formValues)
        {
            StringBuilder postString = new StringBuilder();
            bool first=true;
            foreach (var pair in _formValues)
            {
                if(first)
                    first=false;
                else
                    postString.Append("&");
                postString.AppendFormat("{0}={1}", pair.Key, pair.Value);
            }
           
            Encoding encoding = Encoding.GetEncoding("Windows-1251");
            HttpUtility.UrlEncode(postString.ToString(),  encoding);
            byte[] postBytes = encoding.GetBytes(postString.ToString());
            return postBytes;
        }

        /// <summary>
        /// Auth to Diary.Ru site.
        /// </summary>
        /// <param name="user">User name, login</param>
        /// <param name="password">password</param>
        public void Auth(string user, string password) {
            using (HttpWebResponse response = this._Request(URL_MAIN, "GET", null)) ;
            Dictionary<string, string> map = new Dictionary<string, string>();
            //NameValueCollection map = new NameValueCollection();
            map.Add("user_login", user);
            map.Add("user_pass", password);
            map.Add("save", "on");
            byte[] data = EncodeValues(map);
            using(HttpWebResponse response = this._Request(URL_LOGIN, "POST", data) )  ;
        }

        /// <summary>
        /// Get info for Diary.Ru: New comments and other information.
        /// </summary>
        /// <returns></returns>
        public DiaryRuInfo GetInfo()
        {
            DiaryRuInfo info = null;
            using (HttpWebResponse response = this._Request(URL_INFO, "GET", null))
            {
                DataContractJsonSerializer json = new DataContractJsonSerializer(typeof(DiaryRuInfo));
                using (Stream oStream = response.GetResponseStream())
                {
                    try
                    {
                        info = (DiaryRuInfo)json.ReadObject(oStream);
                    }
                    catch (Exception e)
                    {
                        info = null;
                    }
                }
            }
            return info;
        }
    }
}
