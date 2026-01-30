// Load .env file
DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
// Configure Forwarded Headers for Ngrok/Proxy support
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear(); 
    options.KnownProxies.Clear();
});
// Configure Cookie Policy for External Auth (Essential for Chrome/Edge)
builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.CheckConsentNeeded = context => true;
    options.MinimumSameSitePolicy = SameSiteMode.Unspecified;
    options.Secure = CookieSecurePolicy.Always;
});

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = Microsoft.AspNetCore.Authentication.Facebook.FacebookDefaults.AuthenticationScheme;
})
.AddCookie()
.AddFacebook(options =>
{
    // Load from Environment Variables (set by .env)
    options.AppId = Environment.GetEnvironmentVariable("FACEBOOK_APP_ID") ?? builder.Configuration["Authentication:Facebook:AppId"];
    options.AppSecret = Environment.GetEnvironmentVariable("FACEBOOK_APP_SECRET") ?? builder.Configuration["Authentication:Facebook:AppSecret"];
    
    // Request permission to get user picture
    options.Fields.Add("picture");
    options.Events = new Microsoft.AspNetCore.Authentication.OAuth.OAuthEvents
    {
        OnCreatingTicket = context =>
        {
            var picture = context.User.GetProperty("picture").GetProperty("data").GetProperty("url").GetString();
            if (!string.IsNullOrEmpty(picture))
            {
                context.Identity.AddClaim(new System.Security.Claims.Claim("urn:facebook:picture", picture));
            }
            return Task.CompletedTask;
        }
    };
})
.AddInstagram(options =>
{
    options.ClientId = Environment.GetEnvironmentVariable("INSTAGRAM_CLIENT_ID") ?? builder.Configuration["Authentication:Instagram:ClientId"];
    options.ClientSecret = Environment.GetEnvironmentVariable("INSTAGRAM_CLIENT_SECRET") ?? builder.Configuration["Authentication:Instagram:ClientSecret"];

    options.Events = new Microsoft.AspNetCore.Authentication.OAuth.OAuthEvents
    {
        OnCreatingTicket = context =>
        {
            // Instagram Basic Display API might return different properties
            // We'll try to map common ones
            var username = context.User.GetProperty("username").GetString();
            if (!string.IsNullOrEmpty(username))
            {
                context.Identity.AddClaim(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, username));
            }
            return Task.CompletedTask;
        }
    };
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseForwardedHeaders();

// AGGRESSIVE HTTPS ENFORCEMENT FOR NGROK
app.Use(async (context, next) =>
{
    context.Request.Scheme = "https";
    await next();
});

app.UseHttpsRedirection();
app.UseRouting();

app.UseCookiePolicy();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
