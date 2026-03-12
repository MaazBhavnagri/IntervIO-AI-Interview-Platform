using Microsoft.AspNetCore.Http;

namespace AIInterviewPractice.Models
{
    public class InterviewSettings
    {
        public string InterviewType { get; set; } = "category"; // "category" or "resume"
        public IFormFile? ResumeFile { get; set; }
        public int InterviewTimeMinutes { get; set; } = 10;
        
        public string Category { get; set; } = "DSA";
        public string? CustomCategory { get; set; }
        public string Difficulty { get; set; } = "Medium";
        public int QuestionCount { get; set; } = 3;
        public string? PreferredTopic { get; set; }
        public string EvaluationStrictness { get; set; } = "Normal";
    }
}
