using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace Acklann.Scafman
{
    public class Shell
    {
        public static readonly Regex SwitchPattern = new Regex(@"^[\|>]+", RegexOptions.Compiled);

        public static void Invoke(string location, string command, Switch options, ProjectContext context, Action<string> callback)
        {
            if (string.IsNullOrEmpty(command)) return;

            void dataHandler(object sender, DataReceivedEventArgs e) { callback?.Invoke(e.Data); }
            using (var exe = new Process { StartInfo = CreateProcessInfo(location, context) })
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

        public static string GetArguments(string command, Switch options, ProjectContext context)
        {
            return $"-ExecutionPolicy Bypass -NonInteractive -NoProfile -Command \"{Escape(command)}\"";
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

        public static string[] GetCommands()
        {
            var commands = new List<string>();
            var pattern = new Regex("^[a-z0-9]+-?[a-z0-9]+$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

            var args = CreateProcessInfo(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), new ProjectContext());
            using (var exe = new Process() { StartInfo = args })
            {
                exe.StartInfo.Arguments = GetArguments("Get-Command | select Name", Switch.None, new ProjectContext());
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

        private static string Escape(string command)
        {
            return command.Replace("\"", "\"\"\"")
                          .Replace("\\", "\\\\")
                          .Trim();
        }

        private static ProcessStartInfo CreateProcessInfo(string location, ProjectContext context)
        {
            var info = new ProcessStartInfo("powershell")
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                WorkingDirectory = location,
            };

            return info;
        }
    }
}