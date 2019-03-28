using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Acklann.Powerbar
{
    //https://stackoverflow.com/questions/25772622/how-do-i-echo-into-an-existing-cmd-window


    public static class Shell
    {


        public static void Invoke(string command, ShellOptions options, VSContext context, Action<string> callback)
        {
            if (string.IsNullOrEmpty(command)) return;

            void dataHandler(object s, DataReceivedEventArgs e) { callback?.Invoke(e.Data); }
            using (var exe = new Process { StartInfo = CreateArgs(command, options, context) })
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

        public static ProcessStartInfo CreateArgs(string command, ShellOptions options, VSContext context)
        {
            string pipelineObject = (options.HasFlag(ShellOptions.PipeContext) ?
                $"{context} | ConvertFrom-Json | " : string.Empty);

            var info = new ProcessStartInfo("powershell")
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                Arguments = $"-ExecutionPolicy Bypass -NonInteractive -Command \"{Escape(pipelineObject)}{Escape(command)}\""
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