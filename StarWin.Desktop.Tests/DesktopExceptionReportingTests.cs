namespace StarWin.Desktop.Tests;

public sealed class DesktopExceptionReportingTests
{
    [Fact]
    public void Shell_startup_skips_duplicate_report_when_backend_already_reported()
    {
        Assert.False(DesktopExceptionObservation.ShouldReportShellStartupException(
            new DesktopBackendStartupReportedException()));
        Assert.True(DesktopExceptionObservation.ShouldReportShellStartupException(
            new TimeoutException("Unrelated timeout")));
    }

    [Fact]
    public async Task Background_task_failures_are_observed_and_reported()
    {
        var failure = new InvalidOperationException("Update failed");
        Exception? reported = null;

        await DesktopExceptionObservation.ObserveAsync(
            Task.FromException(failure),
            exception =>
            {
                reported = exception;
                return Task.CompletedTask;
            });

        Assert.Same(failure, reported);
    }
}
