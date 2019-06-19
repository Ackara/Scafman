using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;

namespace Acklann.Templata
{
    public class ConfigurationPage
    {
        internal static bool ShouldCreateTemplateIfMissing;
        internal static string UserItemGroupFile, UserRootProjectName;
        internal static string[] UserTemplateDirectories;

        internal static string UserTemplateDirectory
        {
            get => (UserTemplateDirectories?.Length > 0 ? UserTemplateDirectories[0] : null);
        }

        public class General : DialogPage
        {
            private readonly string[] _builtInTemplateFolders = new string[]
            {
                Path.Combine(Path.GetDirectoryName(typeof(VSPackage).Assembly.Location), "Templates")
            };

            [Category(nameof(General))]
            [DisplayName("Template Directory")]
            [Description("The absolute path of your template directory.")]
            public string[] TemplateDirectories { get; set; }

            [Category(nameof(General))]
            [DisplayName("Item Groups")]
            [Description("The absolute path of your item-group configuration file.")]
            public string ItemGroupsConfigurationFilePath { get; set; }

            [Category(nameof(General))]
            [DisplayName("Default Solution Explorer Folder")]
            [Description("The name of default solution explorer folder name")]
            public string DefaultSolutionExplorerFolderName { get; set; } = "Solution Items";

            [Category(nameof(General))]
            [DisplayName("Create Template If Not Exists")]
            [Description("Determines whether to create the missing template when the compare command is invoked.")]
            public bool CreateTemplateIfMissing { get; set; } = true;

            public override void LoadSettingsFromStorage()
            {
                System.Diagnostics.Debug.WriteLine($"{nameof(ConfigurationPage)}::{nameof(LoadSettingsFromStorage)}");
                base.LoadSettingsFromStorage();
                Update();
            }

            public override void SaveSettingsToStorage()
            {
                System.Diagnostics.Debug.WriteLine($"{nameof(ConfigurationPage)}::{nameof(SaveSettingsToStorage)}");
                base.SaveSettingsToStorage();
                Update();
            }

            private void Update()
            {
                UserRootProjectName = DefaultSolutionExplorerFolderName;
                ShouldCreateTemplateIfMissing = CreateTemplateIfMissing;
                UserItemGroupFile = Environment.ExpandEnvironmentVariables(ItemGroupsConfigurationFilePath ?? string.Empty);

                if (TemplateDirectories?.Length > 0)
                    UserTemplateDirectories = TemplateDirectories.Select((x) => Environment.ExpandEnvironmentVariables(x)).Concat(_builtInTemplateFolders).ToArray();
                else
                    UserTemplateDirectories = _builtInTemplateFolders;
            }
        }
    }
}