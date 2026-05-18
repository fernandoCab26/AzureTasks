using Mayansoft.AzureConnect.Models;
using Mayansoft.AzureConnect.Services;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace Mayansoft.AzureConnect.MCP.Server.Tools
{
    internal class AzureTaskCreatorTools
    {
        private readonly TaskGenerationService _service;
        public AzureTaskCreatorTools(TaskGenerationService service)
        {
            _service = service;
        }
        [McpServerTool]
        [Description("Genera una serie de tareas de Azure DevOps")]
        public async Task<List<AzureTask>> GenerateAzureTasks([Description("Solicitud para generar tareas en Azure DevOps")] GenerateTasksRequest request)
        {
            return await _service.GenerateTasks(request);
        }
        [McpServerTool]
        [Description("Genera una tarea de testing o una tarea general en Azure DevOps")]
        public async Task<AzureTask> GenerateTestingTask([Description("Solicitud para generar una tarea en Azure DevOps")] GenerateSingleTaskRequest request)
        {
            return await _service.GenerateSingleTask(request);
        }
        [McpServerTool]
        [Description("Obtiene el valor del esfuerzo de acuerdo a un size específico de un item de azure Azure DevOps")]
        public  decimal GetEffort([Description("Tamaño del size del item")] int size)
        {
            // Fix: Return a List<AzureTask> instead of a decimal
            var effort = size * .8117m;

            return effort;
        }
    }
}
