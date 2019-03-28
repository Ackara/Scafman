using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Acklann.Powerbar
{
    public static class Shell
    {
        public static void Invoke(string command, VSContext context, Action<string> callback)
        {
            if (string.IsNullOrEmpty(command)) return;

            void dataHandler(object s, DataReceivedEventArgs e) { callback?.Invoke(e.Data); }
            using (var exe = new Process { StartInfo = CreateArgs(command, context) })
            {
                exe.ErrorDataReceived += dataHandler;
                exe.OutputDataReceived += dataHandler;

                exe.Start();
                exe.BeginErrorReadLine();
                exe.BeginOutputReadLine();
                exe.WaitForExit((int)TimeSpan.FromMinutes(5).TotalMilliseconds);
                exe.Close();
            }
        }

        internal static ProcessStartInfo CreateArgs(string command, VSContext context)
        {
            string pipelineObject = string.Empty;
            if (command.StartsWith("|") || command.StartsWith(">"))
            {
                command = command.TrimStart('|', '>', ' ');
                pipelineObject = $"{context} | ConvertFrom-Json | ";
            }

            var info = new ProcessStartInfo("powershell")
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                Arguments = $"-ExecutionPolicy Bypass -NonInteractive -Command \"{Escape(pipelineObject)}{Escape(command)}\""
                //-ExecutionPolicy Bypass -NonInteractive -Command
            };
            AddMSBuildPath(info);

            return info;
        }

        private static ProcessStartInfo AddMSBuildPath(ProcessStartInfo info)
        {
            var msbuild = (from folder in Directory.EnumerateDirectories(@"C:\Windows\Microsoft.NET\Framework\")
                           orderby folder descending
                           let file = Path.Combine(folder, "MSBuild.exe")
                           where File.Exists(file)
                           select folder).FirstOrDefault();

            if (!string.IsNullOrEmpty(msbuild))
            {
                info.EnvironmentVariables["PATH"] = (msbuild + ';' + info.EnvironmentVariables["PATH"]);
            }

            return info;
        }

        private static string Escape(string command)
        {
            return command.Replace("\"", "\"\"\"")
                          .Replace("\\", "\\\\")
                          .Trim();
        }
    }
}