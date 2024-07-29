using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
using Models;
using System.Text;

namespace AzureServices
{
    public class AzureService
    {
        private readonly UserConfiguration _userConfiguration;
        private readonly string _azureDevOpsOrganizationUrl = "https://dev.azure.com/{0}/";

        public AzureService(UserConfiguration userConfiguration)
        {
            _userConfiguration = userConfiguration;
            _azureDevOpsOrganizationUrl = string.Format(_azureDevOpsOrganizationUrl, userConfiguration.Organization);
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
                foreach (string? row in workItems.Skip(1))
                {
                    string[] columns = row.Split(',');

                    string id = columns[0];
                    string type = columns[1];
                    string parentTitle = columns[2];
                    string childTitle = columns[3];
                    string assigned = columns[4];
                    string state = columns[5];
                    string stimated = columns[6];
                    string remaining = columns[7];
                    string completed = columns[8];
                    string activity = columns[9];
                    string iteration = columns[10];

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
                        JsonPatchDocument document =  new JsonPatchDocument();
                        switch (_userConfiguration.ProjectProcess.Trim().ToUpper())
                        {
                            case "AGILE":
                                document = CreateAgileTask(parentId, childTitle, assigned, state, stimated, remaining, completed, activity, iteration);
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

        /// <summary>
        /// Crea una tarea para un projecto Agile
        /// </summary>
        /// <param name="parentId"></param>
        /// <param name="childTitle"></param>
        /// <param name="assigned"></param>
        /// <param name="state"></param>
        /// <param name="stimated"></param>
        /// <param name="remaining"></param>
        /// <param name="completed"></param>
        /// <param name="activity"></param>
        /// <param name="iteration"></param>
        /// <returns></returns>
        private JsonPatchDocument CreateAgileTask(string parentId, string childTitle, string assigned, string state, string stimated, string remaining, string completed, string activity, string iteration)
        {
            JsonPatchDocument document = new JsonPatchDocument();

            document.AddPatch("/fields/System.Title", childTitle);
            document.AddPatch("/fields/System.AssignedTo", assigned);
            document.AddPatch("/fields/System.State", state);
            document.AddPatch("/fields/Microsoft.VSTS.Scheduling.OriginalEstimate", stimated);
            document.AddPatch("/fields/Microsoft.VSTS.Scheduling.RemainingWork", remaining);
            document.AddPatch("/fields/Microsoft.VSTS.Scheduling.CompletedWork", completed);
            document.AddPatch("/fields/Microsoft.VSTS.Common.Activity", activity);
            document.AddPatch("/fields/System.IterationPath", iteration);

            document.AddPatch("/relations/-",
                    new
                    {
                        rel = "System.LinkTypes.Hierarchy-Reverse",
                        url = $"{_azureDevOpsOrganizationUrl}{_userConfiguration.Project}/_workitems/{parentId}",
                        attributes = new { name = "Parent" }
                    });
            return document;
        }
    }
}
