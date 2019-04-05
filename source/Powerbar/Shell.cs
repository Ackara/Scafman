using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Acklann.Powerbar
{
    //https://stackoverflow.com/questions/25772622/how-do-i-echo-into-an-existing-cmd-window

    public class Shell
    {
        public static readonly Regex SwitchPattern = new Regex(@"^[\|>]+", RegexOptions.Compiled);

        public static void Invoke(string location, string command, Switch options, VSContext context, Action<string> callback)
        {
            if (string.IsNullOrEmpty(command)) return;

            void dataHandler(object sender, DataReceivedEventArgs e) { callback?.Invoke(e.Data); }
            using (var exe = new Process { StartInfo = CreateProcessInfo(location, options, context) })
            {
                exe.ErrorDataReceived += dataHandler;
                exe.OutputDataReceived += dataHandler;
                exe.StartInfo.Arguments = GetArguments(command, options, context);

                exe.Start();
                exe.BeginOutputReadLine();
                exe.BeginErrorReadLine();
                exe.WaitForExit((int)TimeSpan.FromMinutes(5).TotalMilliseconds);
                exe.Close();
            }
        }

        public static string GetArguments(string command, Switch options, VSContext context)
        {
            string noExit = (options.HasFlag(Switch.RunCommandInWindow) ? "-NoExit " : string.Empty);
            string pipelineObject = (options.HasFlag(Switch.PipeContext) ?
                $"{context} | ConvertFrom-Json | " : string.Empty);

            return $"{noExit}-ExecutionPolicy Bypass -NonInteractive -Command \"{Escape(pipelineObject)}{Escape(command)}\"";
        }

        public static string CompleteCommand(string input, in string[] commandList)
        {
            if (string.IsNullOrEmpty(input)) return input;

            string[] args = input.Split(' ');
            string target = args[args.Length - 1];
            target = (from c in commandList where c.StartsWith(target, StringComparison.OrdinalIgnoreCase) select c).FirstOrDefault();
            if (!string.IsNullOrEmpty(target)) args[args.Length - 1] = target;

            return string.Join(" ", args);
        }

        public static Switch ExtractOptions(ref string command)
        {
            if (string.IsNullOrEmpty(command)) return Switch.None;
            var options = Switch.AddFile;

            Match match = SwitchPattern.Match(command);
            if (match.Success)
            {
                options = Switch.None;

                if (match.Value.Contains('>')) options |= (Switch.RunCommand);
                if (match.Value.Contains(">>")) options |= (Switch.RunCommandInWindow);

                if (match.Value.Contains('|')) options |= (Switch.RunCommand | Switch.PipeContext);
                if (match.Value.Contains("||")) options |= Switch.RunCommandInWindow;
            }

            command = command.TrimStart('|', '>', ' ');
            return options;
        }

        public static string[] GetCommands()
        {
            var commands = new List<string>();
            var pattern = new Regex("^[a-z0-9]+-?[a-z0-9]+$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

            var args = CreateProcessInfo(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), Switch.None, new VSContext());
            using (var exe = new Process() { StartInfo = args })
            {
                exe.StartInfo.Arguments = GetArguments("Get-Command | select Name", Switch.None, new VSContext());
                exe.Start();

                if (exe.StandardOutput != null)
                {
                    string line = null;
                    exe.StandardOutput?.ReadLine();
                    exe.StandardOutput?.ReadLine();
                    while (!exe.StandardOutput.EndOfStream)
                    {
                        line = exe.StandardOutput.ReadLine().Trim();
                        if (pattern.IsMatch(line)) commands.Add(line);
                    }
                }
            }

            return commands.ToArray();
        }

        private static string GetMSBuildPath()
        {
            return (from folder in Directory.EnumerateDirectories(@"C:\Windows\Microsoft.NET\Framework\")
                    orderby folder descending
                    let file = Path.Combine(folder, "MSBuild.exe")
                    where File.Exists(file)
                    select folder).FirstOrDefault();
        }

        private static string Escape(string command)
        {
            return command.Replace("\"", "\"\"\"")
                          .Replace("\\", "\\\\")
                          .Trim();
        }

        private static ProcessStartInfo CreateProcessInfo(string location, Switch options, VSContext context)
        {
            bool openingWindow = options.HasFlag(Switch.RunCommandInWindow);

            var info = new ProcessStartInfo("powershell")
            {
                //LoadUserProfile = true,
                WorkingDirectory = location,
                CreateNoWindow = !openingWindow,
                UseShellExecute = openingWindow,
                RedirectStandardError = !openingWindow,
                RedirectStandardOutput = !openingWindow,
            };

            if (!openingWindow)
            {
                string msbuild = GetMSBuildPath();
                if (!string.IsNullOrEmpty(msbuild)) info.EnvironmentVariables["PATH"] = (msbuild + ';' + info.EnvironmentVariables["PATH"]);
            }

            return info;
        }
    }
}