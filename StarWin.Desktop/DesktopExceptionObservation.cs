internal static class DesktopExceptionObservation
{
    public static bool ShouldReportShellStartupException(Exception exception)
        => exception is not DesktopBackendStartupReportedException;

    public static async Task ObserveAsync(
        Task task,
        Func<Exception, Task> onException)
    {
        ArgumentNullException.ThrowIfNull(task);
        ArgumentNullException.ThrowIfNull(onException);

        try
        {
            await task;
        }
        catch (Exception exception)
        {
            await onException(exception);
        }
    }
}
