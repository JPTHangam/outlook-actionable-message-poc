namespace EmailApproval
{
    public class ExpenseItem
    {
        public int Id { get; set; }
        public int ApprovalRequestId { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }

    // Full Approval Request with items
    public class ApprovalRequest
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string RequestedBy { get; set; } = string.Empty;
        public string ApproverEmail { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = "Pending";
        public Guid ApprovalToken { get; set; } = Guid.NewGuid();
        public string? Comments { get; set; }
        public DateTime RequestedDate { get; set; } = DateTime.Now;
        public DateTime? ApprovedDate { get; set; }
        public DateTime? RejectedDate { get; set; }

        public List<ExpenseItem> Items { get; set; } = new();
    }

    // DTO for creating request
    public class CreateApprovalRequestDto
    {
        public string Title { get; set; } = string.Empty;
        public string RequestedBy { get; set; } = string.Empty;
        public string ApproverEmail { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public List<ExpenseItemDto> Items { get; set; } = new();
    }

    // DTO for expense item
    public class ExpenseItemDto
    {
        public string ItemName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }
   
}
