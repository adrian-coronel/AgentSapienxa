using AgentSapienxa.Domain.Common;

namespace AgentSapienxa.Domain.Catalog;

public class CatalogItem : Entity
{
    public Guid CompanyId { get; private set; }
    public string? Code { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? ShortDescription { get; private set; }
    public string? Features { get; private set; }
    public string? Details { get; private set; }
    public string? Syllabus { get; private set; }
    public string? Projects { get; private set; }
    public string? Link { get; private set; }
    public Guid? InstructorId { get; private set; }
    public decimal Cost { get; private set; }
    public string? Places { get; private set; }
    public string? AvailablePlaces { get; private set; }
    public DateOnly? StartDate { get; private set; }

    public Instructor? Instructor { get; private set; }

    private CatalogItem() { }

    public void AssignToCompany(Guid companyId) => CompanyId = companyId;

    public static CatalogItem Create(
        string title,
        decimal cost,
        string? code = null,
        string? shortDescription = null,
        string? features = null,
        string? details = null,
        string? syllabus = null,
        string? projects = null,
        string? link = null,
        Guid? instructorId = null,
        string? places = null,
        string? availablePlaces = null,
        DateOnly? startDate = null,
        Guid? companyId = null)
    {
        return new CatalogItem
        {
            CompanyId = companyId ?? Guid.Empty,
            Title = title,
            Cost = cost,
            Code = code,
            ShortDescription = shortDescription,
            Features = features,
            Details = details,
            Syllabus = syllabus,
            Projects = projects,
            Link = link,
            InstructorId = instructorId,
            Places = places,
            AvailablePlaces = availablePlaces,
            StartDate = startDate
        };
    }
}
