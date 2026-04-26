namespace NameScan.Features.Checks;

public sealed record PlatformCheckResult(
    string Platform,
    string Url,
    CheckStatus Status,
    ConfidenceLevel Confidence,
    string? Note);
