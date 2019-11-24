
using Acklann.GlobN;
using Acklann.Scafman.Extensions;
using Acklann.Scafman.Models;
using Acklann.Scafman.Views;
using EnvDTE;
using EnvDTE80;
using Microsoft;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using NuGet.VisualStudio;
using System;
using System.ComponentModel.Design;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Interop;
using Task = System.Threading.Tasks.Task;

namespace Acklann.Scafman
{
    [Guid(Symbol.Package.GuidString)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [InstalledProductRegistration("#110", "#112", Symbol.Version, IconResourceID = 400)]
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [ProvideOptionPage(typeof(ConfigurationPage.General), Symbol.Name, nameof(ConfigurationPage.General), 0, 0, true)]
    //[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    public sealed class VSPackage : AsyncPackage
    {
        //var color = Microsoft.VisualStudio.PlatformUI.VSColorTheme.GetThemedColor(Microsoft.VisualStudio.PlatformUI.EnvironmentColors.ToolWindowBackgroundColorKey)

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            //await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var settings = (ConfigurationPage.General)GetDialogPage(typeof(ConfigurationPage.General));
            _dte = (await GetServiceAsync(typeof(DTE)) as DTE2);
            _model = await CommandPromptViewModel.RestoreAsync();
            Assumes.Present(_dte);

            var commandService = (await GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService);
            if (commandService != null)
            {
                commandService.AddCommand(new OleMenuCommand(OnAddItemFromTemplateCommandInvoked, new CommandID(Symbol.CmdSet.Guid, Symbol.CmdSet.AddItemFromTemplateCommandId)));

                commandService.AddCommand(new OleMenuCommand(OnCompareActiveDocumentWithTemplateCommandInvoked, null, OnActiveDocumentStatusQueried, new CommandID(Symbol.CmdSet.Guid, Symbol.CmdSet.CompareActiveDocumentWithTemplateCommandId)));

                commandService.AddCommand(new OleMenuCommand(OnOpenTemplateDirectoryCommandInvoked, new CommandID(Symbol.CmdSet.Guid, Symbol.CmdSet.OpenTemplateDirectoryCommandId)));
                commandService.AddCommand(new OleMenuCommand(OnOpenGroupConfigurationFileCommandInvoked, new CommandID(Symbol.CmdSet.Guid, Symbol.CmdSet.OpenItemGroupConfigurationFileCommandId)));

                commandService.AddCommand(new OleMenuCommand(OnGotoConfigurationPageCommandInvoked, new CommandID(Symbol.CmdSet.Guid, Symbol.CmdSet.GotoConfigurationPageCommandId)));
            }
        }

        private string PromptUser(string location)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            _model.Reset();
            _model.Location = location;

            var dialog = new CommandPrompt(_model) { Owner = (System.Windows.Window)HwndSource.FromHwnd(new IntPtr(_dte.MainWindow.HWnd)).RootVisual };
            bool? outcome = dialog.ShowDialog();
#pragma warning disable VSTHRD110 // Observe result of async calls
            _model.SaveAsync();
#pragma warning restore VSTHRD110 // Observe result of async calls

            if (!outcome.HasValue || !outcome.Value) return null;
            else return (_model.UserInput ?? string.Empty).Trim();
        }

        private void AddItemFromTemplate(Location location, string command = null)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            Helper.GetContext(_dte, out Project project, out ProjectContext context);
            _model.Project = project?.FullName;
            string cwd = Helper.GetLocation(context, location);
            if (string.IsNullOrEmpty(command)) command = PromptUser(cwd);
            if (string.IsNullOrEmpty(command)) return;
            PrintInfo(context);

            if (location == Location.Solution) project = null;// The should force files to be added to a solution folder instead of the last selected project.

            if (File.Exists(ConfigurationPage.UserItemGroupFile))
                command = Template.ExpandItemGroups(command, ConfigurationPage.UserItemGroupFile);

            IVsPackageInstaller installer = null; IComponentModel componentModel = null; IVsPackageInstallerServices nuget = null;

            foreach (Command item in Template.Interpret(command))
            {
                switch (item.Kind)
                {
                    case Switch.AddFolder:
                        (project ?? Helper.GetSolutionFolder(_dte)).AddFolder(item.Input.ExpandPath(cwd));
                        break;

                    case Switch.AddFile:
                        (string filePath, int startPosition) = (project ?? Helper.GetSolutionFolder(_dte)).AddTemplateFile(item.Input, cwd, context, ConfigurationPage.UserTemplateDirectories);
                        if (File.Exists(filePath))
                        {
                            VsShellUtilities.OpenDocument(this, filePath);
                            Helper.MoveActiveDocumentCursorTo(startPosition);
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
        }

        // ==================== Command Handlers ==================== //

        private void OnAddItemFromTemplateCommandInvoked(object sender, EventArgs e)
        {
            AddItemFromTemplate(Location.Current, null);
        }

        private void OnCompareActiveDocumentWithTemplateCommandInvoked(object sender, EventArgs e)
        {
            string documentPath = _dte.ActiveDocument?.FullName;
            if (string.IsNullOrEmpty(documentPath)) return;

            string templateFile = Template.Find(documentPath, ConfigurationPage.UserTemplateDirectories);

            if (string.IsNullOrEmpty(templateFile))
            {
                DialogResult answer = MessageBox.Show(
                    $"I could not find a matching template. Do you want to create one?",
                    $"Compare Active Document with Template | {Symbol.Name}",
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

        private void OnOpenTemplateDirectoryCommandInvoked(object sender, EventArgs e)
        {
            if (Directory.Exists(ConfigurationPage.UserTemplateDirectory)) System.Diagnostics.Process.Start(ConfigurationPage.UserTemplateDirectory);
        }

        private void OnOpenGroupConfigurationFileCommandInvoked(object sender, EventArgs e)
        {
            if (File.Exists(ConfigurationPage.UserItemGroupFile)) VsShellUtilities.OpenDocument(this, ConfigurationPage.UserItemGroupFile);
        }

        private void OnGotoConfigurationPageCommandInvoked(object sender, EventArgs e)
        {
            ShowOptionPage(typeof(ConfigurationPage.General));
        }

        private void OnActiveDocumentStatusQueried(object sender, EventArgs e)
        {
            if (sender is OleMenuCommand command)
            {
                command.Enabled = _dte.ActiveDocument?.FullName != null;
            }
        }

        #region Backing Members

        private DTE2 _dte;
        private CommandPromptViewModel _model;

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