namespace ProtonDrive.App.SystemIntegration;

public interface INumberSuffixedNameGenerator
{
    IEnumerable<string> GenerateNames(string initialName, NameType type, int maxLength = 255);
}
