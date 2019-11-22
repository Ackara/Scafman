using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Text;

namespace Acklann.Scafman
{
    public static class NPM
    {
        public static bool Install(string projectFolder, string name, string version, bool tool = false)
        {
            if (string.IsNullOrEmpty(name) || !Directory.Exists(projectFolder)) return false;

            string filePath = Path.Combine(projectFolder, "package.json");
            JObject package = (File.Exists(filePath) ? JObject.Parse(ReadFile(filePath)) : new JObject());

            JObject dependencies;
            JToken temp = package[nameof(dependencies)];
            if (temp == null)
            {
                dependencies = new JObject();
                package.Add(nameof(dependencies), dependencies);
            }
            else dependencies = (JObject)temp;

            temp = dependencies.SelectToken(name);
            if (temp == null) dependencies.Add(new JProperty(name, (version ?? string.Empty)));
            else dependencies.Property(name).Value = (version ?? string.Empty);

            using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Read))
            using (var writer = new StreamWriter(stream, Encoding.UTF8))
            {
                writer.Write(package.ToString(Formatting.Indented));
                writer.Flush();
            }

            return true;
        }

        public static bool Install(string projectFolder, Package package) => Install(projectFolder, package.Name, package.Version);

        private static string ReadFile(string path)
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }
    }
}