using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitHubRepositoryAnalyzerApp
{
    public class TagsGitConfiguration
    {
        public static void ConfigureGitHooks()
        {
            var processInfo = new ProcessStartInfo("powershell.exe", "-ExecutionPolicy Bypass -File setup-hooks.ps1")
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using (var process = new Process { StartInfo = processInfo })
            {
                process.Start();
                process.WaitForExit();

                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();

                if (process.ExitCode != 0)
                {
                    Console.WriteLine("Falha na configuração dos hooks do Git:");
                    Console.WriteLine(error);
                    Environment.Exit(process.ExitCode);
                }
                else
                {
                    Console.WriteLine("Hooks do Git configurados com sucesso.");
                    Console.WriteLine(output);
                }
            }
        }
    }
}
