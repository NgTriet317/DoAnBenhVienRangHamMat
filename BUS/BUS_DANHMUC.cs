using DAL;
using ET;

namespace BUS;

/// <summary>
/// Đầu mối đọc và lưu danh mục hệ thống theo định nghĩa cho phép; DAL quản lý quyền, phiên và giao dịch.
/// </summary>
public sealed class BUS_DANHMUC
{
    private readonly DAL_DANHMUC dal=new();
    /// <summary>
    /// Tra danh mục theo từ khóa và trạng thái; kết quả kèm dấu vân tay để đối chiếu khi sửa.
    /// </summary>
    public IReadOnlyList<Dictionary<string,object?>> Lay(string bang,string search,string status,NguCanhNghiepVu actor)
        =>dal.Lay(bang,search,status,actor);
    /// <summary>
    /// Chuyển yêu cầu thêm, sửa hoặc đổi trạng thái danh mục tới DAL cùng bản gốc và phiên đăng nhập.
    /// </summary>
    /// <param name="bang">Tên bảng thuộc danh mục hệ thống cho phép.</param>
    /// <param name="values">Các giá trị trường nhập cần chuẩn hóa theo định nghĩa danh mục.</param>
    /// <param name="original">Bản đã đọc kèm DauVanTay khi sửa; null khi thêm mới.</param>
    /// <param name="actor">Người thao tác được DAL xác thực lại với quyền quản trị.</param>
    /// <param name="session">Phiên đăng nhập đang mở của người thao tác.</param>
    /// <param name="active">Có giá trị để chỉ đổi trạng thái; null để lưu nội dung.</param>
    /// <returns>Bản vừa lưu kèm dấu vân tay mới để tiếp tục đối chiếu.</returns>
    public Dictionary<string,object?> Luu(string bang,IDictionary<string,object?> values,IDictionary<string,object?>? original,NguCanhNghiepVu actor,Guid session,bool? active=null)
        =>dal.Luu(bang,values,original,actor,session,active);
}
