using System;
using ProtonDrive.App.Configuration;
using ProtonDrive.Shared.Reporting;
using Sentry;
using Sentry.Extensibility;

namespace ProtonDrive.App.Reporting;

internal sealed class ErrorReporting : IErrorReporting
{
    private readonly SentryOptionsProvider _optionsProvider;

    private IDisposable _errorReportingHub;

    public ErrorReporting(SentryOptionsProvider optionsProvider)
    {
        _optionsProvider = optionsProvider;
        _errorReportingHub = DisabledHub.Instance;
    }

    public bool IsEnabled
    {
        get => SentrySdk.IsEnabled;
        set
        {
            if (value == SentrySdk.IsEnabled)
            {
                return;
            }

            if (value)
            {
                _errorReportingHub = SentrySdk.Init(_optionsProvider.GetOptions());
            }
            else
            {
                _errorReportingHub.Dispose();
            }
        }
    }

    public void CaptureException(Exception ex)
    {
        SentrySdk.CaptureException(ex);
    }

    public void CaptureError(string message)
    {
        SentrySdk.CaptureMessage(message, SentryLevel.Error);
    }
}
