using Acklann.GlobN;
using Microsoft.VisualStudio.Shell;
using NuGet.VisualStudio;
using System;
using System.IO;

namespace Acklann.Templata.Extensions
{
    internal static class ProjectExtensions
    {
        public static EnvDTE.ProjectItems AddFolder(this EnvDTE.Project project, string path)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (project.Kind == "{66A26720-8FB5-11D2-AA7E-00C04F688DDE}")/* virtual folder */
            {
                return project.ProjectItems;
            }

            EnvDTE.ProjectItems folder = project.ProjectItems;
            string location = Path.GetDirectoryName(project.FullName);
            string relativePath = path.Replace(location, string.Empty).Trim(' ', '/', '\\');

            foreach (string name in relativePath.Split(new char[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (folder == null) throw new IOException($"'{location}' already exist.");

                location = Path.Combine(location, name);
                if (Directory.Exists(location))
                    folder = folder.Item(name)?.ProjectItems;
                else
                    folder = folder.AddFolder(name).ProjectItems;
            }

            return folder;
        }

        public static EnvDTE.ProjectItem AddFile(this EnvDTE.Project project, string path, string content)
        {
            if (File.Exists(path)) return null;
            ThreadHelper.ThrowIfNotOnUIThread();

            EnvDTE.ProjectItems folder = AddFolder(project, Path.GetDirectoryName(path));
            File.WriteAllText(path, content);
            return folder?.AddFromFile(path);
        }

        public static (string, int) AddTemplateFile(this EnvDTE.Project project, string path, string cwd, ProjectContext context, params string[] templateDirectories)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            string templatePath, outFile;
            int index = path.IndexOf('>');
            if (index == -1) templatePath = outFile = path;
            else
            {
                templatePath = path.Substring(0, index).Trim();
                outFile = path.Substring(index + 1).Trim();
                if (string.IsNullOrEmpty(outFile) || string.IsNullOrEmpty(templatePath)) return (null, -1);
            }

            outFile = ((Glob)outFile).ExpandPath(cwd);
            if (File.Exists(outFile)) return (outFile, -1);

            // Building the template.
            int position = -1; string fileContent = string.Empty;
            templatePath = Template.Find(Path.GetFileName(templatePath), templateDirectories);
            if (!string.IsNullOrEmpty(templatePath))
            {
                fileContent = Template.Replace(File.ReadAllText(templatePath), context, outFile, cwd);
                fileContent = Template.RemoveCaret(fileContent, out position);
            }

            // Create the template file.
            AddFile(project, outFile, fileContent);
            return (outFile, position);
        }

        public static void InstallNuGetPackage(this EnvDTE.Project project, Package package, IVsPackageInstallerServices nuget, IVsPackageInstaller installer, EnvDTE.StatusBar status)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (string.IsNullOrEmpty(package.Name)) return;

            if (!nuget.IsPackageInstalled(project, package.Name))
            {
                status.Text = $"{Vsix.Name} | Installing {package}...";
                status.Animate(true, EnvDTE.vsStatusAnimation.vsStatusAnimationSync);

                try
                {
                    installer.InstallPackage(null, project, package.Name, package.Version, false);
                    status.Text = $"{Vsix.Name} | Installed {package}";
                }
                catch { status.Text = $"{Vsix.Name} | Unable to install {package}"; }
                finally { status.Animate(false, EnvDTE.vsStatusAnimation.vsStatusAnimationSync); }
            }
        }
    }
}