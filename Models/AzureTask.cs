﻿namespace Models
{
    public class AzureTask
    {
        public int ParentId { get; set; }
        public int ComponentGroup { get;set; }
        public string Name { get; set; }
        public decimal OriginalStimated { get; set; }
        public string AssignedTo { get; set; }
    }
}