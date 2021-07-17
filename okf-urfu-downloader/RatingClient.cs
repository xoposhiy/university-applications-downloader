using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using System.Web;
using HtmlAgilityPack;

namespace okf_urfu_downloader
{
    public class RatingClient
    {
        public readonly string Login;
        public readonly string Password;
        public readonly string JSessionId;

        public RatingClient(string login, string password)
        {
            Login = login;
            Password = password;
        }

        public RatingClient(string jSessionId)
        {
            JSessionId = jSessionId;
        }

        public async Task<HtmlDocument> Get(string url)
        {
            var response = JSessionId == null 
                ? await GetAfterAuth(url, Login, Password) 
                : await GetWithJSessionId(url, JSessionId);

            await using var stream = await response.Content.ReadAsStreamAsync();
            var htmlDocument = new HtmlDocument();
            htmlDocument.Load(stream);
            htmlDocument.Load("doc.html");
            return htmlDocument;
        }

        private async Task<HttpResponseMessage> GetWithJSessionId(string url, string jSessionId)
        {
            //https://okf.urfu.ru/fx/uni/ru.naumen.uni.published_jsp?cc=uncasso2k3g080000m350gp6lpfth30c&unit=undiin18ggl5g0000iud4ege0ubra5rc&uuid=fakeobUNI_EntrantCoreRoot&activeComponent=EntrantRating
            //https://okf.urfu.ru/fx/uni/ru.naumen.uni.published_jsp?cc=uncasso2k3g080000m350gp742lv52qs&unit=undiin18ggl5g0000iud4ege0ubra5rc&uuid=fakeobUNI_EntrantCoreRoot&activeComponent=EntrantRating
            var cookieContainer = new CookieContainer();    
            var cookies = new Cookie("JSESSIONID", jSessionId);
            cookieContainer.Add(new Uri("https://okf.urfu.ru"), cookies);
            using HttpMessageHandler handler = new HttpClientHandler(){ CookieContainer = cookieContainer};
            using var client = new HttpClient(handler);
            return await client.GetAsync(url);
        }

        private async Task<HttpResponseMessage> GetAfterAuth(string url, string login, string password)
        {
            HttpContent content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("login", login),
                new KeyValuePair<string, string>("password", password),
                new KeyValuePair<string, string>("LogonFormSubmit", "Войти")
            });
            using var client = new HttpClient();
            return await client.PostAsync(
                "https://okf.urfu.ru/fx/$uni/ru.naumen.uni.ui.login_jsp?backUrl=" + HttpUtility.UrlEncode(url),
                content);
        }
    }
}