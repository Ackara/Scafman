namespace Acklann.Powerbar
{
    public struct VSContext
    {
        public VSContext(string solution, string project, string item, string ns)
        {
            SolutionFilePath = solution;
            ProjectFilePath = project;
            ProjectItemPath = item;
            RootNamespace = ns ?? "MyNamespace";
        }

        public readonly string SolutionFilePath;

        public readonly string ProjectFilePath;

        public readonly string ProjectItemPath;

        public readonly string RootNamespace;

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            string escape(string value) => $"''{value}''";

            var properties = string.Join(",", new string[] {
                $"{escape(nameof(SolutionFilePath))}: {escape(SolutionFilePath)}",
                $"{escape(nameof(ProjectFilePath))}: {escape(ProjectFilePath)}",
                $"{escape(nameof(ProjectItemPath))}: {escape(ProjectItemPath)}",
                $"{escape(nameof(RootNamespace))}: {escape(RootNamespace)}",
            });
            return $"'{{ {properties} }}'";
        }

        #region Operators

        public static implicit operator string(VSContext obj)
        {
            return obj.ToString();
        }

        #endregion Operators
    }
}