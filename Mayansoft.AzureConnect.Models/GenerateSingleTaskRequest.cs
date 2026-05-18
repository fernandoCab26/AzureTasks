namespace Mayansoft.AzureConnect.Models
{
    public class GenerateSingleTaskRequest
    {
        public string ParentId { get; set; }

        public OperationalTaskType TaskType { get; set; }

        public decimal OriginalEstimated { get; set; }
        public string AssignedTo { get; set; } = string.Empty;
    }
}
