using Models;

namespace AzureTasks.UI.Views
{
    public interface IMainView
    {
        public string ItemType { get; set; }
        public string Id { get; set; }
        public string ItemName { get; set; }
        public string Area { get; set; }
        public string Iteration { get; set; }
        public string ProjectProcess { get; }
        public string DevepmentSize { get; set; }
        public string TestingSize { get; set; }
        public string OtherTime { get; set; }
        public string TotalTime { get; set; }
        public string Organization { get; set; }
        public string Pat { get; set; }
        public string Project { get; set; }

        public string AssignedTo { get; }
        public string Reviewer { get; }
        public string TestingAssignedTo { get; }
        public string TestingReviewer { get; }

        public string OthersTasksAssignedTo { get; }
        public event EventHandler GenerateTaskEvent;
        public event EventHandler ImportTaskHandler;
        public event EventHandler ClearTasksHandler;
        public event EventHandler AddComponentHandler;
        public event EventHandler SearchItemHandler;


        public void BindingGeneratedTasks(List<AzureTask> azureTasks);
        public void BindingDevTeam(List<TeamMember> teamMembers);
        public void BidingComponents(List<TaskComponent> components);
        void BindingProcessTasks(List<DevelopmentTask> processTasks, List<NormalTask> testingTasks, List<NormalTask> otherTasks);
        void BindingProcess(List<string> process, string selectedValue);
    }
}
