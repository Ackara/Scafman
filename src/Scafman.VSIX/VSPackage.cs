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
    //[ProvideAutoLoad(Microsoft.VisualStudio.VSConstants.UICONTEXT.SolutionExistsAndFullyLoaded_string, PackageAutoLoadFlags.BackgroundLoad)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    public sealed class VSPackage : AsyncPackage
    {
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            _filePrompt = await Models.FilenamePromptViewModel.RestoreAsync();
            _commandPrompt = await Models.CommandPromptViewModel.RestoreAsync();

            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            _dte = (await GetServiceAsync(typeof(EnvDTE.DTE)) as EnvDTE.DTE);
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

        private string GetCommandFromUser(string cwd)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            _commandPrompt.Reset();
            _commandPrompt.Location = cwd;

            var dialog = new Views.CommandPrompt(_commandPrompt) { Owner = (System.Windows.Window)HwndSource.FromHwnd(new IntPtr(_dte.MainWindow.HWnd)).RootVisual };
            bool? outcome = dialog.ShowDialog();
            _commandPrompt.SaveAsync();

            if (!outcome.HasValue || !outcome.Value) return null;
            else return (_commandPrompt.UserInput ?? string.Empty).Trim();
        }

        private string GetFilenameFromUser(string cwd, string defaultName)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            _filePrompt.Initialize(cwd, defaultName);
            var dialog = new Views.FilenamePrompt(_filePrompt) { Owner = (System.Windows.Window)HwndSource.FromHwnd(new IntPtr(_dte.MainWindow.HWnd)).RootVisual };
            dialog.Title = string.Format(LocalizedString.WindowTitleFormat, "Filename");
            bool? outcome = dialog.ShowDialog();
            _filePrompt.SaveAsync();

            return ((outcome.HasValue && outcome.Value && _filePrompt.HasValidInput) ? _filePrompt.FullPath : null);
        }

        // ==================== Event Handlers ==================== //

        private void OnAddNewItemCommandInvoked(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            _dte.GetContext(out EnvDTE.Project project, out ProjectContext context);

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
                        (project ?? _dte.GetSolutionFolder()).AddFolder(item.Input.ExpandPath(cwd));
                        break;

                    case Switch.AddFile:
                        (project ?? _dte.GetSolutionFolder()).AddTemplateFile(item.Input, cwd, context, ConfigurationPage.GetAllTemplateDirectories(), out string newFilePath, out int startingPosition);
                        if (File.Exists(newFilePath))
                        {
                            VsShellUtilities.OpenDocument(this, newFilePath);
                            Helper.MoveActiveDocumentCursorTo(startingPosition);
                            try { _dte.ExecuteCommand("SolutionExplorer.SyncWithActiveDocument"); } catch (COMException) { }
                            _dte.ActiveDocument?.Activate();
                        }
                        break;

                    case Switch.NugetPackage:
                        if (project == null) break;
                        if (componentModel == null) componentModel = (IComponentModel)GetGlobalService(typeof(SComponentModel));
                        if (nuget == null) nuget = componentModel.GetService<IVsPackageInstallerServices>();
                        if (installer == null) installer = componentModel.GetService<IVsPackageInstaller>();

                        project.InstallNuGetPackage(item.Input, nuget, installer, _dte.StatusBar);
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

            string documentPath = _dte.ActiveDocument?.FullName;
            if (string.IsNullOrEmpty(documentPath)) return;

            string templateFile = Template.Find(documentPath, ConfigurationPage.UserTemplateDirectories);

            if (string.IsNullOrEmpty(templateFile))
            {
                DialogResult answer = MessageBox.Show(
                    $"I could not find a matching template. Do you want to create one?",
                    string.Format(windowTitleFormat, "Compare Active Document with Template"),
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);

                if (answer == DialogResult.Yes)
                {
                    templateFile = Path.Combine(ConfigurationPage.UserTemplateDirectory, Path.GetFileName(documentPath));

                    if (!Directory.Exists(ConfigurationPage.UserTemplateDirectory)) Directory.CreateDirectory(ConfigurationPage.UserTemplateDirectory);
                    File.Create(templateFile).Dispose();
                }
            }

            if (!string.IsNullOrEmpty(templateFile))
            {
                _dte.LaunchDiffTool(documentPath, templateFile);
            }
        }

        private void OnExportActiveDocumentAsTemplateCommandInvoked(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            string documentPath = _dte.ActiveDocument?.FullName;
            if (string.IsNullOrEmpty(documentPath)) return;

            if (!Directory.Exists(ConfigurationPage.UserTemplateDirectory))
            {
                DialogResult answer = MessageBox.Show(
                    $"You have not specified a template directory yet. Do you want to?",
                    string.Format(windowTitleFormat, "Set Template Directory"),
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);

                if (answer == DialogResult.Yes)
                {
                    ShowOptionPage(typeof(ConfigurationPage));
                }

                return;
            }

            string outPath = GetFilenameFromUser(ConfigurationPage.UserTemplateDirectory, Path.GetFileName(documentPath));
            if (string.IsNullOrEmpty(outPath)) return;

            string folder = Path.GetDirectoryName(outPath);
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

            using (var inStream = new FileStream(documentPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var outStream = new FileStream(outPath, FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                inStream.CopyTo(outStream);
                outStream.Flush();
            }

            VsShellUtilities.OpenDocument(this, outPath);
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
                string documentPath = _dte.ActiveDocument?.FullName;
                command.Enabled = documentPath != null;

                if (Helper.AreEqual(command.CommandID, Metadata.CmdSet.Guid, Metadata.CmdSet.ExportActiveDocumentAsTemplateCommandId))
                {
                    command.Text = string.Format("Export {0} as Template", (Path.GetFileName(documentPath) ?? "Document"));
                }
            }
        }

        #region Backing Members

        internal const string
            statusbarFormat = (Metadata.Name + ": {0}"),
            windowTitleFormat = ("{0} | " + Metadata.Name);

        private EnvDTE.DTE _dte;
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