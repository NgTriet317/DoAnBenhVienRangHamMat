using DAL;
using ET;

namespace BUS;

/// <summary>
/// Đầu mối nhật ký và vòng đời phiên; có thể dùng chung giao dịch nghiệp vụ để ghi audit nhất quán.
/// </summary>
public sealed class BUS_NHATKY
{
    private readonly DAL_NHATKY dal;
    public BUS_NHATKY(System.Data.IDbTransaction? transaction=null)=>dal=new(transaction);
    /// <summary>
    /// Chuyển mã tài khoản tới DAL để ghi thất bại có giới hạn tần suất; không truyền mật khẩu hay input đăng nhập.
    /// </summary>
    public void GhiDangNhapThatBai(string account)=>dal.GhiDangNhapThatBai(account);
    /// <summary>
    /// Ghi tên các trường thay đổi cùng mã đối tượng trong giao dịch nghiệp vụ đang dùng.
    /// </summary>
    /// <remarks>DAL yêu cầu giao dịch được truyền vào và chỉ nhận bảng, hành động, tên trường thuộc phạm vi cho phép.</remarks>
    public void GhiThayDoi(string table,string id,string action,IEnumerable<string> fields,NguCanhNghiepVu actor,Guid session)
        =>dal.GhiThayDoi(table,id,action,fields,actor,session);
    /// <summary>
    /// Tạo phiên sau khi xác thực lại người dùng và ghi sự kiện đăng nhập thành công qua DAL.
    /// </summary>
    public Guid BatDauPhien(NguCanhNghiepVu actor)=>dal.BatDauPhien(actor);
    /// <summary>
    /// Cập nhật hoạt động cuối của đúng phiên còn hiệu lực sau khi DAL xác thực lại người dùng.
    /// </summary>
    public void Heartbeat(Guid session,NguCanhNghiepVu actor)=>dal.Heartbeat(session,actor);
    /// <summary>
    /// Đóng phiên thuộc tài khoản trên principal hiện tại và ghi đăng xuất nếu phiên đang mở.
    /// </summary>
    public void KetThucPhien(Guid session)=>dal.KetThucPhien(session);
    /// <summary>
    /// Đọc một trang nhật ký thao tác hoặc phiên đăng nhập dưới quyền quản trị.
    /// </summary>
    /// <param name="filter">Bộ lọc thời gian UTC, tài khoản và phân trang cần kiểm tra.</param>
    /// <param name="actor">Ngữ cảnh người dùng được DAL xác thực quyền quản trị.</param>
    /// <param name="sessions">true để đọc phiên đăng nhập; false để đọc thao tác nghiệp vụ.</param>
    public TrangNhatKy LayTrang(BoLocNhatKy filter,NguCanhNghiepVu actor,bool sessions=false)=>dal.LayTrang(filter,actor,sessions);
    /// <summary>
    /// Đọc số đăng nhập, đăng nhập thất bại và thao tác thay đổi trong ngày UTC+7 dưới quyền quản trị.
    /// </summary>
    public (int DangNhap,int DangNhapThatBai,int ThayDoi) ThongKe(NguCanhNghiepVu actor)=>dal.ThongKe(actor);
}
