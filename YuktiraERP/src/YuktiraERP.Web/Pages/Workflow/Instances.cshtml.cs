using Microsoft.AspNetCore.Mvc.RazorPages;
namespace YuktiraERP.Web.Pages.Workflow;
public class InstancesModel : PageModel
{
    public List<WorkflowInstance> Instances { get; set; } = new();
    public void OnGet()
    {
        Instances = new List<WorkflowInstance>
        {
            new() { Id = "WF-001", Name = "PO Approval", Status = "Running", Started = DateTime.Now.AddDays(-2) },
            new() { Id = "WF-002", Name = "Leave Request", Status = "Completed", Started = DateTime.Now.AddDays(-5) },
            new() { Id = "WF-003", Name = "Invoice Approval", Status = "Pending", Started = DateTime.Now.AddDays(-1) },
        };
    }
}
public class WorkflowInstance
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime Started { get; set; }
}
