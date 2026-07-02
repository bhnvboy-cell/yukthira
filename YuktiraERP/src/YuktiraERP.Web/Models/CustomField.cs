using System.ComponentModel.DataAnnotations;
namespace YuktiraERP.Web.Models;
public class CustomField
{
    [Required, StringLength(20)] public string Module { get; set; } = string.Empty;
    [Required, StringLength(50)] public string EntityName { get; set; } = string.Empty;
    [Required, StringLength(50)] public string FieldName { get; set; } = string.Empty;
    [StringLength(20)] public string FieldType { get; set; } = "Text";
    public bool IsRequired { get; set; }
    [StringLength(50)] public string DefaultValue { get; set; } = string.Empty;
}
