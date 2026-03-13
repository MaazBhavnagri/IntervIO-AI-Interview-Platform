using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using AIInterviewPractice.Services;

namespace AIInterviewPractice.Controllers
{
    public class DashboardController : Controller
    {
        private readonly InterviewService _interviewService;

        public DashboardController(InterviewService interviewService)
        {
            _interviewService = interviewService;
        }

        public IActionResult Index()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserId")) && string.IsNullOrEmpty(Request.Cookies["UserId"]))
                return RedirectToAction("Login", "Auth");

            var stats = _interviewService.GetDashboardStats();
            return View(stats);
        }

        public IActionResult History()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserId")) && string.IsNullOrEmpty(Request.Cookies["UserId"]))
                return RedirectToAction("Login", "Auth");

            var sessions = _interviewService.GetAllSessions();
            return View(sessions);
        }

        [HttpPost]
        public IActionResult DeleteSession(string sessionId)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserId")) && string.IsNullOrEmpty(Request.Cookies["UserId"]))
                return RedirectToAction("Login", "Auth");

            _interviewService.DeleteSession(sessionId);
            return RedirectToAction("History");
        }
    }
}
