internal sealed class DesktopBackendStartupReportedException : Exception
{
    public DesktopBackendStartupReportedException()
        : base("The desktop backend exited after reporting its startup failure.")
    {
    }
}
