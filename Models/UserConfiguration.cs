namespace Models
{
    public class UserConfiguration
    {
        /// <summary>
        /// Personal access token  con permisos de lectura y escritura de items 
        /// </summary>
        public string Pat { get; set; }
        public string Organization { get; set; }
        public string Project { get; set; }
        /// <summary>
        /// Tipo del processo del proyecto : Agile, Scrum o CMMi
        /// De esto dependerá de los campos de las tareas
        /// </summary>
        public string ProjectProcess { get; set; }
        /// <summary>
        /// Miembros del equipo
        /// </summary>
        public List<TeamMember> Team { get; set; }
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
    }
}
