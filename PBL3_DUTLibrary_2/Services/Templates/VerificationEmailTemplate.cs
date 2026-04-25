// Services/Templates/VerificationEmailTemplate.cs
using System;

namespace PBL3_DUTLibrary_2.Services.Templates
{
    public static class VerificationEmailTemplate
    {
        public static string GetTemplate(string verificationCode)
        {
            return $@"
            <html>
            <head>
                <style>
                    body {{
                        font-family: Arial, sans-serif;
                        line-height: 1.6;
                        color: #333;
                        margin: 0;
                        padding: 0;
                    }}
                    .container {{
                        max-width: 600px;
                        margin: 0 auto;
                        padding: 20px;
                        border: 1px solid #ddd;
                        border-radius: 5px;
                    }}
                    .header {{
                        background-color: #007bff;
                        color: white;
                        padding: 20px;
                        text-align: center;
                        border-radius: 5px 5px 0 0;
                    }}
                    .content {{
                        padding: 20px;
                    }}
                    .code {{
                        font-size: 24px;
                        font-weight: bold;
                        color: #007bff;
                        text-align: center;
                        padding: 20px;
                        background-color: #f8f9fa;
                        border-radius: 5px;
                        margin: 20px 0;
                    }}
                    .footer {{
                        text-align: center;
                        padding: 20px;
                        font-size: 12px;
                        color: #666;
                        border-top: 1px solid #ddd;
                    }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <h1>DUTLibrary</h1>
                    </div>
                    <div class='content'>
                        <h2>Password Recovery Verification Code</h2>
                        <p>Hello,</p>
                        <p>We received a request to reset your password. Please use the following verification code:</p>
                        <div class='code'>
                            {verificationCode}
                        </div>
                        <p>This verification code will expire in 10 minutes.</p>
                        <p>If you did not request a password reset, please ignore this email.</p>
                    </div>
                    <div class='footer'>
                        <p>This is an automated email, please do not reply.</p>
                        <p>&copy; {DateTime.Now.Year} DUTLibrary. All rights reserved.</p>
                    </div>
                </div>
            </body>
            </html>";
        }
    }
}