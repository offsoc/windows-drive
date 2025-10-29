namespace ProtonDrive.Shared.Reporting;

public interface IErrorReporting
{
    bool IsEnabled { get; set; }

    void CaptureException(Exception ex);

    void CaptureError(string message);
}
