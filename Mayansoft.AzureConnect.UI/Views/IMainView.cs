using Mayansoft.AzureConnect.Models;
using System;
using System.Collections.Generic;

namespace Mayansoft.AzureConnect.UI.Views
{
    public interface IMainView
    {
       string ItemType { get; set; }
       string Id { get; set; }
       string ItemName { get; set; }
       string Area { get; set; }
       string Iteration { get; set; }
       string ProjectProcess { get; }
       string DevepmentSize { get; set; }
       string TestingSize { get; set; }
       string OtherTime { get; set; }
       string TotalTime { get; set; }
       string Organization { get; set; }
       string Pat { get; set; }
       string Project { get; set; }

       string AssignedTo { get; }
       string Reviewer { get; }
       string TestingAssignedTo { get; }
       string TestingReviewer { get; }

       string OthersTasksAssignedTo { get; }
       event EventHandler GenerateTaskEvent;
       event EventHandler ImportTaskHandler;
       event EventHandler ClearTasksHandler;
       event EventHandler AddComponentHandler;
       event EventHandler SearchItemHandler;


        void BindingGeneratedTasks(List<AzureTask> azureTasks);
        void BindingDevTeam(List<TeamMember> teamMembers);
        void BidingComponents(List<TaskComponent> components);
        void BindingProcessTasks(List<DevelopmentTask> processTasks, List<NormalTask> testingTasks, List<NormalTask> otherTasks);
        void BindingProcess(List<string> process, string selectedValue);
    }
}
