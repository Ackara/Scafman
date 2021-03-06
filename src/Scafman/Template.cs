﻿using Acklann.GlobN;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Acklann.Scafman
{
    public class Template
    {
        internal const char separator = ';';

        internal const char wildcard = '~';

        internal static readonly char[] Separators = new char[] { separator, ',' };

        public static string[] GetAliases(string templatePath)
        {
            if (string.IsNullOrEmpty(templatePath)) return new string[0];

            string[] names;
            var results = new List<string>();

            names = Path.GetFileName(templatePath).Split(Separators, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < names.Length; i++)
                if (!names[i].Contains(wildcard))
                {
                    results.Add(names[i]);
                }

            return results.ToArray();
        }

        public static string[] GetFiles(params string[] templateDirectories)
        {
            if (templateDirectories == null || templateDirectories.Length < 1) return new string[0];

            string folder;
            var results = new Stack<string>();

            for (int i = 0; i < templateDirectories.Length; i++)
                if (Directory.Exists(folder = templateDirectories[i]))
                    foreach (string file in Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories))
                    {
                        results.Push(file);
                    }

            return results.ToArray();
        }

        public static string[] Split(string fileList)
        {
            if (string.IsNullOrEmpty(fileList)) return new string[0];

            Group group; string input = "";
            var result = new List<string>();
            var pattern = new Regex(@"\((?<item>[^\)]+)\)", RegexOptions.IgnoreCase);
            string[] list = fileList.Split(Separators, StringSplitOptions.RemoveEmptyEntries);

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

        public static bool ValidateFilename(string name, out string error)
        {
            if (string.IsNullOrEmpty(name))
            {
                error = "A file name cannot be empty.";
                return false;
            }

            char[] invalid = Path.GetInvalidFileNameChars();
            int xn = name.Length, yn = invalid.Length;

            for (int x = 0; x < xn; x++)
            {
                for (int y = 0; y < yn; y++)
                {
                    if (name[x] == invalid[y])
                    {
                        error = $"A file name cannot contain the '{invalid[y]}' character.";
                        return false;
                    }
                }
            }

            error = null;
            return true;
        }

        public static string GuessFileExtension(string currentDirectory, string projectFile)
        {
            if (Directory.Exists(currentDirectory))
            {
                // We will look at the first (n) extensions within the directory, then pick one at random.
                // The idea is that dominate file-type should be chosen due to it having the highest probability.
                string result = default;
                var extensions = (Directory.EnumerateFiles(currentDirectory).Select(x => Path.GetExtension(x))).Take(10).ToArray();
                if (extensions.Length > 0) result = extensions[new Random().Next(0, extensions.Length)];

                if (!string.IsNullOrEmpty(result) && !result.EndsWith("proj")) return result;
            }

            if (Path.HasExtension(projectFile))
            {
                string extension = Path.GetExtension(projectFile).ToLowerInvariant().Replace("proj", string.Empty);
                switch (extension)
                {
                    case ".nj": return ".ts";
                    case ".pss": return ".ps1";
                    case ".vcx": return ".cpp";
                    default: return extension;
                }
            }

            return string.Empty;
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
                text = Regex.Replace(text, $@"\${pair.Key}\$", (pair.Value ?? string.Empty), RegexOptions.IgnoreCase);
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

            string subFolder = GetSubfolder(outputFilePath, context.ProjectDirectory, currentWorkingDirectory);

            string token;
            foreach (Match match in Regex.Matches(text, @"\$(?<token>[^\$]+)\$", RegexOptions.IgnoreCase))
            {
                token = match.Value;
                switch (match.Groups[nameof(token)].Value.ToLowerInvariant())
                {
                    case "username": text = repl(token, Environment.ExpandEnvironmentVariables("%USERNAME%")); break;
                    case "userdomain": text = repl(token, Environment.ExpandEnvironmentVariables("%USERDOMAIN%")); break;
                    case "machinename": text = repl(token, Environment.ExpandEnvironmentVariables("%COMPUTERNAME%")); break;

                    case "version": text = repl(token, context.Version); break;
                    case "assemblyname": text = repl(token, context.Assemblyname); break;

                    case "namespace": text = repl(token, context.RootNamespace); break;
                    case "rootnamespace": text = repl(token, ToNamespace(context.RootNamespace, subFolder)); break;
                    case "projectname": text = repl(token, context.ProjectName); break;
                    case "safeprojectname": text = repl(token, safe(context.ProjectName)); break;

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
                    case "specificsolutionname": text = repl(token, safe(context.SolutionName)); break;
                }
            }

            return text;
        }

        public static string Find(string fileName, params string[] templateDirectories)
        {
            if (string.IsNullOrEmpty(fileName)) return null;

            string templatePath;
            foreach (string folder in templateDirectories)
                if (Directory.Exists(folder))
                {
                    templatePath = Find(Directory.GetFiles(folder, "*", SearchOption.AllDirectories), fileName);
                    if (!string.IsNullOrEmpty(templatePath)) return templatePath;
                }

            return null;
        }

        /// <summary>
        /// Returns the first template file that match the specified <paramref name="keywords"/>
        /// </summary>
        /// <remarks>
        /// <para>The name of a template can contain mulitple file-names separted by a semi-colon.</para>
        /// </remarks>
        /// <param name="templateFiles">The full path of templates to search from.</param>
        /// <param name="keywords">The keywords.</param>
        internal static string Find(string[] templateFiles, string keywords)
        {
            keywords = Path.GetFileName(keywords);
            (string templatePath, int matchStrength) match = (default, default);
            string[] templateAliases; string name;

            for (int i = 0; i < templateFiles.Length; i++)
            {
                // A template file may contain multiple names. So we have to extract those names/aliases.
                templateAliases = Path.GetFileName(templateFiles[i]).Split(Separators, StringSplitOptions.RemoveEmptyEntries);

                for (int n = 0; n < templateAliases.Length; n++)
                {
                    name = templateAliases[n];
                    if (name.Contains(wildcard))
                    {
                        // To get the best match. We will favor the match with the most characters in its name. Why?
                        // The more characters you match, the more specific.
                        var pattern = new Regex(($"^{name}$").Replace(".", "\\.").Replace("~", ".+"), RegexOptions.IgnoreCase);
                        if (pattern.IsMatch(keywords) && name.Length > match.matchStrength)
                            match = (templateFiles[i], name.Length);
                    }
                    else /* Exact-Match */
                    {
                        // An exact match is prefered, so if found return it right away.
                        if (string.Equals(keywords, name, StringComparison.OrdinalIgnoreCase))
                            return templateFiles[i];
                    }
                }
            }

            return match.templatePath;
        }

        internal static string ToUpDirectoryTokens(string path)
        {
            if (string.IsNullOrEmpty(path)) return string.Empty;

            int depth = path.Split(new char[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries).Length;
            return string.Join("/", Enumerable.Repeat("..", depth));
        }

        private static string ToSafeItem(string path)
        {
            if (string.IsNullOrEmpty(path)) return string.Empty;

            string x = Path.GetFileName(path);
            return x.Substring(0, x.IndexOf('.'));
        }

        private static string ToNamespace(string rootNamespace, string path)
        {
            if (string.IsNullOrEmpty(path)) return rootNamespace;
            else return string.Join(".", rootNamespace, string.Join(".", path.Split(new char[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries))).Trim('.', ' ');
        }
    }
}