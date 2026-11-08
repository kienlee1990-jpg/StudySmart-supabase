using QuanLyTaiKhoanAPI.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace QuanLyTaiKhoanAPI.Interfaces
{
    public interface IPermissionService
    {
        Task<List<string>> GetPermissionsAsync(ApplicationUser user);
    }
}
