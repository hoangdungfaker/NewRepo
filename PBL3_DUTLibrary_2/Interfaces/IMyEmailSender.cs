using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PBL3_DUTLibrary.Interface
{
    public interface IMyEmailSender
    {
        Task SendEmailAsync(string email, string subject, string message);
        Task SendVerificationEmailAsync(string email, string verificationCode);
    }
}
