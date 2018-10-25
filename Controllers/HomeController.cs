﻿using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace MinPlan.Controllers
{
    public class HomeController : Controller
    {
        public async Task<IActionResult> Index()
        {
            // If the user is authenticated, then this is how you can get the access_token and id_token
            if (User.Identity.IsAuthenticated)
            {
                string accessToken = await HttpContext.GetTokenAsync("access_token");
                string idToken = await HttpContext.GetTokenAsync("id_token");
#if DEBUG
                string content = await GetUserInfo(accessToken);
                ViewBag.UserInfo = content;
#endif
            }

            return View();
        }

        private static async Task<string> GetUserInfo(string accessToken)
        {
            HttpClient client = new HttpClient
            {
                BaseAddress = new System.Uri(@"http://localhost:5000")
            };
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
            var result = await client.GetAsync("/connect/userinfo");
            var content = await result.Content.ReadAsStringAsync();
            return content;
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}
