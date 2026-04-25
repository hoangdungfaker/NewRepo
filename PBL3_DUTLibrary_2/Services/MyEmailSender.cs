using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Web;
using PBL3_DUTLibrary.Interface;
using PBL3_DUTLibrary_2.Services.Templates;

namespace PBL3_DUTLibrary.Class
{
    public class MyEmailSender : IMyEmailSender
    {
        public async Task SendEmailAsync(string email, string subject, string message)
        {
            var mail = "takasugi130305@gmail.com";
            var pw = "digw zvxe lfjk ekok";

            var client = new SmtpClient("smtp.gmail.com", 587)
            {
                UseDefaultCredentials = false,
                EnableSsl = true,
                Credentials = new NetworkCredential(mail, pw),
                DeliveryMethod = SmtpDeliveryMethod.Network
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(mail, "DUTLibrary"),
                Subject = subject,
                Body = message,
                IsBodyHtml = true,
            };

            mailMessage.To.Add(email);
            await client.SendMailAsync(mailMessage);

            //return client.SendMailAsync(
            //    new MailMessage(from: mail,
            //    to: email,
            //    subject,
            //    message
            //    ));
        }
        public async Task SendVerificationEmailAsync(string email, string verificationCode)
        {
            var subject = "DUTLibrary Password Recovery Code";
            // var message = $"Your Verification Code is: {verificationCode}";
            var message = VerificationEmailTemplate.GetTemplate(verificationCode);
            await SendEmailAsync(email, subject, message);
        }
    }
}