using System;
using System.Collections.Generic;

namespace AIInterviewPractice.Models
{
    public class InterviewSession
    {
        public string SessionId { get; set; } = Guid.NewGuid().ToString();
        public int InterviewTimeMinutes { get; set; } = 10;
        public string UserId { get; set; } = "GuestUser";
        public DateTime Date { get; set; } = DateTime.Now;
        
        public List<InterviewQuestion> Questions { get; set; } = new List<InterviewQuestion>();
        public List<InterviewAnswer> Answers { get; set; } = new List<InterviewAnswer>();
        public string EvaluationStrictness { get; set; } = "Normal";
        
        // This holds overall score
        public int Score { get; set; }
        
        // Detailed feedback for each question or general feedback
        public List<InterviewResult> Results { get; set; } = new List<InterviewResult>();
        
        // Overall summary if any
        public string Strengths { get; set; } = string.Empty;
        public string Weaknesses { get; set; } = string.Empty;
        public string OverallImprovementSuggestions { get; set; } = string.Empty;
    }
}
