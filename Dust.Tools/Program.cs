using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace Dust.Tools
{
    internal class Program
    {
        static void Main(string[] args)
        {
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
        }
    }
}
