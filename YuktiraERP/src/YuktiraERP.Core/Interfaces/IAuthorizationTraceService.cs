using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace YuktiraERP.Core.Interfaces
{
    public class AuthorizationTraceEntry
    {
        public Guid TenantId { get; set; }
        public Guid? UserId { get; set; }
        public string UserName { get; set; } = "";
        public string Role { get; set; } = "";
        public string SessionId { get; set; } = "";
        public string CorrelationId { get; set; } = "";
        public string ResourceType { get; set; } = "Page";
        public string Resource { get; set; } = "";
        public string Decision { get; set; } = "Allow";
        public string RuleSource { get; set; } = "";
        public string Reason { get; set; } = "";
        public string HttpMethod { get; set; } = "";
        public string HttpPath { get; set; } = "";
        public string IpAddress { get; set; } = "";
        public string UserAgent { get; set; } = "";
    }

    public class AuthorizationTraceQuery
    {
        public Guid? UserId { get; set; }
        public string? UserName { get; set; }
        public string? Decision { get; set; }
        public string? ResourceType { get; set; }
        public string? Resource { get; set; }
        public string? RuleSource { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }

    public class AuthorizationTraceDto
    {
        public Guid Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public Guid? UserId { get; set; }
        public string UserName { get; set; } = "";
        public string Role { get; set; } = "";
        public string ResourceType { get; set; } = "";
        public string Resource { get; set; } = "";
        public string Decision { get; set; } = "";
        public string RuleSource { get; set; } = "";
        public string Reason { get; set; } = "";
        public string CorrelationId { get; set; } = "";
        public string HttpMethod { get; set; } = "";
        public string HttpPath { get; set; } = "";
        public string IpAddress { get; set; } = "";
    }

    public class AuthorizationTraceResult
    {
        public List<AuthorizationTraceDto> Entries { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }

    public interface IAuthorizationTraceService
    {
        Task TraceAsync(AuthorizationTraceEntry entry);
        Task<AuthorizationTraceResult> QueryAsync(AuthorizationTraceQuery query, Guid? tenantId);
        Task ExportToCsvAsync(Guid? tenantId, Stream stream, AuthorizationTraceQuery? filter = null);
        Task<int> PurgeAsync(Guid? tenantId, DateTime? olderThan = null);
    }
}
