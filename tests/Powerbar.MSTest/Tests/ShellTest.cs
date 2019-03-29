using Acklann.Powerbar.ViewModels;
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
            var context = CreateContext();
            string cwd = Path.GetTempPath();
            var result = new StringBuilder();
            void print(string msg) { result.AppendLine(msg); }

            // Act
            Shell.Invoke(cwd, command, ShellOptions.None, context, print);
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
            void print(string msg) { results.AppendLine(msg); }
            string cwd = Path.GetTempPath();

            // Act
            Shell.Invoke(cwd, command, ShellOptions.PipeContext, context, print);
            //System.Console.WriteLine(results);

            // Assert
            results.ToString().ShouldNotBeNullOrEmpty();
            results.ToString().ShouldNotContain("error", Case.Insensitive);
        }

        [TestMethod]
        public void Can_cycle_through_command_history()
        {
            // Arrange
            var sample = new string[] { "a", "b", "c" };
            var sut = new CommandPromptViewModel(sample.Length + 1);

            // Act + Assert
            sut.SelectNext();
            sut.SelectPrevious();

            foreach (var command in sample)
            {
                sut.UserInput = command;
                sut.Commit();
            }

            /// Senario (End of the line): cycle through the entire history.
            sut.SelectPrevious();
            sut.UserInput.ShouldBe("c");

            sut.SelectPrevious().ShouldBe("b");

            sut.SelectNext().ShouldBe("c");

            sut.SelectPrevious().ShouldBe("b");
            sut.SelectPrevious().ShouldBe("a");
            sut.SelectPrevious().ShouldBe("a");

            /// Senario (Loopback & Replace): when the array is full start replacing values from the top.
            sut.UserInput = "d";
            sut.Commit(); // [ a, b, c, d]

            sut.SelectNext();
            sut.UserInput.ShouldBe("d");

            sut.SelectPrevious().ShouldBe("c");
            sut.SelectPrevious().ShouldBe("b");
            sut.SelectPrevious().ShouldBe("a");
            sut.SelectPrevious().ShouldBe("a");

            sut.SelectNext().ShouldBe("b");
            sut.SelectNext().ShouldBe("c");
            sut.SelectNext().ShouldBe("d");
        }

        [DataTestMethod]
        [DataRow("", (ShellOptions.None))]
        [DataRow(null, (ShellOptions.None))]
        [DataRow("|", (ShellOptions.PipeContext))]
        [DataRow(">", (ShellOptions.CreateWindow))]
        [DataRow("'hello' | Write-Host", (ShellOptions.None))]
        [DataRow(@"\ Person.cs", (ShellOptions.CreateNewFile))]
        [DataRow(@"/..\Person.cs", (ShellOptions.CreateNewFile))]
        [DataRow("|>", (ShellOptions.PipeContext | ShellOptions.CreateWindow))]
        public void Can_determine_shell_options_from_a_string(string command, ShellOptions expected)
        {
            var options = Shell.GetOptions(ref command);

            options.ShouldBe(expected);
            command?.ShouldNotMatch(Shell.Switches.ToString());
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