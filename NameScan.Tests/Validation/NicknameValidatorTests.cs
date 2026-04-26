using NameScan.Validation;

namespace NameScan.Tests.Validation;

public sealed class NicknameValidatorTests
{
    private readonly NicknameValidator _validator = new();

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_RejectsEmptyInput(string input)
    {
        var result = _validator.Validate(input);

        Assert.False(result.IsValid);
        Assert.Equal("Digite um nickname para verificar.", result.ErrorMessage);
    }

    [Theory]
    [InlineData("a")]
    [InlineData("abcdefghijklmnopqrstuvwxyzabcde")]
    public void Validate_RejectsInvalidLength(string input)
    {
        var result = _validator.Validate(input);

        Assert.False(result.IsValid);
        Assert.Equal("Use entre 2 e 30 caracteres.", result.ErrorMessage);
    }

    [Theory]
    [InlineData("minha marca")]
    [InlineData("marca!")]
    [InlineData("ação")]
    public void Validate_RejectsUnsupportedCharacters(string input)
    {
        var result = _validator.Validate(input);

        Assert.False(result.IsValid);
        Assert.Equal("Use apenas letras sem acento, números, ponto, underline ou hífen.", result.ErrorMessage);
    }

    [Theory]
    [InlineData(" minhamarca ", "minhamarca")]
    [InlineData("minha.marca", "minha.marca")]
    [InlineData("minha_marca", "minha_marca")]
    [InlineData("minha-marca", "minha-marca")]
    [InlineData("Marca123", "marca123")]
    public void Validate_ReturnsNormalizedNickname(string input, string expected)
    {
        var result = _validator.Validate(input);

        Assert.True(result.IsValid);
        Assert.Equal(expected, result.NormalizedNickname);
        Assert.Null(result.ErrorMessage);
    }
}
