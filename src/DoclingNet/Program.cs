using System.Diagnostics;
using DoclingNet;

class Program
{
    private static AppConfig? _config;
    private static readonly string SettingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Settings.json");

    static async Task Main(string[] args)
    {
        Console.WriteLine("=== Docling Document Converter ===");
        Console.WriteLine();

        try
        {
            // Load configuration
            _config = AppConfig.LoadFromFile(SettingsPath);
            _config.EnsureDirectoriesExist();

            Console.WriteLine($"Configuration loaded successfully!");
            Console.WriteLine($"Raw Documents: {_config.RawDocumentsPath}");
            Console.WriteLine($"Processed Documents: {_config.ProcessedDocumentsPath}");
            Console.WriteLine($"Performance Reports: {_config.PerformanceReportsPath}");
            Console.WriteLine();

            // Main application loop
            bool exitRequested = false;
            while (!exitRequested)
            {
                exitRequested = await RunConversion();

                if (!exitRequested)
                {
                    Console.WriteLine("\nPress any key to continue...");
                    Console.ReadKey();
                    Console.Clear();
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Application error: {ex.Message}");
            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
    }

    private static string GetOutputFormat()
    {
        Console.WriteLine("\n=== PDF Document Converter ===");
        Console.WriteLine("Please select output format:");
        Console.WriteLine("1. Markdown (.md)");
        Console.WriteLine("2. JSON (.json)");
        Console.WriteLine("3. Exit");
        Console.Write("\nEnter your choice (1-3): ");
        
        while (true)
        {
            var choice = Console.ReadLine()?.Trim();
            switch (choice)
            {
                case "1":
                    return "markdown";
                case "2":
                    return "json";
                case "3":
                    return "exit";
                default:
                    Console.Write("Invalid choice. Please select 1, 2, or 3: ");
                    break;
            }
        }
    }

    private static async Task<bool> RunConversion()
    {
        // Ask user for output format
        string outputFormat = GetOutputFormat();
        
        if (outputFormat == "exit")
        {
            Console.WriteLine("Goodbye!");
            return true; // Exit requested
        }
        
        Console.WriteLine("\n=== PDF Conversion using Docling API ===");
        
        var tracker = new PerformanceTracker();
        using var apiClient = new DoclingApiClient(_config!);

        if (outputFormat == "json")
        {
            string jsonFolder = Path.Combine(_config!.ProcessedDocumentsPath, "json");
            Directory.CreateDirectory(jsonFolder);
            
            await ProcessFiles(tracker, async (inputPath, outputPath) =>
            {
                var jsonContent = await apiClient.ConvertPdfToJson(inputPath);
                await File.WriteAllTextAsync(outputPath, jsonContent);
                return jsonContent;
            }, ".json", jsonFolder);
        }
        else
        {
            string markdownFolder = Path.Combine(_config!.ProcessedDocumentsPath, "markdown");
            Directory.CreateDirectory(markdownFolder);
            
            await ProcessFiles(tracker, async (inputPath, outputPath) =>
            {
                var markdownContent = await apiClient.ConvertPdfToMarkdown(inputPath);
                await File.WriteAllTextAsync(outputPath, markdownContent);
                return markdownContent;
            }, ".md", markdownFolder);
        }

        await tracker.SaveReportAsync(_config!.PerformanceReportsPath);
        return false; // Don't exit
    }

    private static async Task ProcessFiles(PerformanceTracker tracker, Func<string, string, Task<string>> convertFunc, string outputExtension = ".md", string? outputFolder = null)
    {
        var pdfFiles = Directory.GetFiles(_config!.RawDocumentsPath, "*.pdf");
        
        if (pdfFiles.Length == 0)
        {
            Console.WriteLine($"No PDF files found in {_config.RawDocumentsPath}");
            return;
        }

        Console.WriteLine($"Found {pdfFiles.Length} PDF files to process...\n");
        
        // Use specific output folder or default to processed documents path
        string targetFolder = outputFolder ?? _config.ProcessedDocumentsPath;
        Console.WriteLine($"Output will be saved to: {targetFolder}\n");

        int processedCount = 0;
        foreach (var pdfFile in pdfFiles)
        {
            var fileName = Path.GetFileName(pdfFile);
            var outputFileName = Path.ChangeExtension(fileName, outputExtension);
            var outputPath = Path.Combine(targetFolder, outputFileName);

            Console.Write($"Processing {fileName}... ");

            var stopwatch = Stopwatch.StartNew();
            var result = new FileProcessingResult
            {
                FileName = fileName,
                InputSizeBytes = new FileInfo(pdfFile).Length
            };

            try
            {
                await convertFunc(pdfFile, outputPath);
                stopwatch.Stop();

                result.Duration = stopwatch.Elapsed;
                result.IsSuccess = true;
                result.OutputSizeBytes = File.Exists(outputPath) ? new FileInfo(outputPath).Length : 0;

                Console.WriteLine($"✅ Success ({result.Duration.TotalSeconds:F2}s)");
                processedCount++;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                result.Duration = stopwatch.Elapsed;
                result.IsSuccess = false;
                result.ErrorMessage = ex.Message;

                Console.WriteLine($"❌ Failed: {ex.Message}");
            }

            tracker.AddResult(result);
        }

        Console.WriteLine($"\nProcessing complete! {processedCount}/{pdfFiles.Length} files converted successfully.");
    }
}
