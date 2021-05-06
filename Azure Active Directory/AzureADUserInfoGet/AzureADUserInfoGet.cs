using System;
using Ayehu.Sdk.ActivityCreation.Interfaces;
using Ayehu.Sdk.ActivityCreation.Extension;
using System.Collections.Generic;
using System.Net.Http;
using System.Net;
using Ayehu.Sdk.ActivityCreation.Helpers;
using System.Text;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.IO;
using System.Data;

namespace Ayehu.Sdk.ActivityCreation
{
    public class AzureADUserInfoGet : IActivity
    {

        public string userId = "";

        public string accessToken = "";

        public string Jsonkeypath = "";

        private bool omitJsonEmptyorNull = false;

        private string contentType = "application/json";

        private string endPoint = "https://graph.microsoft.com";

        private string httpMethod = "GET";

        private string _uriBuilderPath;

        private string _postData;

        private System.Collections.Generic.Dictionary<string, string> _headers;

        private System.Collections.Generic.Dictionary<string, string> _queryStringArray;

        private string uriBuilderPath
        {
            get
            {
                if (string.IsNullOrEmpty(_uriBuilderPath))
                {
                    _uriBuilderPath = string.Format("/v1.0/users/{0}", userId);
                }
                return _uriBuilderPath;
            }
            set
            {
                this._uriBuilderPath = value;
            }
        }

        private string postData
        {
            get
            {
                if (string.IsNullOrEmpty(_postData))
                {
                    _postData = "";
                }
                return _postData;
            }
            set
            {
                this._postData = value;
            }
        }

        private System.Collections.Generic.Dictionary<string, string> headers
        {
            get
            {
                if (_headers == null)
                {
                    _headers = new Dictionary<string, string>() { { "authorization", "Bearer " + accessToken } };
                }
                return _headers;
            }
            set
            {
                this._headers = value;
            }
        }

        private System.Collections.Generic.Dictionary<string, string> queryStringArray
        {
            get
            {
                if (_queryStringArray == null)
                {
                    _queryStringArray = new Dictionary<string, string>();
                }
                return _queryStringArray;
            }
            set
            {
                this._queryStringArray = value;
            }
        }

        public ICustomActivityResult Execute()
        {
            var response = ApiCall();
            uriBuilderPath = string.Format("/v1.0/users/{0}/settings", userId);
            var settingsResponse = ApiCall();
            uriBuilderPath = string.Format("/v1.0/users/{0}/licenseDetails", userId);
            var licenseDetailsResponse = ApiCall();
            string settingsString = "";
            string licenseDetailsString = "";

            if (string.IsNullOrEmpty(settingsResponse.Content.ReadAsStringAsync().Result) == false)
            {
                using (StreamReader sr = new StreamReader(settingsResponse.Content.ReadAsStreamAsync().Result))
                {
                    var json = (JObject)JsonConvert.DeserializeObject(sr.ReadToEnd());
                    var member = json.Value<JToken>();
                    settingsString = "\"contributionToContentDiscoveryAsOrganizationDisabled:\"" + member.Value<string>("contributionToContentDiscoveryAsOrganizationDisabled") + ", " + "\"contributionToContentDiscoveryDisabled:\"" + member.Value<string>("contributionToContentDiscoveryDisabled");
                }
            }

            if (string.IsNullOrEmpty(licenseDetailsResponse.Content.ReadAsStringAsync().Result) == false)
            {
                using (StreamReader sr = new StreamReader(licenseDetailsResponse.Content.ReadAsStreamAsync().Result))
                {
                    var json = (JObject)JsonConvert.DeserializeObject(sr.ReadToEnd());
                    var members = json.Value<JToken>();
                    var member = members.Value<JToken>("value");
                    licenseDetailsString = member.ToString();
                }
            }

            DataTable dt = new DataTable("resultSet");
            if (string.IsNullOrEmpty(response.Content.ReadAsStringAsync().Result) == false)
            {
                using (StreamReader sr = new StreamReader(response.Content.ReadAsStreamAsync().Result))
                {
                    dt.Columns.Add("Id");
                    dt.Columns.Add("Mail");
                    dt.Columns.Add("UserPrincipalName");
                    dt.Columns.Add("Surname");
                    dt.Columns.Add("GivenName");
                    dt.Columns.Add("UserType");
                    dt.Columns.Add("MobilePhone");
                    dt.Columns.Add("OfficeLocation");
                    dt.Columns.Add("LicenseDetails");
                    dt.Columns.Add("Settings");
                    dt.Columns.Add("AccountEnabled");
                    var json = (JObject)JsonConvert.DeserializeObject(sr.ReadToEnd());
                    var member = json.Value<JToken>();
                    dt.Rows.Add
                        (
                        member.Value<string>("id"),
                        member.Value<string>("mail"),
                        member.Value<string>("userPrincipalName") != null ? member.Value<string>("userPrincipalName") : "",
                        member.Value<string>("surname"),
                        member.Value<string>("givenName"),
                        member.Value<string>("@odata.context").Contains("user") ? "User" : "Role",
                        member.Value<string>("mobilePhone"),
                        member.Value<string>("officeLocation"),
                        licenseDetailsString,
                        settingsString,
                        member.Value<string>("accountEnabled")
                        );
                }
            }
            else
            {
                return this.GenerateActivityResult("Success");
            }

            return this.GenerateActivityResult(dt);
        }

        private HttpResponseMessage ApiCall()
        {
            HttpClient client = new HttpClient();
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            ServicePointManager.ServerCertificateValidationCallback = new System.Net.Security.RemoteCertificateValidationCallback(AcceptAllCertifications);
            UriBuilder UriBuilder = new UriBuilder(endPoint);
            UriBuilder.Path = uriBuilderPath;
            UriBuilder.Query = AyehuHelper.queryStringBuilder(queryStringArray);
            HttpRequestMessage myHttpRequestMessage = new HttpRequestMessage(new HttpMethod(httpMethod), UriBuilder.ToString());

            if (contentType == "application/x-www-form-urlencoded")
                myHttpRequestMessage.Content = AyehuHelper.formUrlEncodedContent(postData);
            else
              if (string.IsNullOrEmpty(postData) == false)
                if (omitJsonEmptyorNull)
                    myHttpRequestMessage.Content = new StringContent(AyehuHelper.omitJsonEmptyorNull(postData), Encoding.UTF8, "application/json");
                else
                    myHttpRequestMessage.Content = new StringContent(postData, Encoding.UTF8, contentType);


            foreach (KeyValuePair<string, string> headeritem in headers)
                client.DefaultRequestHeaders.Add(headeritem.Key, headeritem.Value);

            HttpResponseMessage response = client.SendAsync(myHttpRequestMessage).Result;

            switch (response.StatusCode)
            {
                case HttpStatusCode.NoContent:
                case HttpStatusCode.Created:
                case HttpStatusCode.Accepted:
                case HttpStatusCode.OK:
                    {
                        return response;
                    }
                default:
                    {
                        if (string.IsNullOrEmpty(response.Content.ReadAsStringAsync().Result) == false)
                            throw new Exception(response.Content.ReadAsStringAsync().Result);
                        else if (string.IsNullOrEmpty(response.ReasonPhrase) == false)
                            throw new Exception(response.ReasonPhrase);
                        else
                            throw new Exception(response.StatusCode.ToString());
                    }
            }

        }

        private bool AcceptAllCertifications(object sender, System.Security.Cryptography.X509Certificates.X509Certificate certification, System.Security.Cryptography.X509Certificates.X509Chain chain, System.Net.Security.SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }
    }
}