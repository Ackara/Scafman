using EnvDTE80;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace Acklann.Templata
{
    internal static class Helper
    {
        public static void GetContext(DTE2 dte, out EnvDTE.Project project, out ProjectContext context)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (dte == null) throw new ArgumentNullException(nameof(dte));

            project = null;
            var selection = new List<string>();
            string projectPath = null, projectItemPath = null, rootNamespace = null, assemblyName = null, version = null;

            // Capturing all of the items currently selected by the user.
            EnvDTE.SelectedItems selectedItems = dte.SelectedItems;
            if (selectedItems != null)
            {
                foreach (EnvDTE.SelectedItem item in selectedItems)
                    if (item?.ProjectItem?.FileCount > 0)
                    {
                        selection.Add(item.ProjectItem.FileNames[0]);
                        if (!string.IsNullOrEmpty(item?.ProjectItem?.ContainingProject?.FullName))
                        {
                            project = item.ProjectItem.ContainingProject;
                            projectPath = item.ProjectItem.ContainingProject.FullName;
                        }
                    }
                    else if (!string.IsNullOrEmpty(item?.Project?.FullName))
                    {
                        project = item.Project;
                        projectPath = item.Project.FullName;
                        selection.Add(item.Project.FullName);
                    }

                projectItemPath = selection.LastOrDefault();
            }

            if (project != null)
            {
                rootNamespace = TryGetProperty(project, "RootNamespace");
                assemblyName = TryGetProperty(project, "AssemblyName");
                version = TryGetProperty(project, "Version");
            }

            context = new ProjectContext(
                dte.Solution.FullName,
                projectPath, projectItemPath, selection.ToArray(),
                rootNamespace, assemblyName, version
                );
        }

        public static string GetLocation(ProjectContext context, Location location)
        {
            switch (location)
            {
                default:
                case Location.Current:
                    if (!string.IsNullOrEmpty(context.ProjectItemPath))
                        return Path.GetDirectoryName(context.ProjectItemPath);
                    else if (!string.IsNullOrEmpty(context.ProjectFilePath))
                        return Path.GetDirectoryName(context.ProjectFilePath);
                    else if (!string.IsNullOrEmpty(context.SolutionFilePath))
                        return Path.GetDirectoryName(context.SolutionFilePath);
                    else return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

                case Location.Project:
                    return Path.GetDirectoryName(context.ProjectFilePath) ?? throw new DirectoryNotFoundException("Could not find the project directory.");

                case Location.Solution:
                    return Path.GetDirectoryName(context.SolutionFilePath) ?? throw new DirectoryNotFoundException("Could not find the solution directory.");
            }
        }

        public static string TryGetProperty(EnvDTE.Project project, string name)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
            try
            {
                return Convert.ToString(project?.Properties?.Item(name)?.Value);
            }
            catch { /* could not find the property. */ }
            return null;
        }

        public static IWpfTextView GetTextEditor()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var component = (Package.GetGlobalService(typeof(SComponentModel)) as IComponentModel);
            if (component != null)
            {
                IVsEditorAdaptersFactoryService editorAdapter = component.GetService<IVsEditorAdaptersFactoryService>();
                var textManager = (IVsTextManager)ServiceProvider.GlobalProvider.GetService(typeof(SVsTextManager));
                if (textManager != null)
                {
                    textManager.GetActiveView(1, null, out IVsTextView editor);
                    return editorAdapter.GetWpfTextView(editor);
                }
            }

            return null;
        }

        public static void MoveActiveDocumentCursorTo(int position)
        {
            if (position > 0)
            {
                var editor = GetTextEditor();
                if (editor != null) editor.Caret.MoveTo(new SnapshotPoint(editor.TextBuffer.CurrentSnapshot, position));
            }
        }

        public static bool CheckIfEndsWithExtension(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            else return Regex.IsMatch(path, @"\.[a-z0-9]+$", RegexOptions.IgnoreCase);
        }

        public static string Format(LogLevel level, string message)
        {
            if (string.IsNullOrEmpty(message)) return string.Empty;
            else switch (level)
                {
                    default:
                    case LogLevel.Info:
                        return $"INFO: {message}";

                    case LogLevel.Warn:
                        return $"WARN: {message}";

                    case LogLevel.Error:
                        return $"ERROR: {message}";
                }
        }

        #region P/Invoke

        private const int GWL_STYLE = -16, WS_MAXIMIZEBOX = 0x10000, WS_MINIMIZEBOX = 0x20000;

        internal static void HideMinimizeAndMaximizeButtons(this System.Windows.Window window)
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
         * List of possible [EnvDTE.Project].Properties
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