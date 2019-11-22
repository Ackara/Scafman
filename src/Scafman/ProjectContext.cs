using System.Linq;

namespace Acklann.Scafman
{
    public readonly struct ProjectContext
    {
        public ProjectContext(string solution, string project, string item, string[] selectedItems, string ns, string assemblyName, string version)
        {
            SelectedItems = selectedItems;
            SolutionFilePath = solution;
            ProjectFilePath = project;
            ProjectItemPath = item;

            RootNamespace = ns ?? "MyNamespace";
            Assemblyname = assemblyName;
            Version = version;
        }

        public readonly string SolutionFilePath;

        public readonly string ProjectFilePath;

        public readonly string ProjectItemPath;

        public readonly string[] SelectedItems;

        public readonly string RootNamespace;

        public readonly string Assemblyname;

        public readonly string Version;

        public override string ToString()
        {
            string escape(string value) => (value == null ? "null" : $"''{value}''");

            var properties = string.Join(",", new string[] {
                $"{escape(nameof(SolutionFilePath))}: {escape(SolutionFilePath)}",
                $"{escape(nameof(ProjectFilePath))}: {escape(ProjectFilePath)}",
                $"{escape(nameof(ProjectItemPath))}: {escape(ProjectItemPath)}",
                $"{escape(nameof(SelectedItems))}: [{string.Join(",", SelectedItems.Select(x=> escape(x)))}]",

                $"{escape(nameof(RootNamespace))}: {escape(RootNamespace)}",
                $"{escape(nameof(Assemblyname))}: {escape(Assemblyname)}",
                $"{escape(nameof(Version))}: {escape(Version)}",
            });
            return $"'{{ {properties} }}'";
        }
    }
}