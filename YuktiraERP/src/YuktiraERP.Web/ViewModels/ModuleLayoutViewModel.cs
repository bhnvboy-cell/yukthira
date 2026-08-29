namespace YuktiraERP.Web.ViewModels;

public class ModuleLayoutViewModel
{
    public string ModuleTitle { get; set; } = "";
    public string ModuleCode { get; set; } = "";
    public List<KpiCard> Kpis { get; set; } = new();
    public List<TabItem> Tabs { get; set; } = new();
    public string ActiveTab { get; set; } = "";
    public PrimaryAction? PrimaryAction { get; set; }
    public List<GridTab> GridTabs { get; set; } = new();
    public int PageSize { get; set; } = 25;
    public int CurrentPage { get; set; } = 1;
}

public class KpiCard
{
    public string Label { get; set; } = "";
    public string Value { get; set; } = "";
    public string? Subtext { get; set; }
    public string Type { get; set; } = "default";
}

public class TabItem
{
    public string Key { get; set; } = "";
    public string Label { get; set; } = "";
    public int Count { get; set; }
    public PrimaryAction? PrimaryAction { get; set; }
}

public class PrimaryAction
{
    public string Label { get; set; } = "";
    public string Href { get; set; } = "#";
    public string Icon { get; set; } = "bi-plus-lg";
}

public class GridTab
{
    public string Key { get; set; } = "";
    public List<GridColumn> Columns { get; set; } = new();
    public List<Dictionary<string, string?>> Rows { get; set; } = new();
    public Func<Dictionary<string, string?>, string?>? ViewHref { get; set; }
    public Func<Dictionary<string, string?>, string?>? EditHref { get; set; }
}

public class GridColumn
{
    public string Key { get; set; } = "";
    public string Label { get; set; } = "";
    public string Type { get; set; } = "text";
    public string Align { get; set; } = "left";
    public bool Visible { get; set; } = true;
    public string? Format { get; set; }
}
