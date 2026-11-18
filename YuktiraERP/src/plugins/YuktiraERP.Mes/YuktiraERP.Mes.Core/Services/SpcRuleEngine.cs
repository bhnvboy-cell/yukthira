namespace YuktiraERP.Mes.Core.Services;

public static class SpcRuleEngine
{
    /// <summary>
    /// Evaluates Western Electric rules 1-4 against ordered subgroup means.
    /// Returns violated rule IDs (e.g., WE_RULE_1, WE_RULE_2, WE_RULE_3, WE_RULE_4).
    /// </summary>
    public static IReadOnlyList<string> EvaluateWesternElectric(
        IReadOnlyList<double> series,
        double cl,
        double ucl,
        double lcl)
    {
        var violations = new List<string>();
        if (series.Count == 0) return violations;

        var sigmaUpper = (ucl - cl) / 3.0;
        var sigmaLower = (cl - lcl) / 3.0;
        if (sigmaUpper <= 0 || sigmaLower <= 0) return violations;

        var last = series[^1];

        // Rule 1: 1 point beyond 3 sigma (UCL/LCL)
        if (last > ucl || last < lcl)
            violations.Add("WE_RULE_1");

        // Rule 2: 2 of 3 consecutive points beyond 2 sigma on the same side of CL
        if (series.Count >= 3)
        {
            var last3 = series.Skip(series.Count - 3).ToArray();
            if (last3.Count(v => v > cl + 2 * sigmaUpper) >= 2 ||
                last3.Count(v => v < cl - 2 * sigmaLower) >= 2)
            {
                violations.Add("WE_RULE_2");
            }
        }

        // Rule 3: 4 of 5 consecutive points beyond 1 sigma on the same side of CL
        if (series.Count >= 5)
        {
            var last5 = series.Skip(series.Count - 5).ToArray();
            if (last5.Count(v => v > cl + sigmaUpper) >= 4 ||
                last5.Count(v => v < cl - sigmaLower) >= 4)
            {
                violations.Add("WE_RULE_3");
            }
        }

        // Rule 4: 8 consecutive points on the same side of CL
        if (series.Count >= 8)
        {
            var last8 = series.Skip(series.Count - 8).ToArray();
            if (last8.All(v => v > cl) || last8.All(v => v < cl))
            {
                violations.Add("WE_RULE_4");
            }
        }

        return violations;
    }

    public static (double Cp, double Cpk) ComputeCapability(
        IReadOnlyList<double> samples,
        double lsl,
        double usl)
    {
        if (samples.Count < 2 || usl <= lsl) return (0, 0);

        var mean = samples.Average();
        var variance = samples.Sum(x => Math.Pow(x - mean, 2)) / (samples.Count - 1);
        var stdDev = Math.Sqrt(variance);
        if (stdDev <= 1e-12) return (0, 0);

        var cp = (usl - lsl) / (6.0 * stdDev);
        var cpu = (usl - mean) / (3.0 * stdDev);
        var cpl = (mean - lsl) / (3.0 * stdDev);
        var cpk = Math.Min(cpu, cpl);

        return (Math.Round(cp, 4), Math.Round(cpk, 4));
    }
}
