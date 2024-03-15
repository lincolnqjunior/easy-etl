using Library.Infra.ColumnActions;
using Moq;

namespace Tests.Infra
{
    public class ColumnActionFactoryTests
    {
        [Fact]
        public void CreateAction_ReturnsDefaultColumnAction_ForUnspecifiedAction()
        {
            // Arrange
            var configMock = new Mock<IColumnAction>();
            configMock.SetupGet(c => c.Action).Returns(ColumnAction.Default);
            configMock.SetupGet(c => c.Name).Returns("TestName");
            configMock.SetupGet(c => c.Position).Returns(1);
            configMock.SetupGet(c => c.IsHeader).Returns(true);
            configMock.SetupGet(c => c.OutputName).Returns("TestOutputName");
            configMock.SetupGet(c => c.OutputType).Returns(typeof(string));

            // Act
            var result = ColumnActionFactory.CreateAction(configMock.Object);

            // Assert
            Assert.IsType<DefaultColumnAction>(result);
        }

        [Theory]
        [InlineData(ColumnAction.Ignore)]
        [InlineData(ColumnAction.Parse)]
        [InlineData(ColumnAction.Replace)]
        [InlineData(ColumnAction.Split)]
        public void CreateAction_ThrowsNotImplementedException_ForSpecifiedActions(ColumnAction action)
        {
            // Arrange
            var configMock = new Mock<IColumnAction>();
            configMock.SetupGet(c => c.Action).Returns(action);

            // Act & Assert
            Assert.Throws<NotImplementedException>(() => ColumnActionFactory.CreateAction(configMock.Object));
        }
    }
}
