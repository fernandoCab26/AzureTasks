using Mayansoft.AzureConnect.Models;
using System.ComponentModel;

namespace Mayansoft.AzureConnect.Services
{
    public class AzureTaskFactory
    {
        protected readonly string taskName = "{0} {1}: Task {2} {3}";
        public AzureTask CreateTask(ProcessTask taskTemplate, ParentContext context, decimal hours, string assignedTo)
        {
            AzureTask azureTask = new AzureTask();
            string workItemType = ProcessDictionaries.WorkItemTypes[context.Type];
            azureTask.ParentId = context.ParentId;
            azureTask.ComponentGroup = 0;
            azureTask.Name = string.Format(taskName, workItemType, context.ParentId, $"01.{taskTemplate.Id:00}", taskTemplate.Name);
            azureTask.OriginalStimated = hours;
            azureTask.Activity = taskTemplate.Activity;
            azureTask.AssignedTo = assignedTo;

            return azureTask;
        }
    }
}
