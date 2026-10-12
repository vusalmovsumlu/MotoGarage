namespace MotoGarage.Application.DTOs.Admin;

public record AdminAuditDto(long Id, int AdminId, string AdminName, string Action,
    string TargetType, int TargetId, string TargetName, string? Reason, DateTime OccurredAtUtc);
public record AdminAuditPage(int Total, int Page, int PageSize, List<AdminAuditDto> Items);
