using Microsoft.EntityFrameworkCore;
using YuktiraERP.Core.Dtos;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;

namespace YuktiraERP.Infrastructure.Services;

/// <summary>
/// SPC control-chart engine (read-only).
/// Subgroups inspection results by inspection lot, builds X̄ / R / S charts with
/// standard SPC constants (n = 1..25), computes process capability indices and
/// evaluates Nelson rules 1-8 plus the Western Electric zone rules on the X̄ series.
/// </summary>
public class SpcEngineService : ISpcEngineService
{
    private readonly YuktiraDbContext _db;
    private readonly ITenantContext _tenant;

    public SpcEngineService(YuktiraDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    // ══════════════════════════════════════════════════════════════════
    // Standard SPC constants for subgroup sizes n = 1..25 (index = n)
    // ══════════════════════════════════════════════════════════════════

    private static readonly double[] A2Table =
    {
        0.000, 0.000, 1.880, 1.023, 0.729, 0.577, 0.483, 0.419, 0.373, 0.337, 0.308,
        0.285, 0.266, 0.249, 0.235, 0.223, 0.212, 0.203, 0.194, 0.187, 0.180,
        0.173, 0.167, 0.162, 0.157, 0.153
    };

    private static readonly double[] D3Table =
    {
        0.000, 0.000, 0.000, 0.000, 0.000, 0.000, 0.000, 0.076, 0.136, 0.184, 0.223,
        0.256, 0.283, 0.307, 0.328, 0.347, 0.363, 0.378, 0.391, 0.404, 0.415,
        0.425, 0.434, 0.443, 0.451, 0.459
    };

    private static readonly double[] D4Table =
    {
        0.000, 0.000, 3.267, 2.574, 2.282, 2.114, 2.004, 1.924, 1.864, 1.816, 1.777,
        1.744, 1.717, 1.693, 1.672, 1.653, 1.637, 1.622, 1.608, 1.596, 1.585,
        1.575, 1.566, 1.557, 1.548, 1.541
    };

    private static readonly double[] B3Table =
    {
        0.000, 0.000, 0.000, 0.000, 0.000, 0.000, 0.030, 0.118, 0.185, 0.239, 0.284,
        0.321, 0.354, 0.382, 0.406, 0.428, 0.448, 0.466, 0.482, 0.497, 0.510,
        0.523, 0.534, 0.545, 0.555, 0.565
    };

    private static readonly double[] B4Table =
    {
        0.000, 0.000, 3.267, 2.568, 2.266, 2.089, 1.970, 1.882, 1.815, 1.761, 1.716,
        1.679, 1.646, 1.618, 1.594, 1.572, 1.552, 1.534, 1.518, 1.503, 1.490,
        1.477, 1.466, 1.455, 1.445, 1.435
    };

    private static readonly double[] D2Table =
    {
        0.000, 1.000, 1.128, 1.693, 2.059, 2.326, 2.534, 2.704, 2.847, 2.970, 3.078,
        3.173, 3.258, 3.336, 3.407, 3.472, 3.532, 3.588, 3.640, 3.689, 3.735,
        3.778, 3.819, 3.858, 3.895, 3.931
    };

    private static readonly double[] C4Table =
    {
        0.000, 1.000, 0.7979, 0.8862, 0.9213, 0.9400, 0.9515, 0.9594, 0.9650, 0.9693, 0.9727,
        0.9754, 0.9776, 0.9794, 0.9810, 0.9823, 0.9835, 0.9845, 0.9854, 0.9862, 0.9869,
        0.9876, 0.9882, 0.9887, 0.9892, 0.9896
    };

    private static double At(double[] table, int n)
    {
        if (n < 1) n = 1;
        if (n > 25) n = 25;
        return table[n];
    }

    public static double GetA2(int n) => At(A2Table, n);
    public static double GetD3(int n) => At(D3Table, n);
    public static double GetD4(int n) => At(D4Table, n);
    public static double GetB3(int n) => At(B3Table, n);
    public static double GetB4(int n) => At(B4Table, n);
    public static double GetD2(int n) => At(D2Table, n);
    public static double GetC4(int n) => At(C4Table, n);

    // ══════════════════════════════════════════════════════════════════
    // Public pure math helpers (unit-testable)
    // ══════════════════════════════════════════════════════════════════

    /// <summary>Sample standard deviation (n-1 denominator); 0 for n &lt; 2.</summary>
    public static double SampleStdDev(IReadOnlyList<double> values)
    {
        if (values == null || values.Count < 2) return 0;
        var mean = 0.0;
        for (var i = 0; i < values.Count; i++) mean += values[i];
        mean /= values.Count;
        var sumSq = 0.0;
        for (var i = 0; i < values.Count; i++)
        {
            var d = values[i] - mean;
            sumSq += d * d;
        }
        return Math.Sqrt(sumSq / (values.Count - 1));
    }

    /// <summary>Builds a subgroup summary from the raw measurements of one lot.</summary>
    public static SpcSubgroupDto BuildSubgroup(int index, string lotNumber, DateTime timestamp, IReadOnlyList<decimal> values)
    {
        var subgroup = new SpcSubgroupDto
        {
            Index = index,
            LotNumber = lotNumber ?? "",
            Label = lotNumber ?? "",
            Timestamp = timestamp,
            N = values == null ? 0 : values.Count
        };

        if (subgroup.N == 0) return subgroup;

        var doubles = new List<double>(subgroup.N);
        foreach (var v in values) doubles.Add((double)v);

        var sum = 0.0;
        foreach (var d in doubles) sum += d;
        subgroup.Mean = sum / doubles.Count;

        if (subgroup.N >= 2)
        {
            var min = doubles[0];
            var max = doubles[0];
            foreach (var d in doubles)
            {
                if (d < min) min = d;
                if (d > max) max = d;
            }
            subgroup.Range = max - min;
            subgroup.StdDev = SampleStdDev(doubles);
        }

        return subgroup;
    }

    /// <summary>Average of the subgroup ranges (n ≥ 2 subgroups only); 0 when none.</summary>
    public static double ComputeRBar(IReadOnlyList<SpcSubgroupDto> subgroups)
    {
        if (subgroups == null) return 0;
        var sum = 0.0;
        var count = 0;
        foreach (var s in subgroups)
        {
            if (s.N >= 2 && s.Range.HasValue)
            {
                sum += s.Range.Value;
                count++;
            }
        }
        return count == 0 ? 0 : sum / count;
    }

    /// <summary>Average of the subgroup standard deviations (n ≥ 2 subgroups only); 0 when none.</summary>
    public static double ComputeSBar(IReadOnlyList<SpcSubgroupDto> subgroups)
    {
        if (subgroups == null) return 0;
        var sum = 0.0;
        var count = 0;
        foreach (var s in subgroups)
        {
            if (s.N >= 2 && s.StdDev.HasValue)
            {
                sum += s.StdDev.Value;
                count++;
            }
        }
        return count == 0 ? 0 : sum / count;
    }

    /// <summary>X̄ limits: grandMean ± A2(n) · R̄.</summary>
    public static (double Ucl, double Cl, double Lcl) XbarLimits(double grandMean, double rBar, int n)
        => (grandMean + GetA2(n) * rBar, grandMean, grandMean - GetA2(n) * rBar);

    /// <summary>R limits: CL = R̄, UCL = D4(n) · R̄, LCL = D3(n) · R̄.</summary>
    public static (double Ucl, double Cl, double Lcl) RChartLimits(double rBar, int n)
        => (GetD4(n) * rBar, rBar, GetD3(n) * rBar);

    /// <summary>S limits: CL = S̄, UCL = B4(n) · S̄, LCL = B3(n) · S̄.</summary>
    public static (double Ucl, double Cl, double Lcl) SChartLimits(double sBar, int n)
        => (GetB4(n) * sBar, sBar, GetB3(n) * sBar);

    /// <summary>Builds the X̄ chart (per-subgroup limits) from the subgroups.</summary>
    public static SpcChartDto BuildXbarChart(IReadOnlyList<SpcSubgroupDto> subgroups, string title, string unit)
    {
        var chart = new SpcChartDto { Title = title, Unit = unit ?? "" };
        if (subgroups == null || subgroups.Count == 0) return chart;

        var grandMean = 0.0;
        foreach (var s in subgroups) grandMean += s.Mean;
        grandMean /= subgroups.Count;

        var rBar = ComputeRBar(subgroups);

        for (var i = 0; i < subgroups.Count; i++)
        {
            var s = subgroups[i];
            var limits = XbarLimits(grandMean, rBar, s.N);
            var point = new SpcPointDto
            {
                Index = i,
                Label = s.Label,
                Value = s.Mean,
                Cl = limits.Cl,
                Ucl = limits.Ucl,
                Lcl = limits.Lcl
            };
            point.Violating = IsBeyondLimits(point);
            chart.Points.Add(point);
        }
        return chart;
    }

    /// <summary>Builds the R chart; subgroups with n &lt; 2 get a null point value.</summary>
    public static SpcChartDto BuildRangeChart(IReadOnlyList<SpcSubgroupDto> subgroups, string title, string unit)
    {
        var chart = new SpcChartDto { Title = title, Unit = unit ?? "" };
        if (subgroups == null || subgroups.Count == 0) return chart;

        var rBar = ComputeRBar(subgroups);
        for (var i = 0; i < subgroups.Count; i++)
        {
            var s = subgroups[i];
            var limits = RChartLimits(rBar, s.N);
            var point = new SpcPointDto
            {
                Index = i,
                Label = s.Label,
                Value = s.N >= 2 ? s.Range : null,
                Cl = limits.Cl,
                Ucl = limits.Ucl,
                Lcl = limits.Lcl
            };
            point.Violating = IsBeyondLimits(point);
            chart.Points.Add(point);
        }
        return chart;
    }

    /// <summary>Builds the S chart; subgroups with n &lt; 2 get a null point value.</summary>
    public static SpcChartDto BuildSChart(IReadOnlyList<SpcSubgroupDto> subgroups, string title, string unit)
    {
        var chart = new SpcChartDto { Title = title, Unit = unit ?? "" };
        if (subgroups == null || subgroups.Count == 0) return chart;

        var sBar = ComputeSBar(subgroups);
        for (var i = 0; i < subgroups.Count; i++)
        {
            var s = subgroups[i];
            var limits = SChartLimits(sBar, s.N);
            var point = new SpcPointDto
            {
                Index = i,
                Label = s.Label,
                Value = s.N >= 2 ? s.StdDev : null,
                Cl = limits.Cl,
                Ucl = limits.Ucl,
                Lcl = limits.Lcl
            };
            point.Violating = IsBeyondLimits(point);
            chart.Points.Add(point);
        }
        return chart;
    }

    /// <summary>True when the point value lies outside its UCL / LCL.</summary>
    public static bool IsBeyondLimits(SpcPointDto point)
    {
        if (point == null || !point.Value.HasValue) return false;
        if (point.Ucl.HasValue && point.Value.Value > point.Ucl.Value) return true;
        if (point.Lcl.HasValue && point.Value.Value < point.Lcl.Value) return true;
        return false;
    }

    /// <summary>
    /// Process capability / performance indices.
    /// Cp/Cpk use sigma_within (R̄/d2); Pp/Ppk use sigma_overall (sample stdev of all values).
    /// Returns null when the specification limits are degenerate (LSL == USL or both 0).
    /// Individual indices stay null when their sigma estimate is 0.
    /// </summary>
    public static SpcCapabilityDto? ComputeCapability(double? lsl, double? usl, double mu, double sigmaWithin, double sigmaOverall)
    {
        if (!lsl.HasValue || !usl.HasValue) return null;
        var lower = lsl.Value;
        var upper = usl.Value;
        if (lower == upper) return null;
        if (lower == 0 && upper == 0) return null;

        var dto = new SpcCapabilityDto
        {
            Lsl = lower,
            Usl = upper,
            Mu = mu,
            SigmaWithin = sigmaWithin,
            SigmaOverall = sigmaOverall
        };

        if (sigmaWithin > 0)
        {
            dto.Cp = (upper - lower) / (6.0 * sigmaWithin);
            dto.Cpk = Math.Min((upper - mu) / (3.0 * sigmaWithin), (mu - lower) / (3.0 * sigmaWithin));
        }

        if (sigmaOverall > 0)
        {
            dto.Pp = (upper - lower) / (6.0 * sigmaOverall);
            dto.Ppk = Math.Min((upper - mu) / (3.0 * sigmaOverall), (mu - lower) / (3.0 * sigmaOverall));
        }

        return dto;
    }

    // ══════════════════════════════════════════════════════════════════
    // Run rules: Nelson 1-8 + Western Electric zone rules
    // ══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Evaluates Nelson rules 1-8 and the Western Electric zone rules on a
    /// control-chart series. WE occurrences that exactly repeat an already
    /// reported violation (notably Nelson rule 1) are dropped.
    /// </summary>
    public static List<SpcViolationDto> EvaluateRules(IReadOnlyList<SpcPointDto> points)
    {
        var violations = new List<SpcViolationDto>();
        if (points == null || points.Count == 0) return violations;

        var count = points.Count;
        var val = new double?[count];
        var cl = new double?[count];
        var ucl = new double?[count];
        var lcl = new double?[count];

        for (var i = 0; i < count; i++)
        {
            var p = points[i];
            val[i] = p?.Value;
            cl[i] = p?.Cl;
            ucl[i] = p?.Ucl;
            lcl[i] = p?.Lcl;
        }

        bool Valid(int i)
            => val[i].HasValue && cl[i].HasValue && ucl[i].HasValue && lcl[i].HasValue;

        double? SigmaAt(int i)
        {
            if (!Valid(i)) return null;
            var sigma = (ucl[i]!.Value - cl[i]!.Value) / 3.0;
            return sigma > 0 ? sigma : null;
        }

        bool Above(int i, double zone)
        {
            var sigma = SigmaAt(i);
            if (!sigma.HasValue) return false;
            return val[i]!.Value > cl[i]!.Value + zone * sigma.Value;
        }

        bool Below(int i, double zone)
        {
            var sigma = SigmaAt(i);
            if (!sigma.HasValue) return false;
            return val[i]!.Value < cl[i]!.Value - zone * sigma.Value;
        }

        bool Within(int i, double zone)
        {
            var sigma = SigmaAt(i);
            if (!sigma.HasValue) return false;
            return Math.Abs(val[i]!.Value - cl[i]!.Value) <= zone * sigma.Value;
        }

        static void Add(List<SpcViolationDto> list, string id, string name, string description, IEnumerable<int> indexes)
        {
            var set = indexes.Distinct().OrderBy(x => x).ToList();
            if (set.Count == 0) return;
            list.Add(new SpcViolationDto { RuleId = id, RuleName = name, Description = description, PointIndexes = set });
        }

        static bool HasSameIndexes(List<SpcViolationDto> list, IEnumerable<int> indexes)
        {
            var set = indexes.Distinct().OrderBy(x => x).ToList();
            return list.Any(v => v.PointIndexes.Count == set.Count && v.PointIndexes.SequenceEqual(set));
        }

        // ── Nelson 1: one point beyond 3σ ──────────────────────────────
        var beyond = new List<int>();
        for (var i = 0; i < count; i++)
        {
            if (Valid(i) && (val[i]!.Value > ucl[i]!.Value || val[i]!.Value < lcl[i]!.Value)) beyond.Add(i);
        }
        Add(violations, "1", "Beyond Control Limit",
            "One point beyond 3 sigma (outside the control limits).", beyond);

        // ── Nelson 2: nine consecutive points on the same side of CL ───
        var runs = new List<int>();
        var side = 0;
        var runStart = 0;
        for (var i = 0; i <= count; i++)
        {
            var s = 0;
            if (i < count && Valid(i))
            {
                if (val[i]!.Value > cl[i]!.Value) s = 1;
                else if (val[i]!.Value < cl[i]!.Value) s = -1;
            }
            if (i < count && s == side && s != 0) continue;
            if (side != 0 && i - runStart >= 9)
            {
                for (var j = runStart; j < i; j++) runs.Add(j);
            }
            side = s;
            runStart = i;
        }
        Add(violations, "2", "Nine Points Same Side",
            "Nine consecutive points on the same side of the center line.", runs);

        // ── Nelson 3: six consecutive points trending ──────────────────
        var trend = new List<int>();
        for (var i = 0; i + 5 < count; i++)
        {
            var ok = true;
            for (var j = i; j < i + 6; j++)
            {
                if (!Valid(j)) { ok = false; break; }
            }
            if (!ok) continue;
            var up = true;
            var down = true;
            for (var j = i; j < i + 5; j++)
            {
                var a = val[j]!.Value;
                var b = val[j + 1]!.Value;
                if (!(b > a)) up = false;
                if (!(b < a)) down = false;
            }
            if (up || down)
            {
                for (var j = i; j <= i + 5; j++) trend.Add(j);
            }
        }
        Add(violations, "3", "Six Points Trending",
            "Six consecutive points steadily increasing or decreasing.", trend);

        // ── Nelson 4: fourteen alternating points ──────────────────────
        var alternation = new List<int>();
        for (var i = 0; i + 13 < count; i++)
        {
            var ok = true;
            for (var j = i; j < i + 14; j++)
            {
                if (!Valid(j)) { ok = false; break; }
            }
            if (!ok) continue;
            var pattern = 0;
            var matched = true;
            for (var j = i; j < i + 13; j++)
            {
                var diff = val[j + 1]!.Value - val[j]!.Value;
                var dir = diff > 0 ? 1 : diff < 0 ? -1 : 0;
                if (dir == 0) { matched = false; break; }
                if (pattern == 0) pattern = dir;
                else if (pattern != dir) { matched = false; break; }
                pattern = -pattern;
            }
            if (matched)
            {
                for (var j = i; j <= i + 13; j++) alternation.Add(j);
            }
        }
        Add(violations, "4", "Fourteen Points Alternating",
            "Fourteen consecutive points alternating up and down.", alternation);

        // ── Nelson 5 / WE-2: two of three beyond 2σ on the same side ───
        var twoOfThree = new List<int>();
        for (var i = 0; i + 2 < count; i++)
        {
            if (!Valid(i) || !Valid(i + 1) || !Valid(i + 2)) continue;
            var above = 0;
            var below = 0;
            for (var j = i; j < i + 3; j++)
            {
                if (Above(j, 2)) above++;
                if (Below(j, 2)) below++;
            }
            if (above >= 2 || below >= 2)
            {
                for (var j = i; j < i + 3; j++) twoOfThree.Add(j);
            }
        }
        Add(violations, "5", "Two of Three Beyond 2 Sigma",
            "Two out of three consecutive points beyond 2 sigma on the same side.", twoOfThree);
        Add(violations, "WE-2", "Two of Three Beyond 2 Sigma (WE)",
            "Western Electric zone rule: two of three consecutive points beyond 2 sigma on the same side.", twoOfThree);

        // ── Nelson 6 / WE-3: four of five beyond 1σ on the same side ───
        var fourOfFive = new List<int>();
        for (var i = 0; i + 4 < count; i++)
        {
            var ok = true;
            for (var j = i; j < i + 5; j++)
            {
                if (!Valid(j)) { ok = false; break; }
            }
            if (!ok) continue;
            var above = 0;
            var below = 0;
            for (var j = i; j < i + 5; j++)
            {
                if (Above(j, 1)) above++;
                if (Below(j, 1)) below++;
            }
            if (above >= 4 || below >= 4)
            {
                for (var j = i; j < i + 5; j++) fourOfFive.Add(j);
            }
        }
        Add(violations, "6", "Four of Five Beyond 1 Sigma",
            "Four out of five consecutive points beyond 1 sigma on the same side.", fourOfFive);
        Add(violations, "WE-3", "Four of Five Beyond 1 Sigma (WE)",
            "Western Electric zone rule: four of five consecutive points beyond 1 sigma on the same side.", fourOfFive);

        // ── Nelson 7: fifteen points within 1σ ─────────────────────────
        var fifteenWithin = new List<int>();
        for (var i = 0; i + 14 < count; i++)
        {
            var ok = true;
            for (var j = i; j < i + 15; j++)
            {
                if (!Valid(j) || !Within(j, 1)) { ok = false; break; }
            }
            if (!ok) continue;
            for (var j = i; j < i + 15; j++) fifteenWithin.Add(j);
        }
        Add(violations, "7", "Fifteen Points Within 1 Sigma",
            "Fifteen consecutive points within 1 sigma of the center line.", fifteenWithin);

        // ── Nelson 8: eight points beyond 1σ on the same side ──────────
        var eightBeyond = new List<int>();
        for (var i = 0; i + 7 < count; i++)
        {
            var okAbove = true;
            var okBelow = true;
            for (var j = i; j < i + 8; j++)
            {
                if (!Valid(j) || !Above(j, 1)) okAbove = false;
                if (!Valid(j) || !Below(j, 1)) okBelow = false;
            }
            if (okAbove || okBelow)
            {
                for (var j = i; j < i + 8; j++) eightBeyond.Add(j);
            }
        }
        Add(violations, "8", "Eight Points Beyond 1 Sigma",
            "Eight consecutive points beyond 1 sigma on the same side.", eightBeyond);

        // ── WE-1: beyond 3σ (exact repeat of Nelson 1 is deduplicated) ─
        if (beyond.Count > 0 && !HasSameIndexes(violations, beyond))
        {
            Add(violations, "WE-1", "Beyond Control Limit (WE)",
                "Western Electric zone rule: one point beyond 3 sigma.", beyond);
        }

        // ── WE-4: eight points within 1σ ───────────────────────────────
        var eightWithin = new List<int>();
        for (var i = 0; i + 7 < count; i++)
        {
            var ok = true;
            for (var j = i; j < i + 8; j++)
            {
                if (!Valid(j) || !Within(j, 1)) { ok = false; break; }
            }
            if (!ok) continue;
            for (var j = i; j < i + 8; j++) eightWithin.Add(j);
        }
        Add(violations, "WE-4", "Eight Points Within 1 Sigma (WE)",
            "Western Electric zone rule: eight consecutive points within 1 sigma of the center line.", eightWithin);

        return violations;
    }

    // ══════════════════════════════════════════════════════════════════
    // Service operations
    // ══════════════════════════════════════════════════════════════════

    public async Task<SpcAnalysisDto> AnalyzeAsync(SpcQueryDto query, CancellationToken ct = default)
    {
        query ??= new SpcQueryDto();
        var tenantId = _tenant.TenantId;

        var resultsQuery = _db.InspectionResults.AsNoTracking().Where(r => r.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(query.Characteristic))
        {
            var characteristic = query.Characteristic.Trim();
            resultsQuery = resultsQuery.Where(r => r.Characteristic == characteristic);
        }
        if (query.FromDate.HasValue)
        {
            var from = query.FromDate.Value;
            resultsQuery = resultsQuery.Where(r => r.CreatedAt >= from);
        }
        if (query.ToDate.HasValue)
        {
            var to = query.ToDate.Value;
            var toExclusive = to.TimeOfDay == TimeSpan.Zero ? to.AddDays(1) : to;
            resultsQuery = resultsQuery.Where(r => r.CreatedAt < toExclusive);
        }

        var results = await resultsQuery.OrderBy(r => r.CreatedAt).ToListAsync(ct);
        if (results.Count == 0) return new SpcAnalysisDto();

        var lotNumbers = results.Select(r => r.LotNumber).Distinct().ToList();
        var lots = await _db.InspectionLots.AsNoTracking()
            .Where(l => l.TenantId == tenantId && lotNumbers.Contains(l.LotNumber))
            .ToListAsync(ct);

        var lotMap = new Dictionary<string, (string Plant, string MaterialCode, string MaterialName)>(StringComparer.Ordinal);
        foreach (var lot in lots)
        {
            if (!lotMap.ContainsKey(lot.LotNumber))
                lotMap[lot.LotNumber] = (lot.Plant, lot.MaterialCode, lot.MaterialName);
        }

        if (!string.IsNullOrWhiteSpace(query.MaterialCode))
        {
            var materialCode = query.MaterialCode.Trim();
            results = results
                .Where(r => lotMap.TryGetValue(r.LotNumber, out var meta) && meta.MaterialCode == materialCode)
                .ToList();
        }
        if (!string.IsNullOrWhiteSpace(query.Plant))
        {
            var plant = query.Plant.Trim();
            results = results
                .Where(r => lotMap.TryGetValue(r.LotNumber, out var meta) && meta.Plant == plant)
                .ToList();
        }
        if (results.Count == 0) return new SpcAnalysisDto();

        // One characteristic per chart: the requested one, otherwise the densest series.
        var selectedCharacteristic = query.Characteristic?.Trim();
        if (string.IsNullOrWhiteSpace(selectedCharacteristic))
        {
            selectedCharacteristic = results
                .Where(r => !string.IsNullOrWhiteSpace(r.Characteristic))
                .GroupBy(r => r.Characteristic)
                .OrderByDescending(g => g.Count())
                .ThenBy(g => g.Key, StringComparer.Ordinal)
                .Select(g => g.Key)
                .FirstOrDefault();
        }
        if (string.IsNullOrWhiteSpace(selectedCharacteristic)) return new SpcAnalysisDto();

        results = results.Where(r => r.Characteristic == selectedCharacteristic).ToList();
        if (results.Count == 0) return new SpcAnalysisDto();

        var unit = results
            .Where(r => !string.IsNullOrWhiteSpace(r.Unit))
            .GroupBy(r => r.Unit)
            .OrderByDescending(g => g.Count())
            .ThenBy(g => g.Key, StringComparer.Ordinal)
            .Select(g => g.Key)
            .FirstOrDefault() ?? "";

        // Subgroups: one per inspection lot, ordered by the lot's first result.
        var subgroups = new List<SpcSubgroupDto>();
        var lotGroups = results.GroupBy(r => r.LotNumber).ToList();
        foreach (var group in lotGroups)
        {
            var ordered = group.OrderBy(r => r.CreatedAt).ToList();
            var values = ordered.Select(r => r.MeasuredValue).ToList();
            subgroups.Add(BuildSubgroup(0, group.Key, ordered[0].CreatedAt, values));
        }
        subgroups = subgroups.OrderBy(s => s.Timestamp).ToList();
        for (var i = 0; i < subgroups.Count; i++)
        {
            subgroups[i].Index = i + 1;
            subgroups[i].Label = subgroups[i].LotNumber;
        }

        var rBar = ComputeRBar(subgroups);
        var sBar = ComputeSBar(subgroups);

        var characteristicTitle = selectedCharacteristic;
        var xbarChart = BuildXbarChart(subgroups, "X̄ Chart — " + characteristicTitle, unit);
        var rChart = BuildRangeChart(subgroups, "R Chart — " + characteristicTitle, unit);
        var sChart = BuildSChart(subgroups, "S Chart — " + characteristicTitle, unit);

        // Capability
        var allValues = results.Select(r => (double)r.MeasuredValue).ToList();
        var overallMean = 0.0;
        foreach (var v in allValues) overallMean += v;
        overallMean /= allValues.Count;
        var sigmaOverall = SampleStdDev(allValues);

        var lsl = (double?)results.Min(r => r.TargetMin);
        var usl = (double?)results.Max(r => r.TargetMax);

        var meanN = 0.0;
        foreach (var s in subgroups) meanN += s.N;
        meanN /= subgroups.Count;
        var d2 = GetD2((int)Math.Round(meanN));
        var sigmaWithin = d2 > 0 ? rBar / d2 : 0;

        var capability = ComputeCapability(lsl, usl, overallMean, sigmaWithin, sigmaOverall);

        var violations = EvaluateRules(xbarChart.Points);

        return new SpcAnalysisDto
        {
            Characteristic = selectedCharacteristic,
            Unit = unit,
            MaterialCode = string.IsNullOrWhiteSpace(query.MaterialCode) ? null : query.MaterialCode.Trim(),
            Plant = string.IsNullOrWhiteSpace(query.Plant) ? null : query.Plant.Trim(),
            SubgroupCount = subgroups.Count,
            ResultCount = results.Count,
            Subgroups = subgroups,
            Charts = new SpcChartsDto { Xbar = xbarChart, R = rChart, S = sChart },
            Capability = capability,
            Violations = violations,
            OutOfControl = violations.Count > 0
        };
    }

    public async Task<SpcFilterOptionsDto> GetFilterOptionsAsync(CancellationToken ct = default)
    {
        var tenantId = _tenant.TenantId;
        var dto = new SpcFilterOptionsDto();

        var characteristics = await _db.InspectionResults.AsNoTracking()
            .Where(r => r.TenantId == tenantId)
            .Select(r => r.Characteristic)
            .Distinct()
            .ToListAsync(ct);
        dto.Characteristics = characteristics
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Select(c => c.Trim())
            .Distinct(StringComparer.Ordinal)
            .OrderBy(c => c, StringComparer.Ordinal)
            .ToList();

        var lotFacts = await _db.InspectionLots.AsNoTracking()
            .Where(l => l.TenantId == tenantId)
            .Select(l => new { l.MaterialCode, l.Plant })
            .Distinct()
            .ToListAsync(ct);

        dto.MaterialCodes = lotFacts
            .Select(l => l.MaterialCode)
            .Where(m => !string.IsNullOrWhiteSpace(m))
            .Select(m => m.Trim())
            .Distinct(StringComparer.Ordinal)
            .OrderBy(m => m, StringComparer.Ordinal)
            .ToList();

        dto.Plants = lotFacts
            .Select(l => l.Plant)
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => p.Trim())
            .Distinct(StringComparer.Ordinal)
            .OrderBy(p => p, StringComparer.Ordinal)
            .ToList();

        return dto;
    }
}
