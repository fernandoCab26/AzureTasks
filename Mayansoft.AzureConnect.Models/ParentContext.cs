using System;
using System.Collections.Generic;
using System.Text;

namespace Mayansoft.AzureConnect.Models
{
    public class ParentContext
    {
        public int ParentId { get; set; }
        public string ProjectName { get; set; }
        public string AreaPath { get; set; }
        public string IterationPath { get; set; }
        public string Type { get; set; }
    }
}
