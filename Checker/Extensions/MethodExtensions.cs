using Checker.Checks.HttpCheck;
using System.Runtime.CompilerServices;

namespace Checker.Extensions
{
    public static class MethodExtensions
    {
        public static async Task<T> RunWithRetries<T>(
            this Func<CancellationToken, Task<T>> function,
            TimeSpan functionTimeout,
            int numberOfRetries,
            TimeSpan retryDelay,
            Func<Exception, bool> shouldRetryException,
            CancellationToken cancellationToken,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            var tryNumber = 0;
            do
            {
                try
                {
                    var cts = CancellationTokenSource.CreateLinkedTokenSource(
                        cancellationToken,
                        new CancellationTokenSource(functionTimeout).Token);

                    var timeoutTask = Task.Delay(functionTimeout);
                    var functionTask = function(cts.Token);
                    var finishedTask = await Task.WhenAny(timeoutTask, functionTask);
                    if (functionTask.IsCompletedSuccessfully)
                    {
                        return functionTask.Result;
                    }

                    cts.Cancel();

                    if (functionTask.IsFaulted && functionTask.Exception != null)
                    {
                        throw functionTask.Exception.Flatten();
                    }

                    if (finishedTask == timeoutTask && !functionTask.IsCompletedSuccessfully)
                    {
                        throw new TimeoutException();
                    }

                    throw new UnknownException();
                }
                catch (Exception exc)
                {
                    if (cancellationToken.IsCancellationRequested ||
                        !shouldRetryException(exc) ||
                        tryNumber > numberOfRetries)
                    {
                        throw;
                    }

                    await Task.Delay(retryDelay);
                }
                tryNumber++;
            } while (true);
        }
    }
}
