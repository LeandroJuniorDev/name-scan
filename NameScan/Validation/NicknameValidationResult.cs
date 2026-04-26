namespace NameScan.Validation;

public sealed record NicknameValidationResult(
    bool IsValid,
    string NormalizedNickname,
    string? ErrorMessage)
{
    public static NicknameValidationResult Valid(string normalizedNickname) =>
        new(true, normalizedNickname, null);

    public static NicknameValidationResult Invalid(string errorMessage) =>
        new(false, string.Empty, errorMessage);
}
