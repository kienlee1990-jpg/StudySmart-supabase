using Microsoft.AspNetCore.Authorization;

namespace QuanLyTaiKhoanAPI.Authorization
{
    public class PermissionAttribute : AuthorizeAttribute
    {
        public PermissionAttribute(string permission)
        {
            Policy = permission;
        }
    }
}
