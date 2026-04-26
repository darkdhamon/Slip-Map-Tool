using System.Diagnostics;

var packageRoot = AppContext.BaseDirectory;
var appRoot = Path.Combine(packageRoot, "app");
var targetPath = Path.Combine(appRoot, "StarWin.Desktop.exe");

if (!File.Exists(targetPath))
{
    ShowError(
        "Starforged Atlas could not find the packaged desktop application.",
        $"Expected to find:{Environment.NewLine}{targetPath}");
    return;
}

try
{
    var startInfo = new ProcessStartInfo
    {
        FileName = targetPath,
        WorkingDirectory = appRoot,
        UseShellExecute = true
    };

    Process.Start(startInfo);
}
catch (Exception ex)
{
    ShowError(
        "Starforged Atlas could not be started.",
        ex.GetBaseException().Message);
}

static void ShowError(string message, string detail)
{
    var fullMessage = $"{message}{Environment.NewLine}{Environment.NewLine}{detail}";
    System.Windows.Forms.MessageBox.Show(
        fullMessage,
        "Starforged Atlas",
        System.Windows.Forms.MessageBoxButtons.OK,
        System.Windows.Forms.MessageBoxIcon.Error);
}
