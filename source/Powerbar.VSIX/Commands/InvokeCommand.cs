using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;


namespace Acklann.Powerbar.Commands
{
    public class InvokeCommand
    {
        public InvokeCommand()
        {

        }

        public void Execute(string command, Context context)
        {

        }

        private void OnCommandInvoked(object sender, EventArgs e)
        {

        }

        //internal static void Initialize(IMenuCommandService commandService, DTE dte)
        //{

        //}


        #region Singleton
        public static InvokeCommand Instance { get; private set; }

        
        #endregion Singleton

    }
}
