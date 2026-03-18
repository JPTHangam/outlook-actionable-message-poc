namespace EmailApproval
{
    public class ApprovalRequestDto
    {
        public Guid ApprovalToken { get; set; }
        public string? Comments { get; set; }
    }
}
