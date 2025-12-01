namespace ProtonDrive.Shared.HumanVerification;

public interface IHumanVerifier
{
    Task<string?> VerifyAsync(string captchaToken, CancellationToken cancellationToken);
}
