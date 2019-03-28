using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using System.IO;
using System.Text;

namespace Acklann.Powerbar.MSTest.Tests
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
            var result = new StringBuilder();
            var context = CreateContext();
            void print(string msg) { result.AppendLine(msg); }

            // Act
            Shell.Invoke(command, ShellOptions.None, context, print);
            //System.Console.WriteLine(result);

            // Assert
            result.ToString().ShouldNotBeNullOrEmpty();
            result.ToString().ShouldNotContain("error", Case.Insensitive);
        }

        [DataTestMethod]
        [DataRow("Write-Host")]
        [DataRow("select " + nameof(VSContext.RootNamespace))]
        public void Can_pipe_object_powershell(string command)
        {
            // Arrange
            var results = new StringBuilder();
            var context = CreateContext();

            // Act
            Shell.Invoke(command, ShellOptions.PipeContext, context, (msg) => { results.AppendLine(msg); });
            //System.Console.WriteLine(results);

            // Assert
            results.ToString().ShouldNotBeNullOrEmpty();
            results.ToString().ShouldNotContain("error", Case.Insensitive);
        }

        private static VSContext CreateContext()
        {
            string rootFolder = Path.Combine(Path.GetTempPath(), nameof(Powerbar));
            string sln = Path.Combine(rootFolder, "example.sln");
            string proj = Path.Combine(rootFolder, "example/example.proj");
            string item = Path.Combine(rootFolder, "example/Class1.cs");
            var selection = new string[] { item };

            return new VSContext(sln, proj, item, selection, nameof(Powerbar), nameof(Acklann), "0.0.1");
        }
    }
}