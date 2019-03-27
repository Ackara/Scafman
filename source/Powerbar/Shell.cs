using System;
using System.Diagnostics;

namespace Acklann.Powerbar
{
    public static class Shell
    {
        public static void Invoke(string command)
        {
        }

        public static void Invoke(string command, Context context, Action<string> callback)
        {
            if (string.IsNullOrEmpty(command)) return;

            void dataHandler(object s, DataReceivedEventArgs e) { callback?.Invoke(e.Data); }

            var args = new ProcessStartInfo("powershell")
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                Arguments = $"-ExecutionPolicy Bypass -NonInteractive -Command "
            };

            if (context == null)
                args.Arguments += command;
            else
                args.Arguments += $" {command}";

            using (var exe = new Process { StartInfo = args })
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

        internal static ProcessStartInfo SetArguments(Context context)
        {
            var args = new ProcessStartInfo()
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };

            switch (context.Tool.ToLowerInvariant())
            {
                case "powershell>":
                    args.FileName = "powershell";
                    args.Arguments = "-ExecutionPolicy Bypass -NonInteractive -Command ";
                    break;
            }

            return args;
        }
            //Resolve-Path HKLM:\SOFTWARE\Microsoft\MSBuild\ToolsVersions\* | Get-ItemProperty -Name MSBuildToolsPath
    }
}