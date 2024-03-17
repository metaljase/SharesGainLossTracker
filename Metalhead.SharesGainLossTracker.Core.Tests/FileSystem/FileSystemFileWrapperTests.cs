using Moq;
using Xunit;

using Metalhead.SharesGainLossTracker.Core.FileSystem;

namespace Metalhead.SharesGainLossTracker.Core.Tests.FileSystem;

public class FileSystemFileWrapperTests
{
    [Fact]
    public void ReadAllLines_ReturnsExpectedLines()
    {
        // Arrange
        var filePath = @"C:\Temp\MockedFile.csv";
        var expectedResult = MockData.CreateSharesInputCsv();
        var mockFileSystemFileWrapper = new Mock<IFileSystemFileWrapper>();
        mockFileSystemFileWrapper
            .Setup(x => x.ReadAllLines(filePath))
            .Returns(expectedResult);
        var fileSystemFileWrapper = mockFileSystemFileWrapper.Object;

        // Act
        var result = fileSystemFileWrapper.ReadAllLines(filePath);

        // Assert
        Assert.Equal(expectedResult, result);
    }

    [Fact]
    public void Exists_ReturnsTrue_WhenFileExists()
    {
        // Arrange
        string filePath = @"C:\Temp\MockedFile.csv";
        var mockFileSystemFileWrapper = new Mock<IFileSystemFileWrapper>();
        mockFileSystemFileWrapper.Setup(x => x.Exists(filePath)).Returns(true);
        var fileSystemFileWrapper = mockFileSystemFileWrapper.Object;

        // Act
        bool result = fileSystemFileWrapper.Exists(filePath);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Exists_ReturnsFalse_WhenFileDoesNotExist()
    {
        // Arrange
        string filePath = @"C:\Temp\MockedFile.csv";
        var mockFileSystemFileWrapper = new Mock<IFileSystemFileWrapper>();
        mockFileSystemFileWrapper.Setup(x => x.Exists(filePath)).Returns(false);
        var fileSystemFileWrapper = mockFileSystemFileWrapper.Object;

        // Act
        bool result = fileSystemFileWrapper.Exists(filePath);

        // Assert
        Assert.False(result);
    }
}
