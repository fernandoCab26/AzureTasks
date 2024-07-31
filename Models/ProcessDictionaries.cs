namespace Models
{
    public static class ProcessDictionaries
    {
        public static Dictionary<string, string> ItemsFields  = new Dictionary<string, string>
            {
                {"Title 2","System.Title" },
                {"ID", "System.Id" },
                {"Work Item Type","System.WorkItemType" },
                {"Assigned To","System.AssignedTo" },
                {"State","System.State" },
                {"Original Estimate", "Microsoft.VSTS.Scheduling.OriginalEstimate" },
                {"Remaining Work","Microsoft.VSTS.Scheduling.RemainingWork" },
                {"Completed Work","Microsoft.VSTS.Scheduling.CompletedWork" },
                {"Activity","Microsoft.VSTS.Common.Activity" },
                {"Iteration Path","System.IterationPath" }
            };

    }
}
