using System.IO;
using System.Runtime.Serialization;

namespace Acklann.Scafman
{
    [System.Diagnostics.DebuggerDisplay("{GetDebuggerDisplay()}")]
    public struct ItemGroup
    {
        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "fileList")]
        public string[] FileList { get; set; }

        public static ItemGroup[] ReadFile(string filePath)
        {
            if (!File.Exists(filePath)) throw new FileNotFoundException($"Could not find item-group configuraiton file at '{filePath}'.");

            using (Stream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var reader = new StreamReader(stream))
            {
                var settings = new Newtonsoft.Json.JsonSerializerSettings
                {
                    ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver() { NamingStrategy = new Newtonsoft.Json.Serialization.CamelCaseNamingStrategy() }
                };
                return Newtonsoft.Json.JsonConvert.DeserializeObject<ItemGroup[]>(reader.ReadToEnd());
            }
        }

        internal string GetDebuggerDisplay()
        {
            return $"{Name}: {string.Join(",", FileList)}";
        }
    }
}