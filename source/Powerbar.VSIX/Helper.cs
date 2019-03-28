using System.IO;

namespace Acklann.Powerbar
{
    public static class Helper
    {
        public static string GetLocation(this VSContext context, Location location)
        {
            switch (location)
            {
                default:
                case Location.Current:
                    if (!string.IsNullOrEmpty(context.ProjectItemPath))
                        return Path.GetDirectoryName(context.ProjectItemPath);
                    else if (!string.IsNullOrEmpty(context.ProjectFilePath))
                        return Path.GetDirectoryName(context.ProjectFilePath);
                    else if (!string.IsNullOrEmpty(context.ProjectFilePath))
                        return Path.GetDirectoryName(context.SolutionFilePath);
                    else return System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile);

                case Location.Project:
                    return Path.GetDirectoryName(context.ProjectFilePath) ?? throw new DirectoryNotFoundException("Could not find the project directory.");

                case Location.Solution:
                    return Path.GetDirectoryName(context.SolutionFilePath) ?? throw new DirectoryNotFoundException("Could not find the solution directory.");
            }
        }
    }
}