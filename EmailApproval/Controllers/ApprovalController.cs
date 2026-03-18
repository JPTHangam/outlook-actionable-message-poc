using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace EmailApproval.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ApprovalController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly EmailService _emailService;

        public ApprovalController(IConfiguration config, EmailService emailService)
        {
            _config = config;
            _emailService = emailService;
        }

        private SqlConnection GetConnection()
        {
            return new SqlConnection(_config.GetConnectionString("Default"));
        }

        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] ApprovalRequest request)
        {
            request.ApprovalToken = Guid.NewGuid();

            using var conn = GetConnection();
            await conn.OpenAsync();

            var cmd = new SqlCommand(@"
            INSERT INTO ApprovalRequests
            (Title, Description, RequestedBy, ApproverEmail, Status, ApprovalToken)
            VALUES
            (@Title,@Description,@RequestedBy,@ApproverEmail,'Pending',@ApprovalToken)", conn);

            cmd.Parameters.AddWithValue("@Title", request.Title);
            cmd.Parameters.AddWithValue("@Description", request.Department);
            cmd.Parameters.AddWithValue("@RequestedBy", request.RequestedBy);
            cmd.Parameters.AddWithValue("@ApproverEmail", request.ApproverEmail);
            cmd.Parameters.AddWithValue("@ApprovalToken", request.ApprovalToken);

            await cmd.ExecuteNonQueryAsync();

            await _emailService.SendApprovalEmail1(
                request.ApproverEmail,
                request.Title,
                request.ApprovalToken);

            return Ok("Approval email sent");
        }

        [HttpGet("approve")]
        public async Task<IActionResult> Approve(Guid token)
        {
            using var conn = GetConnection();
            await conn.OpenAsync();

            var cmd = new SqlCommand(@"
            UPDATE ApprovalRequests
            SET Status='Approved',
                ApprovedDate=GETDATE()
            WHERE ApprovalToken=@token", conn);

            cmd.Parameters.AddWithValue("@token", token);

            await cmd.ExecuteNonQueryAsync();

            return Content("Request Approved");
        }

        [HttpGet("reject")]
        public async Task<IActionResult> Reject(Guid token)
        {
            using var conn = GetConnection();
            await conn.OpenAsync();

            var cmd = new SqlCommand(@"
            UPDATE ApprovalRequests
            SET Status='Rejected',
                ApprovedDate=GETDATE()
            WHERE ApprovalToken=@token", conn);

            cmd.Parameters.AddWithValue("@token", token);

            await cmd.ExecuteNonQueryAsync();

            return Content("Request Rejected");
        }

        [HttpPost("approve")]
        public async Task<IActionResult> ApproveCard([FromQuery] Guid token, [FromBody] ApprovalRequestDto request)
        {
            using var conn = GetConnection();
            await conn.OpenAsync();

            var cmd = new SqlCommand(@"
        UPDATE ApprovalRequests
        SET Status       = 'Approved',
            ApprovedDate = GETDATE(),
            Comments     = @comments
        WHERE ApprovalToken = @token", conn);

            cmd.Parameters.AddWithValue("@token", token);
            cmd.Parameters.AddWithValue("@comments", request.Comments ?? "");

            int rows = await cmd.ExecuteNonQueryAsync();
            if (rows == 0) return NotFound();

            var body = await BuildRefreshCardBody(token, "✅ Request Approved", "Good", request.Comments, conn);

            Response.Headers.Add("CARD-ACTION-STATUS", "Approved successfully");
            Response.Headers.Add("CARD-UPDATE-IN-BODY", "true");

            return Ok(new
            {
        schema = "http://adaptivecards.io/schemas/adaptive-card.json",
                type = "AdaptiveCard",
                version = "1.0",
                padding = "None",
                body = body
            });
        }


        [HttpPost("reject")]
        public async Task<IActionResult> RejectCard([FromQuery] Guid token, [FromBody] ApprovalRequestDto request)
        {
            using var conn = GetConnection();
            await conn.OpenAsync();

            var cmd = new SqlCommand(@"
        UPDATE ApprovalRequests
        SET Status       = 'Rejected',
            RejectedDate = GETDATE(),
            Comments     = @comments
        WHERE ApprovalToken = @token", conn);

            cmd.Parameters.AddWithValue("@token", token);
            cmd.Parameters.AddWithValue("@comments", request.Comments ?? "");

            int rows = await cmd.ExecuteNonQueryAsync();
            if (rows == 0) return NotFound();

            var body = await BuildRefreshCardBody(token, "❌ Request Rejected", "Attention", request.Comments, conn);

            Response.Headers.Add("CARD-ACTION-STATUS", "Rejected successfully");
            Response.Headers.Add("CARD-UPDATE-IN-BODY", "true");

            return Ok(new
            {
        schema = "http://adaptivecards.io/schemas/adaptive-card.json",
                type = "AdaptiveCard",
                version = "1.0",
                padding = "None",
                body = body
            });
        }

        [HttpPost("createExpense")]
        public async Task<IActionResult> CreateRequest([FromBody] CreateApprovalRequestDto dto)
        {
            using var conn = GetConnection();
            await conn.OpenAsync();

            var token = Guid.NewGuid();
            var totalAmount = dto.Items.Sum(x => x.Amount);

            // Insert main request
            var cmd = new SqlCommand(@"
        INSERT INTO ApprovalRequests 
            (Title, RequestedBy, ApproverEmail, Department, TotalAmount, ApprovalToken)
        VALUES 
            (@title, @requestedBy, @approverEmail, @department, @totalAmount, @token);
        SELECT SCOPE_IDENTITY();", conn);

            cmd.Parameters.AddWithValue("@title", dto.Title);
            cmd.Parameters.AddWithValue("@requestedBy", dto.RequestedBy);
            cmd.Parameters.AddWithValue("@approverEmail", dto.ApproverEmail);
            cmd.Parameters.AddWithValue("@department", dto.Department);
            cmd.Parameters.AddWithValue("@totalAmount", totalAmount);
            cmd.Parameters.AddWithValue("@token", token);

            var requestId = Convert.ToInt32(await cmd.ExecuteScalarAsync());

            // Insert expense items
            foreach (var item in dto.Items)
            {
                var itemCmd = new SqlCommand(@"
            INSERT INTO ExpenseItems (ApprovalRequestId, ItemName, Amount)
            VALUES (@requestId, @itemName, @amount)", conn);

                itemCmd.Parameters.AddWithValue("@requestId", requestId);
                itemCmd.Parameters.AddWithValue("@itemName", item.ItemName);
                itemCmd.Parameters.AddWithValue("@amount", item.Amount);

                await itemCmd.ExecuteNonQueryAsync();
            }

            await _emailService.SendApprovalEmail(
                    dto.ApproverEmail,
                    dto.Title,
                    token,
                    new ApprovalRequest
                    {
                        Id = requestId,
                        RequestedBy = dto.RequestedBy,
                        Department = dto.Department,
                        TotalAmount = totalAmount,
                        Items = dto.Items.Select(i => new ExpenseItem
                        {
                            ItemName = i.ItemName,
                            Amount = i.Amount
                        }).ToList()
                    }
                ); 

            return Ok(new { Id = requestId, ApprovalToken = token });
        }

        private async Task<object[]> BuildRefreshCardBody(Guid token, string statusText, string statusColor, string? comments, SqlConnection conn)
        {
            var selectCmd = new SqlCommand(@"
        SELECT r.Id, r.RequestedBy, r.Department, r.TotalAmount,
               i.ItemName, i.Amount
        FROM ApprovalRequests r
        LEFT JOIN ExpenseItems i ON i.ApprovalRequestId = r.Id
        WHERE r.ApprovalToken = @token", conn);

            selectCmd.Parameters.AddWithValue("@token", token);

            var items = new List<object>();
            string requestId = "", employee = "", department = "", totalAmount = "";

            using (var reader = await selectCmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    requestId = reader["Id"].ToString()!;
                    employee = reader["RequestedBy"].ToString()!;
                    department = reader["Department"].ToString()!;
                    totalAmount = $"${Convert.ToDecimal(reader["TotalAmount"]):F2}";

                    if (!reader.IsDBNull(reader.GetOrdinal("ItemName")))
                    {
                        items.Add(new
                        {
                            type = "ColumnSet",
                            columns = new object[]
                            {
                        new { type = "Column", width = "stretch", items = new[] { new { type = "TextBlock", text = reader["ItemName"].ToString() } } },
                        new { type = "Column", width = "auto",    items = new[] { new { type = "TextBlock", text = $"${Convert.ToDecimal(reader["Amount"]):F2}" } } }
                            }
                        });
                    }
                }
            }

            var body = new List<object>
    {
        new { type = "TextBlock", text = "Expense Approval Request", size = "Large", weight = "Bolder" },
        new {
            type  = "FactSet",
            facts = new[]
            {
                new { title = "Request ID:",   value = requestId   },
                new { title = "Employee:",     value = employee    },
                new { title = "Department:",   value = department  },
                new { title = "Total Amount:", value = totalAmount }
            }
        },
        new { type = "TextBlock", text = "Expense Details", weight = "Bolder", spacing = "Medium" },
        new {
            type    = "ColumnSet",
            columns = new object[]
            {
                new { type = "Column", width = "stretch", items = new[] { new { type = "TextBlock", text = "Item",   weight = "Bolder" } } },
                new { type = "Column", width = "auto",    items = new[] { new { type = "TextBlock", text = "Amount", weight = "Bolder" } } }
            }
        }
    };

            body.AddRange(items);

            // Status text instead of buttons
            body.Add(new
            {
                type = "TextBlock",
                text = statusText,
                weight = "Bolder",
                size = "Medium",
                color = statusColor,
                spacing = "Medium"
            });

            // Show comments if provided
            if (!string.IsNullOrEmpty(comments))
            {
                body.Add(new
                {
                    type = "FactSet",
                    facts = new[]
                    {
                new { title = "Comments:", value = comments }
            }
                });
            }

            return body.ToArray();
        }
    }
}