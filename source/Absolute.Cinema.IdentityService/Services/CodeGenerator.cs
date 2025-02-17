using Absolute.Cinema.IdentityService.Configuration;
using Absolute.Cinema.IdentityService.Interfaces;
using Microsoft.Extensions.Options;

namespace Absolute.Cinema.IdentityService.Services;

public class CodeGenerator : ICodeGenerator
{
    private readonly int _defaultCodeLength;
    private readonly int _defaultDigitRepeatCount;
    private readonly Random _random = new();

    public CodeGenerator(IOptions<CodeGeneratorOptions> options)
    {
        _defaultCodeLength = options.Value.DefaultCodeLength;
        _defaultDigitRepeatCount = options.Value.DefaultDigitRepeatCount;
    }

    public int GenerateConfirmationCode(int? codeLength = null, int? digitRepeatCount = null)
    {
        var length = codeLength ?? _defaultCodeLength;
        var repeatCount = digitRepeatCount ?? _defaultDigitRepeatCount;
        
        if (repeatCount > length || repeatCount < 0)
            throw new ArgumentException();

        var number = new char[length];
        var availableDigits = new List<char> { '1', '2', '3', '4', '5', '6', '7', '8', '9' };
        
        var repeatedDigit = availableDigits[_random.Next(availableDigits.Count)];
        availableDigits.Remove(repeatedDigit);
        
        for (var i = 0; i < repeatCount; i++)
        {
            number[i] = repeatedDigit;
        }

        for (var i = repeatCount; i < length; i++)
        {
            var nextDigit = availableDigits[_random.Next(availableDigits.Count)];
            availableDigits.Remove(nextDigit);
            number[i] = nextDigit;
        }

        _random.Shuffle(number);
        
        return Convert.ToInt32(new string(number));
    }
}