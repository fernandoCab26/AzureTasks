namespace Mayansoft.AzureConnect.Models
{
    public class GenerateTasksRequest
    {
        public string ParentId { get; set; }

        public decimal DevelopmentHours { get; set; }
        public string AssignedTo { get; set; } = string.Empty;
        public  string Reviewer { get; set; } = string.Empty;
    }
}
