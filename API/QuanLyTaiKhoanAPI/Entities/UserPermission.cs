namespace QuanLyTaiKhoanAPI.Entities
{
    public class UserPermission
    {
        public string UserId { get; set; } = null!;
        public ApplicationUser User { get; set; } = null!;

        public int PermissionId { get; set; }
        public Permission Permission { get; set; } = null!;

        public bool IsGranted { get; set; } // true: thêm quy?n, false: ch?n
    }
}
