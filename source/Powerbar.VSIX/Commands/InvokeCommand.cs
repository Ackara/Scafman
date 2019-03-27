using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel.Design;
using System.Globalization;
using Task = System.Threading.Tasks.Task;

namespace Acklann.Powerbar
{
    internal sealed class InvokeCommand
    {
        private InvokeCommand(AsyncPackage package, IMenuCommandService commandService)
        {
            _package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));
            commandService.AddCommand(new MenuCommand(OnCommandInvoked, new CommandID(Symbol.CmdSet.Guid, Symbol.CmdSet.InvokeCommandId)));
        }

        public static InvokeCommand Instance { get; private set; }

        internal static async Task InitializeAsync(AsyncPackage package, IMenuCommandService commandService)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);
            Instance = new InvokeCommand(package, commandService);
        }

        internal void OnCommandInvoked(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            string message = string.Format(CultureInfo.CurrentCulture, "Inside {0}.MenuItemCallback()", this.GetType().FullName);
            string title = "Command1";

            VsShellUtilities.ShowMessageBox(
                this._package,
                message,
                title,
                OLEMSGICON.OLEMSGICON_INFO,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }

        #region Private Members

        private readonly AsyncPackage _package;

        #endregion Private Members
    }
}