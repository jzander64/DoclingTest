using Newtonsoft.Json;

namespace DoclingNet
{
    public class AppConfig
    {
        public string RawDocumentsPath { get; set; } = string.Empty;
        public string ProcessedDocumentsPath { get; set; } = string.Empty;
        public string PerformanceReportsPath { get; set; } = string.Empty;
        public string PythonExePath { get; set; } = string.Empty;
        public string DoclingScriptPath { get; set; } = string.Empty;
        public string DoclingApiUrl { get; set; } = string.Empty;

        public static AppConfig LoadFromFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Settings file not found: {filePath}");
            }

            var json = File.ReadAllText(filePath);
            var config = JsonConvert.DeserializeObject<AppConfig>(json);
            
            if (config == null)
            {
                throw new InvalidOperationException("Failed to deserialize settings file");
            }

            return config;
        }

        public void EnsureDirectoriesExist()
        {
            Directory.CreateDirectory(RawDocumentsPath);
            Directory.CreateDirectory(ProcessedDocumentsPath);
            Directory.CreateDirectory(PerformanceReportsPath);
        }
    }
}