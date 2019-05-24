using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Acklann.Templata.Tests
{
    [TestClass]
    public class IntellisenseTest
    {
        [DataTestMethod]
        //[DataRow("", "")]
        //[DataRow(null, "")]
        //[DataRow("@", "build|css|js")]
        [DataRow("@(b", "build"), DataRow("@(bu", "build"), DataRow("@(build", "build")]
        public void Can_fetch_itemGroup_options(string input, string expected)
        {
            // Arrange
            var sampleFile = SampleFactory.GetFile("itemGroups.json").FullName;

            // Act
            var results = Intellisense.GetOptions(input, sampleFile).Select(x => x.Title).ToArray();
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
            var results = Intellisense.GetOptions(input, sampleFile).First();

            // Assert
            results.FullText.ShouldBe(expected);
        }

    }
}