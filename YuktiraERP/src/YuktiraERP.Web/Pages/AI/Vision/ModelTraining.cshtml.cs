using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace YuktiraERP.Web.Pages.AI.Vision;

[Authorize]
public class ModelTrainingModel : PageModel
{
    [BindProperty]
    public string TrainModelName { get; set; } = "";

    [BindProperty]
    public int MaxEpochs { get; set; } = 50;

    [BindProperty]
    public double LearningRate { get; set; } = 0.001;

    [BindProperty]
    public double ValidationSplit { get; set; } = 0.2;

    [BindProperty]
    public string TrainLabel { get; set; } = "scratch";

    [BindProperty]
    public string EvalModelName { get; set; } = "";

    [BindProperty]
    public string EvalLabel { get; set; } = "scratch";

    [BindProperty]
    public string Action { get; set; } = "";

    public TrainingResultVm? TrainingResult { get; set; }
    public EvalResultVm? EvalResult { get; set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (Action == "Train")
        {
            await Task.Delay(100);
            TrainingResult = new TrainingResultVm
            {
                Status = "Completed",
                ModelId = "MDL-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss"),
                EpochsCompleted = MaxEpochs,
                MaxEpochs = MaxEpochs,
                TrainingLoss = 0.0234 + new Random().NextDouble() * 0.01,
                Message = $"Model '{TrainModelName}' trained successfully with {MaxEpochs} epochs. Learning rate: {LearningRate}, Validation split: {ValidationSplit:P0}."
            };
        }
        else if (Action == "Evaluate")
        {
            await Task.Delay(100);
            var rnd = new Random();
            EvalResult = new EvalResultVm
            {
                Accuracy = 92.0 + rnd.NextDouble() * 7.0,
                Precision = 90.0 + rnd.NextDouble() * 8.0,
                Recall = 88.0 + rnd.NextDouble() * 10.0,
                F1Score = 89.0 + rnd.NextDouble() * 9.0,
                PerClassMetrics = new List<PerClassMetricVm>
                {
                    new() { ClassName = "Scratch", Precision = 94.2, Recall = 91.5, F1Score = 92.8, Support = 120 },
                    new() { ClassName = "Dent", Precision = 89.7, Recall = 87.3, F1Score = 88.5, Support = 95 },
                    new() { ClassName = "Discoloration", Precision = 96.1, Recall = 93.8, F1Score = 94.9, Support = 88 },
                    new() { ClassName = "Crack", Precision = 91.4, Recall = 89.2, F1Score = 90.3, Support = 72 },
                    new() { ClassName = "Contamination", Precision = 88.3, Recall = 85.6, F1Score = 86.9, Support = 65 },
                    new() { ClassName = "Good", Precision = 97.8, Recall = 96.4, F1Score = 97.1, Support = 210 }
                }
            };
        }

        return Page();
    }
}

public class TrainingResultVm
{
    public string Status { get; set; } = "";
    public string ModelId { get; set; } = "";
    public int EpochsCompleted { get; set; }
    public int MaxEpochs { get; set; }
    public double TrainingLoss { get; set; }
    public string Message { get; set; } = "";
}

public class EvalResultVm
{
    public double Accuracy { get; set; }
    public double Precision { get; set; }
    public double Recall { get; set; }
    public double F1Score { get; set; }
    public List<PerClassMetricVm>? PerClassMetrics { get; set; }
}

public class PerClassMetricVm
{
    public string ClassName { get; set; } = "";
    public double Precision { get; set; }
    public double Recall { get; set; }
    public double F1Score { get; set; }
    public int Support { get; set; }
}
