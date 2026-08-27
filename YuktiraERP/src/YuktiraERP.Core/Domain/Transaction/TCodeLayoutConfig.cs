namespace YuktiraERP.Core.Domain.Transaction;

public class TCodeLayoutConfig
{
    public string TCode { get; set; } = "";
    public string Title { get; set; } = "";
    public string Module { get; set; } = "";
    public string Icon { get; set; } = "bi-asterisk";
    public List<ToolbarAction> ToolbarActions { get; set; } = new();
    public List<MetadataField> Metadata { get; set; } = new();
    public List<TabConfig> Tabs { get; set; } = new();
    public List<ColumnDef> Columns { get; set; } = new();
    public List<FooterAction> FooterActions { get; set; } = new();
    public TableToolbarConfig TableToolbar { get; set; } = new();
    public Dictionary<string, object> Options { get; set; } = new();
}

public class ToolbarAction
{
    public string Id { get; set; } = "";
    public string Label { get; set; } = "";
    public string Icon { get; set; } = "";
    public string Style { get; set; } = "default";
    public string Handler { get; set; } = "";
    public bool Disabled { get; set; }
    public string Tooltip { get; set; } = "";
}

public class MetadataField
{
    public string Label { get; set; } = "";
    public string Value { get; set; } = "";
    public string Key { get; set; } = "";
    public string Type { get; set; } = "text";
    public bool Editable { get; set; }
    public string Width { get; set; } = "";
    public List<MetadataField> Group { get; set; } = new();
}

public class TabConfig
{
    public string Id { get; set; } = "";
    public string Label { get; set; } = "";
    public string Icon { get; set; } = "";
    public bool Active { get; set; }
    public List<ColumnDef> Columns { get; set; } = new();
    public string Content { get; set; } = "";
}

public class ColumnDef
{
    public string Key { get; set; } = "";
    public string Label { get; set; } = "";
    public string Type { get; set; } = "text";
    public bool Editable { get; set; }
    public int Width { get; set; }
    public int MinWidth { get; set; }
    public bool Sortable { get; set; } = true;
    public bool Filterable { get; set; }
    public bool Fixed { get; set; }
    public string Align { get; set; } = "left";
    public string Format { get; set; } = "";
    public List<DropdownOption> Options { get; set; } = new();
    public ColumnValidation? Validation { get; set; }
    public string DefaultValue { get; set; } = "";
    public string Tooltip { get; set; } = "";
    public bool Required { get; set; }
    public string DependsOn { get; set; } = "";
    public string RenderAs { get; set; } = "";
}

public class DropdownOption
{
    public string Value { get; set; } = "";
    public string Label { get; set; } = "";
    public string Color { get; set; } = "";
}

public class ColumnValidation
{
    public decimal? Min { get; set; }
    public decimal? Max { get; set; }
    public string Pattern { get; set; } = "";
    public string Message { get; set; } = "";
    public bool Required { get; set; }
}

public class FooterAction
{
    public string Id { get; set; } = "";
    public string Label { get; set; } = "";
    public string Icon { get; set; } = "";
    public string Style { get; set; } = "primary";
    public string Handler { get; set; } = "";
    public bool Disabled { get; set; }
    public bool Confirm { get; set; }
    public string ConfirmMessage { get; set; } = "";
}

public class TableToolbarConfig
{
    public bool ShowSearch { get; set; } = true;
    public bool ShowFilter { get; set; } = true;
    public bool ShowExport { get; set; } = true;
    public bool ShowAddRow { get; set; } = true;
    public bool ShowDeleteRow { get; set; } = true;
    public bool ShowColumnChooser { get; set; }
    public List<ToolbarAction> CustomActions { get; set; } = new();
}
