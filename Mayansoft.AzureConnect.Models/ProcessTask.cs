namespace Mayansoft.AzureConnect.Models
{
    public class ProcessTask
    {
        public string Key { get; set; }
        public bool IsSelected { get; set; } = true;
        public int Id { get; set; }
        public string Name { get; set; }
        public string Activity { get; set; }
    }
}
