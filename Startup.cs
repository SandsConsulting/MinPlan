using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.AzureAD.UI;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MinPlan
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IHostingEnvironment hostingEnvironment)
        {
            Configuration = configuration;
            HostingEnvironment = hostingEnvironment;
        }

        public IConfiguration Configuration { get; }

        public IHostingEnvironment HostingEnvironment { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => HostingEnvironment.IsProduction();
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            })

            /*
             * IDENTITY SERVER
             */
            // .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
            // {
            //     options.RequireHttpsMetadata = HostingEnvironment.IsProduction();
            //     options.Authority = Configuration["IdentityServer:Domain"];
            //     options.ClientId = Configuration["IdentityServer:ClientId"];
            //     options.ClientSecret = Configuration["IdentityServer:ClientSecret"];
            //     options.ResponseType = "code";
            //     options.Scope.Clear();
            //     options.Scope.Add("openid");
            //     options.Scope.Add("profile");
            //     options.Scope.Add("offline_access");
            //     options.Scope.Add("email");
            //     options.Scope.Add("role");
            //     options.CallbackPath = new PathString("/signin-oidc");
            //     options.TokenValidationParameters.NameClaimType = "name";
            //     options.TokenValidationParameters.RoleClaimType = "role";
            //     options.GetClaimsFromUserInfoEndpoint = true;
            //     options.ClaimActions.MapJsonKey("role", "role");
            //     options.SaveTokens = true;
            //     options.Events.OnRemoteFailure = context =>
            //     {
            //         context.HandleResponse();
            //         context.Response.Redirect("/");
            //         return Task.FromResult<object>(null);
            //     };
            // })

            /*
             * Idfy
             */
            .AddOpenIdConnect("idfy", options =>
            {
                options.RequireHttpsMetadata = HostingEnvironment.IsProduction();
                options.Authority = Configuration["Idfy:Domain"];
                options.ClientId = Configuration["Idfy:ClientId"];
                options.ClientSecret = Configuration["Idfy:ClientSecret"];
                options.ResponseType = "code";
                // options.Scope.Add("offline_access");
                options.CallbackPath = new PathString("/signin-idfy");
                options.TokenValidationParameters.NameClaimType = "name";
                options.TokenValidationParameters.RoleClaimType = "role";
                options.SaveTokens = true;
                options.GetClaimsFromUserInfoEndpoint = true;
                options.Events.OnTokenValidated = ctx =>
                {
                    ((ClaimsIdentity)ctx.Principal.Identity).AddClaim(new Claim("sts_config", "Idfy"));
                    return Task.CompletedTask;
                };
            })

            /*
            * AZURE AD
            */
            .AddOpenIdConnect("azure", options =>
            {
                options.Authority = Configuration["AzureAD:Domain"];
                options.ClientId = Configuration["AzureAD:ClientId"];
                options.ClientSecret = Configuration["AzureAD:ClientSecret"];
                options.ResponseType = "code"; 
                options.Scope.Add("offline_access");
                options.Scope.Add("email");
                options.Scope.Add("role");
                options.CallbackPath = new PathString("/signin-oidc-azure");
                options.TokenValidationParameters.NameClaimType = "name";
                options.TokenValidationParameters.RoleClaimType = "role";
                options.SaveTokens = true;
                options.GetClaimsFromUserInfoEndpoint = true;
                options.Events.OnTokenValidated = ctx =>
                {
                    ((ClaimsIdentity)ctx.Principal.Identity).AddClaim(new Claim("sts_config", "AzureAD"));
                    return Task.CompletedTask;
                };
            })

            /*
             * AUTH0 
             */
            .AddOpenIdConnect("auth0", options =>
            {
                options.Authority = Configuration["Auth0:Domain"];
                options.ClientId = Configuration["Auth0:ClientId"];
                options.ClientSecret = Configuration["Auth0:ClientSecret"];
                options.ResponseType = "code";
                options.Scope.Add("offline_access");
                options.CallbackPath = new PathString("/signin-auth0");
                options.TokenValidationParameters.NameClaimType = "name";
                options.GetClaimsFromUserInfoEndpoint = true;
                options.SaveTokens = true;
                options.Events.OnTokenValidated = ctx =>
                {
                    ((ClaimsIdentity)ctx.Principal.Identity).AddClaim(new Claim("sts_config", "Auth0"));
                    return Task.CompletedTask;
                };
            })

            .AddCookie();

            services.AddMvc()
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCookiePolicy();

            app.UseAuthentication();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}

