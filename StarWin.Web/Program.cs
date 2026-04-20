using StarWin.Web;

var builder = StarWinWebHost.CreateBuilder(args);
var app = StarWinWebHost.Build(builder);

await StarWinWebHost.InitializeAsync(app);

app.Run();
