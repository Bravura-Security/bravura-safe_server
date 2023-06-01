using Bit.Core.Models.Mail;

namespace Bit.Core.Auth.Models.Mail;

public class PasswordlessSignInModel : BaseMailModel
{
    public string Url { get; set; }
}
