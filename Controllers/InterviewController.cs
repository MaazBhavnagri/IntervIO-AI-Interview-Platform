using Microsoft.AspNetCore.Mvc;
using AIInterviewPractice.Models;
using AIInterviewPractice.Services;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace AIInterviewPractice.Controllers
{
    public class InterviewController : Controller
    {
        private readonly InterviewService _interviewService;
        private readonly SpeechService _speechService;
        private readonly ResumeService _resumeService;
        private readonly LLMService _llmService;

        public InterviewController(InterviewService interviewService, SpeechService speechService, ResumeService resumeService, LLMService llmService)
        {
            _interviewService = interviewService;
            _speechService = speechService;
            _resumeService = resumeService;
            _llmService = llmService;
        }

        [HttpGet]
        public IActionResult Setup()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserId")) && string.IsNullOrEmpty(Request.Cookies["UserId"]))
                return RedirectToAction("Login", "Auth");

            return View(new InterviewSettings());
        }

        [HttpPost]
        public async Task<IActionResult> Generate(InterviewSettings settings)
        {
            if (settings.Category == "Other" && !string.IsNullOrEmpty(settings.CustomCategory))
            {
                settings.Category = settings.CustomCategory;
            }

            if (settings.InterviewType == "resume")
            {
                if (settings.ResumeFile == null)
                {
                    return BadRequest("Resume file not uploaded");
                }

                var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");
                if (!Directory.Exists(uploadsDir)) Directory.CreateDirectory(uploadsDir);

                var filePath = Path.Combine(uploadsDir, settings.ResumeFile.FileName);
                using (var stream = new System.IO.FileStream(filePath, System.IO.FileMode.Create))
                {
                    await settings.ResumeFile.CopyToAsync(stream);
                }

                var resumeText = _resumeService.ExtractTextFromPdf(filePath);
                
                Console.WriteLine("Interview Type: " + settings.InterviewType);
                Console.WriteLine("Resume extracted length: " + (resumeText?.Length ?? 0));

                var questions = await _llmService.GenerateQuestionsFromResume(resumeText, settings);
                
                var context = HttpContext;
                var userId = context?.Session?.GetString("UserId") ?? context?.Request.Cookies["UserId"] ?? "GuestUser";
                
                var session = new InterviewSession
                {
                    Questions = questions,
                    EvaluationStrictness = settings.EvaluationStrictness,
                    UserId = userId,
                    InterviewTimeMinutes = settings.InterviewTimeMinutes
                };

                _interviewService.StartSession(session);
                return RedirectToAction("Questions", new { sessionId = session.SessionId });
            }
            else
            {
                var session = await _interviewService.SetupNewSessionAsync(settings);
                _interviewService.StartSession(session);
                return RedirectToAction("Questions", new { sessionId = session.SessionId });
            }
        }

        [HttpGet]
        public IActionResult Questions(string sessionId)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserId")) && string.IsNullOrEmpty(Request.Cookies["UserId"]))
                return RedirectToAction("Login", "Auth");

            var session = _interviewService.GetActiveSession(sessionId);
            if (session == null) return RedirectToAction("Setup");

            ViewBag.SessionId = sessionId;
            ViewBag.TimeMinutes = session.InterviewTimeMinutes;
            return View(session.Questions);
        }

        [HttpPost]
        public async Task<IActionResult> Submit(string sessionId, List<string> answerTexts)
        {
            var session = _interviewService.GetActiveSession(sessionId);
            if (session == null) return RedirectToAction("Setup");

            var answers = new List<InterviewAnswer>();
            for (int i = 0; i < session.Questions.Count; i++)
            {
                var q = session.Questions[i];
                var text = answerTexts != null && i < answerTexts.Count ? answerTexts[i] : "";
                answers.Add(new InterviewAnswer { QuestionId = q.Id, AnswerText = text });
            }

            var finalSession = await _interviewService.SubmitAnswersAsync(sessionId, answers);
            if (finalSession == null) return RedirectToAction("Index", "Home");
            
            return RedirectToAction("Result", new { sessionId = finalSession.SessionId });
        }

        [HttpGet]
        public IActionResult Result(string sessionId)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserId")) && string.IsNullOrEmpty(Request.Cookies["UserId"]))
                return RedirectToAction("Login", "Auth");

            var session = _interviewService.GetSessionById(sessionId);
            if (session == null) return RedirectToAction("Index", "Home");

            return View(session);
        }

        [HttpPost]
        public async Task<IActionResult> UploadAudio(IFormFile audioFile)
        {
            if (audioFile == null || audioFile.Length == 0)
                return Json(new { success = false, message = "Empty audio file." });

            var transcription = await _speechService.ConvertAudioToText(audioFile);
            return Json(new { success = true, text = transcription });
        }
    }
}
