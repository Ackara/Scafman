using Acklann.GlobN;
using System;

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
    }
}