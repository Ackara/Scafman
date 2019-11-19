using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using System.IO;
using System.Text;

namespace Acklann.Templata.MSTest.Tests
{
    [TestClass]
    public class ShellTest
    {
        [DataTestMethod]
        [DataRow("Get-Verb")]
        [DataRow("git --version")]
        [DataRow("msbuild /version")]
        [DataRow("Write-Host $env:USERPROFILE")]
        [DataRow("ConvertFrom-Json \"{'name': 'foobar'}\" | Write-Host")]
        [DataRow("ConvertFrom-Json '{''name'': ''foobar''}' | Write-Host")]
        [DataRow("\"{ \"\"name\"\": \"\"foobar\"\" }\" | ConvertFrom-Json")]
        [DataRow("\"{ \"\"path\"\": \"\"C:\\project\\foo.sln\"\" }\" | ConvertFrom-Json")]
        [DataRow("\"{ 'name': '$(([System.Environment]::MachineName))' }\" | ConvertFrom-Json")]
        public void Can_invoke_powershell_command(string command)
        {
            // Arrange
            var context = CreateContext();
            string cwd = Path.GetTempPath();
            var result = new StringBuilder();
            void print(string msg) { result.AppendLine(msg); }

            // Act
            Shell.Invoke(cwd, command, Switch.None, context, print);
            //System.Console.WriteLine(result);

            // Assert
            result.ToString().ShouldNotBeNullOrEmpty();
            result.ToString().ShouldNotContain("error", Case.Insensitive);
        }

        [DataTestMethod]
        [DataRow("", "")]
        [DataRow(null, null)]
        [DataRow("|>Get-It", "|>Get-It")]
        [DataRow("> New-Foo", "> New-Foo")]
        [DataRow("> new-it", "> New-Item")]
        [DataRow("Get-Comm", "Get-Command")]
        [DataRow("ConvertFrom-J", "ConvertFrom-Json")]
        [DataRow("> 'foobar' | Write-H", "> 'foobar' | Write-Host")]
        public void Can_complete_command(string input, string expectedResult)
        {
            var commandList = Shell.GetCommands();

            var result = Shell.CompleteCommand(input, commandList);
            if (input == null) Assert.AreEqual(input, expectedResult);
            result.ShouldBe(expectedResult);
        }

        [TestMethod]
        public void Can_return_available_pwsh_commands()
        {
            var commands = Shell.GetCommands();

            commands.ShouldNotBeEmpty();
            (new string[] { "New-Item", "ConvertFrom-Json" }).ShouldBeSubsetOf(commands);
        }


        private static ProjectContext CreateContext()
        {
            string rootFolder = Path.Combine(Path.GetTempPath(), nameof(Templata));
            string sln = Path.Combine(rootFolder, "example.sln");
            string proj = Path.Combine(rootFolder, "example/example.proj");
            string item = Path.Combine(rootFolder, "example/Class1.cs");
            var selection = new string[] { item };

            return new ProjectContext(sln, proj, item, selection, nameof(Templata), nameof(Acklann), "0.0.1");
        }
    }
}