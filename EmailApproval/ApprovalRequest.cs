namespace EmailApproval
{
    public class ApprovalRequest
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? RequestedBy { get; set; }
        public string? ApproverEmail { get; set; }
        public string? Status { get; set; }
        public Guid ApprovalToken { get; set; }
    }
}
