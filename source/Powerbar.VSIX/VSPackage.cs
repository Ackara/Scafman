using Acklann.Powerbar.ViewModels;
using EnvDTE;
using EnvDTE80;
using Microsoft;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel.Design;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Interop;
using Task = System.Threading.Tasks.Task;

namespace Acklann.Powerbar
{
    [Guid(Symbol.Package.GuidString)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [InstalledProductRegistration("#110", "#112", Vsix.Version, IconResourceID = 400)]
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    public sealed class VSPackage : AsyncPackage
    {
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            _model = CommandPromptViewModel.Restore();
            _console = CreateOutputPane(Vsix.Name);

            _dte = await GetServiceAsync(typeof(DTE)) as DTE2;
            Assumes.Present(_dte);

            var commandService = await GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Assumes.Present(commandService);
            commandService.AddCommand(new MenuCommand(OnCurrentLevelCommandInvoked, new CommandID(Symbol.CmdSet.Guid, Symbol.CmdSet.CurrentLevelCommandId)));
            //commandService.AddCommand(new MenuCommand(OnProjectLevelCommandInvoked, new CommandID(Symbol.CmdSet.Guid, Symbol.CmdSet.ProjectLevelCommandId)));
            //commandService.AddCommand(new MenuCommand(OnSolutionLevelCommandInvoked, new CommandID(Symbol.CmdSet.Guid, Symbol.CmdSet.SolutionLevelCommandId)));
        }

        protected override void Dispose(bool disposing)
        {
            _model.Save();
            base.Dispose(disposing);
        }

        private VSContext CreateContext()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            _dte.GetProjectInfo(out string project, out string projectItem, out string[] selectedItems, out string ns, out string assemblyName, out string version);
            //WriteLine($"solution: {_dte.Solution.FullName}");
            //WriteLine($"project: {project}");
            //WriteLine($"project-item: {projectItem}");
            //WriteLine($"{nameof(selectedItems)}: " + string.Join(" | ", selectedItems));
            //WriteLine($"namespace: {ns}");
            //WriteLine($"assembly: {assemblyName}");
            //WriteLine($"version: {version}");
            //WriteLine("\n");

            return new VSContext(
                _dte.Solution.FullName, project, projectItem, selectedItems,
                ns, assemblyName, version);
        }

        private string PromptUser(string location)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            _model.Clear();
            _model.Location = location;

            var dialog = new CommandPrompt(_model);
            dialog.Owner = (System.Windows.Window)HwndSource.FromHwnd(new IntPtr(_dte.MainWindow.HWnd)).RootVisual;
            dialog.ShowDialog();

            return (_model.UserInput ?? string.Empty).Trim();
        }

        // ==================== COMMANDS ==================== //

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

        private void ExecuteCommand(Location location, string command)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            VSContext context = CreateContext();
            string cwd = context.GetLocation(Location.Current);
            if (string.IsNullOrWhiteSpace(command)) command = PromptUser(cwd);

            ShellOptions options = _model.GetOptions();
            command = command.Trim('|', '>', ' ');

            _console.Clear();
            _console.Activate();
            WriteLine(command + "\r\n");
            Shell.Invoke(cwd, command, options, context, WriteLine);
        }

        #region Private Members

        private readonly Regex _pattern = new Regex("^[|>]{1,2}", RegexOptions.Compiled);

        private DTE2 _dte;
        private IVsOutputWindowPane _console;
        private CommandPromptViewModel _model;

        private void WriteLine(string message)
        {
#pragma warning disable VSTHRD010 // Invoke single-threaded types on Main thread
            _console.OutputStringThreadSafe(message + "\r\n");
#pragma warning restore VSTHRD010 // Invoke single-threaded types on Main thread
        }

        private IVsOutputWindowPane CreateOutputPane(string title = nameof(Powerbar))
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

        #endregion Private Members
    }
}