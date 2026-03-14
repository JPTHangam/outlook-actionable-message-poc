using System.Net;
using System.Net.Mail;
using System.Text.Json;

namespace EmailApproval
{
    public class EmailService
    {
        public async Task SendApprovalEmail(string email, string title, Guid token)
        {
            var card = new
            {
                schema = "http://adaptivecards.io/schemas/adaptive-card.json",
                type = "AdaptiveCard",
                version = "1.0",
                originator = "d8dbc6b8-9a6d-49cb-9b3c-e8c40fe1bc21",
                body = new object[]
                        {
                        new { type = "TextBlock", text = "Expense Approval Request", size = "Large", weight = "Bolder" },
                        new {
                            type = "FactSet",
                            facts = new[]
                            {
                                new { title = "Request ID:", value = "REQ-1001" },
                                new { title = "Employee:", value = "John Smith" },
                                new { title = "Department:", value = "Engineering" },
                                new { title = "Amount:", value = "$450" }
                            }
                        },
                        new { type = "TextBlock", text = "Expense Details", weight = "Bolder", spacing = "Medium" },
                        new {
                            type = "ColumnSet",
                            columns = new[]
                            {
                                new { type = "Column", width = "stretch", items = new[] { new { type = "TextBlock", text = "Item", weight = "Bolder" } } },
                                new { type = "Column", width = "auto",    items = new[] { new { type = "TextBlock", text = "Amount", weight = "Bolder" } } }
                            }
                        },
                        new {
                            type = "ColumnSet",
                            columns = new[]
                            {
                                new { type = "Column", width = "stretch", items = new[] { new { type = "TextBlock", text = "Hotel" } } },
                                new { type = "Column", width = "auto",    items = new[] { new { type = "TextBlock", text = "$250" } } }
                            }
                        },
                        new {
                            type = "ColumnSet",
                            columns = new[]
                            {
                                new { type = "Column", width = "stretch", items = new[] { new { type = "TextBlock", text = "Food" } } },
                                new { type = "Column", width = "auto",    items = new[] { new { type = "TextBlock", text = "$200" } } }
                            }
                        },
                        new { type = "Input.Text", id = "comments", placeholder = "Enter approval comments", isMultiline = true },
                        new {
                            type = "ActionSet",
                            actions = new object[]
                            {
                                new {
                                    type = "Action.Http",
                                    title = "Approve",
                                    method = "POST",
                                    url = $"https://mariano-visualizable-congruously.ngrok-free.dev/api/approval/approve?token={token}",
                                    headers = new[] { new { name = "Content-Type", value = "application/json" } },
                                    body = $"{{ \"ApprovalToken\": \"{token}\" }}"
                                },
                                new {
                                    type = "Action.Http",
                                    title = "Reject",
                                    method = "POST",
                                    url = $"https://mariano-visualizable-congruously.ngrok-free.dev/api/approval/reject?token={token}",
                                    headers = new[] { new { name = "Content-Type", value = "application/json" } },
                                    body = $"{{ \"ApprovalToken\": \"{token}\" }}"
                                }
                        }
                    }
                },
                padding = "None"
            };

            var cardJson = JsonSerializer.Serialize(card, new JsonSerializerOptions { PropertyNamingPolicy = null });

            var body = $@"
            <html>
            <head>
            <script type=""application/adaptivecard+json"">
            {cardJson}
            </script>
            </head>
            <body></body>
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
