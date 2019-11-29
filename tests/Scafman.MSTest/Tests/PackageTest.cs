using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using Shouldly;
using System.IO;

namespace Acklann.Scafman.Tests
{
    [TestClass]
    public class PackageTest
    {
        [DataTestMethod]
        [DataRow("gulp", null)]
        [DataRow("gulp", "4.0.x")]
        [DataRow("gulp", "4.0.2")]
        [DataRow("knockout", "3.5.0")]
        public void Can_add_npm_package(string package, string version)
        {
            // Arrange
            var projectFolder = Path.GetTempPath();
            var packageJson = Path.Combine(projectFolder, "package.json");

            // Act
            var success = NPM.Install(projectFolder, package, version);
            var config = JObject.Parse(File.ReadAllText(packageJson));

            var actualVersion = config.SelectToken($"dependencies.{package}").Value<string>();
            var explictVersion = (string.IsNullOrEmpty(version) ? actualVersion : version);

            // Assert
            success.ShouldBeTrue();
            explictVersion.ShouldBe(actualVersion);
            actualVersion.ShouldNotBeNull();
        }

        [TestMethod]
        public void Can_parse_package_object()
        {
            // Act & Assert
            var text = "";
            var case1 = PackageID.Parse(text);
            case1.Name.ShouldBeNullOrEmpty();
            case1.Version.ShouldBeNullOrEmpty();

            text = "GlobN";
            var case2 = PackageID.Parse(text);
            case2.Name.ShouldBe("GlobN");
            case2.Version.ShouldBeNullOrEmpty();

            text = "gulp-4.0.2";
            var case3 = PackageID.Parse(text);
            case3.Name.ShouldBe("gulp");
            case3.Version.ShouldBe("4.0.2");
        }
    }
}