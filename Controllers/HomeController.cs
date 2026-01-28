using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SocialLoginMeta.Models;

namespace SocialLoginMeta.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        if (!User.Identity.IsAuthenticated)
        {
            return View(new UserProfileViewModel());
        }

        var name = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
        var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        // Using the custom claim mapped in Program.cs
        var photoUrl = User.FindFirst("urn:facebook:picture")?.Value;

        var model = new UserProfileViewModel
        {
            Name = name,
            Email = email,
            PhotoUrl = photoUrl,
            Provider = "Facebook"
        };

        return View(model);
    }

    public IActionResult Login()
    {
        return Challenge(new Microsoft.AspNetCore.Authentication.AuthenticationProperties
        {
            RedirectUri = Url.Action("Privacy", "Home")
        }, Microsoft.AspNetCore.Authentication.Facebook.FacebookDefaults.AuthenticationScheme);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
