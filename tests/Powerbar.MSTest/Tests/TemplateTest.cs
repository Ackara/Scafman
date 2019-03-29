using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
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
            var filePath = Template.Locate(filename, MockFactory.DirectoryName);
            if (expectedFile == null && filePath == null) return;
            var file = new FileInfo(filePath);

            // Assert
            file.Exists.ShouldBeTrue();
            file.Name.ShouldBe(expectedFile);
            file.DirectoryName.ShouldContain(MockFactory.DirectoryName);
        }

        [DataTestMethod]
        [DataRow("", "")]
        [DataRow("index.cshtml", "index.cshtml")]
        [DataRow("@(build)", "build.ps1,tasks.ps1")]
        [DataRow("@(mvc)", "app.css,app.js,index.cshtml")]
        public void Can_expand_item_groups(string input, string expected)
        {
            var config = MockFactory.GetFile("itemgroups.json").FullName;
            var result = Template.ExpandItemGroup(input, config);

            result.ShouldBe(expected);
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