using Acklann.Powerbar.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Acklann.Powerbar.MSTest.Tests
{
    [TestClass]
    public class CommandPromptTest
    {
        [TestMethod]
        public void Can_cycle_through_history()
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
    }
}