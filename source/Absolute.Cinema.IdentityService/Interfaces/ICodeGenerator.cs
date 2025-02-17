namespace Absolute.Cinema.IdentityService.Interfaces;

public interface ICodeGenerator
{
    public int GenerateConfirmationCode(int? codeLength = null, int? digitRepeatCount = null);
}