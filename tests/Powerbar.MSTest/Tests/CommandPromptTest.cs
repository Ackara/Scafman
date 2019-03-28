using Acklann.Powerbar.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Acklann.Powerbar.MSTest.Tests
{
    [TestClass]
    public class CommandPromptTest
    {
        [TestMethod]
        public void Can_iterate_history()
        {
            // Arrange
            var sample = new string[] { "a", "b", "c" };
            var sut = new CommandPromptViewModel(sample.Length);

            // Act + Assert
            foreach (var command in sample)
            {
                sut.UserInput = command;
                sut.Commit();
            }

            /// Senario (End of the line): cycle through the entire history.
            sut.SelectPrevious();
            sut.UserInput.ShouldBe("c");

            sut.SelectPrevious();
            sut.UserInput.ShouldBe("b");

            sut.SelectNext();
            sut.UserInput.ShouldBe("c");

            sut.SelectPrevious();
            sut.UserInput.ShouldBe("b");

            sut.SelectPrevious();
            sut.UserInput.ShouldBe("a");

            sut.SelectPrevious();
            sut.UserInput.ShouldBe("a");

            /// Senario (Loopback & Replace): when the array is full start replacing values from the top.
            sut.UserInput = "d";
            sut.Commit(); // [ d, b, c]

            sut.SelectNext();
            sut.UserInput.ShouldBe("d");

            sut.SelectPrevious();
            sut.UserInput.ShouldBe("c");

            sut.SelectPrevious();
            sut.UserInput.ShouldBe("b");

            sut.SelectPrevious();
            sut.UserInput.ShouldBe("b");

            sut.SelectNext();
            sut.UserInput.ShouldBe("c");

            sut.SelectNext();
            sut.UserInput.ShouldBe("d");

            sut.SelectNext();
            sut.UserInput.ShouldBe("d");
        }
    }
}