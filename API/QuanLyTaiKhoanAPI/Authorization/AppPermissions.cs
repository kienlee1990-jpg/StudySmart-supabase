namespace QuanLyTaiKhoanAPI.Authorization;

public static class AppPermissions
{
    public const string ManageUsers = "ManageUsers";
    public const string ManagePermissions = "ManagePermissions";
    public const string ResetUserPasswords = "ResetUserPasswords";
    public const string ViewSystemLogs = "ViewSystemLogs";
    public const string ManageQuizContent = "ManageQuizContent";

    public static readonly string[] All =
    {
        ManageUsers,
        ManagePermissions,
        ResetUserPasswords,
        ViewSystemLogs,
        ManageQuizContent
    };
}
