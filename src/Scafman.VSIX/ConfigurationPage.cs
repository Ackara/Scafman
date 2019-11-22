﻿using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Acklann.Scafman
{
    public class ConfigurationPage
    {
        internal static string UserItemGroupFile, SolutionFolderName, DiffExecutable;
        internal static string[] UserTemplateDirectories;

        internal static string UserTemplateDirectory
        {
            get => (UserTemplateDirectories?.Length > 0 ? UserTemplateDirectories[0] : null);
        }

        public class General : DialogPage
        {
            public General()
            {
                var knownDiffToolsInstallationPaths = new string[]
                {
                    @"C:\Program Files\Beyond Compare 4\BCompare.exe"
                };

                foreach (string executablePath in knownDiffToolsInstallationPaths)
                    if (File.Exists(executablePath))
                    {
                        DiffTool = executablePath;
                        break;
                    }
            }

            private readonly string[] _builtInTemplateFolders = new string[]
            {
                Path.Combine(Path.GetDirectoryName(typeof(VSPackage).Assembly.Location), "Templates")
            };

            [Category(nameof(General))]
            [DisplayName("Template Directory")]
            [Description("The absolute path of your template directory.")]
            [TypeConverter(typeof(StringArrayConverter))]
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

            [Category(nameof(General))]
            [DisplayName("Preferred diff tool")]
            [Description("The absolute path the preferred diff tool.")]
            public string DiffTool { get; set; }

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
                DiffExecutable = DiffTool;
                SolutionFolderName = DefaultSolutionExplorerFolderName;
                UserItemGroupFile = Environment.ExpandEnvironmentVariables(ItemGroupsConfigurationFilePath ?? string.Empty);

                if (TemplateDirectories?.Length > 0)
                    UserTemplateDirectories = TemplateDirectories.Select((x) => Environment.ExpandEnvironmentVariables(x)).Concat(_builtInTemplateFolders).ToArray();
                else
                    UserTemplateDirectories = _builtInTemplateFolders;
            }
        }
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
    }
}