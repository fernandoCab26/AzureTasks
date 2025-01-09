using System.Collections.Generic;

namespace Mayansoft.AzureConnect.Models
{
    public class UserConfiguration
    {
        /// <summary>
        /// Listas de tareas de acuerdo al proceso.
        /// Agregar las útiles para el equipo
        /// </summary>
        public List<DevelopmentTask> DevTasks { get; set; }
        /// <summary>
        /// Tareas de testing de acuerdo al proceso
        /// </summary>
        public List<NormalTask> TestingTasks { get; set; }
        /// <summary>
        /// Tareas adicionales 
        /// </summary>
        public List<NormalTask> OtherTasks { get; set; }
        /// <summary>
        /// Lista de organizaciones
        /// </summary>
        public List<Organization> Organizations { get; set; }
    }
}
