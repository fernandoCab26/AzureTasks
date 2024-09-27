using AzureServices;
using AzureTasks.UI.Views;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using Models;
using System.IO;
using System.Text.Json;
using System.Windows.Controls;

namespace AzureTasks.UI.Presenter
{
    public class MainViewPresenter
    {
        private readonly IMainView _mainView;
        public event EventHandler<string> ShowMessageHandler;
        protected List<DevelopmentTask> developmentTasks = new List<DevelopmentTask>()
        {
            {
               new DevelopmentTask
               {
                   Index = 1,
                   Name = "Elaboración de código",
                   Percentaje = 80
               }

            },
            {
               new DevelopmentTask
               {
                   Index = 3,
                   Name = "Review",
                   Percentaje = 5
               }

            },
                        {
               new DevelopmentTask
               {
                   Index = 4,
                   Name = "Peer Review",
                   Percentaje = 5
               }

            },
                                    {
               new DevelopmentTask
               {
                   Index = 5,
                   Name = "Pruebas funcionales  ISW",
                   Percentaje = 10
               }

            },
        };
        protected List<TaskComponent> taskComponents = new List<TaskComponent>() {
            new TaskComponent()
            {
                Id = 1,
                Size = 0
            },
            new TaskComponent()
            {
                Id = 2,
                Size = 0
            },
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
            _mainView.BindingProcessTasks(developmentTasks);
            _mainView.BidingComponents(taskComponents);
            //_mainView.ComponentsNumber = 1;

            BindingConfigurations();
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
                    string.IsNullOrEmpty(userConfiguration.Project) ||
                    string.IsNullOrEmpty(userConfiguration.Organization) ||
                    string.IsNullOrEmpty(userConfiguration.ProjectProcess))
                {
                    throw new Exception($"El archivo configuration.json se encuentra incompleto");
                }
            }

            _mainView.ProjectProcess = userConfiguration.ProjectProcess;
            _mainView.Pat = userConfiguration.Pat;
            _mainView.Project = userConfiguration.Project;
            _mainView.Organization = userConfiguration.Organization;
            _mainView.BindingDevTeam(userConfiguration.Team);
        }

        private void MainView_ClearTasksHandler(object? sender, EventArgs e)
        {
            azureTasks.Clear();
            taskComponents.Clear();
            _mainView.BidingComponents(taskComponents);
            _mainView.BindingGeneratedTasks(new List<AzureTask>());
            _mainView.BindingProcessTasks(developmentTasks);
        }

        private void MainView_ImportTaskHandlerAsync(object? sender, EventArgs e)
        {
            try
            {
                AzureService azureService = new AzureService(userConfiguration);
                azureService.ImportWorkItems(azureTasks, _mainView.Area, _mainView.Iteration);
                ShowMessageEvent("Las tareas se han importado correctamente");
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
                foreach (TaskComponent component in taskComponents)
                {
                    foreach (DevelopmentTask task in developmentTasks.Where(t => t.IsSelected))
                    {
                        decimal developmentTime = component.Size;
                        decimal originalStimated = (developmentTime * (Convert.ToDecimal(task.Percentaje) / 100));
                        string workItemType = ProcessDictionaries.WorkItemTypes[_mainView.ItemType];

                        AzureTask azureTask = new()
                        {
                            ParentId = Convert.ToInt32(_mainView.Id),
                            ComponentGroup = component.Id,
                            Name = string.Format(taskName, workItemType, _mainView.Id, $"{component.Id.ToString("00")}.{task.Index.ToString("00")}", task.Name),
                            OriginalStimated = originalStimated,
                            AssignedTo = task.Name.Contains("Peer Review") ? _mainView.Reviewer : _mainView.AssignedTo
                        };
                        azureTasks.Add(azureTask);
                    }

                }

                azureTasks = azureTasks.OrderBy(t => t.Name).ToList();
                _mainView.BindingGeneratedTasks(azureTasks);
            }
            catch (Exception ex)
            {
                ShowMessageEvent(ex.Message);
            }

        }
    }
}
