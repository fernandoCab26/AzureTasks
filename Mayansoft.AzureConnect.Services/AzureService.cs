using Mayansoft.AzureConnect.Models;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Client;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Mayansoft.AzureConnect.Services
{
    public class AzureService
    {
        private readonly UserConfiguration _userConfiguration;
        private readonly string _azureDevOpsOrganizationUrl = "https://dev.azure.com/{0}/";

        private VssConnection _vssConnection; 
        public VssConnection Connection
        {
            get
            {
                if(_vssConnection == null)
                {// Connect to Azure DevOps Services
                    _vssConnection = new VssConnection(new Uri(_azureDevOpsOrganizationUrl), new VssClientCredentials());
                }
                return _vssConnection;
            }
        }

        public AzureService(UserConfiguration userConfiguration)
        {
            _userConfiguration = userConfiguration;
            _azureDevOpsOrganizationUrl = string.Format(_azureDevOpsOrganizationUrl, userConfiguration.Organization);
            // Connect to Azure DevOps Services
            _vssConnection = new VssConnection(new Uri(_azureDevOpsOrganizationUrl), new VssClientCredentials());
        }

        public string ImportWorkItems(string csvPath)
        {
            string[] workItems;
            string message;
            using (StreamReader r = new StreamReader(csvPath, Encoding.GetEncoding("iso-8859-1")))
            {
                workItems = r.ReadToEnd().Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            }

            var creds = new VssBasicCredential(string.Empty, _userConfiguration.Pat);
            // Connect to Azure DevOps Services
            var connection = new VssConnection(new Uri(_azureDevOpsOrganizationUrl), creds);
            // Create instance of WorkItemTrackingHttpClient using VssConnection
            WorkItemTrackingHttpClient witClient = connection.GetClient<WorkItemTrackingHttpClient>();



            try
            {
                string parentId = "";
                int taskCreatedCount = 0;
                int itemProcessed = 0;
                int percentaje = 0;
                //Just skipping the CSV file header
                Dictionary<string, int> headers = new Dictionary<string, int>();


                if (workItems.Length > 0)
                {
                    string[] csvHeaders = workItems[0].Split(',');
                    int position = 0;
                    foreach (string header in csvHeaders)
                    {

                        if (ProcessDictionaries.ItemsFields.TryGetValue(header, out string value))
                        {
                            headers.Add(value, position);

                        }
                        position++;
                    }
                }

                foreach (string row in workItems.Skip(1))
                {
                    // Lista de propiedades existentes en el csv
                    Dictionary<string, string> values = new Dictionary<string, string>();

                    string[] columns = row.Split(',');
                    string id = "";
                    string type = "";
                    foreach (KeyValuePair<string, int> item in headers)
                    {
                        values.Add(item.Key, columns[item.Value]);
                        if (item.Key == "System.Id")
                        {
                            id = columns[item.Value];
                        }
                        if (item.Key == "System.WorkItemType")
                        {
                            type = columns[item.Value];
                        }

                    }


                    // Si el item tiene un id y es un User Story o un requerimiento  se considera como padre 

                    bool isParent = !string.IsNullOrWhiteSpace(id) && (type == "User Story" || type == "Requirement");

                    if (isParent)
                    {
                        parentId = id;
                    }
                    else if (type == "User Story" || type == "Requirement")
                    {
                        parentId = "";
                    }
                    // Sólo se crean tareas con un padre
                    if (type == "Task" && !string.IsNullOrWhiteSpace(parentId))
                    {
                        JsonPatchDocument document = new JsonPatchDocument();
                        switch (_userConfiguration.ProjectProcess.Trim().ToUpper())
                        {
                            case "AGILE":
                                document = CreateAgileTask(values, parentId);
                                break;
                            case "SCRUM":
                                // TODO verificar los nombres de propiedades que aplican en https://learn.microsoft.com/en-us/azure/devops/boards/work-items/guidance/work-item-field?view=azure-devops
                                break;
                            case "CMMI":
                                break;
                            default:
                                message = $"El tipo de proceso {_userConfiguration.ProjectProcess} no es soportado";
                                return message;

                        }

                        //Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models.WorkItem workItemTask = witClient.CreateWorkItemAsync(document, _userConfiguration.Project, type).Result; 

                        taskCreatedCount++;
                    }
                    //itemProcessed++;
                    //percentaje = (itemProcessed * 100) / (workItems.Count() - 1);
                    //Console.WriteLine($"{percentaje}% procesado");
                }
                message = $"{taskCreatedCount} Tareas creadas";
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }

            return message;
        }

        public WorkItem GetWorkItem(string id)
        {
            // Create instance of WorkItemTrackingHttpClient using VssConnection
            WorkItemTrackingHttpClient witClient = Connection.GetClient<WorkItemTrackingHttpClient>();

            WorkItem task = witClient.GetWorkItemAsync(_userConfiguration.Project, int.Parse(id)).Result;

            return task;
        }

        /// <summary>
        /// Crea una tarea para un projecto Agile
        /// </summary>
        /// <param name="parentId"></param>
        /// <param name="Values">Valores obtenidos</param>
        /// <returns></returns>
        private JsonPatchDocument CreateAgileTask(Dictionary<string, string> Values, string parentId)
        {
            JsonPatchDocument document = new JsonPatchDocument();

            foreach (KeyValuePair<string, string> keyValuePair in Values)
            {
                document.AddPatch($"/fields/{keyValuePair.Key}", keyValuePair.Value);
            }

            document.AddPatch("/relations/-",
                    new
                    {
                        rel = "System.LinkTypes.Hierarchy-Reverse",
                        url = $"{_azureDevOpsOrganizationUrl}{_userConfiguration.Project}/_workitems/{parentId}",
                        attributes = new { name = "Parent" }
                    });
            return document;
        }

        public void ImportWorkItems(List<AzureTask> azureTasks, string area, string iterationPath)
        {
            // Create instance of WorkItemTrackingHttpClient using VssConnection
            WorkItemTrackingHttpClient witClient = Connection.GetClient<WorkItemTrackingHttpClient>();

            foreach (var task in azureTasks)
            {
                JsonPatchDocument document; ;
                switch (_userConfiguration.ProjectProcess)
                {
                    case "Agile":
                        document = CreateDevelopmentAgileTask(task, iterationPath, area);
                        break;
                    case "CMMI":
                        document = CreateDevelopmentCmmiTask(task, iterationPath, area);
                        break;
                    default:
                        document = new JsonPatchDocument();
                        break;
                }
                WorkItem workItemTask = witClient.CreateWorkItemAsync(document, _userConfiguration.Project, "Task").Result;
                task.IsCreated = workItemTask.Id.HasValue && workItemTask.Id.Value > 0;
            }

        }

        /// <summary>
        /// Crea una tarea para un projecto Agile
        /// </summary>
        /// <param name="Values">Valores obtenidos</param>
        /// <returns></returns>
        private JsonPatchDocument CreateDevelopmentAgileTask(AzureTask azureTask, string iterationPath, string areaPath)
        {
            JsonPatchDocument document = new JsonPatchDocument();

            AddCommonFields(azureTask, iterationPath, document, areaPath);
            document.AddPatch($"/fields/Microsoft.VSTS.Common.Activity", azureTask.Activity);
            return document;
        }

        /// <summary>
        /// Crea una tarea para un projecto CMMI
        /// </summary>
        /// <param name="Values">Valores obtenidos</param>
        /// <returns></returns>
        private JsonPatchDocument CreateDevelopmentCmmiTask(AzureTask azureTask, string iterationPath, string areaPath)
        {
            JsonPatchDocument document = new JsonPatchDocument();

            AddCommonFields(azureTask, iterationPath, document, areaPath);
            document.AddPatch($"/fields/Microsoft.VSTS.Common.Discipline", azureTask.Activity);
            document.AddPatch($"/fields/Microsoft.VSTS.CMMI.TaskType", azureTask.TaskType);
            return document;
        }

        private void AddCommonFields(AzureTask azureTask, string iterationPath, JsonPatchDocument document, string areaPath)
        {
            document.AddPatch($"/fields/System.Title", azureTask.Name);
            document.AddPatch($"/fields/System.AssignedTo", azureTask.AssignedTo);
            document.AddPatch($"/fields/Microsoft.VSTS.Scheduling.OriginalEstimate", azureTask.OriginalStimated);
            document.AddPatch($"/fields/Microsoft.VSTS.Scheduling.RemainingWork", azureTask.OriginalStimated);
            document.AddPatch($"/fields/Microsoft.VSTS.Scheduling.CompletedWork", 0);
            document.AddPatch($"/fields/System.IterationPath", iterationPath);
            document.AddPatch($"/fields/System.AreaPath", areaPath);

            document.AddPatch("/relations/-",
                    new
                    {
                        rel = "System.LinkTypes.Hierarchy-Reverse",
                        url = $"{_azureDevOpsOrganizationUrl}{_userConfiguration.Project}/_workitems/{azureTask.ParentId}",
                        attributes = new { name = "Parent" }
                    });
        }
    }
}
