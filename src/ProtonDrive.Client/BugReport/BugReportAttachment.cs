namespace ProtonDrive.Client.BugReport;

public sealed record BugReportAttachment(string Name, string FileName, Stream Stream);
