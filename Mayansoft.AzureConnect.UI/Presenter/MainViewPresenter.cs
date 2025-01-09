using Mayansoft.AzureConnect.Models;
using Mayansoft.AzureConnect.Services;
using Mayansoft.AzureConnect.UI.Views;
using Microsoft.TeamFoundation.Common;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Mayansoft.AzureConnect.UI.Presenter
{
    public class MainViewPresenter
    {
        private readonly IMainView _mainView;
        public event EventHandler<string> ShowMessageHandler;
        protected List<DevelopmentTask> developmentTasks = new List<DevelopmentTask>();
        protected List<NormalTask> testingTask = new List<NormalTask>();
        protected List<NormalTask> otherTasks = new List<NormalTask>();
        protected List<TaskComponent> taskComponents = new List<TaskComponent>() {
            new TaskComponent()
            {
                Id = 1,
                Size = 0
            }
        };
        protected List<ListItem<string>> userProjects = new List<ListItem<string>>();
        protected List<ListItem<string>> userOrganizations = new List<ListItem<string>>();
        List<AzureTask> azureTasks = new List<AzureTask>();
        private UserConfiguration userConfiguration;
        private AzureService _azureService;

        protected readonly string taskName = "{0} {1}: Task {2} {3}";
        public MainViewPresenter(IMainView mainView)
        {

            _mainView = mainView;
            _mainView.GenerateTaskEvent += MainView_GenerateTaskEvent;
            _mainView.ImportTaskHandler += MainView_ImportTaskHandlerAsync;
            _mainView.ClearTasksHandler += MainView_ClearTasksHandler;
            _mainView.AddComponentHandler += MainView_AddComponentHandler;
            _mainView.SearchItemHandler += MainView_SearchItemHandler;
            _mainView.OnchangeOrganization += MainView_OnchangeOrganization;
            _mainView.OnchangeProject += MainView_OnchangeProject;
            _mainView.IsLoading = true;
            _mainView.BidingComponents(taskComponents);
            BindingConfigurations();
            _azureService = new AzureService(userConfiguration);
            BindingOrganization();
            _mainView.IsLoading = false;
        }

        private async void MainView_OnchangeProject(object sender, EventArgs e)
        {
            if (!_mainView.Project.IsNullOrEmpty())
            {
                _mainView.IsLoading = true;
                await BindingProcess(_mainView.Project);
                await BindingTeamMembers(_mainView.Project);
                _mainView.IsLoading = false;
            }
        }

        private async void MainView_OnchangeOrganization(object sender, EventArgs e)
        {
            ResetForm();
            userConfiguration.Organizations = userConfiguration.Organizations.Select(o => new Organization() { Name = o.Name, IsDefault = false, Value = o.Value }).ToList();
            foreach (var item in userConfiguration.Organizations)
            {
                if (item.Value == _mainView.Organization)
                {
                    item.IsDefault = true;
                }
            }

            _azureService = new AzureService(userConfiguration);

            userProjects = new List<ListItem<string>>();
            (await _azureService.GetTeamProjectReferences()).ForEach(p => userProjects.Add(new ListItem<string>() { Name = p.Name, Value = p.Name }));
            _mainView.BindingProjects(userProjects, userProjects.FirstOrDefault().Value);
        }

        private async void BindingOrganization()
        {
            string organization = userConfiguration.Organizations.FirstOrDefault(x => x.IsDefault).Name;
            _mainView.BindingOrganization(userConfiguration.Organizations, organization);
            await BindingUserProjects();
        }
        private async Task BindingUserProjects()
        {

            IEnumerable<TeamProjectReference> project = await _azureService.GetTeamProjectReferences();
            project.ForEach(p => userProjects.Add(new ListItem<string>() { Name = p.Name, Value = p.Name }));
            var userProject = userProjects.FirstOrDefault().Value;
            _mainView.BindingProjects(userProjects, userProjects.FirstOrDefault().Value);
            await BindingTeamMembers(project.FirstOrDefault().Id.ToString());
        }

        private async Task BindingTeamMembers(string projectId)
        {
            WebApiTeam team = (await _azureService.GetTeam(projectId))?.FirstOrDefault();
            if (team != null)
            {
                IEnumerable<Microsoft.VisualStudio.Services.WebApi.IdentityRef> teamMembers = await _azureService.GetTeamMembers(projectId, team.Id.ToString());
                List<TeamMember> projectTeam = new List<TeamMember>();
                teamMembers.ForEach(t => projectTeam.Add(new TeamMember() { User = $"{t.DisplayName} <{t.UniqueName}>" }));
                _mainView.BindingDevTeam(projectTeam);

            };
        }

        private async Task BindingProcess(string projectName)
        {


            TeamProject projectDetails = _azureService.GetProjectDetails(projectName);
            var template = projectDetails.Capabilities[TeamProjectCapabilitiesConstants.ProcessTemplateCapabilityName];
            var processId = template[TeamProjectCapabilitiesConstants.ProcessTemplateCapabilityTemplateTypeIdAttributeName];
            Microsoft.TeamFoundation.WorkItemTracking.Process.WebApi.Models.ProcessInfo process = await _azureService.GetProcessById(new Guid(processId));
            if (process.ParentProcessTypeId != new Guid())
            {
                Microsoft.TeamFoundation.WorkItemTracking.Process.WebApi.Models.ProcessInfo parenProcess = await _azureService.GetProcessById(process.ParentProcessTypeId);
                _mainView.ProjectProcess = parenProcess.Name;
            }
            else
            {
                _mainView.ProjectProcess = process.Name;
            }
        }
        private async void MainView_SearchItemHandler(object sender, EventArgs e)
        {
            try
            {
                _mainView.IsLoading = true;
                WorkItem workItem = await _azureService.GetWorkItem(_mainView.Id, _mainView.Project);
                //List<int> childIds = new List<int>();
                //foreach (WorkItemRelation relation in workItem.Relations)
                //{

                //    //get the child links
                //    if (relation.Rel == "System.LinkTypes.Hierarchy-Forward")
                //    {
                //        var lastIndex = relation.Url.LastIndexOf("/");
                //        var itemId = relation.Url.Substring(lastIndex + 1);
                //        childIds.Add(Convert.ToInt32(itemId));
                //    };
                //}
                //IEnumerable<WorkItem> childs = await _azureService.GetWorkItems(childIds);

                workItem.Fields.TryGetValue("System.WorkItemType", out string type);
                workItem.Fields.TryGetValue("System.Title", out string title);
                workItem.Fields.TryGetValue("System.AreaPath", out string area);
                workItem.Fields.TryGetValue("System.IterationPath", out string iteration);
                _mainView.ItemType = type;
                _mainView.ItemName = title;
                _mainView.Area = area;
                _mainView.Iteration = iteration;
                //azureTasks.Clear();
                //foreach (WorkItem child in childs)
                //{
                //    child.Fields.TryGetValue("System.WorkItemType", out string childType);
                //    child.Fields.TryGetValue("System.Title", out string childTitle);
                //    child.Fields.TryGetValue("System.AreaPath", out string childArea);
                //    child.Fields.TryGetValue("System.IterationPath", out string childIteration);
                //    child.Fields.TryGetValue("Microsoft.VSTS.Scheduling.OriginalEstimate", out double childStimated);
                //    child.Fields.TryGetValue("System.AssignedTo", out Microsoft.VisualStudio.Services.WebApi.IdentityRef assignedTo);
                //    child.Fields.TryGetValue("System.TaskType", out string taskType);
                //    child.Fields.TryGetValue("Microsoft.VSTS.Common.Discipline", out string discipline);
                //    child.Fields.TryGetValue("Microsoft.VSTS.Common.Activity", out string activity);
                //    AzureTask azureTask = new AzureTask()
                //    {
                //        ParentId = workItem.Id.Value,
                //        ComponentGroup =0,
                //        OriginalStimated = decimal.Parse(childStimated.ToString()),
                //        Name = childTitle,
                //        Activity = string.IsNullOrEmpty(discipline) ? activity: discipline,
                //        AssignedTo = $"{assignedTo.DisplayName} <{assignedTo.UniqueName}>",
                //        IsCreated = true,
                //        TaskType = childType

                //    };
                //    azureTasks.Add(azureTask);
                //}

                //_mainView.BindingGeneratedTasks(azureTasks);
            }
            catch (Exception ex)
            {

                ShowMessageEvent($"{ex.Message}");
            }
            finally
            {
                _mainView.IsLoading = false;
            }
        }
        public void ShowMessageEvent(string message)
        {
            ShowMessageHandler?.Invoke(this, message);
        }

        private void MainView_AddComponentHandler(object sender, EventArgs e)
        {
            InitializingNewItemEventArgs dataGridEvent = (InitializingNewItemEventArgs)e;
            int? lastId = taskComponents.OrderByDescending(component => component.Id).FirstOrDefault()?.Id;

            if (dataGridEvent.NewItem is TaskComponent row)
            {
                row.Id = lastId.HasValue ? lastId.Value + 1 : 1;
            }
        }

        private void BindingConfigurations()
        {
            userConfiguration = new UserConfiguration();

            using (StreamReader r = new StreamReader("configuration.json"))
            {
                string json = r.ReadToEnd();
                userConfiguration = JsonSerializer.Deserialize<UserConfiguration>(json);
                if (!userConfiguration.Organizations.Any())
                {
                    throw new Exception($"El archivo configuration.json se encuentra incompleto");
                }
            }
            developmentTasks = userConfiguration.DevTasks;
            testingTask = userConfiguration.TestingTasks;
            otherTasks = userConfiguration.OtherTasks;
            _mainView.BindingProcessTasks(developmentTasks, testingTask, otherTasks);
        }

        private void MainView_ClearTasksHandler(object sender, EventArgs e)
        {
            ResetForm();
        }

        private void ResetForm()
        {
            azureTasks.Clear();
            taskComponents.Clear();
            _mainView.BidingComponents(taskComponents);
            _mainView.BindingGeneratedTasks(new List<AzureTask>());
            _mainView.BindingProcessTasks(developmentTasks, testingTask, otherTasks);
        }

        private async void MainView_ImportTaskHandlerAsync(object sender, EventArgs e)
        {
            _mainView.IsLoading = true;
            try
            {
                await _azureService.ImportWorkItems(azureTasks.Where(t => !t.IsCreated).ToList(),_mainView.ProjectProcess,_mainView.Project, _mainView.Area, _mainView.Iteration);
                ShowMessageEvent("Las tareas se han importado correctamente");
                _mainView.BindingGeneratedTasks(azureTasks);
            }
            catch (Exception ex)
            {
                ShowMessageEvent($"{ex.Message} {ex.InnerException.Message}");
            }
            finally
            {
                _mainView.IsLoading = false;
            }

        }

        private void MainView_GenerateTaskEvent(object sender, EventArgs e)
        {
            try
            {
                azureTasks.Clear();
                foreach (TaskComponent component in taskComponents.Where(t => t.Size > 0))
                {
                    foreach (DevelopmentTask task in developmentTasks?.Where(t => t.IsSelected && t.Percentaje > 0))
                    {
                        decimal developmentTime = component.Size;
                        decimal originalStimated = developmentTime * (Convert.ToDecimal(task.Percentaje) / 100);
                        AzureTask azureTask = CreateTask(component, task, originalStimated, "Development");
                        azureTask.AssignedTo = task.Name.Contains("Peer Review") ? _mainView.Reviewer : _mainView.AssignedTo;

                        azureTasks.Add(azureTask);
                    }

                }
                decimal devTime = azureTasks.Where(a => a.Activity == "Development").Sum(t => t.OriginalStimated);
                foreach (NormalTask task in testingTask?.Where(t => t.IsSelected && t.OriginalStimated > 0))
                {
                    TaskComponent component = taskComponents.FirstOrDefault();
                    string activity = _mainView.ProjectProcess == "Agile" ? "Testing" : "Test";
                    AzureTask azureTask = CreateTask(component, task, task.OriginalStimated, activity);
                    azureTask.AssignedTo = task.Name.Contains("Peer Review") ? _mainView.TestingReviewer : _mainView.TestingAssignedTo;
                    azureTasks.Add(azureTask);
                }

                foreach (NormalTask task in otherTasks?.Where(t => t.IsSelected && t.OriginalStimated > 0))
                {
                    TaskComponent component = taskComponents.FirstOrDefault();
                    string activity = _mainView.ProjectProcess == "CMMI" && task.Name.ToUpper().Contains("ANÁLISIS") ? "Analysis" : "Development";
                    AzureTask azureTask = CreateTask(component, task, task.OriginalStimated, activity);
                    azureTask.AssignedTo = _mainView.OthersTasksAssignedTo;
                    azureTasks.Add(azureTask);
                }

                decimal testTime = azureTasks.Where(a => a.Activity == "Testing").Sum(t => t.OriginalStimated);
                decimal otherTime = otherTasks.Where(v => v.IsSelected).Sum(t => t.OriginalStimated);

                azureTasks = azureTasks.OrderBy(t => t.Name).ToList();

                _mainView.DevepmentSize = $"Tiempo Dev: {devTime} hrs";

                _mainView.TestingSize = $"Tiempo Test: {testTime} hrs";

                _mainView.OtherTime = $"Tiempo otras: {otherTime} hrs";
                _mainView.TotalTime = $"Tiemp total:{devTime + testTime + otherTime} hrs";

                _mainView.BindingGeneratedTasks(azureTasks);
            }
            catch (Exception ex)
            {
                ShowMessageEvent($"{ex.Message} {ex.InnerException.Message}");
            }

        }

        private AzureTask CreateTask(TaskComponent component, ProcessTask task, decimal originalStimated, string activity)
        {
            string workItemType = ProcessDictionaries.WorkItemTypes[_mainView.ItemType];
            string componentId = activity == "Development" && component != null ? $"{component.Id:00}." : string.Empty;
            AzureTask azureTask = new AzureTask()
            {
                ParentId = Convert.ToInt32(_mainView.Id),
                ComponentGroup = component != null ? component.Id : 0,
                Name = string.Format(taskName, workItemType, _mainView.Id, $"{componentId}{task.Id:00}", task.Name),
                OriginalStimated = originalStimated,
                Activity = activity
            };
            if (_mainView.ProjectProcess == "CMMI")
            {
                azureTask.TaskType = "Planned";
                azureTask.Activity = task.Name.ToUpper().Contains("ELABORACIÓN DE CÓDIGO") ? "Development Coding" : activity;
            }
            return azureTask;
        }
    }
}
