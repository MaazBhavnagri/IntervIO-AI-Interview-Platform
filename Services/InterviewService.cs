using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using AIInterviewPractice.Models;

namespace AIInterviewPractice.Services
{
    public class InterviewService
    {
        private readonly LLMService _llmService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly string _dataFile = "Data/interviews.json";
        
        // In-memory list to store active sessions ONLY
        private static readonly Dictionary<string, InterviewSession> _activeSessions = new Dictionary<string, InterviewSession>();

        public InterviewService(LLMService llmService, IHttpContextAccessor httpContextAccessor)
        {
            _llmService = llmService;
            _httpContextAccessor = httpContextAccessor;
            if (!Directory.Exists("Data")) Directory.CreateDirectory("Data");
            if (!File.Exists(_dataFile)) File.WriteAllText(_dataFile, "[]");
        }

        private List<InterviewSession> ReadSessions()
        {
            var json = File.ReadAllText(_dataFile);
            return JsonSerializer.Deserialize<List<InterviewSession>>(json) ?? new List<InterviewSession>();
        }

        private void WriteSessions(List<InterviewSession> sessions)
        {
            var json = JsonSerializer.Serialize(sessions, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_dataFile, json);
        }

        public async Task<InterviewSession> SetupNewSessionAsync(InterviewSettings settings)
        {
            var questions = await _llmService.GenerateQuestionsAsync(settings);
            
            var context = _httpContextAccessor.HttpContext;
            var userId = context?.Session?.GetString("UserId") ?? context?.Request.Cookies["UserId"] ?? "GuestUser";
            
            var session = new InterviewSession
            {
                Questions = questions,
                EvaluationStrictness = settings.EvaluationStrictness,
                UserId = userId,
                InterviewTimeMinutes = settings.InterviewTimeMinutes
            };
            
            return session;
        }

        public void StartSession(InterviewSession session)
        {
            _activeSessions[session.SessionId] = session;
        }

        public InterviewSession GetActiveSession(string sessionId)
        {
            if (string.IsNullOrEmpty(sessionId)) return null;

            if (_activeSessions.TryGetValue(sessionId, out var session))
            {
                return session;
            }
            return null;
        }

        public async Task<InterviewSession> SubmitAnswersAsync(string sessionId, List<InterviewAnswer> answers)
        {
            var session = GetActiveSession(sessionId);
            if (session == null) return null;

            session.Answers = answers ?? new List<InterviewAnswer>();
            
            // Evaluate answers via LLM
            var result = await _llmService.EvaluateAnswersAsync(session.Questions, session.Answers, session.EvaluationStrictness);
            
            session.Score = result.OverallScore;
            session.Results = result.Questions ?? new List<InterviewResult>();
            
            // Additional details in session based on result
            session.Strengths = "Evaluated via AI model.";
            session.Weaknesses = "";
            session.OverallImprovementSuggestions = result.MainImprovements;

            // Move from active to finished sessions (save to file)
            var allSessions = ReadSessions();
            allSessions.Add(session);
            WriteSessions(allSessions);
            _activeSessions.Remove(sessionId);
            
            return session;
        }

        public List<InterviewSession> GetAllSessions()
        {
            var context = _httpContextAccessor.HttpContext;
            var userId = context?.Session?.GetString("UserId") ?? context?.Request.Cookies["UserId"] ?? "GuestUser";
            return ReadSessions().Where(s => s.UserId == userId).OrderByDescending(s => s.Date).ToList();
        }

        public InterviewSession GetSessionById(string sessionId)
        {
            var context = _httpContextAccessor.HttpContext;
            var userId = context?.Session?.GetString("UserId") ?? context?.Request.Cookies["UserId"] ?? "GuestUser";
            return ReadSessions().FirstOrDefault(s => s.SessionId == sessionId && s.UserId == userId);
        }

        public bool DeleteSession(string sessionId)
        {
            var context = _httpContextAccessor.HttpContext;
            var userId = context?.Session?.GetString("UserId") ?? context?.Request.Cookies["UserId"] ?? "GuestUser";
            
            var allSessions = ReadSessions();
            var sessionToRemove = allSessions.FirstOrDefault(s => s.SessionId == sessionId && s.UserId == userId);
            
            if (sessionToRemove != null)
            {
                allSessions.Remove(sessionToRemove);
                WriteSessions(allSessions);
                return true;
            }
            
            return false;
        }

        public DashboardStats GetDashboardStats()
        {
            var context = _httpContextAccessor.HttpContext;
            var userId = context?.Session?.GetString("UserId") ?? context?.Request.Cookies["UserId"] ?? "GuestUser";
            var userSessions = ReadSessions().Where(s => s.UserId == userId).OrderBy(s => s.Date).ToList();

            var stats = new DashboardStats
            {
                TotalInterviews = userSessions.Count
            };

            if (userSessions.Count > 0)
            {
                stats.AverageScore = userSessions.Average(s => s.Score);
                
                stats.CategoryPerformance = userSessions
                    .Where(s => s.Questions.Any())
                    .GroupBy(s => s.Questions.First().Category)
                    .ToDictionary(g => g.Key, g => g.Average(s => s.Score));

                if (stats.CategoryPerformance.Any())
                {
                    stats.BestCategory = stats.CategoryPerformance.OrderByDescending(kv => kv.Value).First().Key;
                    stats.WeakCategory = stats.CategoryPerformance.OrderBy(kv => kv.Value).First().Key;
                }

                stats.ScoreHistoryLabels = userSessions.Select(s => s.Date.ToString("MMM dd")).ToList();
                stats.ScoreHistory = userSessions.Select(s => s.Score).ToList();
            }
            
            return stats;
        }
    }
}
