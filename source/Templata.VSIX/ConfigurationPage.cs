using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel;
using System.IO;

namespace Acklann.Templata
{
    public class ConfigurationPage
    {
        internal static bool TemplateDirectoryExists, UserItemGroupFileExists;
        internal static string UserTemplateDirectory, UserItemGroupFile, UserRootProjectName;

        internal static void Load(General config)
        {
            UserRootProjectName = config.DefaultSolutionExplorerFolderName;
            UserTemplateDirectory = Environment.ExpandEnvironmentVariables(config.TemplateDirectory ?? string.Empty);
            UserItemGroupFile = Environment.ExpandEnvironmentVariables(config.ItemGroupsConfigurationFilePath ?? string.Empty);
        }

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
                UserRootProjectName = DefaultSolutionExplorerFolderName;
                UserTemplateDirectory = Environment.ExpandEnvironmentVariables(TemplateDirectory ?? string.Empty);
                UserItemGroupFile = Environment.ExpandEnvironmentVariables(ItemGroupsConfigurationFilePath ?? string.Empty);

                UserItemGroupFileExists = File.Exists(UserItemGroupFile);
                TemplateDirectoryExists = Directory.Exists(UserTemplateDirectory);
            }

            public override void SaveSettingsToStorage()
            {
                base.SaveSettingsToStorage();
                UserTemplateDirectory = TemplateDirectory;
                UserItemGroupFile = ItemGroupsConfigurationFilePath;
                UserRootProjectName = DefaultSolutionExplorerFolderName;

                UserItemGroupFileExists = File.Exists(UserItemGroupFile);
                TemplateDirectoryExists = Directory.Exists(UserTemplateDirectory);
            }
        }
    }
}