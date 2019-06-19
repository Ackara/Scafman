using System;

namespace Acklann.Templata.Extensions
{
    internal static class DTEExtenstions
    {
        public static string GetSelectedFile(this EnvDTE80.DTE2 dte)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

            if (dte.SelectedItems != null)
                foreach (EnvDTE.SelectedItem item in dte.SelectedItems)
                {
                    if (item?.ProjectItem.FileCount > 0)
                    {
                        return item.ProjectItem.FileNames[0];
                    }
                }

            return null;
        }

        public static void LaunchDiffTool(this EnvDTE80.DTE2 dte, string file1, string file2)
        {
            if (string.IsNullOrEmpty(file1)) throw new ArgumentNullException(nameof(file1));
            if (string.IsNullOrEmpty(file2)) throw new ArgumentNullException(nameof(file2));
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

            object args = $"\"{file1}\" \"{file2}\"";
            dte.Commands.Raise("{5D4C0442-C0A2-4BE8-9B4D-AB1C28450942}", 256, ref args, ref args);
        }
    }
}