namespace AgentSapienxa.Domain.Admin;

public static class AdminRoles
{
    public const string Superadmin = "superadmin";
    public const string Admin = "admin";
    public const string Editor = "editor";

    public static bool IsValid(string role) =>
        role == Superadmin || role == Admin || role == Editor;

    public static bool RequiresCompany(string role) =>
        role == Admin || role == Editor;
}
