using Mayansoft.AzureConnect.Services;
using AzureTasks.UI.Views;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using Mayansoft.AzureConnect.Models;
using System.IO;
using System.Text.Json;
using System.Windows.Controls;

namespace AzureTasks.UI.Presenter
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
        List<AzureTask> azureTasks = new List<AzureTask>();
        private UserConfiguration userConfiguration;

        string taskName = "{0} {1}: Task {2} {3}";
        public MainViewPresenter(IMainView mainView)
        {
            _mainView = mainView;
            _mainView.GenerateTaskEvent += MainView_GenerateTaskEvent;
            _mainView.ImportTaskHandler += MainView_ImportTaskHandlerAsync;
            _mainView.ClearTasksHandler += MainView_ClearTasksHandler;
            _mainView.AddComponentHandler += MainView_AddComponentHandler;
            _mainView.SearchItemHandler += MainView_SearchItemHandler;

            //_mainView.ComponentsNumber = 1;

            BindingConfigurations();

            _mainView.BidingComponents(taskComponents);
        }

        private void MainView_SearchItemHandler(object? sender, EventArgs e)
        {
            try
            {
                AzureService azureService = new AzureService(userConfiguration);
                WorkItem workItem = azureService.GetWorkItem(_mainView.Id);
                workItem.Fields.TryGetValue("System.WorkItemType", out string type);
                workItem.Fields.TryGetValue("System.Title", out string title);
                workItem.Fields.TryGetValue("System.AreaPath", out string area);
                workItem.Fields.TryGetValue("System.IterationPath", out string iteration);
                _mainView.ItemType = type;
                _mainView.ItemName = title;
                _mainView.Area = area;
                _mainView.Iteration = iteration;
            }
            catch (Exception ex)
            {

                ShowMessageEvent(ex.Message);
            }
        }
        public void ShowMessageEvent(string message)
        {
            ShowMessageHandler?.Invoke(this, message);
        }

        private void MainView_AddComponentHandler(object? sender, EventArgs e)
        {
            InitializingNewItemEventArgs dataGridEvent = (InitializingNewItemEventArgs)e;
            var row = dataGridEvent.NewItem as TaskComponent;
            int? lastId = taskComponents.OrderByDescending(component => component.Id).FirstOrDefault()?.Id;

            if (row != null)
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
                if (string.IsNullOrEmpty(userConfiguration.Pat) ||
                    string.IsNullOrEmpty(userConfiguration.ProjectProcess))
                {
                    throw new Exception($"El archivo configuration.json se encuentra incompleto");
                }
            }
            developmentTasks = userConfiguration.DevTasks;
            testingTask = userConfiguration.TestingTasks;
            otherTasks = userConfiguration.OtherTasks;
            _mainView.Pat = userConfiguration.Pat;
            //_mainView.Project = userConfiguration.Project;
            //_mainView.Organization = userConfiguration.Organization;
            _mainView.BindingDevTeam(userConfiguration.Team);
            _mainView.BindingProcessTasks(developmentTasks, testingTask, otherTasks);
            List<string> proccess = new List<string>()
            {
                {
                    "CMMI"
                },
                {
                    "Agile"
                },
                {
                    "SCRUM"
                }
            };

            _mainView.BindingProcess(proccess, userConfiguration.ProjectProcess);
        }

        private void MainView_ClearTasksHandler(object? sender, EventArgs e)
        {
            azureTasks.Clear();
            taskComponents.Clear();
            _mainView.BidingComponents(taskComponents);
            _mainView.BindingGeneratedTasks(new List<AzureTask>());
            _mainView.BindingProcessTasks(developmentTasks, testingTask, otherTasks);
        }

        private void MainView_ImportTaskHandlerAsync(object? sender, EventArgs e)
        {
            try
            {
                AzureService azureService = new AzureService(userConfiguration);
                azureService.ImportWorkItems(azureTasks.Where(t => !t.IsCreated).ToList(), _mainView.Area, _mainView.Iteration);
                ShowMessageEvent("Las tareas se han importado correctamente");
                _mainView.BindingGeneratedTasks(azureTasks);
            }
            catch (Exception ex)
            {
                ShowMessageEvent(ex.Message);
            }

        }

        private void MainView_GenerateTaskEvent(object? sender, EventArgs e)
        {
            try
            {
                azureTasks.Clear();
                foreach (TaskComponent component in taskComponents.Where(t => t.Size > 0))
                {
                    foreach (DevelopmentTask task in developmentTasks?.Where(t => t.IsSelected && t.Percentaje > 0))
                    {
                        decimal developmentTime = component.Size;
                        decimal originalStimated = (developmentTime * (Convert.ToDecimal(task.Percentaje) / 100));
                        AzureTask azureTask = CreateTask(component, task, originalStimated, "Development");
                        azureTask.AssignedTo = task.Name.Contains("Peer Review") ? _mainView.Reviewer : _mainView.AssignedTo;

                        azureTasks.Add(azureTask);
                    }

                }
                decimal devTime = azureTasks.Where(a => a.Activity == "Development").Sum(t => t.OriginalStimated);
                foreach (NormalTask task in testingTask?.Where(t => t.IsSelected && t.OriginalStimated > 0))
                {
                    TaskComponent component = taskComponents.FirstOrDefault();
                    string activity = userConfiguration.ProjectProcess == "Agile" ? "Testing" : "Test";
                    AzureTask azureTask = CreateTask(component, task, task.OriginalStimated, activity);
                    azureTask.AssignedTo = task.Name.Contains("Peer Review") ? _mainView.TestingReviewer : _mainView.TestingAssignedTo;
                    azureTasks.Add(azureTask);
                }

                foreach (NormalTask task in otherTasks?.Where(t => t.IsSelected && t.OriginalStimated > 0))
                {
                    TaskComponent? component = taskComponents.FirstOrDefault();
                    string activity = userConfiguration.ProjectProcess == "CMMI" && task.Name.ToUpper().Contains("ANÁLISIS") ? "Analysis" : "Development";
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
                ShowMessageEvent(ex.Message);
            }

        }

        private AzureTask CreateTask(TaskComponent component, ProcessTask task, decimal originalStimated, string activity)
        {
            string workItemType = ProcessDictionaries.WorkItemTypes[_mainView.ItemType];
            string componentId = activity == "Development" && component != null ? $"{component.Id.ToString("00")}." : string.Empty;
            AzureTask azureTask = new()
            {
                ParentId = Convert.ToInt32(_mainView.Id),
                ComponentGroup = component != null ? component.Id : 0,
                Name = string.Format(taskName, workItemType, _mainView.Id, $"{componentId}{task.Id.ToString("00")}", task.Name),
                OriginalStimated = originalStimated,
                Activity = activity
            };
            if (userConfiguration.ProjectProcess == "CMMI")
            {
                azureTask.TaskType = "Planned";
                azureTask.Activity = task.Name.ToUpper().Contains("ELABORACIÓN DE CÓDIGO") ? "Development Coding" : activity;
            }
            return azureTask;
        }
    }
}
