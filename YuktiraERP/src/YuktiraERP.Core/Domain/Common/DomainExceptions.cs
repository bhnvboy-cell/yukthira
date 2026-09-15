namespace YuktiraERP.Core.Domain.Common;
public class DuplicateEntityException : Exception
{
    public DuplicateEntityException(string entity, string key) 
        : base($"Duplicate {entity} detected for key '{key}'.") { }
}
