using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using AIInterviewPractice.Models;
using Microsoft.Extensions.Configuration;

namespace AIInterviewPractice.Services
{
    public class LLMService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public LLMService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            // Get the API key from appsettings.json
            _apiKey = configuration["OpenRouterApiKey"] ?? ""; 
        }

        public async Task<List<InterviewQuestion>> GenerateQuestionsAsync(InterviewSettings settings)
        {
            var prompt = $"You are a technical interviewer.\n\n" +
                         $"Generate {settings.QuestionCount} interview questions.\n\n" +
                         $"Category: {settings.Category}\n" +
                         $"Difficulty: {settings.Difficulty}\n" +
                         $"Topic: {settings.PreferredTopic ?? "General"}\n\n" +
                         $"Instructions:\n" +
                         $"1. Questions must match the selected category.\n" +
                         $"2. Difficulty should match the requested difficulty.\n" +
                         $"3. Questions should be technical and realistic.\n" +
                         $"4. Avoid generic questions.\n" +
                         $"5. Avoid vague questions.\n" +
                         $"6. Focus on conceptual understanding.\n\n" +
                         $"Example:\nCategory: DSA\nDifficulty: Medium\nQuestions could include:\n" +
                         $"Explain how binary search works and its time complexity.\n" +
                         $"What is the difference between a stack and a queue?\n" +
                         $"When should you use a hash table?\n\n" +
                         $"Return JSON only:\n" +
                         $"[\n {{ \"question\": \"...\" }},\n {{ \"question\": \"...\" }}\n]";

            var requestBody = new
            {
                model = "openai/gpt-3.5-turbo", // Use a generic model string supported by OpenRouter
                messages = new[]
                {
                    new { role = "user", content = prompt }
                }
            };

            var request = new HttpRequestMessage(HttpMethod.Post, "https://openrouter.ai/api/v1/chat/completions");
            request.Headers.Add("Authorization", $"Bearer {_apiKey}");
            request.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            try
            {
                // Un-comment to actually call the API, if you provide a valid key in appsettings.json.
                if (!string.IsNullOrEmpty(_apiKey))
                {
                    var response = await _httpClient.SendAsync(request);
                    response.EnsureSuccessStatusCode();

                    var responseBody = await response.Content.ReadAsStringAsync();
                    
                    // Parse the response from OpenRouter
                    using var jsonDoc = JsonDocument.Parse(responseBody);
                    var content = jsonDoc.RootElement
                                    .GetProperty("choices")[0]
                                    .GetProperty("message")
                                    .GetProperty("content")
                                    .GetString() ?? "[]";

                    content = content
                                    .Replace("```json", "")
                                    .Replace("```", "")
                                    .Trim();
                    
                    var generatedList = JsonSerializer.Deserialize<List<Dictionary<string, string>>>(content);
                    
                    var apiQuestions = new List<InterviewQuestion>();
                    int apiId = 1;
                    foreach (var item in generatedList ?? new List<Dictionary<string, string>>())
                    {
                        if (item.TryGetValue("question", out string qText))
                        {
                            apiQuestions.Add(new InterviewQuestion
                            {
                                Id = apiId++,
                                Text = qText,
                                Category = settings.Category
                            });
                        }
                    }
                    if (apiQuestions.Any()) return apiQuestions;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error calling LLM: {ex.Message}");
            }
            
            // Safer Fallback if API fails or key is missing
            var fallback = new List<InterviewQuestion>();
            for (int i = 1; i <= settings.QuestionCount; i++)
            {
                if (settings.Category == "DSA")
                {
                    fallback.Add(new InterviewQuestion { Id = i, Text = "Explain how a binary search algorithm works.", Category = settings.Category });
                }
                else
                {
                    fallback.Add(new InterviewQuestion { Id = i, Text = $"Explain a core concept related to {settings.PreferredTopic ?? settings.Category}.", Category = settings.Category });
                }
            }
            return fallback;
        }

        public async Task<List<InterviewQuestion>> GenerateQuestionsFromResume(string resumeText, InterviewSettings settings)
        {
            Console.WriteLine("Resume length: " + resumeText.Length);
            Console.WriteLine("Calling LLM for resume-based interview questions");

            var prompt = $"You are a senior technical interviewer.\n\n" +
                         $"Your job is to read a candidate's resume and generate technical interview questions based on their skills.\n\n" +
                         $"Resume:\n{resumeText}\n\n" +
                         $"Category: {settings.Category}\n" +
                         $"Difficulty: {settings.Difficulty}\n" +
                         $"Topic: {settings.PreferredTopic ?? "General"}\n" +
                         $"Number of Questions: {settings.QuestionCount}\n\n" +
                         $"Instructions:\n" +
                         $"1. Identify the main technical skills, technologies, frameworks, and programming languages in the resume.\n" +
                         $"2. Generate {settings.QuestionCount} interview questions based specifically on those skills.\n" +
                         $"3. Questions must be technical and relevant to the detected skills.\n" +
                         $"4. Do NOT generate generic interview questions.\n" +
                         $"5. Do NOT ask \"Explain something from your resume\".\n" +
                         $"6. If the resume contains AI/ML skills ask machine learning questions.\n" +
                         $"7. If the resume contains web development skills ask framework or architecture questions.\n" +
                         $"8. If the resume contains programming languages ask language-specific questions.\n\n" +
                         $"Example behavior:\n" +
                         $"If resume contains: Python, Machine Learning, Flask\n" +
                         $"Generate questions like:\n" +
                         $"Explain gradient descent in machine learning.\n" +
                         $"How does Flask handle routing internally?\n" +
                         $"What is the difference between supervised and unsupervised learning?\n\n" +
                         $"Return ONLY JSON:\n" +
                         $"[\n  {{ \"question\": \"...\" }},\n  {{ \"question\": \"...\" }}\n]\n\n" +
                         $"Do not include explanations.";

            var requestBody = new
            {
                model = "openai/gpt-3.5-turbo",
                messages = new[]
                {
                    new { role = "user", content = prompt }
                }
            };

            var request = new HttpRequestMessage(HttpMethod.Post, "https://openrouter.ai/api/v1/chat/completions");
            request.Headers.Add("Authorization", $"Bearer {_apiKey}");
            request.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            var questions = new List<InterviewQuestion>();
            try
            {
                if (string.IsNullOrEmpty(_apiKey))
                {
                    Console.WriteLine("Warning: OpenRouter API Key is missing. Falling back to mock questions.");
                }
                else
                {
                    var response = await _httpClient.SendAsync(request);
                    response.EnsureSuccessStatusCode();

                    var responseBody = await response.Content.ReadAsStringAsync();
                    
                    using var jsonDoc = JsonDocument.Parse(responseBody);
                    var content = jsonDoc.RootElement
                                    .GetProperty("choices")[0]
                                    .GetProperty("message")
                                    .GetProperty("content")
                                    .GetString() ?? "[]";

                    Console.WriteLine("LLM response received");

                    content = content
                                    .Replace("```json", "")
                                    .Replace("```", "")
                                    .Trim();
                                    
                    var generatedList = JsonSerializer.Deserialize<List<Dictionary<string, string>>>(content);
                    
                    int apiId = 1;
                    foreach (var item in generatedList ?? new List<Dictionary<string, string>>())
                    {
                        if (item.TryGetValue("question", out string? qText) && qText != null)
                        {
                            questions.Add(new InterviewQuestion
                            {
                                Id = apiId++,
                                Text = qText,
                                Category = "Resume"
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error calling LLM: {ex.Message}");
            }

            if (questions == null || questions.Count == 0)
            {
                questions = new List<InterviewQuestion>();
                for (int i = 1; i <= settings.QuestionCount; i++)
                {
                    questions.Add(new InterviewQuestion { Id = i, Text = "Describe one technical skill mentioned in your resume and how it works.", Category = "Resume" });
                }
            }
            
            
            return questions;
        }

        public async Task<LLMEvaluationResponse> EvaluateAnswersAsync(List<InterviewQuestion> questions, List<InterviewAnswer> answers, string strictness = "Normal")
        {
            var promptBuilder = new StringBuilder();
            promptBuilder.AppendLine("Evaluate interview answers strictly.");
            promptBuilder.AppendLine();
            
            if (strictness == "Strict")
            {
                promptBuilder.AppendLine("Rules:");
                promptBuilder.AppendLine("- If answer is completely wrong -> score 0");
                promptBuilder.AppendLine("- If answer partially correct -> score 30-60");
                promptBuilder.AppendLine("- If mostly correct -> score 60-80");
                promptBuilder.AppendLine("- If perfect -> score 80-100");
                promptBuilder.AppendLine("Strict mode: Penalize incorrect answers heavily.");
            }

            promptBuilder.AppendLine();
            for (int i = 0; i < questions.Count; i++)
            {
                var q = questions[i];
                var a = answers.Find(ans => ans.QuestionId == q.Id);
                promptBuilder.AppendLine($"Q: {q.Text}");
                promptBuilder.AppendLine($"A: {(a != null && !string.IsNullOrEmpty(a.AnswerText) ? a.AnswerText : "No answer provided")}");
                promptBuilder.AppendLine();
            }

            promptBuilder.AppendLine(@"Return JSON format:

{
  ""questions"":[
    {
      ""question"":""string"",
      ""userAnswer"":""string"",
      ""idealAnswer"":""string"",
      ""score"":0,
      ""feedback"":""string""
    }
  ],
  ""overallScore"":0,
  ""mainImprovements"":""string""
}");

            var requestBody = new
            {
                model = "openai/gpt-3.5-turbo",
                messages = new[]
                {
                    new { role = "user", content = promptBuilder.ToString() }
                }
            };

            var request = new HttpRequestMessage(HttpMethod.Post, "https://openrouter.ai/api/v1/chat/completions");
            request.Headers.Add("Authorization", $"Bearer {_apiKey}");
            request.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            try
            {
                if (!string.IsNullOrEmpty(_apiKey))
                {
                    var response = await _httpClient.SendAsync(request);
                    response.EnsureSuccessStatusCode();

                    var responseBody = await response.Content.ReadAsStringAsync();
                    
                    var jsonDoc = JsonDocument.Parse(responseBody);
                    var content = jsonDoc.RootElement
                                    .GetProperty("choices")[0]
                                    .GetProperty("message")
                                    .GetProperty("content")
                                    .GetString() ?? "{}";

                    content = content.Replace("```json", "").Replace("```", "").Trim();
                    
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var result = JsonSerializer.Deserialize<LLMEvaluationResponse>(content, options);
                    
                    if (result != null) return result;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error evaluating: {ex.Message}");
            }

            // Fallback
            var mockResponse = new LLMEvaluationResponse
            {
                OverallScore = new Random().Next(60, 95),
                MainImprovements = "This is placeholder feedback. Ensure you answer everything thoroughly."
            };
            foreach (var q in questions) {
                var aText = answers.Find(x => x.QuestionId == q.Id)?.AnswerText ?? "";
                mockResponse.Questions.Add(new InterviewResult {
                    Question = q.Text,
                    UserAnswer = aText,
                    IdealAnswer = "An ideal answer covers the concept fully and clearly.",
                    Score = new Random().Next(50, 100),
                    Feedback = "Mock evaluation feedback based on missing OpenRouter valid key."
                });
            }
            return mockResponse;
        }
    }
}
