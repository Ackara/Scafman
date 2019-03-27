namespace Acklann.Powerbar
{
    public struct Context
    {
        public Context(string tool) : this(null, null, null, null, tool)
        {
        }

        public Context(string solution, string project, string item, string ns, string tool = "powershell")
        {
            Tool = tool;
            SolutionFilePath = solution;
            ProjectFilePath = project;
            ProjectItemPath = item;
            RootNamespace = ns;
        }

        public readonly string Tool;

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
            var properties = string.Join(",", new string[] {
                $"\"{nameof(SolutionFilePath)}\": \"{SolutionFilePath}\"",
                $"\"{nameof(ProjectFilePath)}\": \"{ProjectFilePath}\"",
                $"\"{nameof(ProjectItemPath)}\": \"{ProjectItemPath}\"",
                $"\"{nameof(RootNamespace)}\": \"{RootNamespace}\"",
            });
            return $"{{ {properties} }}";
        }

        #region Operators

        public static implicit operator string(Context obj)
        {
            return obj.ToString();
        }

        #endregion Operators
    }
}