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
            cmd.Parameters.AddWithValue("@Description", request.Description);
            cmd.Parameters.AddWithValue("@RequestedBy", request.RequestedBy);
            cmd.Parameters.AddWithValue("@ApproverEmail", request.ApproverEmail);
            cmd.Parameters.AddWithValue("@ApprovalToken", request.ApprovalToken);

            await cmd.ExecuteNonQueryAsync();

            await _emailService.SendApprovalEmail(
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
        public async Task<IActionResult> ApproveCard(ApprovalRequestDto request)
        {
            using var conn = GetConnection();
            await conn.OpenAsync();

                var cmd = new SqlCommand(@"
            UPDATE ApprovalRequests
            SET Status = 'Approved',
                ApprovedDate = GETDATE()
            WHERE ApprovalToken = @token", conn);

            cmd.Parameters.AddWithValue("@token", request.ApprovalToken);

            await cmd.ExecuteNonQueryAsync();

            return Ok(new
            {
                type = "AdaptiveCard",
                version = "1.0",
                body = new[]
                {
                new {
                    type = "TextBlock",
                    text = "✅ Request Approved",
                    weight = "Bolder",
                    size = "Medium"
                    }
                }
            });
        }

        [HttpPost("reject")]
        public async Task<IActionResult> RejectCard([FromBody] ApprovalRequest request)
        {
            using var conn = GetConnection();
            await conn.OpenAsync();

                var cmd = new SqlCommand(@"
            UPDATE ApprovalRequests
            SET Status = 'Rejected',
                ApprovedDate = GETDATE()
            WHERE ApprovalToken = @token", conn);

            cmd.Parameters.AddWithValue("@token", request.ApprovalToken);

            await cmd.ExecuteNonQueryAsync();

            return Ok(new
            {
                type = "AdaptiveCard",
                version = "1.4",
                body = new[]
                {
                    new {
                        type = "TextBlock",
                        text = "❌ Request Rejected",
                        weight = "Bolder",
                        size = "Medium"
                    }
                }
            });
        }
    }
}