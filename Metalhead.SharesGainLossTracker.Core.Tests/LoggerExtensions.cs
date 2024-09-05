using Microsoft.Extensions.Logging;
using Moq;

namespace Metalhead.SharesGainLossTracker.Core.Tests;

public static class LoggerExtensions
{
    public static Mock<ILogger<T>> VerifyLogging<T>(this Mock<ILogger<T>> logger, LogLevel expectedLogLevel, string expectedMessage, Times? times = null)
    {
        times ??= Times.Once();

        Func<object, Type, bool> state = (v, t) => v?.ToString()?.StartsWith(expectedMessage) == true;

        logger.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == expectedLogLevel),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => state(v, t)),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)), (Times)times);

        return logger;
    }
}
