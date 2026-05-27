namespace AgentSapienxa.Domain.Admin;

public class AdminUser
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Name { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string Role { get; private set; } = "admin";
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    private AdminUser() { }

    public static AdminUser Create(string name, string email, string passwordHash, string role = "admin")
    {
        return new AdminUser
        {
            Name = name,
            Email = email,
            PasswordHash = passwordHash,
            Role = role,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void UpdatePassword(string newPasswordHash)
    {
        PasswordHash = newPasswordHash;
    }

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
    }
}
