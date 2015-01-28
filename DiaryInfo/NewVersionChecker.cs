#define UsingDefaultJSON

using System.Runtime.Serialization.Json;
using System.Runtime.Serialization;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DiaryInfo
{
    #region GithubReleasesResponse

    #region DataContract for default JSON Deserialize
    [DataContract]
    public class ReleaseObject
    {
        [DataMember(Name = "url")]
        public string Url { get; set; }
        [DataMember(Name = "html_url")]
        public string Html_url { get; set; }
        [DataMember(Name = "assets_url")]
        public string Assets_url { get; set; }
        [DataMember(Name = "upload_ur")]
        public string Upload_url { get; set; }
        [DataMember(Name = "tarball_url")]
        public string Tarball_url { get; set; }
        [DataMember(Name = "zipball_url")]
        public string Zipball_url { get; set; }
        [DataMember(Name = "id")]
        public int Id { get; set; }
        [DataMember(Name = "tag_name")]
        public string Tag_name { get; set; }
        [DataMember(Name = "target_commitish")]
        public string Target_commitish { get; set; }
        [DataMember(Name = "name")]
        public string Name { get; set; }
        [DataMember(Name = "body")]
        public string Body { get; set; }
        [DataMember(Name = "draft")]
        public bool Draft { get; set; }
        [DataMember(Name = "prerelease")]
        public bool Prerelease { get; set; }
        //[DataMember(Name = "created_at")]
        //public DateTime Created_at { get; set; }
        //[DataMember(Name = "published_at")]
        //public DateTime Published_at { get; set; }
        [DataMember(Name = "author")]
        public Author Author { get; set; }
        [DataMember(Name = "assets")]
        public Asset[] Assets { get; set; }
    }

    public class Author
    {
        public string Login { get; set; }
        public int Id { get; set; }
        public string Avatar_url { get; set; }
        public string Gravatar_id { get; set; }
        public string Url { get; set; }
        public string Html_url { get; set; }
        public string Followers_url { get; set; }
        public string Following_url { get; set; }
        public string Gists_url { get; set; }
        public string Starred_url { get; set; }
        public string Subscriptions_url { get; set; }
        public string Organizations_url { get; set; }
        public string Repos_url { get; set; }
        public string Events_url { get; set; }
        public string Received_events_url { get; set; }
        public string Type { get; set; }
        public bool Site_admin { get; set; }
    }

    public class Asset
    {
        public string Url { get; set; }
        public string Browser_download_url { get; set; }
        public int Id { get; set; }
        public string Name { get; set; }
        public string Label { get; set; }
        public string State { get; set; }
        public string Content_type { get; set; }
        public int Size { get; set; }
        public int Download_count { get; set; }
        public DateTime Created_at { get; set; }
        public DateTime Updated_at { get; set; }
        public Uploader Uploader { get; set; }
    }

    public class Uploader
    {
        public string Login { get; set; }
        public int Id { get; set; }
        public string Avatar_url { get; set; }
        public string Gravatar_id { get; set; }
        public string Url { get; set; }
        public string Html_url { get; set; }
        public string Followers_url { get; set; }
        public string Following_url { get; set; }
        public string Gists_url { get; set; }
        public string Starred_url { get; set; }
        public string Subscriptions_url { get; set; }
        public string Type { get; set; }
        public bool Site_admin { get; set; }
    }

    #endregion
    #endregion

    public class NewVersionChecker
    {
        public const string BrowserUrl = "https://github.com/yastrov/DiaryInfo/releases";
        public const string APIUrl = "https://api.github.com/repos/yastrov/DiaryInfo/releases";

        #region Release, versions helpers
        /// <summary>
        /// Release version from assembly
        /// </summary>
        public string AssemblyVersion
        {
            get
            {
                return Assembly.GetExecutingAssembly().GetName().Version.ToString();
            }
        }
        /// <summary>
        /// Get short version of release (without last "." and last value)
        /// </summary>
        /// <returns></returns>
        public string GetCurrentRelease()
        {
            var version = AssemblyVersion;
            return version.Substring(0, version.LastIndexOf("."));
        }
        /// <summary>
        /// Compare two versions as strings.
        /// </summary>
        /// <param name="first">First verssion</param>
        /// <param name="second">Second version</param>
        /// <returns>1 if first > than second, 0 if equals and -1 if firsl lower than </returns>
        public static int CompareVersions(string first, string second)
        {
            var aa = first.Split('.');
            var bb = second.Split('.');
            var len = Math.Min(aa.Length, bb.Length);
            for (int i = 0; i < len; i++)
            {
                if (int.Parse(aa[i]) > int.Parse(bb[i])) return 1;
                if (int.Parse(aa[i]) < int.Parse(bb[i])) return -1;
            }
            return 0;
        }
        #endregion

        # region Default JSON
        public Boolean HasNewVersion()
        {
            Boolean flag = false;
            var currentRelease = GetCurrentRelease();

                using (HttpWebResponse response = _Request(APIUrl, "GET", null))
                {
                    if (response.StatusCode != HttpStatusCode.OK)
                        return flag;
                    var r = GetObjectListFromJson<ReleaseObject>(response);
                    foreach (var unit in r)
                    {
                        var release = unit.Tag_name.Replace("v", String.Empty);
                        if (CompareVersions(release, currentRelease) > 0)
                        {
                            flag = true;
                            break;
                        }
                    }
                }
            return flag;
        }

        public static IList<T> GetObjectListFromJson<T>(HttpWebResponse response)
        {
            if (response.StatusCode != HttpStatusCode.OK)
                throw new WebException(response.StatusDescription);
            DataContractJsonSerializer json = new DataContractJsonSerializer(typeof(List<T>));
            using (Stream oStream = response.GetResponseStream())
            {
                return (List<T>)json.ReadObject(oStream);
            }
        }

        public async Task<Boolean> HasNewVersionAsync()
        {
            Boolean flag = false;
            var currentRelease = GetCurrentRelease();
                using (HttpWebResponse response = await _RequestAsync(APIUrl, "GET", null))
                {
                    if (response.StatusCode != HttpStatusCode.OK)
                        return flag;
                    var r = GetObjectListFromJson<ReleaseObject>(response);
                    foreach (var unit in r)
                    {
                        var release = unit.Tag_name.Replace("v", String.Empty);
                        if (CompareVersions(release, currentRelease) > 0)
                        {
                            flag = true;
                            break;
                        }
                    }
                }
            return flag;
        }
        #endregion

        #region Requests
        private int _timeout = 5000;
        public int Timeout
        {
            get { return _timeout; }
            set { if (value >= 0) _timeout = value; }
        }
        private static string _userAgent = @"DiaryRuSearcher";

        public string UserAgent
        {
            get { return _userAgent; }
            set { _userAgent = value; }
        }
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

            if (this._timeout != 0)
                request.Timeout = this._timeout;
            request.AllowAutoRedirect = false;
            request.UserAgent = _userAgent;
            request.Headers.Add(HttpRequestHeader.AcceptCharset, "windows-1251,utf-8;q=0.7,*;q=0.3");
            if (content != null)
            {
                request.ContentLength = content.LongLength;
                using (Stream newStream = request.GetRequestStream())
                {
                    newStream.Write(content, 0, content.Length);
                }
            }
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            request = null;
            return response;
        }

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

            if (this._timeout != 0)
                request.Timeout = this._timeout;
            request.AllowAutoRedirect = false;
            request.UserAgent = _userAgent;
            request.Headers.Add(HttpRequestHeader.AcceptCharset, "windows-1251,utf-8;q=0.7,*;q=0.3");
            if (content != null)
            {
                request.ContentLength = content.LongLength;
                using (Stream newStream = await request.GetRequestStreamAsync())
                {
                    newStream.Write(content, 0, content.Length);
                }
            }
            HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync();
            request = null;

            return response;
        }
        #endregion

    }
}
