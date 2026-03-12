using Microsoft.AspNetCore.Mvc;
using AIInterviewPractice.Services;
using Microsoft.AspNetCore.Http;

namespace AIInterviewPractice.Controllers
{
    public class AuthController : Controller
    {
        private readonly UserService _userService;

        public AuthController(UserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (!string.IsNullOrEmpty(HttpContext.Session.GetString("UserId")) || !string.IsNullOrEmpty(Request.Cookies["UserId"]))
                return RedirectToAction("Index", "Dashboard");
                
            return View();
        }

        [HttpPost]
        public IActionResult Login(string username, string password)
        {
            var user = _userService.ValidateLogin(username, password);
            if (user != null)
            {
                var cookieOptions = new CookieOptions { Expires = System.DateTimeOffset.UtcNow.AddDays(30) };
                HttpContext.Session.SetString("UserId", user.Id);
                HttpContext.Session.SetString("Username", user.Username);
                Response.Cookies.Append("UserId", user.Id, cookieOptions);
                Response.Cookies.Append("Username", user.Username, cookieOptions);
                return RedirectToAction("Index", "Dashboard");
            }
            ViewBag.Error = "Invalid username or password.";
            return View();
        }

        [HttpGet]
        public IActionResult Signup()
        {
            if (!string.IsNullOrEmpty(HttpContext.Session.GetString("UserId")) || !string.IsNullOrEmpty(Request.Cookies["UserId"]))
                return RedirectToAction("Index", "Dashboard");
                
            return View();
        }

        [HttpPost]
        public IActionResult Signup(string username, string email, string password)
        {
            var user = _userService.CreateUser(username, email, password);
            if (user != null)
            {
                var cookieOptions = new CookieOptions { Expires = System.DateTimeOffset.UtcNow.AddDays(30) };
                HttpContext.Session.SetString("UserId", user.Id);
                HttpContext.Session.SetString("Username", user.Username);
                Response.Cookies.Append("UserId", user.Id, cookieOptions);
                Response.Cookies.Append("Username", user.Username, cookieOptions);
                return RedirectToAction("Index", "Dashboard");
            }
            ViewBag.Error = "Username or Email already exists.";
            return View();
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            Response.Cookies.Delete("UserId");
            Response.Cookies.Delete("Username");
            return RedirectToAction("Index", "Home");
        }
    }
}
