using Acklann.GlobN;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using NuGet.VisualStudio;
using System;
using System.ComponentModel.Design;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Interop;
using Task = System.Threading.Tasks.Task;

namespace Acklann.Scafman
{
    [Guid(Metadata.Package.GuidString)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration("#110", "#112", Metadata.Version, IconResourceID = 500)]
    [ProvideOptionPage(typeof(ConfigurationPage), Metadata.Name, ConfigurationPage.Category, 0, 0, true)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    public sealed class VSPackage : AsyncPackage
    {
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            _filePrompt = await Models.FilenamePromptViewModel.RestoreAsync();
            _commandPrompt = await Models.CommandPromptViewModel.RestoreAsync();

            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            vs = (await GetServiceAsync(typeof(EnvDTE.DTE)) as EnvDTE.DTE);
            GetDialogPage(typeof(ConfigurationPage));

            var commandService = (await GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService);
            if (commandService != null)
            {
                commandService.AddCommand(new OleMenuCommand(OnAddNewItemCommandInvoked, new CommandID(Metadata.CmdSet.Guid, Metadata.CmdSet.AddNewItemCommandId)));
                commandService.AddCommand(new OleMenuCommand(OnCompareActiveDocumentWithTemplateCommandInvoked, null, OnActiveDocumentStatusQueried, new CommandID(Metadata.CmdSet.Guid, Metadata.CmdSet.CompareActiveDocumentWithTemplateCommandId)));
                commandService.AddCommand(new OleMenuCommand(OnExportActiveDocumentAsTemplateCommandInvoked, null, OnActiveDocumentStatusQueried, new CommandID(Metadata.CmdSet.Guid, Metadata.CmdSet.ExportActiveDocumentAsTemplateCommandId)));

                commandService.AddCommand(new MenuCommand(OnOpenTemplateDirectoryCommandInvoked, new CommandID(Metadata.CmdSet.Guid, Metadata.CmdSet.OpenTemplateDirectoryCommandId)));
                commandService.AddCommand(new MenuCommand(OnOpenItemGroupConfigurationFileCommandInvoked, new CommandID(Metadata.CmdSet.Guid, Metadata.CmdSet.OpenItemGroupConfigurationFileCommandId)));
                commandService.AddCommand(new MenuCommand(OnGotoConfigruationPageCommandInvoked, new CommandID(Metadata.CmdSet.Guid, Metadata.CmdSet.GotoConfigurationPageCommandId)));
            }
        }

        private string GetCommandFromUser(string cwd, string title = "Enter a command")
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            _commandPrompt.Reset();
            _commandPrompt.Location = cwd;

            var dialog = new Views.CommandPrompt(_commandPrompt) { Owner = (System.Windows.Window)HwndSource.FromHwnd(new IntPtr(vs.MainWindow.HWnd)).RootVisual };
            dialog.Title = LocalizedString.GetWindowTitle(title);
            bool gotValue = (dialog.ShowDialog() ?? false);
            _commandPrompt.SaveAsync();

            return ((gotValue ? _commandPrompt.UserInput : null) ?? string.Empty).Trim();
        }

        private string GetFilenameFromUser(string cwd, string defaultName, string title = "Enter a file name")
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            _filePrompt.Initialize(cwd, defaultName);

            var dialog = new Views.FilenamePrompt(_filePrompt) { Owner = (System.Windows.Window)HwndSource.FromHwnd(new IntPtr(vs.MainWindow.HWnd)).RootVisual };
            dialog.Title = LocalizedString.GetWindowTitle(title);
            bool gotValue = (dialog.ShowDialog() ?? false);
            _filePrompt.SaveAsync();

            return (gotValue ? _filePrompt.FullPath : null);
        }

        // ==================== Event Handlers ==================== //

        private void OnAddNewItemCommandInvoked(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            vs.GetContext(out EnvDTE.Project project, out ProjectContext context);

            string command = null;
            _commandPrompt.Project = context.ProjectFilePath;
            string cwd = Helper.GetLocation(context);
            if (string.IsNullOrEmpty(command)) command = GetCommandFromUser(cwd);
            if (string.IsNullOrEmpty(command)) return;
            PrintInfo(context);

            if (File.Exists(ConfigurationPage.UserItemGroupConfigurationFilePath))
                command = Template.ExpandItemGroups(command, ConfigurationPage.UserItemGroupConfigurationFilePath);

            IVsPackageInstaller installer = null; IComponentModel componentModel = null; IVsPackageInstallerServices nuget = null;
            foreach (Command item in Template.Interpret(command))
                switch (item.Kind)
                {
                    case Switch.AddFolder:
                        (project ?? vs.GetSolutionFolder()).AddFolder(item.Input.ExpandPath(cwd));
                        break;

                    case Switch.AddFile:
                        (project ?? vs.GetSolutionFolder()).AddTemplateFile(item.Input, cwd, context, ConfigurationPage.GetAllTemplateDirectories(), out string newFilePath, out int startingPosition);
                        if (File.Exists(newFilePath))
                        {
                            VsShellUtilities.OpenDocument(this, newFilePath);
                            Helper.MoveActiveDocumentCursorTo(startingPosition);
                            try { vs.ExecuteCommand("SolutionExplorer.SyncWithActiveDocument"); } catch (COMException) { }
                            vs.ActiveDocument?.Activate();
                        }
                        break;

                    case Switch.NugetPackage:
                        if (project == null) break;
                        if (componentModel == null) componentModel = (IComponentModel)GetGlobalService(typeof(SComponentModel));
                        if (nuget == null) nuget = componentModel.GetService<IVsPackageInstallerServices>();
                        if (installer == null) installer = componentModel.GetService<IVsPackageInstaller>();

                        project.InstallNuGetPackage(item.Input, nuget, installer, vs.StatusBar);
                        break;

                    case Switch.NPMPackage:
                        if (project == null) break;
                        else if (!string.IsNullOrEmpty(context.ProjectFilePath)) NPM.Install(Path.GetDirectoryName(context.ProjectFilePath), item.Input);
                        break;
                }
        }

        private void OnCompareActiveDocumentWithTemplateCommandInvoked(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            const string title = "Compare Active Document with Template";

            string documentPath = vs.ActiveDocument?.FullName;
            if (string.IsNullOrEmpty(documentPath)) return;

            string templateFile = Template.Find(documentPath, ConfigurationPage.UserTemplateDirectories);

            if (string.IsNullOrEmpty(templateFile))
            {
                DialogResult answer = MessageBox.Show(
                    $"I could not find a matching template. Do you want to create one?",
                    LocalizedString.GetWindowTitle(title),
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);

                if (answer == DialogResult.Yes)
                {
                    templateFile = GetFilenameFromUser(ConfigurationPage.UserTemplateDirectory, Path.GetFileName(documentPath), title);
                    if (string.IsNullOrEmpty(templateFile)) return;

                    string folder = Path.GetDirectoryName(templateFile);
                    if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
                    File.Create(templateFile).Dispose();
                }
            }

            if (!string.IsNullOrEmpty(templateFile))
            {
                vs.LaunchDiffTool(documentPath, templateFile);
            }
        }

        private void OnExportActiveDocumentAsTemplateCommandInvoked(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            const string title = "Set Template Directory";

            string documentPath = vs.ActiveDocument?.FullName;
            if (string.IsNullOrEmpty(documentPath)) return;

            if (!Directory.Exists(ConfigurationPage.UserTemplateDirectory))
            {
                DialogResult answer = MessageBox.Show(
                    $"Before I can create your template I need to know where to save it; a template directory have not yet being specified. Do you want to add a directory?",
                    LocalizedString.GetWindowTitle(title),
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);

                if (answer == DialogResult.Yes)
                {
                    ShowOptionPage(typeof(ConfigurationPage));
                }
                return;
            }

            string newTemplateFilePath = GetFilenameFromUser(ConfigurationPage.UserTemplateDirectory, Path.GetFileName(documentPath), title);
            if (string.IsNullOrEmpty(newTemplateFilePath)) return;

            string folder = Path.GetDirectoryName(newTemplateFilePath);
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
            using (var source = new FileStream(documentPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var destination = new FileStream(newTemplateFilePath, FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                source.CopyTo(destination);
                destination.Flush();
            }

            VsShellUtilities.OpenDocument(this, newTemplateFilePath);
        }

        private void OnOpenTemplateDirectoryCommandInvoked(object sender, EventArgs e)
        {
            if (Directory.Exists(ConfigurationPage.UserTemplateDirectory)) System.Diagnostics.Process.Start(ConfigurationPage.UserTemplateDirectory);
        }

        private void OnOpenItemGroupConfigurationFileCommandInvoked(object sender, EventArgs e)
        {
            if (File.Exists(ConfigurationPage.UserItemGroupConfigurationFilePath)) VsShellUtilities.OpenDocument(this, ConfigurationPage.UserItemGroupConfigurationFilePath);
        }

        private void OnGotoConfigruationPageCommandInvoked(object sender, EventArgs e)
        {
            ShowOptionPage(typeof(ConfigurationPage));
        }

        private void OnActiveDocumentStatusQueried(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (sender is OleMenuCommand command)
            {
                string documentPath = vs.ActiveDocument?.FullName;
                command.Enabled = documentPath != null;

                if (Helper.AreEqual(command.CommandID, Metadata.CmdSet.Guid, Metadata.CmdSet.ExportActiveDocumentAsTemplateCommandId))
                {
                    command.Text = string.Format("Export {0} as Template", (Path.GetFileName(documentPath) ?? "Document"));
                }
            }
        }

        #region Backing Members

        internal EnvDTE.DTE vs;
        private Models.FilenamePromptViewModel _filePrompt;
        private Models.CommandPromptViewModel _commandPrompt;

        private void PrintInfo(ProjectContext context)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"solution: {context.SolutionFilePath}");
            System.Diagnostics.Debug.WriteLine($"project: {context.ProjectFilePath}");
            System.Diagnostics.Debug.WriteLine($"project-item: {context.ProjectItemPath}");
            System.Diagnostics.Debug.WriteLine($"selected-items: " + string.Join(" | ", context.SelectedItems));
            System.Diagnostics.Debug.WriteLine($"namespace: {context.RootNamespace}");
            System.Diagnostics.Debug.WriteLine($"assembly: {context.Assemblyname}");
            System.Diagnostics.Debug.WriteLine($"version: {context.Version}");
            System.Diagnostics.Debug.WriteLine("\n");
#endif
        }

        #endregion Backing Members
    }
}