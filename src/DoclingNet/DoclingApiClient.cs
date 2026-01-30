using System.Net.Http.Headers;
using Newtonsoft.Json;

namespace DoclingNet
{
    public class DoclingApiClient : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _doclingApiUrl;

        public DoclingApiClient(AppConfig config)
        {
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromMinutes(10) // Set a reasonable timeout
            };
            _doclingApiUrl = config.DoclingApiUrl;
        }

        public DoclingApiClient(HttpClient httpClient, AppConfig config)
        {
            _httpClient = httpClient;
            _doclingApiUrl = config.DoclingApiUrl;
        }

        public async Task<string> ConvertPdfToMarkdown(string pdfFilePath)
        {
            return await ConvertPdfToFormat(pdfFilePath, "md");
        }

        public async Task<string> ConvertPdfToJson(string pdfFilePath)
        {
            return await ConvertPdfToFormat(pdfFilePath, "json");
        }

        private async Task<string> ConvertPdfToFormat(string pdfFilePath, string format)
        {
            // Try the synchronous endpoint first (in case it's been fixed)
            try
            {
                return await TrySynchronousConversion(pdfFilePath, format);
            }
            catch (Exception syncEx)
            {
                Console.WriteLine($"Synchronous conversion failed: {syncEx.Message}");
                Console.WriteLine("Trying async approach...");
                
                // Fall back to async approach
                try
                {
                    return await TryAsynchronousConversion(pdfFilePath, format);
                }
                catch (Exception asyncEx)
                {
                    // If both approaches fail, provide detailed error information
                    throw new Exception($"Both synchronous and asynchronous conversion approaches failed.\n\n" +
                                       $"Synchronous error: {syncEx.Message}\n\n" +
                                       $"Asynchronous error: {asyncEx.Message}\n\n" +
                                       $"This version of Docling API might be configured differently or requires additional setup.\n" +
                                       $"Please check the Docling documentation at {_doclingApiUrl}/docs for the correct API usage.");
                }
            }
        }

        private async Task<string> TrySynchronousConversion(string pdfFilePath, string format)
        {
            var requestUrl = $"{_doclingApiUrl}/v1/convert/file";

            // Try different PDF backends for problematic PDFs
            var backends = new[] { "dlparse_v4", "dlparse_v2", "pypdfium2" };
            
            foreach (var backend in backends)
            {
                try
                {
                    Console.WriteLine($"Trying synchronous conversion with PDF backend: {backend}");
                    
                    await using var fileStream = File.OpenRead(pdfFilePath);
                    using var streamContent = new StreamContent(fileStream);
                    streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");

                    using var formData = new MultipartFormDataContent();
                    formData.Add(streamContent, "files", Path.GetFileName(pdfFilePath));
                    formData.Add(new StringContent(format), "to_formats"); // Use the format parameter
                    formData.Add(new StringContent(backend), "pdf_backend");
                    
                    // Add additional parameters to help with problematic PDFs
                    formData.Add(new StringContent("false"), "force_ocr"); // Don't force OCR initially
                    formData.Add(new StringContent("true"), "do_ocr"); // But allow OCR if needed
                    formData.Add(new StringContent("false"), "abort_on_error"); // Continue processing even with errors

                    var response = await _httpClient.PostAsync(requestUrl, formData);

                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"✅ Synchronous conversion succeeded with backend: {backend}");
                        return content;
                    }
                    
                    var error = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"❌ Backend {backend} failed: {error}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Backend {backend} threw exception: {ex.Message}");
                }
            }

            throw new Exception($"All PDF backends failed for synchronous conversion");
        }

        private async Task<string> TryAsynchronousConversion(string pdfFilePath, string format)
        {
            // Start async conversion
            var taskId = await StartConversionAsync(pdfFilePath, format);
            Console.WriteLine($"Started async conversion with task ID: {taskId}");
            
            // Poll for completion and get result
            var result = await WaitForCompletionAsync(taskId, format);
            
            return result;
        }

        private async Task<string> StartConversionAsync(string pdfFilePath, string format)
        {
            var requestUrl = $"{_doclingApiUrl}/v1/convert/file/async";

            await using var fileStream = File.OpenRead(pdfFilePath);
            using var streamContent = new StreamContent(fileStream);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");

            using var formData = new MultipartFormDataContent();
            formData.Add(streamContent, "files", Path.GetFileName(pdfFilePath));
            formData.Add(new StringContent(format), "to_formats"); // Use the format parameter
            
            // Use more robust PDF processing options for problematic PDFs
            formData.Add(new StringContent("pypdfium2"), "pdf_backend"); // Try different backend
            formData.Add(new StringContent("false"), "force_ocr"); // Don't force OCR initially
            formData.Add(new StringContent("true"), "do_ocr"); // But allow OCR if needed
            formData.Add(new StringContent("false"), "abort_on_error"); // Continue processing even with errors
            formData.Add(new StringContent("600.0"), "document_timeout"); // Increase timeout

            var response = await _httpClient.PostAsync(requestUrl, formData);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Failed to start async conversion (Status: {response.StatusCode}): {error}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var taskResponse = JsonConvert.DeserializeObject<AsyncTaskResponse>(responseContent);
            
            if (taskResponse?.task_id == null)
            {
                throw new Exception($"Invalid task response: {responseContent}");
            }

            return taskResponse.task_id;
        }

        private async Task<string> WaitForCompletionAsync(string taskId, string format)
        {
            var maxAttempts = 100; // Increase this; complex PDFs can take 1-2 mins
            var delay = TimeSpan.FromSeconds(2);

            Console.WriteLine($"Polling for task completion (task ID: {taskId})...");

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                // 1. Check Status
                var statusUrl = $"{_doclingApiUrl}/v1/status/poll/{taskId}";
                var statusResponse = await _httpClient.GetAsync(statusUrl);

                if (statusResponse.IsSuccessStatusCode)
                {
                    var statusContent = await statusResponse.Content.ReadAsStringAsync();
                    var statusObj = JsonConvert.DeserializeObject<TaskStatusResponse>(statusContent);

                    Console.WriteLine($"Task Status: {statusObj?.task_status} (Attempt {attempt + 1})");

                    if (statusObj?.task_status == "success") // Or "completed" depending on version
                    {
                        // 2. Fetch Result (Only once status is success)
                        var resultUrl = $"{_doclingApiUrl}/v1/result/{taskId}";
                        var resultResponse = await _httpClient.GetAsync(resultUrl);

                        if (resultResponse.IsSuccessStatusCode)
                        {
                            var resultContent = await resultResponse.Content.ReadAsStringAsync();
                            // Parse your specific format from the result
                            if (TryParseResult(resultContent, format, out string finalOutput))
                            {
                                return finalOutput;
                            }
                        }
                    }
                    else if (statusObj?.task_status == "failure" || statusObj?.task_status == "error")
                    {
                        throw new Exception($"Task failed: {statusContent}");
                    }
                }

                // Wait before next poll
                await Task.Delay(delay);
            }

            throw new TimeoutException($"Task {taskId} timed out after {maxAttempts} attempts.");
        }

        private bool TryParseResult(string content, string format, out string resultContent)
        {
            resultContent = string.Empty;

            try
            {
                // First, try to parse as a conversion result JSON
                var conversionResult = JsonConvert.DeserializeObject<ConversionResult>(content);
                if (conversionResult?.document != null)
                {
                    if (format == "json" && conversionResult.document.json_content != null)
                    {
                        resultContent = conversionResult.document.json_content;
                        return true;
                    }
                    else if (format == "md" && conversionResult.document.md_content != null)
                    {
                        resultContent = conversionResult.document.md_content;
                        return true;
                    }
                }

                // Check if it's a task status with completed status
                var taskStatus = JsonConvert.DeserializeObject<TaskStatusResponse>(content);
                if (taskStatus?.task_status == "completed" || taskStatus?.task_status == "success")
                {
                    Console.WriteLine($"Task completed, but {format} content not found in response");
                    return false;
                }
            }
            catch (JsonException)
            {
                // If it's not JSON, check if it looks like the expected format content
                if (content.Length > 0 && !content.Trim().StartsWith("{") && !content.Trim().StartsWith("<"))
                {
                    // Might be direct content
                    resultContent = content;
                    return true;
                }
            }

            return false;
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }

    // Response models for JSON deserialization
    public class AsyncTaskResponse
    {
        public string? task_id { get; set; }
        public string? task_type { get; set; }
        public string? task_status { get; set; }
        public int? task_position { get; set; }
        public object? task_meta { get; set; }
    }

    public class TaskStatusResponse
    {
        public string? task_id { get; set; }
        public string? task_type { get; set; }
        public string? task_status { get; set; }
        public int? task_position { get; set; }
        public object? task_meta { get; set; }
    }

    public class ConversionResult
    {
        public DocumentResponse? document { get; set; }
        public string? status { get; set; }
        public object? errors { get; set; }
        public double? processing_time { get; set; }
    }

    public class DocumentResponse
    {
        public string? filename { get; set; }
        public string? md_content { get; set; }
        public string? json_content { get; set; }
        public string? html_content { get; set; }
        public string? text_content { get; set; }
        public string? doctags_content { get; set; }
    }
}