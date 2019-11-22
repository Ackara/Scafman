using Acklann.GlobN;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Acklann.Scafman
{
    public class Template
    {
        private const char separator = ';';

        public static string[] Split(string fileList)
        {
            if (string.IsNullOrEmpty(fileList)) return new string[0];

            Group group; string input = "";
            var result = new List<string>();
            var pattern = new Regex(@"\((?<item>[^\)]+)\)", RegexOptions.IgnoreCase);
            string[] list = fileList.Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < list.Length; i++)
            {
                group = null;
                input = list[i].Trim();
                while (pattern.IsMatch(input))
                {
                    Match match = pattern.Match(input);
                    group = match.Groups["item"];
                    if (group.Success)
                    {
                        input = input.Remove(match.Index, match.Length);
                        input = input.Insert(match.Index, "{0}");

                        foreach (string value in group.Value.Split('|'))
                        {
                            result.Add(string.Format(input, value.Trim()));
                        }
                    }
                }

                if (group == null) result.Add(input);
            }
            return result.ToArray();
        }

        public static IEnumerable<Command> Interpret(string input)
        {
            if (string.IsNullOrEmpty(input)) yield break;
            foreach (string item in Split(input)) yield return Command.Parse(item);
        }

        public static string GuessExtension(string projectFile, string location)
        {
            if (Directory.Exists(location))
            {
                var extensions = (from x in Directory.EnumerateFiles(location) select Path.GetExtension(x))
                    .Take(3)
                    .Distinct();
                if (extensions.Count() == 1) return extensions.First();
            }

            if (string.IsNullOrEmpty(projectFile)) return string.Empty;
            else
            {
                string extension = Path.GetExtension(projectFile).Replace("proj", string.Empty).ToLowerInvariant();
                switch (extension)
                {
                    case ".nj": return ".ts";
                    case ".pss": return ".ps1";
                    case ".vcx": return ".cpp";
                    default: return extension;
                }
            }
        }

        public static string RemoveCaret(string content, out int position)
        {
            position = 0;
            if (string.IsNullOrEmpty(content)) return string.Empty;

            Match match = Regex.Match(content, @"(\$caret\$|\$end\$)", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                position = match.Index;
                return content.Remove(match.Index, match.Length);
            }
            return content;
        }

        public static string ExpandItemGroups(string input, string configurationFilePath)
        {
            if (string.IsNullOrEmpty(input)) return input;
            return ExpandItemGroup(input, ItemGroup.ReadFile(configurationFilePath));
        }

        public static string ExpandItemGroup(string input, params ItemGroup[] itemGroups)
        {
            if (string.IsNullOrEmpty(input)) return input;
            var pattern = new Regex(@"@\((?<name>[a-z_0-9-]+)\)", RegexOptions.IgnoreCase);

            Match match = pattern.Match(input);
            while (match.Success)
            {
                string key = match.Groups["name"].Value;
                string value = (from i in itemGroups
                                where string.Equals(i.Name, key, StringComparison.OrdinalIgnoreCase)
                                select string.Join(char.ToString(separator), i.FileList)
                                ).FirstOrDefault();
                input = input.Replace(match.Value, (value ?? string.Empty));
                match = pattern.Match(input);
            }

            return input;
        }

        public static string GetSubfolder(string filePath, string projectFolder, string currentWorkingDirectory)
        {
            if (string.IsNullOrEmpty(filePath)) return string.Empty;
            if (string.IsNullOrEmpty(projectFolder)) return string.Empty;
            if (string.IsNullOrEmpty(currentWorkingDirectory)) return string.Empty;

            string absolutePath = ((Glob)filePath).ExpandPath(currentWorkingDirectory).Replace('/', '\\');
            string subfolder = Path.GetDirectoryName(absolutePath.Replace(projectFolder.Replace('/', '\\'), string.Empty)).Trim('/', '\\', ' ');
            return (subfolder.Length == absolutePath.Length ? string.Empty : subfolder);
        }

        public static string Replace(string text, params (string, string)[] replacementTokens)
        {
            if (string.IsNullOrEmpty(text) || replacementTokens?.Length < 1) return text;

            foreach ((string token, string value) in replacementTokens)
            {
                text = Regex.Replace(text, $@"\${token}\$", value, RegexOptions.IgnoreCase);
            }

            return text;
        }

        public static string Replace(string text, IEnumerable<KeyValuePair<string, string>> replacementTokens)
        {
            if (string.IsNullOrEmpty(text) || replacementTokens == null) return text;

            foreach (var pair in replacementTokens)
            {
                text = Regex.Replace(text, $@"(\${pair.Key}\$|{{{pair.Key}}})", (pair.Value ?? string.Empty), RegexOptions.IgnoreCase);
            }

            return text;
        }

        public static string Replace(string text, ProjectContext context, string outputFilePath, string currentWorkingDirectory = null)
        {
            // Visual Studio Tokens: https://docs.microsoft.com/en-us/visualstudio/ide/template-parameters?view=vs-2019#reserved-template-parameters

            if (string.IsNullOrEmpty(text)) return text;

            var illegalPattern = new Regex(@"[^a-z_0-9]+", RegexOptions.IgnoreCase);
            string safe(string x) => illegalPattern.Replace(x, string.Empty);
            string repl(string k, string v) => (string.IsNullOrEmpty(v) ? text : text.Replace(k, v));

            string subFolder = GetSubfolder(outputFilePath, Path.GetDirectoryName(context.ProjectFilePath), currentWorkingDirectory);

            foreach (Match match in Regex.Matches(text, @"\$(?<token>[^\$]+)\$", RegexOptions.IgnoreCase))
            {
                string token = match.Value;
                switch (match.Groups["token"].Value.ToLowerInvariant())
                {
                    case "username": text = repl(token, Environment.ExpandEnvironmentVariables("%USERNAME%")); break;
                    case "userdomain": text = repl(token, Environment.ExpandEnvironmentVariables("%USERDOMAIN%")); break;
                    case "machinename": text = repl(token, Environment.ExpandEnvironmentVariables("%COMPUTERNAME%")); break;

                    case "version": text = repl(token, context.Version); break;
                    case "assemblyname": text = repl(token, context.Assemblyname); break;

                    case "namespace": text = repl(token, context.RootNamespace); break;
                    case "rootnamespace": text = repl(token, ToNamespace(context.RootNamespace, subFolder)); break;
                    case "projectname": text = repl(token, Path.GetFileNameWithoutExtension(context.ProjectFilePath)); break;
                    case "safeprojectname": text = repl(token, safe(Path.GetFileNameWithoutExtension(context.ProjectFilePath))); break;

                    case "subfolder": text = repl(token, subFolder); break;
                    case "foldername": text = repl(token, Path.GetFileName(Path.GetDirectoryName(outputFilePath))); break;
                    case "relativepath":
                    case "projectrelativepath":
                        text = repl(token, ToUpDirectoryTokens(subFolder));
                        break;

                    case "filename": text = repl(token, Path.GetFileName(outputFilePath)); break;
                    case "itemname": text = repl(token, Path.GetFileNameWithoutExtension(outputFilePath)); break;
                    case "safeitemname": text = repl(token, safe(ToSafeItem(outputFilePath))); break;

                    case "time": text = repl(token, DateTime.Now.ToString()); break;
                    case "year": text = repl(token, DateTime.Now.Year.ToString()); break;

                    case "guid": text = repl(token, Guid.NewGuid().ToString()); break;

                    case "solutionname":
                    case "specificsolutionname": text = repl(token, safe(Path.GetFileNameWithoutExtension(context.SolutionFilePath))); break;
                }
            }

            return text;
        }

        public static string Find(string fileName, params string[] templateDirectories)
        {
            if (string.IsNullOrEmpty(fileName)) throw new ArgumentNullException(nameof(fileName));

            string templatePath;
            foreach (string folder in templateDirectories)
                if (Directory.Exists(folder))
                {
                    templatePath = Find(Directory.GetFiles(folder, "*", SearchOption.AllDirectories), fileName);
                    if (!string.IsNullOrEmpty(templatePath)) return templatePath;
                }

            return null;
        }

        internal static string Find(string[] templateFiles, string filename)
        {
            if (string.IsNullOrEmpty(filename)) throw new ArgumentNullException(nameof(filename));
            filename = Path.GetFileName(filename);

            // Attemp #1: Checking to see if I can get an exact match first.
            foreach (string path in templateFiles.Where(x => !x.Contains("~")))
                if (string.Equals(filename, Path.GetFileName(path), StringComparison.OrdinalIgnoreCase))
                {
                    return path;
                }

            // Attempt #2: Treating (~) as a wildcard, find the first template that best math the
            // file name.
            foreach (string path in templateFiles.Where(x => x.Contains("~")).OrderByDescending(x => x.Length))
            {
                // TODO: Replace with GlobN
                //Glob pattern = Path.GetFileName(path).Replace('~', '*');
                var pattern = new Regex($"^{Path.GetFileName(path).Replace(".", @"\.").Replace("~", ".+")}$", RegexOptions.IgnoreCase);
                if (pattern.IsMatch(filename)) return path;
            }

            return null;
        }

        internal static string ToUpDirectoryTokens(string path)
        {
            if (string.IsNullOrEmpty(path)) return string.Empty;

            int depth = path.Split(new char[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries).Length;
            return string.Join("/", Enumerable.Repeat("..", depth));
        }

        internal static string ToSafeItem(string path)
        {
            if (string.IsNullOrEmpty(path)) return string.Empty;

            string x = Path.GetFileName(path);
            return x.Substring(0, x.IndexOf('.'));
        }

        internal static string ToNamespace(string rootNamespace, string path)
        {
            if (string.IsNullOrEmpty(path)) return rootNamespace;
            else return string.Join(".", rootNamespace, string.Join(".", path.Split(new char[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries))).Trim('.', ' ');
        }
    }
}