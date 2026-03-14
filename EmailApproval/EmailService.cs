using System.Net;
using System.Net.Mail;

namespace EmailApproval
{
    public class EmailService
    {
        public async Task SendApprovalEmail(string email, string title, Guid token)
        {
            var approveUrl = $"https://localhost:7282/api/approval/approve?token={token}";
            var rejectUrl = $"https://localhost:7282/api/approval/reject?token={token}";

            var body = @"
<html>
<head>
<script type=""application/adaptivecard+json"">
{
  ""$schema"": ""http://adaptivecards.io/schemas/adaptive-card.json"",
  ""type"": ""AdaptiveCard"",
  ""version"": ""1.0"",
  ""originator"": ""d8dbc6b8-9a6d-49cb-9b3c-e8c40fe1bc21"",
  ""body"": [
    {
      ""type"": ""TextBlock"",
      ""text"": ""Expense Approval Request"",
      ""size"": ""Large"",
      ""weight"": ""Bolder""
    },
    {
      ""type"": ""FactSet"",
      ""facts"": [
        { ""title"": ""Request ID:"", ""value"": ""REQ-1001"" },
        { ""title"": ""Employee:"", ""value"": ""John Smith"" },
        { ""title"": ""Department:"", ""value"": ""Engineering"" },
        { ""title"": ""Amount:"", ""value"": ""$450"" }
      ]
    },
    {
      ""type"": ""TextBlock"",
      ""text"": ""Expense Details"",
      ""weight"": ""Bolder"",
      ""spacing"": ""Medium""
    },
    {
      ""type"": ""ColumnSet"",
      ""columns"": [
        {
          ""type"": ""Column"",
          ""width"": ""stretch"",
          ""items"": [
            { ""type"": ""TextBlock"", ""text"": ""Item"", ""weight"": ""Bolder"" }
          ]
        },
        {
          ""type"": ""Column"",
          ""width"": ""auto"",
          ""items"": [
            { ""type"": ""TextBlock"", ""text"": ""Amount"", ""weight"": ""Bolder"" }
          ]
        }
      ]
    },
    {
      ""type"": ""ColumnSet"",
      ""columns"": [
        {
          ""type"": ""Column"",
          ""width"": ""stretch"",
          ""items"": [
            { ""type"": ""TextBlock"", ""text"": ""Hotel"" }
          ]
        },
        {
          ""type"": ""Column"",
          ""width"": ""auto"",
          ""items"": [
            { ""type"": ""TextBlock"", ""text"": ""$250"" }
          ]
        }
      ]
    },
    {
      ""type"": ""ColumnSet"",
      ""columns"": [
        {
          ""type"": ""Column"",
          ""width"": ""stretch"",
          ""items"": [
            { ""type"": ""TextBlock"", ""text"": ""Food"" }
          ]
        },
        {
          ""type"": ""Column"",
          ""width"": ""auto"",
          ""items"": [
            { ""type"": ""TextBlock"", ""text"": ""$200"" }
          ]
        }
      ]
    },
    {
      ""type"": ""Input.Text"",
      ""id"": ""comments"",
      ""placeholder"": ""Enter approval comments"",
      ""isMultiline"": true
    },
    {
      ""type"": ""ActionSet"",
      ""actions"": [
        {
          ""type"": ""Action.Http"",
          ""title"": ""Approve"",
          ""method"": ""POST"",
          ""url"": ""https://mariano-visualizable-congruously.ngrok-free.dev/api/approval/approve"",
          ""headers"": [
            { ""name"": ""Content-Type"", ""value"": ""application/json"" }
          ],
          ""body"": ""{ \""ApprovalToken\"": \""GUID_TOKEN_VALUE\"" }""
        },
        {
          ""type"": ""Action.Http"",
          ""title"": ""Reject"",
          ""method"": ""POST"",
          ""url"": ""https://mariano-visualizable-congruously.ngrok-free.dev/api/approval/reject"",
          ""headers"": [
            { ""name"": ""Content-Type"", ""value"": ""application/json"" }
          ],
          ""body"": ""{ \""ApprovalToken\"": \""GUID_TOKEN_VALUE\"" }""
        }
      ]
    }
  ],
  ""padding"": ""None""
}
</script>
</head>
<body>
</body>
</html>";

            var mail = new MailMessage();
            mail.From = new MailAddress("Harishk@iGoldTechSystems.onmicrosoft.com");
            mail.To.Add(email);
            mail.Subject = "Approval Request";
            mail.Body = body;
            mail.IsBodyHtml = true;
            mail.Headers.Add("ActionableMessage", "true"); 
            mail.Headers.Add("X-ActionableMessage-Developer", "true");
            //var smtp = new SmtpClient("smtp.hostinger.com", 587);
            //smtp.Credentials = new NetworkCredential(
            //    "harishk@igoldtechsystems.com",
            //    "ETSdgl@1114"
            //);
            var smtp = new SmtpClient("smtp.office365.com", 587);
            smtp.Credentials = new NetworkCredential(
                "Harishk@iGoldTechSystems.onmicrosoft.com",
                "Moonshine@2026"
            );
            smtp.EnableSsl = true;

            await smtp.SendMailAsync(mail);
        }
    }
}
