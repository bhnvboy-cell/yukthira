using System;

namespace YuktiraERP.Core.Security
{
    public static class SecurityRoleRank
    {
        public const string SuperUser = "SUPER_USER";
        public const string Admin = "ADMIN";
        public const string PowerUser = "POWER_USER";
        public const string NormalUser = "NORMAL_USER";
        public const string ReadOnly = "READ_ONLY";

        public static int GetRank(string? role)
        {
            if (string.IsNullOrWhiteSpace(role)) return 0;
            return role.Trim().ToUpperInvariant() switch
            {
                SuperUser => 100,
                Admin => 80,
                PowerUser => 60,
                NormalUser => 40,
                ReadOnly => 20,
                _ => 0
            };
        }

        public static bool IsSuperUser(string? role) =>
            string.Equals(role?.Trim(), SuperUser, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Returns true when the user's role rank satisfies the required role.
        /// SUPER_USER always passes. An empty/unknown requirement is treated as NORMAL_USER.
        /// </summary>
        public static bool Meets(string? userRole, string? requiredRole)
        {
            if (IsSuperUser(userRole)) return true;
            var required = GetRank(requiredRole);
            if (required == 0) required = GetRank(NormalUser);
            return GetRank(userRole) >= required;
        }

        public static string Describe(string? role) =>
            string.IsNullOrWhiteSpace(role) ? "unknown" : role.Trim().ToUpperInvariant();
    }
}
