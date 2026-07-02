using System.ComponentModel.DataAnnotations;

namespace YuktiraERP.Web.Models;

public class MaterialMaster
{
    [Required, StringLength(20)]
    public string Code { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(20)]
    public string Type { get; set; } = "RAW";

    [Required, StringLength(10)]
    public string UOM { get; set; } = "EA";

    [Range(0, 999999)]
    public decimal Stock { get; set; }

    [Range(0, 999999.99)]
    public decimal Price { get; set; }

    [Required, StringLength(20)]
    public string Status { get; set; } = "Active";
}
