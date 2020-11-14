using System;
using System.Collections.Generic;

namespace Acklann.Scafman
{
    public class Intellisense
    {
        internal const int DEFAULT_LIMIT = 3;

        public static IntellisenseItem[] GetTemplates(string input, string[] templatePaths, int take = DEFAULT_LIMIT)
        {
            if (string.IsNullOrEmpty(input) || templatePaths == null || templatePaths.Length < 1) return new IntellisenseItem[0];

            string[] aliases;
            string alias, keyword, folderOperator = null;
            var matches = new List<IntellisenseItem>(take);
            int index = (input.LastIndexOf(Template.Separators) + 1);

            keyword = input.Substring(index).TrimStart(Command.GetFolderOperators());
            if (string.IsNullOrEmpty(keyword)) return new IntellisenseItem[0];

            folderOperator = input.Substring(index);
            folderOperator = (
                folderOperator.StartsWith("\\") || folderOperator.StartsWith("/") ?
                    folderOperator.Substring(0, (Utilities.LastIndexOf(folderOperator, '\\', '/') + 1))
                    : null);

            for (int x = 0; x < templatePaths.Length; x++)
            {
                aliases = Template.GetAliases(templatePaths[x]);
                for (int y = 0; y < aliases.Length; y++)
                {
                    if (!string.IsNullOrEmpty(alias = aliases[y]))
                        if (alias.StartsWith(keyword, StringComparison.InvariantCultureIgnoreCase))
                        {
                            matches.Add(new IntellisenseItem(
                                alias,
                                templatePaths[x],
                                string.Concat(input.Substring(0, index), folderOperator, alias)
                                ));

                            if (matches.Count >= take) return matches.ToArray();
                        }
                }
            }

            return matches.ToArray();
        }

        public static IntellisenseItem[] GetItemGroups(string input, in ItemGroup[] options, int take = DEFAULT_LIMIT)
        {
            if (string.IsNullOrEmpty(input) || options == null || options.Length < 1) return new IntellisenseItem[0];

            int startIndex = input.LastIndexOf('@');
            if (startIndex == -1) return new IntellisenseItem[0];

            var matches = new List<IntellisenseItem>(take);
            string term = input.Substring(startIndex).TrimStart('@', '(', ' '), command;

            int count = 0; ItemGroup item;
            for (int i = 0; i < options.Length; i++)
            {
                item = options[i];
                if (item.Name.Trim().StartsWith(term, StringComparison.OrdinalIgnoreCase))
                {
                    command = string.Concat(input.Substring(0, startIndex), $"@({item.Name})");

                    matches.Add(new IntellisenseItem(item.Name, string.Join(" | ", item.FileList), command));
                    if (++count >= take) break;
                }
            }

            var results = new IntellisenseItem[count];
            matches.CopyTo(0, results, 0, count);
            return results;
        }

        public static IntellisenseItem[] GetItemGroups(string input, string configurationFile, int take = DEFAULT_LIMIT)
        {
            return GetItemGroups(input, ItemGroup.ReadFile(configurationFile), take);
        }
    }
}