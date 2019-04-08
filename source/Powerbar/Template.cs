using Acklann.GlobN;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Acklann.Powerbar
{
    public class Template
    {
        private const string separator = ";";

        public static string[] Split(string fileList)
        {
            if (string.IsNullOrEmpty(fileList)) return new string[0];

            Group item = null;
            string input = "";
            var result = new List<string>();
            var pattern = new Regex(@"\((?<item>[a-z 0-9_|.]+)\)", RegexOptions.IgnoreCase);
            string[] list = fileList.Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < list.Length; i++)
            {
                item = null;
                input = list[i];
                while (pattern.IsMatch(input))
                {
                    Match match = pattern.Match(input);
                    item = match.Groups["item"];
                    if (item.Success)
                    {
                        input = input.Remove(match.Index, match.Length);
                        input = input.Insert(match.Index, "{0}");

                        foreach (string value in item.Value.Split('|'))
                        {
                            result.Add(string.Format(input, value.Trim()));
                        }
                    }
                }

                if (item == null) result.Add(input);
            }
            return result.ToArray();
        }

        public static string GetExtension(string projectFile, string location)
        {
            if (string.IsNullOrEmpty(projectFile)) return string.Empty;

            if (Directory.Exists(location))
            {
                var extensions = (from f in Directory.EnumerateFiles(location) select Path.GetExtension(f))
                    .Take(3)
                    .Distinct();
                if (extensions.Count() == 1) return extensions.First();
            }

            string extension = Path.GetExtension(projectFile).Replace("proj", "");
            switch (extension.ToLowerInvariant())
            {
                case ".nj":
                    return ".ts";

                case ".pss":
                    return ".ps1";

                case ".vcx":
                    return ".cpp";
            }

            return extension;
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

        public static string ExpandItemGroup(string input, string configurationFilePath, bool appendFileExtension = false)
        {
            if (string.IsNullOrEmpty(input)) return input;
            if (!File.Exists(configurationFilePath)) throw new FileNotFoundException($"Could not item-group configuration file at '{configurationFilePath}'.");

            using (Stream file = File.OpenRead(configurationFilePath))
            {
                var serializer = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(ItemGroup[]));
                var groups = (ItemGroup[])serializer.ReadObject(file);
                return ExpandItemGroup(input, appendFileExtension, groups);
            }
        }

        public static string ExpandItemGroup(string input, bool appendFileExtension, params ItemGroup[] itemGroups)
        {
            if (string.IsNullOrEmpty(input)) return input;
            var pattern = new Regex(@"@\((?<name>[a-z_0-9-]+)\)", RegexOptions.IgnoreCase);

            Match match = pattern.Match(input);
            while (match.Success)
            {
                string key = match.Groups["name"].Value;
                var value = (from i in itemGroups
                             where string.Equals(i.Name, key, StringComparison.OrdinalIgnoreCase)
                             select string.Join(separator, i.FileList)).FirstOrDefault();
                input = input.Replace(match.Value, (value ?? string.Empty));
                match = pattern.Match(input);
            }

            return input;
        }

        public static string GetSubfolder(string filePath, string projectFolder, string currentWorkingDirectory)
        {
            if (string.IsNullOrEmpty(filePath)) throw new ArgumentNullException(nameof(filePath));
            if (string.IsNullOrEmpty(projectFolder)) throw new ArgumentNullException(nameof(projectFolder));
            if (string.IsNullOrEmpty(currentWorkingDirectory)) throw new ArgumentNullException(nameof(currentWorkingDirectory));

            string absolutePath = ((Glob)filePath).ExpandPath(currentWorkingDirectory).Replace('/', '\\');
            string subfolder = Path.GetDirectoryName(absolutePath.Replace(projectFolder.Replace('/', '\\'), string.Empty)).Trim('/', '\\', ' ');
            return (subfolder.Length == absolutePath.Length ? string.Empty : subfolder);
        }

        public static string Replace(string text, IEnumerable<KeyValuePair<string, string>> tokens)
        {
            if (string.IsNullOrEmpty(text) || tokens == null) return text;

            foreach (var pair in tokens)
                text = Regex.Replace(text, $@"(\${pair.Key}\$|{{{pair.Key}}})", Environment.ExpandEnvironmentVariables(pair.Value ?? string.Empty), RegexOptions.IgnoreCase);

            return text;
        }

        public static IEnumerable<KeyValuePair<string, string>> GetReplacmentTokens()
        {
            // Visual Studio Tokens: https://docs.microsoft.com/en-us/visualstudio/ide/template-parameters?view=vs-2019#reserved-template-parameters

            var time = DateTime.Now;
            return new KeyValuePair<string, string>[]
            {
                new KeyValuePair<string, string>("year", $"{time.Year}"),
                new KeyValuePair<string, string>("time", $"{time:YYYY-MM-DD}"),
                new KeyValuePair<string, string>("guid", $"{Guid.NewGuid()}"),
            };
        }

        public static string Find(string filename, params string[] templateDirectories)
        {
            if (string.IsNullOrEmpty(filename)) throw new ArgumentNullException(nameof(filename));

            string templatePath;
            foreach (string folder in templateDirectories)
                if (Directory.Exists(folder))
                {
                    templatePath = Find(Directory.GetFiles(folder, "*", SearchOption.AllDirectories), filename);
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

            // Attempt #2: Treating (~) as a wildcard, find the first template that best math the file name.
            foreach (string path in templateFiles.Where(x => x.Contains("~")).OrderByDescending(x => x.Length))
            {
                // TODO: Replace with GlobN
                //Glob pattern = Path.GetFileName(path).Replace('~', '*');
                var pattern = new Regex($"^{Path.GetFileName(path).Replace(".", @"\.").Replace("~", ".+")}$", RegexOptions.IgnoreCase);
                if (pattern.IsMatch(filename)) return path;
            }

            return null;
        }
    }
}