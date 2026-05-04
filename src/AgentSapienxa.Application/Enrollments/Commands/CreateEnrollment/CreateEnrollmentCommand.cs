using MediatR;

namespace AgentSapienxa.Application.Enrollments.Commands.CreateEnrollment;

public record CreateEnrollmentCommand(
    string PhoneNumber,
    Guid CatalogItemId) : IRequest<CreateEnrollmentResult>;

public record CreateEnrollmentResult(Guid EnrollmentId, bool AlreadyEnrolled, string Message);
