using Acklann.GlobN;
using System;
using System.IO;
using System.Linq;

namespace Acklann.Scafman
{
    public static class Utilities
    {
        public static void GetFullPaths(this ProjectContext context, string itemname, out string itemPath, out string currentDirectory)
        {
            if (string.IsNullOrEmpty(itemname)) throw new ArgumentNullException(nameof(itemname));

            if (itemname.StartsWith(@"\\") || itemname.StartsWith("//"))
                currentDirectory = context.SolutionDirectory;
            else if (itemname.StartsWith(@"\") || itemname.StartsWith("/"))
                currentDirectory = context.ProjectDirectory;
            else
                currentDirectory = context.CurrentDirectory;

            itemPath = ((Glob)itemname.TrimStart('\\', '/', ' ')).ExpandPath(currentDirectory);
        }

        public static string GetFullPath(this ProjectContext context, string itemname)
        {
            GetFullPaths(context, itemname, out string path, out _);
            return path;
        }

        public static string GuessTemplatePath(string fileName, string templateDirectory)
        {
            if (string.IsNullOrEmpty(fileName)) return fileName;

            if (Directory.Exists(templateDirectory))
            {
                string match = Directory.EnumerateFiles(templateDirectory, $"*{Path.GetExtension(fileName)}", SearchOption.AllDirectories).FirstOrDefault();
                if (!string.IsNullOrEmpty(match))
                {
                    return Path.Combine(
                        Path.GetDirectoryName(
                            match.Replace(templateDirectory, string.Empty).TrimStart('\\', '/')),
                        Path.GetFileName(fileName)
                        );
                }
            }

            return Path.GetFileName(fileName);
        }

        public static bool Contains(this string text, char c)
        {
            if (string.IsNullOrEmpty(text)) return false;

            for (int i = 0; i < text.Length; i++)
            {
                if (char.Equals(char.ToUpperInvariant(text[i]), char.ToUpperInvariant(c))) return true;
            }

            return false;
        }

        public static int LastIndexOf(this string text, params char[] characters)
        {
            if (string.IsNullOrEmpty(text)) return -1;

            int index = -1;
            for (int x = 0; x < text.Length; x++)
                for (int y = 0; y < characters.Length; y++)
                    if (text[x] == characters[y])
                    {
                        index = x;
                        break;
                    }

            return index;
        }

        public static string GetLastSegment(string text)
        {
            if (string.IsNullOrEmpty(text)) return default;
            return text.Substring(LastIndexOf(text, Template.Separators) + 1);
        }
    }
}