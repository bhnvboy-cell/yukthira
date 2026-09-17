namespace YuktiraERP.Core.Dtos;

public class SecurityImportRowDto
{
    public string Module { get; set; } = "";
    public string SubProcess { get; set; } = "";
    public string AppsTcodes { get; set; } = "";
    public string AppDescription { get; set; } = "";
    public string MasterRole { get; set; } = "";
    public string Catalog { get; set; } = "";
    public string Space { get; set; } = "";
}

public class CompositeRoleImportRowDto
{
    public string Module { get; set; } = "";
    public string CompositeRole { get; set; } = "";
    public string DerivedRole { get; set; } = "";
    public string MasterRole { get; set; } = "";
}

public class SecurityImportResultDto
{
    public bool Success { get; set; }
    public int MasterRolesCreated { get; set; }
    public int CompositeRolesCreated { get; set; }
    public int DerivedRolesCreated { get; set; }
    public int TCodeAssignmentsCreated { get; set; }
    public int Errors { get; set; }
    public List<SecurityImportErrorDto> ErrorDetails { get; set; } = new();
    public string BatchNumber { get; set; } = "";
    public long ElapsedMs { get; set; }
}

public class SecurityImportErrorDto
{
    public int RowNumber { get; set; }
    public string Column { get; set; } = "";
    public string ErrorCode { get; set; } = "";
    public string Message { get; set; } = "";
}

public class MasterRoleDto
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Module { get; set; } = "";
    public string SubProcess { get; set; } = "";
    public string Catalog { get; set; } = "";
    public string Space { get; set; } = "";
    public string Description { get; set; } = "";
    public string Status { get; set; } = "Active";
}

public class CompositeRoleDto
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Module { get; set; } = "";
    public string Description { get; set; } = "";
    public string Status { get; set; } = "Active";
}

public class DerivedRoleDto
{
    public string DerivedRoleId { get; set; } = "";
    public string DerivedRoleName { get; set; } = "";
    public string MasterRoleId { get; set; } = "";
    public List<TCodeAssignmentDto> TCodeAssignments { get; set; } = new();
}

public class TCodeAssignmentDto
{
    public string TransactionCode { get; set; } = "";
    public string AppDescription { get; set; } = "";
    public bool HasAccess { get; set; }
}

public class RoleHierarchyDto
{
    public string CompositeRoleId { get; set; } = "";
    public string CompositeRoleName { get; set; } = "";
    public List<DerivedRoleDto> DerivedRoles { get; set; } = new();
}

public class RoleMatrixImportRequest
{
    public List<SecurityImportRowDto> MasterRoleRows { get; set; } = new();
    public List<CompositeRoleImportRowDto> CompositeRoleRows { get; set; } = new();
}

public class UserRoleAssignRequest
{
    public string UserId { get; set; } = "";
    public string CompositeRoleId { get; set; } = "";
}

public class UserRoleAssignResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public int PermissionsGranted { get; set; }
}

public class UserRoleAssignmentDto
{
    public string UserId { get; set; } = "";
    public string CompositeRoleId { get; set; } = "";
    public string CompositeRoleName { get; set; } = "";
    public DateTime AssignedAt { get; set; }
}

public class RoleTCodeAssignmentDto
{
    public string TransactionCode { get; set; } = "";
    public string Description { get; set; } = "";
    public string Module { get; set; } = "";
    public bool HasAccess { get; set; }
}

public class DataSyncV2UploadResultDto
{
    public bool Success { get; set; }
    public string Module { get; set; } = "";
    public int TotalRows { get; set; }
    public int ValidRows { get; set; }
    public int ErrorRows { get; set; }
    public List<ImportRowErrorDto> Errors { get; set; } = new();
    public string SessionToken { get; set; } = "";
}
