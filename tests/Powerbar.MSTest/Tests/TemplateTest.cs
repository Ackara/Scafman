using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Acklann.Powerbar.Tests
{
    [TestClass]
    public class TemplateTest
    {
        [DataTestMethod]
        [DataRow("Person.cs", "~.cs")]
        [DataRow("package.json", null)]
        [DataRow("IAnimal.cs", "I~.cs")]
        [DataRow("Symbol.cs", "symbol.cs")]
        [DataRow("HomeController.cs", "~Controller.cs")]
        public void Can_match_filename_to_a_template(string filename, string expectedFile)
        {
            // Act
            var filePath = Template.Find(filename, MockFactory.DirectoryName);
            if (expectedFile == null && filePath == null) return;
            var file = new FileInfo(filePath);

            // Assert
            file.Exists.ShouldBeTrue();
            file.Name.ShouldBe(expectedFile);
            file.DirectoryName.ShouldContain(MockFactory.DirectoryName);
        }

        [DataTestMethod]
        [DataRow("/app/person", ".cs")]
        public void Can_guess_file_extension(string path, string expectedExtension)
        {
            string projectFile = Path.Combine(Path.GetTempPath(), "app.csproj");

            var result = Template.GetExtension(projectFile, Path.GetTempPath());
            if (path == null) Assert.AreEqual(result, string.Empty);
            else result.ShouldBe(expectedExtension);
        }

        [DataTestMethod]
        [DataRow("", "")]
        [DataRow("index.cshtml", "index.cshtml")]
        [DataRow("@(build)", "build.ps1;tasks.ps1")]
        [DataRow("@(mvc)", "app.css;app.js;index.cshtml")]
        public void Can_expand_item_groups(string input, string expected)
        {
            var config = MockFactory.GetFile("itemgroups.json").FullName;
            var result = Template.ExpandItemGroup(input, config);

            if (input == null) Assert.AreEqual(result, expected);
            else result.ShouldBe(expected);
        }

        [DataTestMethod]
        [DataRow("", "")]
        [DataRow(null, null)]
        [DataRow("index.cshtml", "index.cshtml")]
        [DataRow("app.css,app.js", "app.css;app.js")]
        [DataRow("script.(ts|js)", "script.ts;script.js")]
        [DataRow("index.html;app.(ts|js)", "index.html;app.ts;app.js")]
        [DataRow("../css/(button.css|table.css)", "../css/button.css;../css/table.css")]
        [DataRow("wwwroot/img/(mobile|desktop)/wallpaper.jpg", "wwwroot/img/mobile/wallpaper.jpg;wwwroot/img/desktop/wallpaper.jpg")]
        public void Can_split_inline_group(string input, string expected)
        {
            var result = Template.Split(input);

            if (input == null) result.ShouldBeEmpty();
            else result.ShouldBe(expected.Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries));
        }

        [DataTestMethod]
        [DataRow("../person.cs", "")]
        [DataRow("person.cs", "Models")]
        [DataRow("viewModel/person.cs", "Models\\viewModel")]
        [DataRow(".\\viewModel\\person.cs", "Models\\viewModel")]
        public void Can_determine_a_project_subfolder(string relativePath, string expected)
        {
            var projectFolder = Path.Combine(Path.GetTempPath(), nameof(Powerbar), "src", "Foo");
            var location = Path.Combine(Path.GetTempPath(), nameof(Powerbar), "src", "Foo", "Models");

            var result = Template.GetSubfolder(relativePath, projectFolder, location);
            result.ShouldBe(expected, StringCompareShould.IgnoreCase);
        }

        [TestMethod]
        public void Can_replace_tokens_with_real_values()
        {
            // Arrange
            IEnumerable<KeyValuePair<string, string>> tokens = new Dictionary<string, string>()
            {
                 {"safeitemname", "Test" },
                 {"rootnamespace", nameof(Powerbar) },
                 {"guid", "abc-def" },
            };
            tokens = Enumerable.Concat(tokens, Template.GetReplacmentTokens()).ToArray();

            var content = File.ReadAllText(MockFactory.GetFile("~Controller.cs").FullName);

            // Act
            var result = Template.Replace(content, tokens);

            // Assert
            result.ShouldNotBeNullOrEmpty();
            result.ShouldContain($"class Test");
            result.ShouldMatch(@"\[Guid\(""[a-z0-9-]+""\)\]");
            result.ShouldContain($"namespace {nameof(Powerbar)}");
        }
    }
}