using AgentSapienxa.Domain.Common;

namespace AgentSapienxa.Domain.Catalog;

public class Instructor : Entity
{
    public Guid CompanyId { get; private set; }
    public string InstructorName { get; private set; } = string.Empty;
    public string? Email { get; private set; }
    public string? PhoneNumber { get; private set; }
    public string? ProfilePicture { get; private set; }
    public string? Expertise { get; private set; }
    public string? InstructorSummary { get; private set; }

    private Instructor() { }

    public void AssignToCompany(Guid companyId) => CompanyId = companyId;

    public static Instructor Create(
        string name,
        string? email = null,
        string? phone = null,
        string? picture = null,
        string? expertise = null,
        string? summary = null,
        Guid? companyId = null)
    {
        return new Instructor
        {
            CompanyId = companyId ?? Guid.Empty,
            InstructorName = name,
            Email = email,
            PhoneNumber = phone,
            ProfilePicture = picture,
            Expertise = expertise,
            InstructorSummary = summary
        };
    }
}
