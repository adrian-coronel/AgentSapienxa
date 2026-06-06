namespace AgentSapienxa.Domain.Admin;

public class AdminUser
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid? CompanyId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string Role { get; private set; } = AdminRoles.Admin;
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    private AdminUser() { }

    public static AdminUser Create(string name, string email, string passwordHash, string role = AdminRoles.Admin, Guid? companyId = null)
    {
        ValidateRoleAndCompany(role, companyId);

        return new AdminUser
        {
            CompanyId = companyId,
            Name = name,
            Email = email,
            PasswordHash = passwordHash,
            Role = role,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void AssignToCompany(Guid companyId) => CompanyId = companyId;

    public void ChangeRole(string newRole, Guid? newCompanyId)
    {
        ValidateRoleAndCompany(newRole, newCompanyId);
        Role = newRole;
        CompanyId = newCompanyId;
    }

    public void UpdatePassword(string newPasswordHash)
    {
        PasswordHash = newPasswordHash;
    }

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
    }

    private static void ValidateRoleAndCompany(string role, Guid? companyId)
    {
        if (!AdminRoles.IsValid(role))
            throw new ArgumentException($"Rol inválido: '{role}'. Valores permitidos: superadmin, admin, editor.", nameof(role));

        if (role == AdminRoles.Superadmin && companyId is not null)
            throw new ArgumentException("Un superadmin no debe estar asociado a una empresa.", nameof(companyId));

        if (AdminRoles.RequiresCompany(role) && companyId is null)
            throw new ArgumentException($"El rol '{role}' requiere una empresa asignada.", nameof(companyId));
    }
}
