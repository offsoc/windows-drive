namespace ProtonDrive.App.Authentication;

[Flags]
public enum MultiFactorAuthenticationMethods
{
    None = 0,
    Totp = Client.Authentication.MultiFactorAuthenticationMethods.Totp,
    Fido2 = Client.Authentication.MultiFactorAuthenticationMethods.Fido2,
}
