using Mayansoft.AzureConnect.Models;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.Process.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.Process.WebApi.Models;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Client;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.Organization.Client;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Mayansoft.AzureConnect.Services
{
    public class AzureService
    {
        private readonly string _azureDevOpsOrganizationUrl = "https://dev.azure.com/{0}/";

        private VssConnection _vssConnection;
        public VssConnection Connection
        {
            get
            {
                if (_vssConnection == null)
                {// Connect to Azure DevOps Services
                    _vssConnection = new VssConnection(new Uri(_azureDevOpsOrganizationUrl), new VssClientCredentials());
                }
                return _vssConnection;
            }
        }

        public AzureService(UserConfiguration userConfiguration)
        {
            string defaultOrganization = userConfiguration.Organizations.FirstOrDefault(o => o.IsDefault)?.Value;
            _azureDevOpsOrganizationUrl = string.Format(_azureDevOpsOrganizationUrl, defaultOrganization);
            var credentials = new VssClientCredentials();
            // Connect to Azure DevOps Services
            _vssConnection = new VssConnection(new Uri(_azureDevOpsOrganizationUrl), credentials);
        }
        public async Task<WorkItem> GetWorkItem(string id,string project)
        {
            // Create instance of WorkItemTrackingHttpClient using VssConnection
            WorkItemTrackingHttpClient witClient = Connection.GetClient<WorkItemTrackingHttpClient>();

            WorkItem task = await witClient.GetWorkItemAsync(project, int.Parse(id),null,null,WorkItemExpand.All,null);

            return task;
        }
        public async Task<IEnumerable<WorkItem>> GetWorkItems(List<int> ids)
        {
            // Create instance of WorkItemTrackingHttpClient using VssConnection
            WorkItemTrackingHttpClient witClient = Connection.GetClient<WorkItemTrackingHttpClient>();
            var items = await witClient.GetWorkItemsAsync(ids);
            return items;
        }

        /// <summary>
        /// Crea una tarea para un projecto Agile
        /// </summary>
        /// <param name="parentId"></param>
        /// <param name="Values">Valores obtenidos</param>
        /// <returns></returns>
        private JsonPatchDocument CreateAgileTask(Dictionary<string, string> Values, string project, string parentId)
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
                        url = $"{_azureDevOpsOrganizationUrl}{project}/_workitems/{parentId}",
                        attributes = new { name = "Parent" }
                    });
            return document;
        }

        public async Task ImportWorkItems(List<AzureTask> azureTasks, string process,string project, string area, string iterationPath)
        {
            // Create instance of WorkItemTrackingHttpClient using VssConnection
            WorkItemTrackingHttpClient witClient = Connection.GetClient<WorkItemTrackingHttpClient>();

            foreach (var task in azureTasks)
            {
                JsonPatchDocument document; ;
                switch (process)
                {
                    case "Agile":
                        document = CreateDevelopmentAgileTask(task,project, iterationPath, area);
                        break;
                    case "CMMI":
                        document = CreateDevelopmentCmmiTask(task, project, iterationPath, area);
                        break;
                    default:
                        document = new JsonPatchDocument();
                        break;
                }
                WorkItem workItemTask = await witClient.CreateWorkItemAsync(document, project, "Task");
                task.IsCreated = workItemTask.Id.HasValue && workItemTask.Id.Value > 0;
            }

        }

        /// <summary>
        /// Crea una tarea para un projecto Agile
        /// </summary>
        /// <param name="Values">Valores obtenidos</param>
        /// <returns></returns>
        private JsonPatchDocument CreateDevelopmentAgileTask(AzureTask azureTask, string project, string iterationPath, string areaPath)
        {
            JsonPatchDocument document = new JsonPatchDocument();

            AddCommonFields(azureTask,project, iterationPath, document, areaPath);
            document.AddPatch($"/fields/Microsoft.VSTS.Common.Activity", azureTask.Activity);
            return document;
        }

        /// <summary>
        /// Crea una tarea para un projecto CMMI
        /// </summary>
        /// <param name="Values">Valores obtenidos</param>
        /// <returns></returns>
        private JsonPatchDocument CreateDevelopmentCmmiTask(AzureTask azureTask, string project, string iterationPath, string areaPath)
        {
            JsonPatchDocument document = new JsonPatchDocument();

            AddCommonFields(azureTask,project, iterationPath, document, areaPath);
            document.AddPatch($"/fields/Microsoft.VSTS.Common.Discipline", azureTask.Activity);
            document.AddPatch($"/fields/Microsoft.VSTS.CMMI.TaskType", azureTask.TaskType);
            return document;
        }

        private void AddCommonFields(AzureTask azureTask, string project, string iterationPath, JsonPatchDocument document, string areaPath)
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
                        url = $"{_azureDevOpsOrganizationUrl}{project}/_workitems/{azureTask.ParentId}",
                        attributes = new { name = "Parent" }
                    });
        }

        public async Task<IEnumerable<TeamProjectReference>> GetTeamProjectReferences()
        {

            ProjectHttpClient projectClient = Connection.GetClient<ProjectHttpClient>();

            return  await projectClient.GetProjects();
        }
        public TeamProject GetProjectDetails(string projectName)
        {
            ProjectHttpClient projectClient = Connection.GetClient<ProjectHttpClient>();
            return projectClient.GetProject(projectName, includeCapabilities:true).Result;
        }
        public IEnumerable<Process> GetProjectProcesses()
        {
            ProcessHttpClient processClient = Connection.GetClient<ProcessHttpClient>();
            
            return processClient.GetProcessesAsync().Result;
        }

        public async Task<ProcessInfo> GetProcessById(Guid processId)
        {
            WorkItemTrackingProcessHttpClient processClient = Connection.GetClient<WorkItemTrackingProcessHttpClient>();
            return await processClient.GetProcessByItsIdAsync(processId);
        }

        public async Task<IEnumerable<WebApiTeam>> GetTeam(string projectId)
        {
            TeamHttpClient teamClient = Connection.GetClient<TeamHttpClient>();
            return await teamClient.GetTeamsAsync(projectId);
        }

        public async Task<IEnumerable<IdentityRef>> GetTeamMembers(string projectId, string teamId)
        {
            TeamHttpClient identityClient = Connection.GetClient<TeamHttpClient>();
            return await identityClient.GetTeamMembers(projectId,teamId);
        }
    }
}
