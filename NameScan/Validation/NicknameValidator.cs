using System.Text.RegularExpressions;

namespace NameScan.Validation;

public sealed partial class NicknameValidator
{
    public NicknameValidationResult Validate(string? input)
    {
        var normalized = (input ?? string.Empty).Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(normalized))
        {
            return NicknameValidationResult.Invalid("Digite um nickname para verificar.");
        }

        if (normalized.Length < 2 || normalized.Length > 30)
        {
            return NicknameValidationResult.Invalid("Use entre 2 e 30 caracteres.");
        }

        if (!AllowedCharacters().IsMatch(normalized))
        {
            return NicknameValidationResult.Invalid("Use apenas letras sem acento, números, ponto, underline ou hífen.");
        }

        return NicknameValidationResult.Valid(normalized);
    }

    [GeneratedRegex("^[a-z0-9._-]+$")]
    private static partial Regex AllowedCharacters();
}
