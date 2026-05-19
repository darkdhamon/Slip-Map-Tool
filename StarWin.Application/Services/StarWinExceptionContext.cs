namespace StarWin.Application.Services;

public sealed record StarWinExceptionContext(
    string HostKind,
    string? Operation = null,
    string? Route = null,
    string? RequestId = null,
    string? TraceIdentifier = null,
    string? AppVersion = null,
    IReadOnlyDictionary<string, string?>? AdditionalData = null);
