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
        internal VSContext CreateContext()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            _dte.GetProjectInfo(out string project, out string projectItem, out string[] selectedItems, out string ns, out string assemblyName, out string version);

            WriteLine($"solution: {_dte.Solution.FullName}");
            WriteLine($"project: {project}");
            WriteLine($"project-item: {projectItem}");
            WriteLine($"{nameof(selectedItems)}: " + string.Join(" | ", selectedItems));
            WriteLine($"namespace: {ns}");
            WriteLine($"assembly: {assemblyName}");
            WriteLine($"version: {version}");
            WriteLine("\n");

            return new VSContext(
                _dte.Solution.FullName,
                project,
                projectItem, selectedItems,
                ns, assemblyName, version);
        }

        internal string PromptUser(Location location, VSContext context)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            _model.Location = context.GetLocation(location);
            _model.Clear();

            var dialog = new CommandPrompt(_model);
            dialog.Owner = (System.Windows.Window)HwndSource.FromHwnd(new IntPtr(_dte.MainWindow.HWnd)).RootVisual;
            dialog.ShowDialog();

            return (_model.UserInput ?? string.Empty).Trim();
        }

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            _model = new CommandPromptViewModel();
            _console = CreateOutputPane(Vsix.Name);

            _dte = await GetServiceAsync(typeof(DTE)) as DTE2;
            Assumes.Present(_dte);

            var commandService = await GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Assumes.Present(commandService);
            commandService.AddCommand(new MenuCommand(OnCurrentLevelCommandInvoked, new CommandID(Symbol.CmdSet.Guid, Symbol.CmdSet.CurrentLevelCommandId)));
        }

        // ==================== COMMANDS ==================== //

        private void OnCurrentLevelCommandInvoked(object sender, EventArgs e)
        {
            VSContext context = CreateContext();
            string command = PromptUser(Location.Current, context);

            ExecuteCommand(command, context);
        }

        private void ExecuteCommand(string command, VSContext context)
        {
            if (string.IsNullOrWhiteSpace(command)) return;
            ShellOptions options = _model.GetOptions();
            command = command.Trim('|', '>', ' ');

            ThreadHelper.ThrowIfNotOnUIThread();

            _console.Clear();
            _console.Activate();
            WriteLine(command + "\r\n");
            //Shell.Invoke(command, context, Print);
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