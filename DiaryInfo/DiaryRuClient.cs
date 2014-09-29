using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Threading;

namespace DiaryInfo
{
    // Classes for JSON parsing.
    // Many classes for future.
    [DataContract]
    public class NewComments
    {
        [DataMember(Name = "count")]
        public int Count { get; set; }
    }
    [DataContract]
    public struct Discuss
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
        public Discuss Discuss { get; set; }
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
            if (this.Error != null)
                return true;
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
        /// Has Error string in Response from remote server.
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
        private readonly SemaphoreSlim _mutex = new SemaphoreSlim(1);
        private int _timeout = 5000;
        public int Timeout
        {
            get { return _timeout; }
            set { if(value >= 0) _timeout = value; }
        }
        private static string _userAgent =
            @"Mozilla/5.0 (Windows; U; Windows NT 5.1; ru; rv:1.9.1.2) Gecko/20090729 Firefox/3.5.2";

        public string UserAgent
        {
            get { return _userAgent; }
            set { _userAgent = value; }
        }

        private static string _contentType = "application/x-www-form-urlencoded; charset=windows-1251";

        public string ContentType
        {
            get { return _contentType; }
            set { _contentType = value; }
        }

        public DiaryRuClient()
        {
            this._cookies = new CookieContainer();
        }

        #region Common
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
        public static byte[] EncodeValues(Dictionary<string, string> _formValues)
        {
            StringBuilder postString = new StringBuilder();
            bool first = true;
            foreach (var pair in _formValues)
            {
                if (first)
                    first = false;
                else
                    postString.Append("&");
                postString.AppendFormat("{0}={1}", pair.Key, pair.Value);
            }
            Encoding encoding = Encoding.GetEncoding("Windows-1251");
            return encoding.GetBytes(postString.ToString());
        }
        #endregion

        #region NonAsync
        /// <summary>
        /// Do Abstract Request.
        /// </summary>
        /// <param name="url">url</param>
        /// <param name="requestMethod">GET or POST or smth</param>
        /// <param name="content">byte array content</param>
        /// <returns>Response object</returns>
        protected HttpWebResponse _Request(String url, String requestMethod, byte[] content)
        {
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(url);
            request.ProtocolVersion = new Version(1, 0);
            request.Method = requestMethod;
            if (!String.IsNullOrEmpty(this._referer))
                request.Referer = this._referer;

            if (this._timeout != 0)
                request.Timeout = this._timeout;
            request.AllowAutoRedirect = false;
            request.UserAgent = _userAgent;
            request.Headers.Add(HttpRequestHeader.AcceptCharset, "windows-1251,utf-8;q=0.7,*;q=0.3");
            request.CookieContainer = this._cookies;
            if (content != null)
            {
                request.ContentType = _contentType;
                request.ContentLength = content.LongLength;
                using (Stream newStream = request.GetRequestStream())
                {
                    newStream.Write(content, 0, content.Length);
                }
            }
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            request = null;
            this._BugFix_CookieDomain(this._cookies);
            this._referer = url;
            return response;
        }

        /// <summary>
        /// Auth to Diary.Ru site.
        /// </summary>
        /// <param name="user">User name, login</param>
        /// <param name="password">password</param>
        public void Auth(string user, string password)
        {
            using (HttpWebResponse response = this._Request(URL_MAIN, "GET", null))
            {
                if (response.StatusCode != HttpStatusCode.OK)
                    throw new WebException(response.StatusDescription);
            }
            Dictionary<string, string> map = new Dictionary<string, string>();
            //NameValueCollection map = new NameValueCollection();
            map.Add("user_login", user);
            map.Add("user_pass", password);
            map.Add("save", "on");
            byte[] data = EncodeValues(map);
            using (HttpWebResponse response = this._Request(URL_LOGIN, "POST", data))
            {
                if (response.StatusCode != HttpStatusCode.OK)
                    throw new WebException(response.StatusDescription);
            }
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
        #endregion
        
        #region Async
        /// <summary>
        /// Do Abstract Request.
        /// </summary>
        /// <param name="url">url</param>
        /// <param name="requestMethod">GET or POST or smth</param>
        /// <param name="content">byte array content</param>
        /// <returns>Response object</returns>
        protected async Task<HttpWebResponse> _RequestAsync(String url, String requestMethod, byte[] content)
        {
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(url);
            request.ProtocolVersion = new Version(1, 0);
            request.Method = requestMethod;
            if (!String.IsNullOrEmpty(this._referer))
                request.Referer = this._referer;

            if (this._timeout != 0)
                request.Timeout = this._timeout;
            request.AllowAutoRedirect = false;
            request.UserAgent = _userAgent;
            request.Headers.Add(HttpRequestHeader.AcceptCharset, "windows-1251,utf-8;q=0.7,*;q=0.3");
            request.CookieContainer = this._cookies;
            if (content != null)
            {
                request.ContentType = _contentType;
                request.ContentLength = content.LongLength;
                using (Stream newStream = await request.GetRequestStreamAsync())
                {
                    newStream.Write(content, 0, content.Length);
                }
            }
            HttpWebResponse response = null;
            await _mutex.WaitAsync();
            try
            {
                response = (HttpWebResponse)await request.GetResponseAsync();
                request = null;
                this._BugFix_CookieDomain(this._cookies);
            }
            finally {
                _mutex.Release();
            }
            this._referer = url;
            return response;
        }

        /// <summary>
        /// Auth to Diary.Ru site.
        /// </summary>
        /// <param name="user">User name, login</param>
        /// <param name="password">password</param>
        public async Task AuthAsync(string user, string password)
        {
            using (HttpWebResponse response = await this._RequestAsync(URL_MAIN, "GET", null).ConfigureAwait(false))
            {
                if (response.StatusCode != HttpStatusCode.OK)
                    throw new WebException(response.StatusDescription);
            }
            Dictionary<string, string> map = new Dictionary<string, string>();
            //NameValueCollection map = new NameValueCollection();
            map.Add("user_login", user);
            map.Add("user_pass", password);
            map.Add("save", "on");
            byte[] data = EncodeValues(map);
            using (HttpWebResponse response = await this._RequestAsync(URL_LOGIN, "POST", data).ConfigureAwait(false))
            {
                if (response.StatusCode != HttpStatusCode.OK)
                    throw new WebException(response.StatusDescription);
            }
        }

        /// <summary>
        /// Get info for Diary.Ru: New comments and other information.
        /// </summary>
        /// <returns></returns>
        public async Task<DiaryRuInfo> GetInfoAsync()
        {
            using (HttpWebResponse response = await this._RequestAsync(URL_INFO, "GET", null).ConfigureAwait(false))
            {
                DataContractJsonSerializer json = new DataContractJsonSerializer(typeof(DiaryRuInfo));
                using (Stream oStream = response.GetResponseStream())
                {
                    try
                    {
                        return (DiaryRuInfo)json.ReadObject(oStream);
                    }
                    catch (Exception e)
                    {
                        return null;
                    }
                }
            }
        }
        #endregion
    }
}