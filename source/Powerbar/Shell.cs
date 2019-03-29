using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Acklann.Powerbar
{
    //https://stackoverflow.com/questions/25772622/how-do-i-echo-into-an-existing-cmd-window

    public class Shell
    {
        public static readonly Regex Switches = new Regex(@"^[/\\\|>]+", RegexOptions.Compiled);

        public static void Invoke(string location, string command, ShellOptions options, VSContext context, Action<string> callback)
        {
            if (string.IsNullOrEmpty(command)) return;

            void dataHandler(object sender, DataReceivedEventArgs e) { callback?.Invoke(e.Data); }
            using (var exe = new Process { StartInfo = CreateProcessInfo(location, command, options, context) })
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

        public static ShellOptions GetOptions(ref string command)
        {
            if (string.IsNullOrEmpty(command)) return ShellOptions.None;
            var options = ShellOptions.None;

            Match match = Switches.Match(command);
            if (match.Success)
            {
                if (match.Value.Contains('|')) options |= ShellOptions.PipeContext;
                if (match.Value.Contains('>')) options |= ShellOptions.CreateWindow;
                if (match.Value.Contains('/')) options |= ShellOptions.CreateNewFile;
                if (match.Value.Contains('\\')) options |= ShellOptions.CreateNewFile;
            }

            command = command.TrimStart('|', '/', '\\', '>', ' ');
            return options;
        }

        internal static ProcessStartInfo CreateProcessInfo(string location, string command, ShellOptions options, VSContext context)
        {
            string pipelineObject = (options.HasFlag(ShellOptions.PipeContext) ?
                $"{context} | ConvertFrom-Json | " : string.Empty);

            var info = new ProcessStartInfo("powershell")
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                WorkingDirectory = location,
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