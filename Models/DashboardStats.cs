using System.Collections.Generic;

namespace AIInterviewPractice.Models
{
    public class DashboardStats
    {
        public int TotalInterviews { get; set; }
        public double AverageScore { get; set; }
        public Dictionary<string, double> CategoryPerformance { get; set; } = new Dictionary<string, double>();
        public string BestCategory { get; set; } = "N/A";
        public string WeakCategory { get; set; } = "N/A";
        public List<int> ScoreHistory { get; set; } = new List<int>();
        public List<string> ScoreHistoryLabels { get; set; } = new List<string>();
    }
}
