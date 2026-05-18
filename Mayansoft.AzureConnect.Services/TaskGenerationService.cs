using Mayansoft.AzureConnect.Models;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Mayansoft.AzureConnect.Services
{
    public class TaskGenerationService
    {
        private readonly AzureTaskFactory _factory;
        private readonly AzureService _azureService;
        public UserConfiguration userConfiguration { get; set; }

        public TaskGenerationService(AzureTaskFactory factory)
        {
            _factory = factory;
            BindingConfigurations();
            _azureService = new AzureService(userConfiguration);
        }

        public async Task<List<AzureTask>> GenerateTasks(GenerateTasksRequest request)
        {
            WorkItem parent = await _azureService.GetWorkItem(request.ParentId, "Bepensa - DSD Bebidas - OpeCD 2.0");
            List<AzureTask> generatedTasks = new List<AzureTask>();
            ParentContext context = new ParentContext
            {
                ParentId = parent.Id.Value,
                ProjectName = parent.Fields["System.TeamProject"].ToString(),
                AreaPath = parent.Fields["System.AreaPath"].ToString(),
                IterationPath = parent.Fields["System.IterationPath"].ToString(),
                Type = parent.Fields["System.WorkItemType"].ToString()
            };

            foreach (var taskTemplate in userConfiguration.DevTasks)
            {
                decimal originalStimated = request.DevelopmentHours * (Convert.ToDecimal(taskTemplate.Percentaje) / 100);
                string assignedTo = taskTemplate.Key.Contains("peer_review") ? request.Reviewer : request.AssignedTo;
                AzureTask azureTask = _factory.CreateTask(taskTemplate, context, originalStimated, assignedTo);
                generatedTasks.Add(azureTask);
            }
            return generatedTasks;
        }

        public async Task<AzureTask> GenerateSingleTask(GenerateSingleTaskRequest request)
        {
            WorkItem parent = await _azureService.GetWorkItem(request.ParentId, "Bepensa - DSD Bebidas - OpeCD 2.0");
            ParentContext context = new ParentContext
            {
                ParentId = parent.Id.Value,
                ProjectName = parent.Fields["System.TeamProject"].ToString(),
                AreaPath = parent.Fields["System.AreaPath"].ToString(),
                IterationPath = parent.Fields["System.IterationPath"].ToString(),
                Type = parent.Fields["System.WorkItemType"].ToString()
            };
            // Unir la lista de tares con la plantilla para obtener el template correcto
            IEnumerable<NormalTask> templates = userConfiguration.TestingTasks.Union(userConfiguration.OtherTasks);
            var template = ResolveTemplate(request.TaskType, templates);

            AzureTask azureTask = _factory.CreateTask(template, context, request.OriginalEstimated, request.AssignedTo);

            return azureTask;

        }

        private void BindingConfigurations()
        {
            userConfiguration = new UserConfiguration();
            string rutaConfig = Path.Combine(AppContext.BaseDirectory, "configuration.json");
            string json = File.ReadAllText(rutaConfig);
            userConfiguration = JsonSerializer.Deserialize<UserConfiguration>(json);
            if (!userConfiguration.Organizations.Any())
            {
                throw new Exception($"El archivo configuration.json se encuentra incompleto");
            }


        }

        public ProcessTask ResolveTemplate(OperationalTaskType task, IEnumerable<NormalTask> templates)
        {
            switch (task)
            {
                case OperationalTaskType.TestDesign:
                    return templates.FirstOrDefault(t => t.Key.Contains("test_design"));
                case OperationalTaskType.TestExecution:
                    return templates.FirstOrDefault(t => t.Key.Contains("test_execution"));
                case OperationalTaskType.DefectRegistration:
                    return templates.FirstOrDefault(t => t.Key.Contains("defect_registration"));
                case OperationalTaskType.PeerReviewSpecification:
                    return templates.FirstOrDefault(t => t.Key.Contains("peer_review_specification"));
                case OperationalTaskType.PeerReviewTest:
                    return templates.FirstOrDefault(t => t.Key.Contains("peer_review_test"));
                case OperationalTaskType.Specification:
                    return templates.FirstOrDefault(t => t.Key.Contains("specification"));
                case OperationalTaskType.Analysis:
                    return templates.FirstOrDefault(t => t.Key.Contains("analysis"));
                case OperationalTaskType.Documentation:
                    return templates.FirstOrDefault(t => t.Key.Contains("documentation"));
                case OperationalTaskType.VideoTraining:
                    return templates.FirstOrDefault(t => t.Key.Contains("video_training"));
                default:
                    throw new Exception($"No se encontró una plantilla para la tarea {task}");
            }
        }
    }
}

