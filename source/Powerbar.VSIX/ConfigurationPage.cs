using Microsoft.VisualStudio.Shell;
using System.ComponentModel;

namespace Acklann.Powerbar
{
    public class ConfigurationPage
    {
        internal static string TemplateDirectory, ItemGroupFile;

        public class General : DialogPage
        {
            [Category(nameof(General))]
            [DisplayName("Template Directory")]
            [Description("The absolute path of your template directory.")]
            public string UserTemplateDirectory { get; set; }

            [Category(nameof(General))]
            [DisplayName("Item Groups")]
            [Description("The absolute path of your item-group configuration file.")]
            public string ItemGroupsConfigurationFilePath { get; set; }

            public override void LoadSettingsFromStorage()
            {
                base.LoadSettingsFromStorage();
                TemplateDirectory = UserTemplateDirectory;
                ItemGroupFile = ItemGroupsConfigurationFilePath;
                System.Diagnostics.Debug.WriteLine($"Called {nameof(LoadSettingFromStorage)}");
                System.Diagnostics.Debug.WriteLine($" -> {UserTemplateDirectory}");
                System.Diagnostics.Debug.WriteLine($" -> {ItemGroupsConfigurationFilePath}");
            }

            public override void SaveSettingsToStorage()
            {
                base.SaveSettingsToStorage();
                TemplateDirectory = UserTemplateDirectory;
                ItemGroupFile = ItemGroupsConfigurationFilePath;
                System.Diagnostics.Debug.WriteLine($"Called {nameof(SaveSettingsToStorage)}");
                System.Diagnostics.Debug.WriteLine($" -> {UserTemplateDirectory}");
                System.Diagnostics.Debug.WriteLine($" -> {ItemGroupsConfigurationFilePath}");
            }
        }
    }
}