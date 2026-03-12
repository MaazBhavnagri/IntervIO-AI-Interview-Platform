using Microsoft.AspNetCore.Mvc;

namespace AIInterviewPractice.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
