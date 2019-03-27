using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using Shouldly;
using System.Text;

namespace Acklann.Powerbar.MSTest.Tests
{
    [TestClass]
    public class ShellTest
    {
        [TestMethod]
        public void Can_invoke_powershell_command()
        {
            var result = new StringBuilder();
            Shell.Invoke("Get-Verb", new Context(), (msg) => { result.AppendLine(msg); });

            result.ToString().ShouldNotBeNullOrEmpty();
        }

        [TestMethod]
        public void Can_invoke_msbuild_command()
        {
        }

        [TestMethod]
        public void Should_represent_context_as_json()
        {
            var sut = new Context("powershell");
            var result = JObject.Parse(sut);

            result.ShouldNotBeNull();
        }

    }
}