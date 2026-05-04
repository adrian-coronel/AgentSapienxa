using MediatR;

namespace AgentSapienxa.Application.Leads.Commands.CaptureLead;

public record CaptureLeadCommand(
    string PhoneNumber,
    string? Name,
    string? Email,
    string? ContactMethod) : IRequest<CaptureLeadResult>;

public record CaptureLeadResult(Guid LeadId, bool IsNew, string Message);
