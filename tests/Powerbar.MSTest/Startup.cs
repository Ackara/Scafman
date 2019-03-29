using Acklann.Diffa;
using Acklann.Diffa.Reporters;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[assembly: Reporter(typeof(DiffReporter))]
[assembly: ApprovedFolder("approved-results")]

namespace Acklann.Powerbar
{
    [TestClass]
    public class Startup
    {
    }
}