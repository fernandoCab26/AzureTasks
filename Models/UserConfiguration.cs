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
        public List<TeamMember> Team { get; set; }
    }
}
