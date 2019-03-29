using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Acklann.Powerbar
{
    public class Template
    {
        public static string Locate(string filename, string folder)
        {
            if (string.IsNullOrEmpty(filename)) throw new ArgumentNullException(nameof(filename));
            if (!Directory.Exists(folder)) throw new DirectoryNotFoundException($"Could not find directory at '{folder}'.");

            return Find(filename, Directory.GetFiles(folder));
        }

        public static string Find(string filename, params string[] templateFiles)
        {
            if (string.IsNullOrEmpty(filename)) throw new ArgumentNullException(nameof(filename));
            filename = Path.GetFileName(filename);

            // Attemp #1: Checking to see if I can get an exact match first.
            foreach (string path in templateFiles)
                if (string.Equals(filename, Path.GetFileName(path), StringComparison.OrdinalIgnoreCase))
                {
                    return path;
                }

            // Attempt #2: Treating (~) as a wildcard, find the first template that best math the file name.
            foreach (string path in templateFiles.OrderByDescending(x => x.Length))
            {
                // TODO: Replace DotNet.Globbing with GlobN
                var pattern = DotNet.Globbing.Glob.Parse(Path.GetFileName(path).Replace('~', '*'));
                if (pattern.IsMatch($"{filename}")) return path;
            }

            return null;
        }

        public static string ExpandItemGroup(string input, string configurationFilePath)
        {
            if (string.IsNullOrEmpty(input)) return input;
            if (!File.Exists(configurationFilePath)) throw new FileNotFoundException($"Could not item-group configuration file at '{configurationFilePath}'.");

            using (Stream file = File.OpenRead(configurationFilePath))
            {
                var serializer = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(ItemGroup[]));
                var groups = (ItemGroup[])serializer.ReadObject(file);
                return ExpandItemGroup(input, groups);
            }
        }

        public static string ExpandItemGroup(string input, params ItemGroup[] itemGroups)
        {
            if (string.IsNullOrEmpty(input)) return input;
            var pattern = new Regex(@"@\((?<name>[a-z_0-9-]+)\)", RegexOptions.IgnoreCase);

            Match match = pattern.Match(input);
            while (match.Success)
            {
                string key = match.Groups["name"].Value;
                var value = (from i in itemGroups
                             where string.Equals(i.Name, key, StringComparison.OrdinalIgnoreCase)
                             select string.Join(",", i.FileList)).FirstOrDefault();
                input = input.Replace(match.Value, (value ?? string.Empty));
                match = pattern.Match(input);
            }

            return input;
        }

        public static string Replace(string content, IEnumerable<KeyValuePair<string, string>> tokens)
        {
            if (string.IsNullOrEmpty(content) || tokens == null) return content;

            foreach (var pair in tokens)
                content = Regex.Replace(content, $@"(\${pair.Key}\$|{{{pair.Key}}})", Environment.ExpandEnvironmentVariables(pair.Value ?? string.Empty), RegexOptions.IgnoreCase);

            return content;
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
    }
}