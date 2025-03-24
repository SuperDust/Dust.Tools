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
using System.Xml.Linq;
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

    public interface IHttpRequestHandle
    {
        /// <summary>
        /// 全局配置拦截
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        void Config(HttpRequestMessage config);

        /// <summary>
        /// 全局成功拦截
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>

        void ResponseSuccess(ResponseBody response);

        /// <summary>
        /// 全局异常拦截
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        void ResponseError(ResponseBody response);
    }

    public static class HttpRegister
    {
        private static IHttpRequestHandle httpRequestHandle;

        public static void Init(IHttpRequestHandle httpRequest)
        {
            httpRequestHandle = httpRequest;
        }

        /// <summary>
        /// 全局拦截
        /// </summary>
        /// <param name="requestMessage"></param>
        /// <returns></returns>

        public static void Config(HttpRequestMessage config)
        {
            httpRequestHandle?.Config(config);
        }

        /// <summary>
        /// 全局成功拦截
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>

        public static void ResponseSuccess(ResponseBody response)
        {
            httpRequestHandle?.ResponseSuccess(response);
        }

        /// <summary>
        /// 全局异常拦截
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        public static void ResponseError(ResponseBody response)
        {
            httpRequestHandle?.ResponseError(response);
        }
    }

    /// <summary>
    /// 请求类
    /// </summary>
    public class HttpHelper
    {
        private Action<ResponseBody> SuccessCallBack;

        private Action<ResponseBody> ErrorCallBack;

        private Action<HttpContent> SetContentCallBack;

        private Action<HttpRequestHeaders> SetHeaderCallBack;

        public ResponseBody Body { get; set; }

        /// <summary>
        /// 发送请求
        /// </summary>
        /// <param name="httpMethod"></param>
        /// <param name="url"></param>
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
                HttpRegister.ResponseError(Body);
                ErrorCallBack?.Invoke(Body);
            }
        }

        /// <summary>
        /// 发送请求
        /// </summary>
        /// <param name="httpMethod"></param>
        /// <param name="url"></param>
        /// <param name="data"></param>

        public void Send(HttpMethod httpMethod, string url, MultipartFormDataContent data)
        {
            try
            {
                SetContentCallBack = (
                    content =>
                    {
                        content = data;
                    }
                );
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
                HttpRegister.ResponseError(Body);
                ErrorCallBack?.Invoke(Body);
            }
        }

        /// <summary>
        /// 发送请求
        /// </summary>
        /// <param name="httpMethod"></param>
        /// <param name="url"></param>
        /// <param name="data"></param>
        /// <param name="contentType"></param>
        public void Send(
            HttpMethod httpMethod,
            string url,
            dynamic data,
            string contentType = "application/json"
        )
        {
            try
            {
                string dataParams = string.Empty;
                if (data != null)
                {
                    dataParams = JsonConvert.SerializeObject(data);
                }

                SetContentCallBack = (
                    content =>
                    {
                        StringContent strcontent = new StringContent(
                            data,
                            Encoding.UTF8,
                            contentType
                        );
                        content = strcontent;
                    }
                );
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
                HttpRegister.ResponseError(Body);
                ErrorCallBack?.Invoke(Body);
            }
        }

        /// <summary>
        /// 发送请求
        /// </summary>
        /// <param name="httpMethod"></param>
        /// <param name="url"></param>
        /// <param name="data"></param>
        /// <param name="contentType"></param>
        public void Send(
            HttpMethod httpMethod,
            string url,
            string data,
            string contentType = "application/json"
        )
        {
            try
            {
                SetContentCallBack = (
                    content =>
                    {
                        StringContent strcontent = new StringContent(
                            data,
                            Encoding.UTF8,
                            contentType
                        );
                        content = strcontent;
                    }
                );
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
                HttpRegister.ResponseError(Body);
                ErrorCallBack?.Invoke(Body);
            }
        }

        /// <summary>
        /// 设置请求头
        /// </summary>
        /// <param name="headers"></param>
        /// <returns></returns>

        public HttpHelper SetHeaders(Action<HttpRequestHeaders> headers)
        {
            SetHeaderCallBack = headers;
            return this;
        }

        /// <summary>
        /// 请求成功
        /// </summary>
        /// <param name="callBack"></param>
        /// <returns></returns>

        public HttpHelper OnSuccess(Action<ResponseBody> callBack)
        {
            SuccessCallBack = callBack;
            return this;
        }

        /// <summary>
        /// 请求失败
        /// </summary>
        /// <param name="callBack"></param>
        /// <returns></returns>

        public HttpHelper OnError(Action<ResponseBody> callBack)
        {
            ErrorCallBack = callBack;
            return this;
        }

        /// <summary>
        /// 请求方法
        /// </summary>
        /// <param name="url"></param>
        /// <param name="method"></param>
        /// <returns></returns>
        private async Task RequestAsync(string url, HttpMethod method)
        {
            using (HttpRequestMessage request = new HttpRequestMessage(method, url))
            {
                SetHeaderCallBack?.Invoke(request.Headers);
                SetContentCallBack?.Invoke(request.Content);
                HttpRegister.Config(request);
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
                        HttpRegister.ResponseSuccess(Body);
                        SuccessCallBack?.Invoke(Body);
                        return;
                    }
                }
            }
            HttpRegister.ResponseError(Body);
            ErrorCallBack?.Invoke(Body);
        }
    }
}
