namespace StarWin.Application.Services;

public interface IStarWinExceptionReporter
{
    Task ReportExceptionAsync(
        Exception exception,
        StarWinExceptionContext context,
        CancellationToken cancellationToken = default);
}
