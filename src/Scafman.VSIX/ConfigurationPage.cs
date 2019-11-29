using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Acklann.Scafman
{
    public class ConfigurationPage : DialogPage
    {
        public ConfigurationPage()
        {
            var knownDiffTools = new string[]
            {
                    @"C:\Program Files\Beyond Compare 4\BCompare.exe"
            };

            foreach (string executablePath in knownDiffTools)
                if (File.Exists(executablePath))
                {
                    DiffTool = executablePath;
                    break;
                }
        }

        internal const string Category = "General";
        internal static string UserItemGroupConfigurationFilePath, SolutionFolderName, DiffExecutable;
        internal static string[] UserTemplateDirectories;

        private static readonly string[] _builtInTemplateFolders = new string[]
        {
            Path.Combine(Path.GetDirectoryName(typeof(VSPackage).Assembly.Location), "Templates")
        };

        [Category(Category)]
        [DisplayName("Template Directory")]
        [Description("The absolute path of your template directory.")]
        [TypeConverter(typeof(StringArrayConverter))]
        public string[] TemplateDirectories
        {
            get => UserTemplateDirectories;
            set { UserTemplateDirectories = value; }
        }

        [Category(Category)]
        [DisplayName("Item Groups")]
        [Description("The absolute path of your item-group configuration file.")]
        public string ItemGroupConfigurationFile
        {
            get => UserItemGroupConfigurationFilePath;
            set { UserItemGroupConfigurationFilePath = Environment.ExpandEnvironmentVariables(value); }
        }

        [Category(Category)]
        [DisplayName("Solution Folder Name")]
        [Description("The name of folder name to place files in when at the solution level.")]
        public string FolderName
        {
            get => (SolutionFolderName ?? "Solution Items");
            set { SolutionFolderName = value; }
        }

        [Category(Category)]
        [DisplayName("Preferred diff tool")]
        [Description("The absolute path the preferred diff tool.")]
        public string DiffTool
        {
            get => DiffExecutable;
            set { DiffExecutable = value; }
        }

        internal static string UserTemplateDirectory
        {
            get => (UserTemplateDirectories?.Length > 0 ? UserTemplateDirectories[0] : null);
        }

        internal static string[] GetAllTemplateDirectories()
        {
            if (UserTemplateDirectories?.Length > 0)
                return UserTemplateDirectories.Select((x) => Environment.ExpandEnvironmentVariables(x)).Concat(_builtInTemplateFolders).ToArray();
            else
                return _builtInTemplateFolders;
        }

        #region Backing Members

        private class StringArrayConverter : TypeConverter
        {
            // LINK: https://stackoverflow.com/questions/24291249/dialogpage-string-array-not-persisted
            private const char SEPARATOR = '|';

            // Load
            public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
            {
                return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
            }

            public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
            {
                return ((value is string text) ? text.Split(new char[] { SEPARATOR }, StringSplitOptions.RemoveEmptyEntries) : base.ConvertFrom(context, culture, value));
            }

            // Save
            public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
            {
                return destinationType == typeof(string[]) || base.CanConvertTo(context, destinationType);
            }

            public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
            {
                return ((destinationType == typeof(string) && (value is string[] array)) ? string.Join(char.ToString(SEPARATOR), array) : base.ConvertTo(context, culture, value, destinationType));
            }
        }

        #endregion Backing Members
    }
}