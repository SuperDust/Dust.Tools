using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Policy;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Dust.Tools
{
    public class ResponseBody
    {
        public int? Code { get; set; }

        public string Message { get; set; }

        public Exception E { get; set; }
        public string ResultString { get; set; }

        public dynamic ResultDynamic { get; set; }

        public byte[] ResultByte { get; set; }

        public Stream ToStream()
        {
            Stream stream = new MemoryStream(ResultByte);
            return stream;
        }

        public T ToObject<T>()
        {
            return JsonConvert.DeserializeObject<T>(ResultString);
        }
    }

    /*
         var http = new HttpHelper();
            http.OnSuccess(res =>
                {
                    Console.WriteLine("OnSuccess ...");
                })
                .OnError(res =>
                {
                    Console.WriteLine("OnError ...");
                })
                .Send(HttpMethod.Get, "https://reqres.in/api/users?page=2");

            http.OnSuccess(res =>
                {
                    Console.WriteLine("OnSuccess ...");
                })
                .OnError(res =>
                {
                    Console.WriteLine("OnError ...");
                })
                .Send(
                    HttpMethod.Post,
                    "https://reqres.in/api/register",
                    new { email = "eve.holt@reqres.in", password = "pistol" }
                );

            http.OnSuccess(res =>
                {
                    Console.WriteLine("OnSuccess ...");
                })
                .OnError(res =>
                {
                    Console.WriteLine("OnError ...");
                })
                .Send(
                    HttpMethod.Put,
                    "https://reqres.in/api/users/2",
                    new { name = "morpheus", job = "zion resident" }
                );
    */
    /// <summary>
    /// 请求类
    /// </summary>
    public class HttpHelper
    {
        private Action<ResponseBody> SuccessCallBack;

        private Action<ResponseBody> ErrorCallBack;

        private Action<HttpRequestMessage> StartCallBack;

        private static Dictionary<string, string> headerCollection;

        private static readonly char splitChart = '&';

        public ResponseBody Body { get; set; }

        public HttpHelper SetHeaders(Dictionary<string, string> headers)
        {
            headerCollection = headers;
            return this;
        }

        public HttpHelper OnSuccess(Action<ResponseBody> callBack)
        {
            SuccessCallBack = callBack;
            return this;
        }

        public HttpHelper OnError(Action<ResponseBody> callBack)
        {
            ErrorCallBack = callBack;
            return this;
        }

        public HttpHelper OnStart(Action<HttpRequestMessage> callBack)
        {
            StartCallBack = callBack;
            return this;
        }

        public void Send(HttpMethod httpMethod, string url)
        {
            try
            {
                RequestAsync(url, httpMethod).Wait();
            }
            catch (Exception ex)
            {
                Body = new ResponseBody
                {
                    Code = -1,
                    Message = "错误：" + ex.Message,
                    E = ex,
                };
                ErrorCallBack?.Invoke(Body);
            }
        }

        public void Send(HttpMethod httpMethod, string url, dynamic data)
        {
            try
            {
                string dataParams = string.Empty;
                if (data != null)
                {
                    dataParams = JsonConvert.SerializeObject(data);
                }
                RequestAsync(url, httpMethod, dataParams).Wait();
            }
            catch (Exception ex)
            {
                Body = new ResponseBody
                {
                    Code = -1,
                    Message = "错误：" + ex.Message,
                    E = ex,
                };
                ErrorCallBack?.Invoke(Body);
            }
        }

        public void Send(HttpMethod httpMethod, string url, NameValueCollection data)
        {
            try
            {
                string dataParams = string.Empty;
                if (data != null)
                {
                    string text = string.Empty;
                    string[] allKeys = data.AllKeys;
                    foreach (string text2 in allKeys)
                    {
                        string text3 = data[text2];
                        text = text + "&" + text2.ToLower() + "=" + text3;
                    }
                    dataParams = text.TrimStart(new char[1] { splitChart });
                }
                RequestAsync(url, httpMethod, dataParams).Wait();
            }
            catch (Exception ex)
            {
                Body = new ResponseBody
                {
                    Code = -1,
                    Message = "错误：" + ex.Message,
                    E = ex,
                };
                ErrorCallBack?.Invoke(Body);
            }
        }

        public void Send(HttpMethod httpMethod, string url, string data)
        {
            try
            {
                RequestAsync(url, httpMethod, data).Wait();
            }
            catch (Exception ex)
            {
                Body = new ResponseBody
                {
                    Code = -1,
                    Message = "错误：" + ex.Message,
                    E = ex,
                };
                ErrorCallBack?.Invoke(Body);
            }
        }

        private async Task RequestAsync(
            string url,
            HttpMethod method,
            string data = null,
            string contentType = "application/json"
        )
        {
            using (HttpRequestMessage request = new HttpRequestMessage(method, url))
            {
                if (headerCollection != null && headerCollection.Count > 0)
                {
                    foreach (var item in headerCollection)
                    {
                        request.Headers.Add(item.Key, item.Value);
                    }
                }
                if (data != null)
                {
                    StringContent strcontent = new StringContent(data, Encoding.UTF8, contentType);
                    request.Content = strcontent;
                }
                StartCallBack?.Invoke(request);
                HttpClient client = new HttpClient();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
                using (HttpResponseMessage response = await client.SendAsync(request))
                {
                    Body = new ResponseBody
                    {
                        Code = (int)response.StatusCode,
                        Message = response.ReasonPhrase,
                    };
                    using (var memoryStream = new MemoryStream())
                    {
                        await response.Content.CopyToAsync(memoryStream);
                        Body.ResultByte = memoryStream.ToArray();
                        Body.ResultString = Encoding.UTF8.GetString(Body.ResultByte);
                    }
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        try
                        {
                            Body.ResultDynamic = JsonConvert.DeserializeObject(Body.ResultString);
                        }
                        catch (Exception) { }
                        SuccessCallBack?.Invoke(Body);
                        return;
                    }
                }
            }
            ErrorCallBack?.Invoke(Body);
        }
    }
}
