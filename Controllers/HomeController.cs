using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using IdentityModel.Client;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Newtonsoft.Json;

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
            var sts_config = User.Claims.FirstOrDefault(claim => claim.Type == "sts_config")?.Value;

            var discoClient = new DiscoveryClient(_configuration[$"{sts_config}:Domain"]);
            discoClient.Policy.ValidateIssuerName = false;
            discoClient.Policy.ValidateEndpoints = false;
            var disco = await discoClient.GetAsync();

            string accessToken = await HttpContext.GetTokenAsync("access_token");
            HttpClient client = new HttpClient ();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
            var result = await client.GetAsync(disco.UserInfoEndpoint);
            var userinfo = await result.Content.ReadAsStringAsync();

            return Json(userinfo);
        }

        public async Task<JsonResult> GetToken()
        {
            var sts_config = User.Claims.FirstOrDefault(claim => claim.Type == "sts_config")?.Value;

            var discoClient = new DiscoveryClient(_configuration[$"{sts_config}:Domain"]);
            discoClient.Policy.ValidateIssuerName = false;
            discoClient.Policy.ValidateEndpoints = false;
            var disco = await discoClient.GetAsync();

            var result = await new HttpClient().PostAsync(disco.TokenEndpoint, new FormUrlEncodedContent(new[]
                                                                                {
                                                                                    new KeyValuePair<string, string>("grant_type", "refresh_token"),
                                                                                    new KeyValuePair<string, string>("client_id", _configuration[$"{sts_config}:ClientId"]),
                                                                                    new KeyValuePair<string, string>("client_secret", _configuration[$"{sts_config}:ClientSecret"]),
                                                                                    new KeyValuePair<string, string>("refresh_token", await HttpContext.GetTokenAsync("refresh_token")),
                                                                                    new KeyValuePair<string, string>("scope", "openid profile"),
                                                                                }));
            var content = await result.Content.ReadAsStringAsync();

            dynamic json = JsonConvert.DeserializeObject(content);
            string accessTokenNew = json["access_token"];
            string idTokenNew = json["id_token"];
            string refreshTokenNew = json["refresh_token"];

            var auth = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            auth.Properties.UpdateTokenValue(OpenIdConnectParameterNames.AccessToken, accessTokenNew);
            auth.Properties.UpdateTokenValue(OpenIdConnectParameterNames.RefreshToken, refreshTokenNew);
            auth.Properties.UpdateTokenValue(OpenIdConnectParameterNames.IdToken, idTokenNew);

            return Json(content);
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}
