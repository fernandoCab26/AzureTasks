using Microsoft.VisualStudio.Services.WebApi.Patch;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;

namespace AzureServices
{
    public static class Extensions
    {
        public static void AddPatch(this JsonPatchDocument document,
            string path, object value)
        {
            document.Add(new JsonPatchOperation
            {
                From = null,
                Operation = Operation.Add,
                Path = path,
                Value = value
            });
        }
    }
}
