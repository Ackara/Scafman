using EnvDTE80;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;

namespace Acklann.Powerbar
{
    public static class Helper
    {
        public static void GetProjectInfo(this DTE2 dte, out string project, out string projectItem, out string[] userSelection, out string rootNamespace, out string assemblyName, out string version)
        {
            if (dte == null) throw new ArgumentNullException(nameof(dte));
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

            var selection = new List<string>();
            project = rootNamespace = assemblyName = version = string.Empty;

            // Checking to see if the user has any project(s) selected.
            // If they don't have any selected return the start-up project.
            bool noProjectsWereSelected = true;
            EnvDTE.SelectedItems selectedItems = dte.SelectedItems;
            if (selectedItems != null)
            {
                foreach (EnvDTE.SelectedItem item in selectedItems)
                    if (item?.ProjectItem?.FileCount > 0)
                    {
                        selection.Add(item.ProjectItem.FileNames[0]);
                        if (!string.IsNullOrEmpty(item?.ProjectItem?.ContainingProject?.FullName))
                        {
                            noProjectsWereSelected = false;
                            project = item.ProjectItem.ContainingProject.FullName;
                            rootNamespace = Convert.ToString(item.ProjectItem.ContainingProject.Properties?.Item("RootNamespace")?.Value);
                            assemblyName = Convert.ToString(item.ProjectItem.ContainingProject.Properties?.Item("AssemblyName")?.Value);
                            version = Convert.ToString(item.ProjectItem.ContainingProject.Properties?.Item("Version")?.Value);
                        }
                    }
            }

            // Checking to see if their is a start-up project
            // since no project files where selected.
            if (noProjectsWereSelected)
            {
                EnvDTE.SolutionBuild solution = dte.Solution.SolutionBuild;

                if (solution?.StartupProjects != null)
                    foreach (var item in (Array)solution.StartupProjects)
                    {
                        EnvDTE.Project startupProjects = dte.Solution.Item(item);
                        if (startupProjects != null)
                        {
                            project = startupProjects.FullName;
                            rootNamespace = Convert.ToString(startupProjects.Properties?.Item("RootNamespace")?.Value);
                            assemblyName = Convert.ToString(startupProjects.Properties?.Item("AssemblyName")?.Value);
                            version = Convert.ToString(startupProjects.Properties?.Item("Version")?.Value);
                        }
                    }
            }

            userSelection = selection.ToArray();
            projectItem = selection.LastOrDefault();
        }

        public static string GetLocation(this VSContext context, Location location)
        {
            switch (location)
            {
                default:
                case Location.Current:
                    if (!string.IsNullOrEmpty(context.ProjectItemPath))
                        return Path.GetDirectoryName(context.ProjectItemPath);
                    else if (!string.IsNullOrEmpty(context.ProjectFilePath))
                        return Path.GetDirectoryName(context.ProjectFilePath);
                    else if (!string.IsNullOrEmpty(context.ProjectFilePath))
                        return Path.GetDirectoryName(context.SolutionFilePath);
                    else return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

                case Location.Project:
                    return Path.GetDirectoryName(context.ProjectFilePath) ?? throw new DirectoryNotFoundException("Could not find the project directory.");

                case Location.Solution:
                    return Path.GetDirectoryName(context.SolutionFilePath) ?? throw new DirectoryNotFoundException("Could not find the solution directory.");
            }
        }

        #region P/Invoke

        private const int GWL_STYLE = -16, WS_MAXIMIZEBOX = 0x10000, WS_MINIMIZEBOX = 0x20000;

        internal static void HideMinimizeAndMaximizeButtons(this Window window)
        {
            IntPtr hwnd = new System.Windows.Interop.WindowInteropHelper(window).Handle;
            var currentStyle = GetWindowLong(hwnd, GWL_STYLE);

            SetWindowLong(hwnd, GWL_STYLE, (currentStyle & ~WS_MAXIMIZEBOX & ~WS_MINIMIZEBOX));
        }

        [DllImport("user32.dll")]
        extern private static int GetWindowLong(IntPtr hwnd, int index);

        [DllImport("user32.dll")]
        extern private static int SetWindowLong(IntPtr hwnd, int index, int value);

        #endregion P/Invoke

        /**
         * List of possible [EnvDTE.Projecty].Properties
         * --------------------------------------------
         *
         * OutputType = 2
         * OutputName =
         * Product = Bar
         * LocalPath = C:\Users\Ackeem\Projects\Foo\Bar
         * SupportedTargetFrameworks =
         * StartupObject =
         * FullPath = C:\Users\Ackeem\Projects\Foo\Bar\
         * Description =
         * DelaySign =
         * Copyright =
         * PackageId = Bar
         * RepositoryType =
         * AssemblyOriginatorKeyFile =
         * TargetFSharpCoreVersion =
         * GeneratePackageOnBuild = False
         * TargetFrameworkMonikers = .NETStandard,Version=v2.0
         * RootNamespace = Bar
         * PackageReleaseNotes =
         * RepositoryUrl =
         * OutputFileName = Bar.dll
         * Version = 1.0.0
         * PackageLicenseUrl =
         * FileName = Bar.csproj
         * DefaultNamespace = Bar
         * ReferencePath =
         * TargetFrameworkMoniker = .NETStandard,Version=v2.0
         * PackageProjectUrl =
         * PackageRequireLicenseAcceptance = False
         * AssemblyName = Bar
         * URL = C:\Users\Ackeem\Projects\Foo\Bar\Bar.csproj
         * OutputTypeEx = 2
         * Win32ResourceFile =
         * ApplicationIcon =
         * SignAssembly = False
         * FileVersion = 1.0.0.0
         * TargetFramework = 131072
         * CanUseTargetFSharpCoreVersion =
         * NeutralLanguage =
         * AutoGenerateBindingRedirects =
         * RunPostBuildEvent =
         * Company = Bar
         * FullProjectFileName = C:\Users\Ackeem\Projects\Foo\Bar\Bar.csproj
         * PostBuildEvent =
         * PackageIconUrl =
         * TargetFrameworks =
         * ApplicationManifest = DefaultManifest
         * PreBuildEvent =
         * AssemblyVersion = 1.0.0.0
         * Authors = Bar
         * Name =
         * **/
    }
}