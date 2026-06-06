using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SRGMTool
{
    public class PythonRunner
    {
        private readonly string _scriptDir;

        public PythonRunner()
        {
            // Script is copied next to the executable
            _scriptDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "python");
        }

        public async Task<AnalysisResult> RunAnalysisAsync(string csvPath)
        {
            string scriptPath = Path.Combine(_scriptDir, "srgm.py");
            if (!File.Exists(scriptPath))
                throw new FileNotFoundException($"Python script not found at: {scriptPath}");

            string python = "python";
            string stdout = string.Empty;
            string stderr = string.Empty;
            int exitCode = -1;

            try
            {
                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = python,
                        Arguments = $"\"{scriptPath}\" \"{csvPath}\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WorkingDirectory = _scriptDir
                    }
                };

                process.Start();
                var stdoutTask = process.StandardOutput.ReadToEndAsync();
                var stderrTask = process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                stdout = await stdoutTask;
                stderr = await stderrTask;
                exitCode = process.ExitCode;
            }
            catch (Exception ex) when (ex.Message.Contains("cannot find") || ex is System.ComponentModel.Win32Exception)
            {
                throw new InvalidOperationException(
                    "Python not found. Please install Python 3 and ensure it's on your PATH.", ex);
            }

            if (exitCode != 0 || string.IsNullOrWhiteSpace(stdout))
            {
                string detail = string.IsNullOrWhiteSpace(stderr) ? "No error details available." : stderr;
                throw new InvalidOperationException($"Python script failed (exit code {exitCode}):\n{detail}");
            }

            var result = JsonConvert.DeserializeObject<AnalysisResult>(stdout);
            if (result == null)
                throw new InvalidOperationException("Failed to parse Python output as JSON.");

            if (!string.IsNullOrWhiteSpace(result.Error))
                throw new InvalidOperationException($"Script error: {result.Error}");

            return result;
        }
    }
}
