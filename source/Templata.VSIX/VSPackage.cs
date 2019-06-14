using Acklann.GlobN;
using Acklann.Templata.Models;
using EnvDTE;
using EnvDTE80;
using Microsoft;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using NuGet.VisualStudio;
using System;
using System.ComponentModel.Design;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Interop;
using Task = System.Threading.Tasks.Task;

namespace Acklann.Templata
{
    [Guid(Symbol.Package.GuidString)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [InstalledProductRegistration("#110", "#112", Vsix.Version, IconResourceID = 400)]
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [ProvideOptionPage(typeof(ConfigurationPage.General), Vsix.Name, nameof(ConfigurationPage.General), 0, 0, true)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    public sealed class VSPackage : AsyncPackage
    {
        //var defaultBackground = VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowBackgroundColorKey);

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var settings = (ConfigurationPage.General)GetDialogPage(typeof(ConfigurationPage.General));
            ConfigurationPage.Load(settings);

            _dte = (await GetServiceAsync(typeof(DTE)) as DTE2);
            _model = await CommandPromptViewModel.RestoreAsync();

            _console = CreateOutputWindow(Vsix.Name);
            Assumes.Present(_dte);

            var commandService = (await GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService);
            if (commandService != null)
            {
                commandService.AddCommand(new MenuCommand(OnCurrentLevelCommandInvoked, new CommandID(Symbol.CmdSet.Guid, Symbol.CmdSet.CurrentLevelCommandId)));
                commandService.AddCommand(new MenuCommand(OnProjectLevelCommandInvoked, new CommandID(Symbol.CmdSet.Guid, Symbol.CmdSet.ProjectLevelCommandId)));
                commandService.AddCommand(new MenuCommand(OnSolutionLevelCommandInvoked, new CommandID(Symbol.CmdSet.Guid, Symbol.CmdSet.SolutionLevelCommandId)));
                commandService.AddCommand(new MenuCommand(OnConfigurationCommandInvoked, new CommandID(Symbol.CmdSet.Guid, Symbol.CmdSet.ConfigurationPageCommandId)));

                commandService.AddCommand(new MenuCommand(OnOpenTemplateDirectoryCommandInvoked, new CommandID(Symbol.CmdSet.Guid, Symbol.CmdSet.OpenTemplateDirectoryCommandId)));
                commandService.AddCommand(new MenuCommand(OnOpenGrougConfigurationFileCommandInvoked, new CommandID(Symbol.CmdSet.Guid, Symbol.CmdSet.OpenItemGroupConfigurationFileCommandId)));
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

        // ==================== Command Handlers ==================== //

        private void OnAddFileToTemplateDirectory(object sender, EventArgs e)
        {
            if (ConfigurationPage.TemplateDirectoryExists)
            {
            }
        }

        private void OnOpenTemplateDirectoryCommandInvoked(object sender, EventArgs e)
        {
            if (ConfigurationPage.TemplateDirectoryExists) System.Diagnostics.Process.Start(ConfigurationPage.UserTemplateDirectory);
        }

        private void OnOpenGrougConfigurationFileCommandInvoked(object sender, EventArgs e)
        {
            if (ConfigurationPage.UserItemGroupFileExists) VsShellUtilities.OpenDocument(this, ConfigurationPage.UserItemGroupFile);
        }

        private void OnCurrentLevelCommandInvoked(object sender, EventArgs e)
        {
            ExecuteCommand(Location.Current, null);
        }

        private void OnProjectLevelCommandInvoked(object sender, EventArgs e)
        {
            ExecuteCommand(Location.Project, null);
        }

        private void OnSolutionLevelCommandInvoked(object sender, EventArgs e)
        {
            ExecuteCommand(Location.Solution, null);
        }

        private void OnConfigurationCommandInvoked(object sender, EventArgs e)
        {
            ShowOptionPage(typeof(ConfigurationPage.General));
        }

        private void ExecuteCommand(Location location, string command = null)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            Helper.GetContext(_dte, out Project project, out ProjectContext context);
            _model.Project = project?.FullName;
            string cwd = Helper.GetLocation(context, location);
            if (string.IsNullOrEmpty(command)) command = PromptUser(cwd);
            if (string.IsNullOrEmpty(command)) return;
            PrintDebugInfo(context);

            if (location == Location.Solution) project = null;// The should force files to be added to a solution folder instead of the last selected project.

            if (File.Exists(ConfigurationPage.UserItemGroupFile))
                command = Template.ExpandItemGroup(command, ConfigurationPage.UserItemGroupFile);

            IVsPackageInstaller installer = null; IComponentModel componentModel = null; IVsPackageInstallerServices nuget = null;

            foreach (Command item in Template.Interpret(command))
            {
                switch (item.Kind)
                {
                    case Switch.AddFolder:
                        project.AddFolder(item.Input.ExpandPath(cwd));
                        break;

                    case Switch.AddFile:
                        (string filePath, int startPosition) = project.AddTemplateFile(item.Input, cwd, context, ConfigurationPage.UserTemplateDirectory, _builtinTemplateDirectory);
                        if (File.Exists(filePath))
                        {
                            VsShellUtilities.OpenDocument(this, filePath);
                            Helper.MoveActiveDocumentCursorTo(startPosition);
                            _dte.ExecuteCommand("SolutionExplorer.SyncWithActiveDocument");
                            _dte.ActiveDocument.Activate();
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

        #region Private Members

        private readonly string _builtinTemplateDirectory = Path.Combine(Path.GetDirectoryName(typeof(VSPackage).Assembly.Location), "Templates");

        private DTE2 _dte;
        private IVsOutputWindowPane _console;
        private CommandPromptViewModel _model;

        private IVsOutputWindowPane CreateOutputWindow(string title = nameof(Templata))
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var outputWindow = (IVsOutputWindow)GetService(typeof(SVsOutputWindow));
            if (outputWindow != null)
            {
                var guid = new Guid("d74cfc8e-4d98-44b6-b6ad-975af82658fb");
                outputWindow.CreatePane(ref guid, title, 1, 1);
                outputWindow.GetPane(ref guid, out IVsOutputWindowPane pane);
                return pane;
            }

            return null;
        }

        private void WriteLine(string message)
        {
#pragma warning disable VSTHRD010 // Invoke single-threaded types on Main thread
            //_console.OutputStringThreadSafe(message + "\r\n");
#pragma warning restore VSTHRD010 // Invoke single-threaded types on Main thread
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"{nameof(Templata)}> {message}");
#endif
        }

        private void WriteLine(string message, LogLevel level) => WriteLine(Helper.Format(level, message));

        private void WriteLine(string format, params object[] args) => WriteLine(string.Format(format, args));

        private void PrintDebugInfo(ProjectContext context)
        {
#if DEBUG
            WriteLine($"solution: {context.SolutionFilePath}");
            WriteLine($"project: {context.ProjectFilePath}");
            WriteLine($"project-item: {context.ProjectItemPath}");
            WriteLine($"selected-items: " + string.Join(" | ", context.SelectedItems));
            WriteLine($"namespace: {context.RootNamespace}");
            WriteLine($"assembly: {context.Assemblyname}");
            WriteLine($"version: {context.Version}");
            WriteLine("\n");
#endif
        }

        #endregion Private Members
    }
}