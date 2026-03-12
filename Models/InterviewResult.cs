using System.Collections.Generic;

namespace AIInterviewPractice.Models
{
    public class InterviewResult
    {
        public string Question { get; set; } = string.Empty;
        public string UserAnswer { get; set; } = string.Empty;
        public string IdealAnswer { get; set; } = string.Empty;
        public int Score { get; set; }
        public string Feedback { get; set; } = string.Empty;
    }

    public class LLMEvaluationResponse
    {
        public List<InterviewResult> Questions { get; set; } = new List<InterviewResult>();
        public int OverallScore { get; set; }
        public string MainImprovements { get; set; } = string.Empty;
    }
}
