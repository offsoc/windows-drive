using Microsoft.Web.WebView2.Wpf;

namespace ProtonDrive.App.Windows.Dialogs.HumanVerification;

internal partial class HumanVerificationDialogWindow : IClosableDialog
{
    public HumanVerificationDialogWindow(CoreWebView2CreationProperties creationProperties)
    {
        InitializeComponent();

        // WebView2's CreationProperties must be set before the control initializes its Core WebView2 environment.
        // If you bind CreationProperties to a view-model property, the binding is typically applied after the dialog's
        // components are constructed and often after the WebView2 begins initializing (especially if the Source property is set).
        // That results in the property being ignored, or the environment being in a bad state, and nothing navigates.
        WebView2.CreationProperties = creationProperties;
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        WebView2.Dispose();
    }
}
