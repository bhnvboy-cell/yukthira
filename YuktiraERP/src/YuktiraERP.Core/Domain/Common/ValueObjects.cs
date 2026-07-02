namespace YuktiraERP.Core.Domain.Common;

public record Address(string Line1, string Line2, string City, string State, string Country, string PostalCode);
public record Money(decimal Amount, string Currency = "USD");
public record Quantity(decimal Value, string Uom = "EA");
public record AuditEntry(string Field, string OldValue, string NewValue);
