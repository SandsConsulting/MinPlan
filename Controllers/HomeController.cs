using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace MinPlan.Controllers
{
    public class HomeController : Controller
    {
        private readonly IConfiguration _configuration;

        public HomeController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<IActionResult> Index()
        {
            // If the user is authenticated, then this is how you can get the access_token and id_token
            if (User.Identity.IsAuthenticated)
            {
                string accessToken = await HttpContext.GetTokenAsync("access_token");
                string idToken = await HttpContext.GetTokenAsync("id_token");
                string refreshToken = await HttpContext.GetTokenAsync("refresh_token");

                ViewBag.AccessToken = accessToken;
                ViewBag.IdToken = idToken;
                ViewBag.RefreshToken = refreshToken;
            }

            return View();
        }

        public async Task<JsonResult> GetUserInfo()
        {
            string accessToken = await HttpContext.GetTokenAsync("access_token");
            HttpClient client = new HttpClient { BaseAddress = new System.Uri("http://localhost:5000") };
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
            var result = await client.GetAsync("/connect/userinfo");
            var userinfo = await result.Content.ReadAsStringAsync();

            return Json(userinfo);
        }

        public async Task<JsonResult> GetToken()
        {
            string refreshToken = await HttpContext.GetTokenAsync("refresh_token");
            var client = new HttpClient { BaseAddress = new System.Uri("http://localhost:5000") };
            var result = await client.PostAsync("/connect/token", new FormUrlEncodedContent(new[]
                                                                    {
                                                                        new KeyValuePair<string, string>("grant_type", "refresh_token"),
                                                                        new KeyValuePair<string, string>("client_id", _configuration["IdentityServer:ClientId"]),
                                                                        new KeyValuePair<string, string>("client_secret", _configuration["IdentityServer:ClientSecret"]),
                                                                        new KeyValuePair<string, string>("refresh_token", refreshToken),
                                                                        new KeyValuePair<string, string>("scope", "openid profile"),
                                                                    }));
            var content = await result.Content.ReadAsStringAsync();

            dynamic json = Newtonsoft.Json.JsonConvert.DeserializeObject(content);
            string accessTokenNew = json["access_token"];
            string idTokenNew = json["id_token"];
            string refreshTokenNew = json["refresh_token"];

            var authMiddleware = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            authMiddleware.Properties.StoreTokens(new List<AuthenticationToken>()
            {
                new AuthenticationToken{ Name = OpenIdConnectParameterNames.AccessToken, Value = accessTokenNew },
                new AuthenticationToken{ Name = OpenIdConnectParameterNames.RefreshToken, Value = refreshTokenNew },
                new AuthenticationToken{ Name = OpenIdConnectParameterNames.IdToken, Value = idTokenNew }
            });

            await HttpContext.SignInAsync(authMiddleware.Principal, authMiddleware.Properties);

            return Json(content);
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}
