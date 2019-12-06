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
    }
}