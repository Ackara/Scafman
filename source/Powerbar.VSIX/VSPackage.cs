using Acklann.GlobN;
using Acklann.Powerbar.ViewModels;
using EnvDTE;
using EnvDTE80;
using Microsoft;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Interop;
using Task = System.Threading.Tasks.Task;

namespace Acklann.Powerbar
{
    [Guid(Symbol.Package.GuidString)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [InstalledProductRegistration("#110", "#112", Vsix.Version, IconResourceID = 400)]
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [ProvideOptionPage(typeof(ConfigurationPage.General), Vsix.Name, nameof(ConfigurationPage.General), 0, 0, true)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    public sealed class VSPackage : AsyncPackage
    {
        private const string HELP_LINK = "http://gigobyte.com";

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var settings = (ConfigurationPage.General)GetDialogPage(typeof(ConfigurationPage.General));
            _model = CommandPromptViewModel.Restore();
            _console = CreateOutputWindow(Vsix.Name);

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

        private void GetContext(out VSContext context, out EnvDTE.Project vsProject)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            _dte.GetProjectInfo(out string project, out string projectItem, out string[] selectedItems, out string ns, out string assemblyName, out string version, out vsProject);

            context = new VSContext(
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

        private void AddItemToProject(EnvDTE.Project project, string location, string fileList, VSContext context)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (string.IsNullOrEmpty(fileList)) return;
            if (project == null) { WriteLine("Could not find any active projects.", LogLevel.Error); return; }
            if (string.IsNullOrEmpty(location)) { WriteLine("Could not determine your current location", LogLevel.Error); return; }

            if (File.Exists(ConfigurationPage.ItemGroupFile))
                fileList = Template.ExpandItemGroup(fileList, ConfigurationPage.ItemGroupFile);

            foreach (Glob path in fileList.Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (path.IsFolder())
                {
                    string folder = path.ExpandPath(location);
                    if (!Directory.Exists(folder)) project.ProjectItems.AddFromDirectory(folder);
                    continue;
                }

                string template = string.Empty;
                string name = Path.GetFileName(path);
                string templatePath = Template.Find(name, ConfigurationPage.TemplateDirectory, _builtinTemplateDirectory);
                if (File.Exists(templatePath))
                {
                    string ns = Template.GetSubfolder(path, Path.GetDirectoryName(context.ProjectFilePath), location).Replace('\\', '.');
                    _tokens.Upsert(context);
                    _tokens.Upsert("rootnamespace", $"{context.RootNamespace}.{ns}".TrimEnd('.'));
                    _tokens.Upsert("itemname", Path.GetFileNameWithoutExtension(name).SafeName());
                    _tokens.Upsert("safeitemname", Path.GetFileNameWithoutExtension(name).SafeName());
                    template = Template.Replace(File.ReadAllText(templatePath), _tokens);
                }
                else WriteLine("Could not find a template for '{0}'; remember you can always create your own templates. Visit {1} for more information.", name, HELP_LINK);

                string newFile = path.ExpandPath(location);
                if (File.Exists(newFile)) MessageBox.Show(string.Format("{0} file already exists.", name), nameof(Powerbar), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                else
                {
                    // Creating the file then add it to solution explorer.
                    string folder = Path.GetDirectoryName(newFile);
                    if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
                    template = Template.RemoveCaret(template, out int cursorPosition);
                    File.WriteAllText(newFile, template);
                    project.ProjectItems.AddFromFile(newFile);
                    VsShellUtilities.OpenDocument(this, newFile);

                    // Move the text cursor into position.
                    Helper.MoveActiveDocumentCursorTo(cursorPosition);
                    _dte.ExecuteCommand("SolutionExplorer.SyncWithActiveDocument");
                    _dte.ActiveDocument.Activate();
                }
            }
        }

        // ==================== Command Handlers ==================== //

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

        private void ExecuteCommand(Location location, string command = null)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            GetContext(out VSContext context, out Project vsProject);
            ShowInfo(context);
            string cwd = context.GetLocation(location);
            if (string.IsNullOrEmpty(command)) command = PromptUser(cwd);
            if (string.IsNullOrEmpty(command)) return;

            Switch options = Shell.GetOptions(ref command);
            if (options.HasFlag(Switch.CreateNewFile)) AddItemToProject(vsProject, cwd, command, context);
            else
            {
                _console.Clear();
                _console.Activate();
                WriteLine(command + "\r\n");
                Shell.Invoke(cwd, command, options, context, WriteLine);
            }
        }

        #region Private Members

        private readonly IDictionary<string, string> _tokens = Template.GetReplacmentTokens().ToDictionary(x => x.Key, x => x.Value);
        private readonly string _builtinTemplateDirectory = Path.Combine(Path.GetDirectoryName(typeof(VSPackage).Assembly.Location), "Templates");

        private DTE2 _dte;
        private IVsOutputWindowPane _console;
        private CommandPromptViewModel _model;

        private void WriteLine(string message)
        {
#pragma warning disable VSTHRD010 // Invoke single-threaded types on Main thread
            _console.OutputStringThreadSafe(message + "\r\n");
#pragma warning restore VSTHRD010 // Invoke single-threaded types on Main thread
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"{nameof(Powerbar)}> {message}");
#endif
        }

        private void WriteLine(string message, LogLevel level) => WriteLine(level.Format(message));

        private void WriteLine(string format, params object[] args) => WriteLine(string.Format(format, args));

        private IVsOutputWindowPane CreateOutputWindow(string title = nameof(Powerbar))
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

        private void ShowInfo(VSContext context)
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