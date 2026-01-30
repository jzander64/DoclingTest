# Docling Document Converter

A C# console application that processes PDF documents using two different Docling conversion methods:
- **Black Box (CLI)**: Uses Python CLI through process execution
- **API Client**: Uses Docling Serve API through HTTP requests

## Features

- Menu-driven interface for selecting conversion methods
- Performance tracking and detailed reporting
- Configurable paths through Settings.json
- Automatic directory creation
- Comprehensive error handling and logging
- Markdown-formatted performance reports

## Setup

### 1. Configuration

Edit the `Settings.json` file to configure your paths:

```json
{
  "RawDocumentsPath": "C:\\Users\\JeremyZander\\Desktop\\Spike\\DoclingTest\\console_test\\Raw_Documents",
  "ProcessedDocumentsPath": "C:\\Users\\JeremyZander\\Desktop\\Spike\\DoclingTest\\console_test\\Processed_Documents",
  "PerformanceReportsPath": "C:\\Users\\JeremyZander\\Desktop\\Spike\\DoclingTest\\console_test\\Performance",
  "PythonExePath": "C:\\path\\to\\your\\venv\\Scripts\\python.exe",
  "DoclingScriptPath": "C:\\path\\to\\your\\venv\\Lib\\site-packages\\docling\\cli.py",
  "DoclingApiUrl": "http://localhost:5001"
}
```

### 2. For CLI Method (Black Box)

1. Install Python with Docling package in a virtual environment
2. Update `PythonExePath` to point to your virtual environment's Python executable
3. Update `DoclingScriptPath` to point to the docling CLI script

### 3. For API Method

1. Start Docling Serve on the configured port (default: http://localhost:5001)
2. Ensure the API is accessible from the application

### 4. Folder Structure

The application will automatically create the following directories:
- `Raw_Documents/`: Place your PDF files here
- `Processed_Documents/`: Converted Markdown files will be saved here
- `Performance/`: Performance reports will be generated here

## Usage

1. Build and run the application:
   ```bash
   dotnet run
   ```

2. Select your preferred conversion method:
   - **Option 1**: Black Box (CLI) Conversion
   - **Option 2**: Docling Serve (API) Conversion
   - **Option 3**: Exit

3. The application will:
   - Process all PDF files in the Raw_Documents folder
   - Convert them to Markdown format
   - Save results to the Processed_Documents folder
   - Generate a detailed performance report

## Performance Reports

Performance reports include:

### Session Summary
- Total files processed
- Success/failure counts
- Success rate percentage
- Total input/output file sizes

### Performance Metrics
- Average processing time
- Fastest/slowest processing times
- Throughput (files per second)

### Individual File Results
- Detailed table with file-by-file results
- Input/output sizes and processing times
- Success/failure status

### Error Details
- List of failed files with error messages

## Dependencies

- .NET 8.0
- Newtonsoft.Json 13.0.3

## Error Handling

The application includes comprehensive error handling for:
- Missing configuration files
- Invalid file paths
- Network connectivity issues (API method)
- Python execution errors (CLI method)
- File I/O operations

## Example Report Output

```markdown
# Docling Performance Report

**Session ID:** a1b2c3d4
**Generated:** 2025-11-14 10:30:15
**Duration:** 45.67 seconds

## 📊 Session Summary

| Metric | Value |
|--------|-------|
| **Total Files Processed** | 10 |
| **Successful** | 9 |
| **Failed** | 1 |
| **Success Rate** | 90.0% |
| **Total Input Size** | 15.43 MB |
| **Total Output Size** | 3.21 MB |

## ⚡ Performance Metrics

| Metric | Value |
|--------|-------|
| **Average Processing Time** | 4.563 seconds |
| **Fastest Processing** | 1.234 seconds |
| **Slowest Processing** | 8.901 seconds |
| **Throughput (Files/sec)** | 0.197 files/second |
```