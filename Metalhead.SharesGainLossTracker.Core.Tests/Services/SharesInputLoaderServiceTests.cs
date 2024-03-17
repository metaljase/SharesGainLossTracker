using Moq;
using Xunit;

using Metalhead.SharesGainLossTracker.Core.Services;

namespace Metalhead.SharesGainLossTracker.Core.Tests.Services;

public class SharesInputLoaderServiceTests
{
    private readonly SharesInputLoaderService _sut;
    private readonly Mock<ISharesInputLoader> _mockSharesInputLoader = new();

    public SharesInputLoaderServiceTests()
    {
        _sut = new SharesInputLoaderService(_mockSharesInputLoader.Object);
    }

    [Fact]
    public void LoadSharesInput_ReturnsSharesInput_GivenValidFileFullPath()
    {
        // Arrange
        var shareInputFileFullPath = @"C:\Temp\SharesInputFile.csv";
        var expectedShares = MockData.CreateSharesInput();
        _mockSharesInputLoader.Setup(x => x.CreateSharesInput(shareInputFileFullPath)).Returns(expectedShares);

        // Act
        var actualShares = _sut.LoadSharesInput(shareInputFileFullPath);

        // Assert
        Assert.Equal(expectedShares, actualShares);
    }
}
