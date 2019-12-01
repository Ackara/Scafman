using System;

namespace Acklann.Scafman
{
    public readonly struct ProjectContext
    {
        public ProjectContext(string solution, string project, string item, string ns, string assemblyName, string version)
        {
            SolutionFilePath = solution;
            ProjectFilePath = project;
            SelectedItemPath = item;

            RootNamespace = ns ?? "MyNamespace";
            Assemblyname = assemblyName;
            Version = version;
        }

        public readonly string SolutionFilePath;

        public readonly string ProjectFilePath;

        public readonly string SelectedItemPath;

        public readonly string RootNamespace;

        public readonly string Assemblyname;

        public readonly string Version;

        public string SolutionDirectory
        {
            get => System.IO.Path.GetDirectoryName(SolutionFilePath);
        }

        public string SolutionName
        {
            get => System.IO.Path.GetFileNameWithoutExtension(SolutionFilePath);
        }

        public string ProjectDirectory
        {
            get => System.IO.Path.GetDirectoryName(ProjectFilePath);
        }

        public string ProjectName
        {
            get => System.IO.Path.GetFileNameWithoutExtension(ProjectFilePath);
        }

        public string CurrentDirectory
        {
            get
            {
                if (!string.IsNullOrEmpty(SelectedItemPath))
                    return System.IO.Path.GetDirectoryName(SelectedItemPath);
                else if (!string.IsNullOrEmpty(ProjectFilePath))
                    return System.IO.Path.GetDirectoryName(ProjectFilePath);
                else if (!string.IsNullOrEmpty(SolutionFilePath))
                    return System.IO.Path.GetDirectoryName(SolutionFilePath);
                else
                    return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            }
        }

        public override string ToString()
        {
            string escape(string value) => (value == null ? "null" : $"''{value}''");

            var properties = string.Join(",", new string[] {
                $"{escape(nameof(SolutionFilePath))}: {escape(SolutionFilePath)}",
                $"{escape(nameof(ProjectFilePath))}: {escape(ProjectFilePath)}",
                $"{escape(nameof(SelectedItemPath))}: {escape(SelectedItemPath)}",

                $"{escape(nameof(RootNamespace))}: {escape(RootNamespace)}",
                $"{escape(nameof(Assemblyname))}: {escape(Assemblyname)}",
                $"{escape(nameof(Version))}: {escape(Version)}",
            });
            return $"'{{ {properties} }}'";
        }
    }
}