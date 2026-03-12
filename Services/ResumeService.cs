using System;
using System.IO;
using System.Text;
using UglyToad.PdfPig;

namespace AIInterviewPractice.Services
{
    public class ResumeService
    {
        public string ExtractTextFromPdf(string filePath)
        {
            if (!File.Exists(filePath))
                return string.Empty;

            var textBuilder = new StringBuilder();

            try
            {
                using (PdfDocument document = PdfDocument.Open(filePath))
                {
                    foreach (var page in document.GetPages())
                    {
                        textBuilder.AppendLine(page.Text);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("PDF Error: " + ex.Message);
            }

            string resumeText = textBuilder.ToString();

            if(string.IsNullOrWhiteSpace(resumeText))
            {
                Console.WriteLine("Resume extraction returned empty text.");
                return "";
            }

            return resumeText;
        }
    }
}
