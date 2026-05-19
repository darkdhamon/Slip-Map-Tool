using StarWin.Application.Services;
using StarWin.Infrastructure.Services;
using StarWin.Web;

var startupExceptionReporter = new StarWinExceptionReporter();

try
{
    var builder = StarWinWebHost.CreateBuilder(args);
    var app = StarWinWebHost.Build(builder);

    await StarWinWebHost.InitializeAsync(app);
    await app.RunAsync();
}
catch (Exception ex)
{
    await startupExceptionReporter.ReportExceptionAsync(
        ex,
        new StarWinExceptionContext(
            HostKind: "Web",
            Operation: "Standalone web host startup"));
    throw;
}
