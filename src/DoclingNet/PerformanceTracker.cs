using System.Diagnostics;
using System.Text;

namespace DoclingNet
{
    public class FileProcessingResult
    {
        public string FileName { get; set; } = string.Empty;
        public long InputSizeBytes { get; set; }
        public long OutputSizeBytes { get; set; }
        public TimeSpan Duration { get; set; }
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class PerformanceTracker
    {
        private readonly List<FileProcessingResult> _results = new();
        private readonly Stopwatch _sessionStopwatch = new();
        private readonly string _sessionId;
        private readonly DateTime _startTime;

        public PerformanceTracker()
        {
            _sessionId = Guid.NewGuid().ToString("N")[..8];
            _startTime = DateTime.Now;
            _sessionStopwatch.Start();
        }

        public void AddResult(FileProcessingResult result)
        {
            _results.Add(result);
        }

        public void StopSession()
        {
            _sessionStopwatch.Stop();
        }

        public string GenerateMarkdownReport()
        {
            StopSession();

            var totalFiles = _results.Count;
            var successfulFiles = _results.Count(r => r.IsSuccess);
            var failedFiles = totalFiles - successfulFiles;
            var successRate = totalFiles > 0 ? (successfulFiles * 100.0 / totalFiles) : 0;

            var totalInputSize = _results.Sum(r => r.InputSizeBytes) / (1024.0 * 1024.0); // MB
            var totalOutputSize = _results.Sum(r => r.OutputSizeBytes) / (1024.0 * 1024.0); // MB

            var successfulResults = _results.Where(r => r.IsSuccess).ToList();
            var averageProcessingTime = successfulResults.Any() ? successfulResults.Average(r => r.Duration.TotalSeconds) : 0;
            var fastestProcessing = successfulResults.Any() ? successfulResults.Min(r => r.Duration.TotalSeconds) : 0;
            var slowestProcessing = successfulResults.Any() ? successfulResults.Max(r => r.Duration.TotalSeconds) : 0;
            var throughput = _sessionStopwatch.Elapsed.TotalSeconds > 0 ? successfulFiles / _sessionStopwatch.Elapsed.TotalSeconds : 0;

            var sb = new StringBuilder();
            
            sb.AppendLine("# Docling Performance Report");
            sb.AppendLine();
            sb.AppendLine($"**Session ID:** {_sessionId}");
            sb.AppendLine($"**Generated:** {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"**Duration:** {_sessionStopwatch.Elapsed.TotalSeconds:F2} seconds");
            sb.AppendLine();
            
            sb.AppendLine("## 📊 Session Summary");
            sb.AppendLine();
            sb.AppendLine("| Metric | Value |");
            sb.AppendLine("|--------|-------|");
            sb.AppendLine($"| **Total Files Processed** | {totalFiles} |");
            sb.AppendLine($"| **Successful** | {successfulFiles} |");
            sb.AppendLine($"| **Failed** | {failedFiles} |");
            sb.AppendLine($"| **Success Rate** | {successRate:F1}% |");
            sb.AppendLine($"| **Total Input Size** | {totalInputSize:F2} MB |");
            sb.AppendLine($"| **Total Output Size** | {totalOutputSize:F2} MB |");
            sb.AppendLine();
            
            sb.AppendLine("## ⚡ Performance Metrics");
            sb.AppendLine();
            sb.AppendLine("| Metric | Value |");
            sb.AppendLine("|--------|-------|");
            sb.AppendLine($"| **Average Processing Time** | {averageProcessingTime:F3} seconds |");
            sb.AppendLine($"| **Fastest Processing** | {fastestProcessing:F3} seconds |");
            sb.AppendLine($"| **Slowest Processing** | {slowestProcessing:F3} seconds |");
            sb.AppendLine($"| **Throughput (Files/sec)** | {throughput:F3} files/second |");
            sb.AppendLine();
            
            sb.AppendLine("## 📁 Individual File Results");
            sb.AppendLine();
            sb.AppendLine("| Filename | Input Size (KB) | Duration (s) | Status | Output Size (KB) |");
            sb.AppendLine("|----------|-----------------|--------------|--------|------------------|");
            
            foreach (var result in _results)
            {
                var inputSizeKb = result.InputSizeBytes / 1024.0;
                var outputSizeKb = result.OutputSizeBytes / 1024.0;
                var status = result.IsSuccess ? "✅ success" : "❌ failed";
                
                sb.AppendLine($"| {result.FileName} | {inputSizeKb:F1} | {result.Duration.TotalSeconds:F3} | {status} | {outputSizeKb:F1} |");
            }
            sb.AppendLine();

            var failedResults = _results.Where(r => !r.IsSuccess).ToList();
            if (failedResults.Any())
            {
                sb.AppendLine("## ❌ Failed Files");
                sb.AppendLine();
                foreach (var failed in failedResults)
                {
                    sb.AppendLine($"- **{failed.FileName}**: {failed.ErrorMessage ?? "Unknown error"}");
                }
                sb.AppendLine();
            }

            sb.AppendLine("---");
            sb.AppendLine("*Generated by Docling Conversion Monitor*");
            sb.AppendLine($"*Report saved: {DateTime.Now:yyyy-MM-dd HH:mm:ss}*");

            return sb.ToString();
        }

        public async Task SaveReportAsync(string reportPath)
        {
            var report = GenerateMarkdownReport();
            var fileName = $"DoclingReport_{_sessionId}_{_startTime:yyyyMMdd_HHmmss}.md";
            var fullPath = Path.Combine(reportPath, fileName);
            
            await File.WriteAllTextAsync(fullPath, report);
            Console.WriteLine($"Performance report saved to: {fullPath}");
        }
    }
}