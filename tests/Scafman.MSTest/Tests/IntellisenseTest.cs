using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using System;
using System.Linq;

namespace Acklann.Scafman.Tests
{
    [TestClass]
    public class IntellisenseTest
    {
        [DataRow("", null)]
        [DataRow(null, null)]
        [DataRow("sym", "symbol.cst")]
        [DataRow("s", "script.ts,symbol.cst")]
        [DataRow("@(build);SYM", "@(build);symbol.cst")]
        [DataTestMethod]
        public void Can_fetch_template_options(string input, string expected)
        {
            // Arrange
            var templates = Template.GetNames(SampleFactory.DirectoryName);
            var expectedResults = (string.IsNullOrEmpty(expected) ? new string[0] : expected.Split(','));

            // Act
            var results = Intellisense.GetTemplates(input, templates).Select(x => x.FullText).ToArray();

            // Assert
            results.ShouldBe(expectedResults, ignoreOrder: true);
        }

        [DataTestMethod]
        [DataRow("", "")]
        [DataRow(null, "")]
        [DataRow("@", "build|css|js")]
        [DataRow("@(b", "build"), DataRow("@(bu", "build"), DataRow("@(build", "build")]
        public void Can_fetch_itemGroup_options(string input, string expected)
        {
            // Arrange
            var sampleFile = SampleFactory.GetFile("itemGroups.json").FullName;

            // Act
            var results = Intellisense.GetItemGroups(input, sampleFile).Select(x => x.Title).ToArray();
            var expectedResults = expected.Split(new char[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries);

            // Assert
            expectedResults.ShouldBeSubsetOf(results);
        }

        [DataTestMethod]
        [DataRow("@", "@(build)")]
        [DataRow("@(b", "@(build)"), DataRow("@(bu", "@(build)")]
        [DataRow("foo.cs, @(", "foo.cs, @(build)"), DataRow("foo.cs, @(cs", "foo.cs, @(css)")]
        public void Can_complete_itemGroup_command(string input, string expected)
        {
            // Arrange
            var sampleFile = SampleFactory.GetFile("itemGroups.json").FullName;

            // Act
            var results = Intellisense.GetItemGroups(input, sampleFile).First();

            // Assert
            results.FullText.ShouldBe(expected);
        }
    }
}