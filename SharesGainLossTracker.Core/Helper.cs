using System;
using System.Linq;
using System.Threading.Tasks;

namespace Metalhead.Helpers
{
    public static class Helper
    {
        public static async Task ExponentialRetryAsync(int maximumRetries, Func<Task> action, Func<Exception, bool> isRetryableException)
        {
            await ExponentialRetryAsync(
                maximumRetries,
                action,
                isRetryableException,
                retryDelay =>
                {
                    return retryDelay switch
                    {
                        1 => 0,
                        2 => 1000,
                        3 => 5000,
                        4 => 10000,
                        _ => 30000,
                    };
                });
        }

        public static async Task ExponentialRetryAsync(
            int maximumRetries,
            Func<Task> action,
            Func<Exception, bool> isRetryableException,
            Func<int, int> retryDelay)
        {
            var done = false;
            var attempts = 0;

            while (!done)
            {
                attempts++;
                try
                {
                    await action().ConfigureAwait(false);
                    done = true;
                }
                catch (Exception ex)
                {
                    if (!isRetryableException(ex))
                    {
                        // If an AggregateException, and AggregateException isn't retryable, check if all the inner exceptions are retryable.
                        if (ex is AggregateException aex)
                        {
                            if (aex.Flatten().InnerExceptions.Any(e => !isRetryableException(e)))
                            {
                                throw;
                            }
                        }
                        else
                        {
                            throw;
                        }
                    }

                    if (attempts > maximumRetries)
                    {
                        throw;
                    }

                    // Back-off and retry later.
                    await Task.Delay(retryDelay(attempts));
                }
            }
        }

        public static void ExponentialRetry(int maximumRetries, Action action, Func<Exception, bool> isRetryableException)
        {
            ExponentialRetry(
                maximumRetries,
                action,
                isRetryableException,
                retryDelay =>
                {
                    return retryDelay switch
                    {
                        1 => 0,
                        2 => 1000,
                        3 => 5000,
                        4 => 10000,
                        _ => 30000,
                    };
                });
        }

        public static void ExponentialRetry(
            int maximumRetries,
            Action action,
            Func<Exception, bool> isRetryableException,
            Func<int, int> retryDelay)
        {
            var done = false;
            var attempts = 0;

            while (!done)
            {
                attempts++;
                try
                {
                    action();
                    done = true;
                }
                catch (Exception ex)
                {
                    if (!isRetryableException(ex))
                    {
                        // If an AggregateException, and AggregateException isn't retryable, check if all the inner exceptions are retryable.
                        if (ex is AggregateException aex)
                        {
                            if (aex.Flatten().InnerExceptions.Any(e => !isRetryableException(e)))
                            {
                                throw;
                            }
                        }
                        else
                        {
                            throw;
                        }
                    }

                    if (attempts >= maximumRetries)
                    {
                        throw;
                    }

                    // Back-off and retry later.
                    System.Threading.Thread.Sleep(retryDelay(attempts));
                }
            }
        }
    }
}
