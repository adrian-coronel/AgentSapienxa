using AgentSapienxa.Domain.Enrollments;
using FluentAssertions;

namespace AgentSapienxa.UnitTests.Enrollments;

public class EnrollmentStatusTests
{
    [Theory]
    [InlineData(EnrollmentStatus.Interesado, EnrollmentStatus.PendientePago, true)]
    [InlineData(EnrollmentStatus.Interesado, EnrollmentStatus.EscaladoAHumano, true)]
    [InlineData(EnrollmentStatus.Interesado, EnrollmentStatus.Inactivo, true)]
    [InlineData(EnrollmentStatus.Interesado, EnrollmentStatus.Pagado, false)]
    [InlineData(EnrollmentStatus.PendientePago, EnrollmentStatus.Pagado, true)]
    [InlineData(EnrollmentStatus.PendientePago, EnrollmentStatus.EscaladoAHumano, true)]
    [InlineData(EnrollmentStatus.PendientePago, EnrollmentStatus.Inactivo, true)]
    [InlineData(EnrollmentStatus.Pagado, EnrollmentStatus.Inactivo, false)]
    [InlineData(EnrollmentStatus.Inactivo, EnrollmentStatus.Interesado, false)]
    public void CanTransitionTo_ReturnsExpected(string from, string to, bool expected)
    {
        EnrollmentStatus.CanTransitionTo(from, to).Should().Be(expected);
    }

    [Fact]
    public void Enrollment_Escalate_SetsStatusAndAgent()
    {
        var enrollment = Enrollment.Create(Guid.NewGuid(), Guid.NewGuid());
        var agentId = Guid.NewGuid();

        enrollment.Escalate(agentId, "Solicita hablar con asesor");

        enrollment.Status.Should().Be(EnrollmentStatus.EscaladoAHumano);
        enrollment.SaleAgentId.Should().Be(agentId);
        enrollment.Observation.Should().Be("Solicita hablar con asesor");
    }
}
