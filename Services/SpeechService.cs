using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace AIInterviewPractice.Services
{
    public class SpeechService
    {
        private readonly HttpClient _httpClient;
        private readonly string _openAiApiKey;

        public SpeechService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _openAiApiKey = configuration["OpenAIApiKey"] ?? string.Empty;
        }

        public async Task<string> ConvertAudioToText(IFormFile audioFile)
        {
            if (audioFile == null || audioFile.Length == 0)
                return "No audio recorded.";

            if (string.IsNullOrEmpty(_openAiApiKey))
            {
                return "Error: OpenAI API Key is missing in appsettings.json. Please add \"OpenAIApiKey\": \"YOUR_KEY\" to use Whisper API.";
            }

            try
            {
                using var content = new MultipartFormDataContent();
                
                // Read from IFormFile into a stream
                using var stream = audioFile.OpenReadStream();
                var fileContent = new StreamContent(stream);
                fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(audioFile.ContentType ?? "audio/webm");
                
                content.Add(fileContent, "file", audioFile.FileName);
                content.Add(new StringContent("whisper-1"), "model");
                
                var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/audio/transcriptions");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _openAiApiKey);
                request.Content = content;

                var response = await _httpClient.SendAsync(request);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorDetails = await response.Content.ReadAsStringAsync();
                    return $"Error from Whisper API: {response.StatusCode} - {errorDetails}";
                }

                var responseBody = await response.Content.ReadAsStringAsync();
                var jsonDoc = JsonDocument.Parse(responseBody);
                if (jsonDoc.RootElement.TryGetProperty("text", out JsonElement textElement))
                {
                    return textElement.GetString() ?? "Could not transcribe audio.";
                }

                return "Transcription successful but no text returned.";
            }
            catch (Exception ex)
            {
                return $"Exception during transcription: {ex.Message}";
            }
        }
    }
}
