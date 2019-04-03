﻿using Acklann.Powerbar.ViewModels;
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
            Shell.Invoke(cwd, command, Switch.None, context, print);
            //System.Console.WriteLine(result);

            // Assert
            result.ToString().ShouldNotBeNullOrEmpty();
            result.ToString().ShouldNotContain("error", Case.Insensitive);
        }

        [DataTestMethod]
        [DataRow("Write-Host")]
        [DataRow("select " + nameof(VSContext.RootNamespace))]
        public void Can_pipe_a_psobject(string command)
        {
            // Arrange
            var results = new StringBuilder();
            var context = CreateContext();
            void print(string msg) { results.AppendLine(msg); }
            string cwd = Path.GetTempPath();

            // Act
            Shell.Invoke(cwd, command, Switch.PipeContext, context, print);
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
        [DataRow("", (Switch.None))]
        [DataRow(null, (Switch.None))]
        [DataRow(@"Models/", (Switch.AddFile))]
        [DataRow(@"Person.cs", (Switch.AddFile))]
        [DataRow("> Write-Host", (Switch.RunCommand))]
        [DataRow("> 'hello' | Write-Host", (Switch.RunCommand))]
        [DataRow("| Write-Host", (Switch.PipeContext | Switch.RunCommand))]
        [DataRow("|> Write-Host", (Switch.RunCommand | Switch.PipeContext))]
        [DataRow(">| Write-Host", (Switch.RunCommand | Switch.PipeContext))]
        [DataRow(">> Write-Host", (Switch.RunCommand | Switch.RunCommandInWindow))]
        [DataRow("|| Write-Host", (Switch.RunCommand | Switch.PipeContext | Switch.RunCommandInWindow))]
        [DataRow("|>> Write-Host", (Switch.RunCommand | Switch.RunCommandInWindow | Switch.PipeContext))]
        [DataRow("||> Write-Host", (Switch.RunCommand | Switch.RunCommandInWindow | Switch.PipeContext))]
        public void Can_extract_switches_from_a_command(string command, Switch expected)
        {
            var options = Shell.ExtractOptions(ref command);

            options.ShouldBe(expected);
            command?.ShouldNotMatch(Shell.SwitchePattern.ToString());
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