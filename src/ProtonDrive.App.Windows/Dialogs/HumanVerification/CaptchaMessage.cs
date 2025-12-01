namespace ProtonDrive.App.Windows.Dialogs.HumanVerification;

internal sealed record CaptchaMessage(string Type, string Token, int Height);
