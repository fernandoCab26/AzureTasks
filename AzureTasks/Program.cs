using AzureServices;
using Models;
using System.Text.Json;

class Program
{
    static void Main(string[] args)
    {

        try
        {
            UserConfiguration userConfiguration = new UserConfiguration();

            using (StreamReader r = new StreamReader("configuration.json"))
            {
                string json = r.ReadToEnd();
                userConfiguration = JsonSerializer.Deserialize<UserConfiguration>(json);
                if (string.IsNullOrEmpty(userConfiguration.Pat) ||
                    string.IsNullOrEmpty(userConfiguration.Project) ||
                    string.IsNullOrEmpty(userConfiguration.Organization) ||
                    string.IsNullOrEmpty(userConfiguration.ProjectProcess))
                {
                    Console.WriteLine($"El archivo configuration.json se encuentra incompleto");
                    Console.ReadLine();
                    return;
                }
            }

            string csvPath = args.Length > 0 ? args[0] : "";

            if (string.IsNullOrWhiteSpace(csvPath))
            {
                Console.WriteLine("Ingresar ruta del csv: ");
                csvPath = Console.ReadLine();
            }

            AzureService azureService = new AzureService(userConfiguration);
            string message = "";
            Console.WriteLine(message);
            message = azureService.ImportWorkItems(csvPath);

            Console.WriteLine(message);

            Console.ReadLine();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            Console.ReadLine();
        }
    }
}