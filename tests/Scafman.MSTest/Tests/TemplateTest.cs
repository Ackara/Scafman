using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Acklann.Scafman.Tests
{
    [TestClass]
    public class TemplateTest
    {
        [TestMethod]
        public void Can_fetch_template_files()
        {
            // Act
            var results = Template.GetFiles(SampleFactory.DirectoryName);

            // Assert
            results.ShouldNotBeEmpty();
            results.ShouldAllBe(x => File.Exists(x));
            results.Length.ShouldBeGreaterThanOrEqualTo(4);
        }

        [DataTestMethod]
        [DataRow("", null)]
        [DataRow(null, null)]
        [DataRow("Person.cs", "~.cs")]
        [DataRow("package.json", null)]
        [DataRow("Article.cst", "~.cst")]
        [DataRow("IAnimal.cst", "I~.cst")]
        [DataRow("Symbol.cst", "symbol.cst")]
        [DataRow("FooTest.cst", "~Test.cst")]
        [DataRow("Helper.cs", "Helper.cs,~Exentions.cs")]
        public void Can_match_filename_to_a_template(string filename, string template)
        {
            // Arrange
            var directory = Path.Combine(Path.GetTempPath(), "mock-templates");
            string expectedFile = Path.Combine(directory, (template ?? string.Empty));
            string folder = Path.GetDirectoryName(expectedFile);
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
            if (!File.Exists(expectedFile) && Path.HasExtension(expectedFile)) File.Create(expectedFile).Dispose();

            // Act
            var result = Template.Find(filename, directory);
            result = Path.GetFileName(result);

            // Assert
            result.ShouldBe(template);
        }

        [TestMethod]
        public void Can_guess_file_extension()
        {
            // Arrange
            var cases = new (string, string)[]
            {
                ("package.json", ".cs"),
                ("css/_layout.css", ".cs"),
                ("css/_buttons.css", ".css"),
            };

            string folder = Path.Combine(Path.GetTempPath(), "guess-ext");
            if (Directory.Exists(folder)) Directory.Delete(folder, recursive: true);
            Directory.CreateDirectory(folder);

            var proj = Path.Combine(folder, "app.csproj");
            File.Create(proj).Dispose();

            // Act
            var case1 = Template.GuessFileExtension(folder, proj);

            var file = Path.Combine(folder, "css/_layout.css");
            folder = Path.GetDirectoryName(file);
            Directory.CreateDirectory(folder);
            File.Create(file).Dispose();
            var case2 = Template.GuessFileExtension(folder, proj);

            // Assert
            case1.ShouldBe(".cs");
            case2.ShouldBe(".css");
        }

        [DataTestMethod]
        [DataRow("", "")]
        [DataRow("index.cshtml", "index.cshtml")]
        [DataRow("@(build)", "build.ps1;tasks.ps1")]
        [DataRow("@(mvc)", "app.css;app.js;index.cshtml")]
        public void Can_expand_item_groups(string input, string expected)
        {
            var config = SampleFactory.GetFile("itemgroups.json").FullName;
            var result = Template.ExpandItemGroups(input, config);

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
        [DataRow("Tests/(Unit|Functional)/", "Tests/Unit/;Tests/Functional/")]
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
        [DataRow("viewModel/person.cs", @"Models\viewModel")]
        [DataRow(@".\viewModel\person.cs", @"Models\viewModel")]
        public void Can_determine_a_project_subfolder(string relativePath, string expected)
        {
            var projectFolder = Path.Combine(Path.GetTempPath(), nameof(Scafman), "src", "Foo");
            var location = Path.Combine(Path.GetTempPath(), nameof(Scafman), "src", "Foo", "Models");

            var result = Template.GetSubfolder(relativePath, projectFolder, location);
            result.ShouldBe(expected, StringCompareShould.IgnoreCase);
        }

        [TestMethod]
        public void Can_replace_template_tokens()
        {
            // Arrange
            var text = File.ReadAllText(SampleFactory.GetFile("~Controller.cst").FullName);
            var baseFolder = Path.Combine(Path.GetTempPath(), nameof(Scafman));
            var outFile = Path.Combine(baseFolder, "src", "Core", "Models", "Person.cs");

            var context = new ProjectContext(
                Path.Combine(baseFolder, "Sample.sln"),
                Path.Combine(baseFolder, "src", "Core", "Core.csproj"),
                outFile,
                $"{nameof(Scafman)}.Web",
                nameof(Scafman),
                "0.0.1"
                );

            // Act
            string case1 = Template.Replace(text, context, outFile, Path.GetDirectoryName(outFile));

            text = File.ReadAllText(SampleFactory.GetFile("script.ts").FullName);
            string case2 = Template.Replace(text, context, outFile, Path.GetDirectoryName(outFile));

            // Assert
            case1.ShouldNotBeNullOrEmpty();
            case1.ShouldNotContain("$guid$");
            case1.ShouldNotContain("$safeitemname$");
            case1.ShouldNotContain("$rootnamespace$");

            case2.ShouldNotContain("$projectrelativepath$");
            case2.StartsWith("../index.d.ts");
        }

        [DataTestMethod]
        [DataRow("", "", Switch.None)]
        [DataRow(null, "", Switch.None)]
        [DataRow("file.cs", "file.cs", Switch.AddFile)]
        [DataRow("folder/", "folder/", Switch.AddFolder)]
        [DataRow(":dapper", "dapper", Switch.NugetPackage)]
        [DataRow("npm:knockout", "knockout", Switch.NPMPackage)]
        [DataRow("folder/sub/", "folder/sub/", Switch.AddFolder)]
        public void Can_convert_string_to_command(string input, string expectedInput, Switch expectedOption)
        {
            // Act + Assert
            var case1 = Command.Parse(input);
            case1.Input.ShouldBe(expectedInput);
            case1.Kind.ShouldBe(expectedOption);
        }

        [DataTestMethod]
        [DataRow("", false)]
        [DataRow(null, false)]
        [DataRow("foo.txt", true)]
        [DataRow("f*o.txt", false)]
        public void Can_validate_file_name(string name, bool expected)
        {
            var result = Template.ValidateFilename(name, out string message);

            result.ShouldBe(expected, message);
            if (result == false) message.ShouldNotBeNullOrEmpty();
        }

        [DataTestMethod]
        [DataRow("", null)]
        [DataRow(null, null)]
        [DataRow("~kind.cs", null)]
        [DataRow("readme.md", "readme.md")]
        [DataRow("~kind.cs,symbol.cs", "symbol.cs")]
        [DataRow("symbol.cs,~kind.cs", "symbol.cs")]
        public void Can_get_template_aliases(string fileName, string expected)
        {
            // Arrange
            var expectedResult = new string[0];
            if (!string.IsNullOrEmpty(expected)) expectedResult = expected.Split(',', ';');

            // Act
            var result = Template.GetAliases(fileName);

            // Assert
            result.ShouldBe(expectedResult, ignoreOrder: true);
        }

        [DataTestMethod]
        [DynamicData(nameof(GetInput), DynamicDataSourceType.Method)]
        public void Can_separate_files_and_packages(string input, Command[] expected)
        {
            var results = Template.Interpret(input).ToArray();
            results.ShouldBeSubsetOf(expected);
        }

        private static IEnumerable<object[]> GetInput()
        {
            yield return new object[] { null, new Command[0] };
            yield return new object[] { string.Empty, new Command[0] };
            yield return new object[] { "app.ts", new Command[] { "app.ts" } };
            yield return new object[] { "app.ts,npm:chartjs", new Command[] { "app.ts", "npm:chartjs" } };
            yield return new object[] { "npm:(chartjs|knockout)", new Command[] { "npm:chartjs", "npm:knockout" } };
            yield return new object[] { "(person|animal)Suite.cs,:FakeItEasy,:Diffa", new Command[] { "personSuite.cs", "animalSuite.cs", "nuget:FakeItEasy", "nuget:Diffa" } };
        }
    }
}