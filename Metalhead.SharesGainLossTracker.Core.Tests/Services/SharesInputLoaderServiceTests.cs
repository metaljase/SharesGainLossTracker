using Microsoft.Extensions.Logging;

using Metalhead.SharesGainLossTracker.Core.Models;
using Metalhead.SharesGainLossTracker.Core.Services;
using Moq;
using Xunit;

namespace Metalhead.SharesGainLossTracker.Core.Tests.Services;

public class SharesInputLoaderServiceTests
{
    private readonly SharesInputLoaderService _sut;
    private readonly Mock<ILogger<SharesInputLoaderService>> _mockLogger = new();
    private readonly Mock<IProgress<ProgressLog>> _mockProgress = new();
    private readonly Mock<ISharesInputLoader> _mockSharesInputLoader = new();

    public SharesInputLoaderServiceTests()
    {
        _mockProgress = new Mock<IProgress<ProgressLog>>();

        _sut = new SharesInputLoaderService(_mockLogger.Object, _mockProgress.Object, _mockSharesInputLoader.Object);
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
