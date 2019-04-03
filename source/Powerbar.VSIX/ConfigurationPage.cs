using Microsoft.VisualStudio.Shell;
using System.ComponentModel;

namespace Acklann.Powerbar
{
    public class ConfigurationPage
    {
        internal static string UserTemplateDirectory, UserItemGroupFile, UserRootProjectName;

        public class General : DialogPage
        {
            [Category(nameof(General))]
            [DisplayName("Template Directory")]
            [Description("The absolute path of your template directory.")]
            public string TemplateDirectory { get; set; }

            [Category(nameof(General))]
            [DisplayName("Item Groups")]
            [Description("The absolute path of your item-group configuration file.")]
            public string ItemGroupsConfigurationFilePath { get; set; }

            [Category(nameof(General))]
            [DisplayName("Default Solution Explorer Folder")]
            [Description("The name of default solution explorer folder name")]
            public string DefaultSolutionExplorerFolderName { get; set; } = "Solution Items";

            public override void LoadSettingsFromStorage()
            {
                base.LoadSettingsFromStorage();
                UserTemplateDirectory = TemplateDirectory;
                UserItemGroupFile = ItemGroupsConfigurationFilePath;
                UserRootProjectName = DefaultSolutionExplorerFolderName;
            }

            public override void SaveSettingsToStorage()
            {
                base.SaveSettingsToStorage();
                UserTemplateDirectory = TemplateDirectory;
                UserItemGroupFile = ItemGroupsConfigurationFilePath;
                UserRootProjectName = DefaultSolutionExplorerFolderName;
            }
        }
    }
}