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

                foreach (string? row in workItems.Skip(1))
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
                                document = CreateAgileTask(values,parentId);
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
    }
}
