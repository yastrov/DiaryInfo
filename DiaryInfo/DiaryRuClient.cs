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
    #region JSON represent as classes
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
    public class Umails
    {
        [DataMember(Name = "count")]
        public int Count { get; set; }
        [DataMember(Name = "0")]
        public UmailZeroInfo Umail { get; set; }
        
        public override string ToString()
        {
            if (this.Umail != null)
                return this.Umail.ToString();
            else return string.Empty;
        }
    }
    [DataContract]
    public class UmailZeroInfo
    {
        [DataMember(Name="from_username")]
        public string FromUserName { get; set; }
        [DataMember(Name = "title")]
        public string Title { get; set; }
        
        public override string ToString()
        {
            if (this.FromUserName != null && this.Title != null)
                return String.Format("From: {0}\nTitle: {1}", this.FromUserName, this.Title);
            else return string.Empty;
        }
    }
    [DataContract]
    public class UserInfo
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
            if (this.HasError())
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
            str.Append(this.UserInfo.UserName);
            if (this.NewComments.Count > 0)
                str.Append("\n").Append("NewComments: ").Append(this.NewComments.Count);
            if (this.Discuss.Count > 0)
                str.Append("\n").Append("Discuss: ").Append(this.Discuss.Count);
            if(this.Umails.Count > 0)
                str.Append("\n").Append("Umails: ").Append(this.Umails.Count)
                .Append("\n").Append(this.Umails.ToString());
            string result = str.ToString();
            if (result.Length < 63)
                return result;
            else return result.Substring(0, 63);
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
    #endregion
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
        private static readonly Uri _diaryuri = new Uri("http://diary.ru/");
        public static readonly string CookiesFileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "DiaryInfoCookies.data");
        private int _timeout = 5000;
        public int Timeout
        {
            get { return _timeout; }
            set { if(value >= 0) _timeout = value; }
        }
        private static string _userAgent = @"DiaryInfo";

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
            ;
        }

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
            this._cookies = new CookieContainer();
            using (HttpWebResponse response = this._Request(URL_MAIN, "GET", null))
            {
                if (response.StatusCode != HttpStatusCode.OK)
                    throw new WebException(response.StatusDescription);
            }
            string postString = String.Format("user_login={0}&user_pass={1}&save=on", user, password);
            Encoding encoding = Encoding.GetEncoding("Windows-1251");
            byte[] data = encoding.GetBytes(postString);
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
            using (HttpWebResponse response = this._Request(URL_INFO, "GET", null))
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
            this._cookies = new CookieContainer();
            using (HttpWebResponse response = await this._RequestAsync(URL_MAIN, "GET", null).ConfigureAwait(false))
            {
                if (response.StatusCode != HttpStatusCode.OK)
                    throw new WebException(response.StatusDescription);
            }
            string postString = String.Format("user_login={0}&user_pass={1}&save=on", user, password);
            Encoding encoding = Encoding.GetEncoding("Windows-1251");
            byte[] data = encoding.GetBytes(postString);
            using (HttpWebResponse response = await this._RequestAsync(URL_LOGIN, "POST", data).ConfigureAwait(false))
            {
                if (response.StatusCode != HttpStatusCode.OK)
                    throw new WebException(response.StatusDescription);
            }
        }

        /// <summary>
        /// Secure Auth to Diary.Ru site.
        /// </summary>
        /// <param name="user">User name, login</param>
        /// <param name="password">password</param>
        public async Task AuthSecureAsync(string user, System.Security.SecureString password)
        {
            this._cookies = new CookieContainer();
            using (password)
            {
                string pass = new NetworkCredential(string.Empty, password).Password;
                string postString = String.Format("user_login={0}&user_pass={1}&save=on",user, pass);
                Encoding encoding = Encoding.GetEncoding("Windows-1251");
                byte[] data = encoding.GetBytes(postString);
                using (HttpWebResponse response = await this._RequestAsync(URL_LOGIN, "POST", data).ConfigureAwait(false))
                {
                    if (response.StatusCode != HttpStatusCode.OK)
                        throw new WebException(response.StatusDescription);
                }
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

        #region Coockie Serialisation And Deserialisation
        private static void SerializeCookies(Stream stream, CookieCollection cookies, Uri address)
        {
            DataContractSerializer formatter = new DataContractSerializer(typeof(List<Cookie>));
            List<Cookie> cookieList = new List<Cookie>();
            for (var enumerator = cookies.GetEnumerator(); enumerator.MoveNext(); )
            {
                var cookie = enumerator.Current as Cookie;
                if (cookie == null) continue;
                cookieList.Add(cookie);
            }
            formatter.WriteObject(stream, cookieList);
        }

        private static CookieContainer DeserializeCookies(Stream stream, Uri uri)
        {
            List<Cookie> cookies = new List<Cookie>();
            CookieContainer container = new CookieContainer();
            DataContractSerializer formatter = new DataContractSerializer(typeof(List<Cookie>));
            cookies = (List<Cookie>)formatter.ReadObject(stream);
            CookieCollection cookieco = new CookieCollection();
            foreach (Cookie cookie in cookies)
            {
                cookieco.Add(cookie);
            }
            container.Add(uri, cookieco);
            return container;
        }

        public bool SaveCookies()
        {
            try
            {
                using (FileStream fs = File.Create(DiaryRuClient.CookiesFileName))
                {
                    DiaryRuClient.SerializeCookies(fs, this._cookies.GetCookies(_diaryuri), _diaryuri);
                    return true;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public bool LoadCookies()
        {
            if (!File.Exists(DiaryRuClient.CookiesFileName))
                return false;
            try
            {
                using (FileStream fs = File.Open(DiaryRuClient.CookiesFileName, FileMode.Open))
                {
                    this._cookies = DiaryRuClient.DeserializeCookies(fs, _diaryuri);
                    return true;
                }
            }
            catch (Exception ex)
            {
                return false;
            }

        }
        #endregion
    }
}