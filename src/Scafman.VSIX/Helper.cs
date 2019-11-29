using Acklann.GlobN;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using NuGet.VisualStudio;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Acklann.Scafman
{
    internal static class Helper
    {
        // ==================== EnvDTE.Project ==================== //

        public static void AddTemplateFile(this EnvDTE.Project project, string filePath, string cwd, ProjectContext context, string[] templateDirectories, out string outFile, out int position)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            position = -1;
            string templatePath = outFile = filePath;
            outFile = ((Glob)outFile).ExpandPath(cwd);
            if (File.Exists(outFile)) return;

            // Building the template.
            string fileContent = string.Empty;
            templatePath = Template.Find(Path.GetFileName(templatePath), templateDirectories);
            if (!string.IsNullOrEmpty(templatePath))
            {
                fileContent = Template.Replace(File.ReadAllText(templatePath), context, outFile, cwd);
                fileContent = Template.RemoveCaret(fileContent, out position);
            }

            // Create the template file.
            AddFile(project, outFile, fileContent);
        }

        public static EnvDTE.ProjectItem AddFile(this EnvDTE.Project project, string path, string content)
        {
            if (File.Exists(path)) return null;
            ThreadHelper.ThrowIfNotOnUIThread();

            EnvDTE.ProjectItems folder = AddFolder(project, Path.GetDirectoryName(path));
            File.WriteAllText(path, content);
            return folder?.AddFromFile(path);
        }

        public static EnvDTE.ProjectItems AddFolder(this EnvDTE.Project project, string fullPath)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (project.Kind == "{66A26720-8FB5-11D2-AA7E-00C04F688DDE}")/* virtual folder */
            {
                return project.ProjectItems;
            }

            EnvDTE.ProjectItems folder = project.ProjectItems;
            string temp = Path.GetDirectoryName(project.FullName);
            string relativePath = fullPath.Replace(temp, string.Empty).Trim(' ', '/', '\\');

            foreach (string name in relativePath.Split(new char[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (folder == null) throw new IOException($"'{temp}' already exist.");

                temp = Path.Combine(temp, name);
                if (Directory.Exists(temp))
                    folder = folder.Item(name)?.ProjectItems;
                else
                    folder = folder.AddFolder(name).ProjectItems;
            }

            return folder;
        }

        public static EnvDTE.Project GetSolutionFolder(this EnvDTE.DTE dte, string name = default)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (dte == null) throw new ArgumentNullException(nameof(dte));
            if (string.IsNullOrEmpty(name)) name = ConfigurationPage.SolutionFolderName;

            foreach (EnvDTE.Project item in dte.Solution.Projects)
                if (string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    return item;
                }

            return (dte.Solution as EnvDTE80.Solution2)?.AddSolutionFolder(name);
        }

        public static void InstallNuGetPackage(this EnvDTE.Project project, PackageID package, IVsPackageInstallerServices nuget, IVsPackageInstaller installer, EnvDTE.StatusBar status)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (string.IsNullOrEmpty(package.Name)) return;

            if (!nuget.IsPackageInstalled(project, package.Name))
            {
                status.Text = string.Format(VSPackage.statusbarFormat, "Installing {package}...");
                status.Animate(true, EnvDTE.vsStatusAnimation.vsStatusAnimationSync);

                try
                {
                    installer.InstallPackage(null, project, package.Name, package.Version, false);
                    status.Text = string.Format(VSPackage.statusbarFormat, $"Installed {package}");
                }
                catch { status.Text = string.Format(VSPackage.statusbarFormat, $"Unable to install {package}"); }
                finally { status.Animate(false, EnvDTE.vsStatusAnimation.vsStatusAnimationSync); }
            }
        }

        // ==================== EnvDTE.DTE ==================== //

        public static void GetContext(this EnvDTE.DTE dte, out EnvDTE.Project project, out ProjectContext context)
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
                            projectPath = project.FullName;
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

        public static string GetLocation(this ProjectContext context)
        {
            if (!string.IsNullOrEmpty(context.ProjectItemPath))
                return Path.GetDirectoryName(context.ProjectItemPath);
            else if (!string.IsNullOrEmpty(context.ProjectFilePath))
                return Path.GetDirectoryName(context.ProjectFilePath);
            else if (!string.IsNullOrEmpty(context.SolutionFilePath))
                return Path.GetDirectoryName(context.SolutionFilePath);
            else
                return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        }

        // ==================== Misc ==================== //
        public static void LaunchDiffTool(this EnvDTE.DTE dte, string file1, string file2)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (string.IsNullOrEmpty(file1)) throw new ArgumentNullException(nameof(file1));
            if (string.IsNullOrEmpty(file2)) throw new ArgumentNullException(nameof(file2));

            object args = $"\"{file1}\" \"{file2}\"";

            if (File.Exists(ConfigurationPage.DiffExecutable))
            {
                using (var diff = new System.Diagnostics.Process())
                {
                    diff.StartInfo = new System.Diagnostics.ProcessStartInfo()
                    {
                        FileName = ConfigurationPage.DiffExecutable,
                        Arguments = (string)args
                    };

                    diff.Start();
                }
            }
            else
            {
                dte.Commands.Raise("{5D4C0442-C0A2-4BE8-9B4D-AB1C28450942}", 256, ref args, ref args);
                if (!string.IsNullOrEmpty(ConfigurationPage.DiffExecutable)) dte.StatusBar.Text = $"Counld not find '{ConfigurationPage.DiffExecutable}'.";
            }
        }

        public static bool AreEqual(System.ComponentModel.Design.CommandID a, Guid bGuild, int bID)
        {
            return a.Guid.Equals(bGuild) && a.ID == bID;
        }

        public static void MoveActiveDocumentCursorTo(int position)
        {
            if (position > 0)
            {
                var editor = GetTextEditor();
                if (editor != null) editor.Caret.MoveTo(new SnapshotPoint(editor.TextBuffer.CurrentSnapshot, position));
            }
        }

        private static IWpfTextView GetTextEditor()
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

        private static string TryGetProperty(EnvDTE.Project project, string name)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            try
            {
                return Convert.ToString(project?.Properties?.Item(name)?.Value);
            }
            catch { /* could not find the property. */ }
            return null;
        }
    }
}