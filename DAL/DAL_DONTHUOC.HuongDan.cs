using ET;
using Dapper;
using System.Data;
using Microsoft.Data.SqlClient;
namespace DAL;
/// <summary>
/// Tầng truy cập dữ liệu đơn thuốc, thực hiện truy vấn và kiểm tra trong phạm vi kết nối/giao dịch của dbconnect.
/// </summary>
public partial class DAL_DONTHUOC
{
    /// <summary>
    /// Đọc cấu hình hướng dẫn thuốc và các nhóm liên quan cho màn hình quản lý.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>ThucHien</c>, <c>ConfigUser</c>, <c>DAL_QUYENNGHIEPVU.KiemTraMa</c>, <c>ReadGuidance</c>.</remarks>
    /// <param name="drug">Giá trị thuốc dùng khi thực hiện thao tác; kiểu string.</param>
    /// <param name="user">Ngữ cảnh định danh và quyền nghiệp vụ của người thực hiện; DAL có thể xác thực lại theo dữ liệu hiện tại.</param>
    /// <returns>Đối tượng kiểu BanNhapHuongDanThuoc được tìm hoặc tính toán; có thể null theo nhánh xử lý trong hàm.</returns>
    public BanNhapHuongDanThuoc? LayHuongDan(string drug,NguCanhNghiepVu user)=>ThucHien((db,tx)=>{ConfigUser(db,tx,user,false);DAL_QUYENNGHIEPVU.KiemTraMa(drug);var c=ReadGuidance(db,tx,drug);return c is null?null:new BanNhapHuongDanThuoc(c,RxFingerprint(c));});
    /// <summary>
    /// Lưu bản cấu hình hướng dẫn thuốc theo đầu vào và quyền cấu hình của người thực hiện. Các truy vấn có gợi ý khóa giữ bản dữ liệu liên quan trong phạm vi giao dịch.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>ThucHien</c>, <c>ArgumentNullException.ThrowIfNull</c>, <c>RxLock</c>, <c>ConfigUser</c>.</remarks>
    /// <param name="config">Giá trị cấu hình dùng khi thực hiện thao tác; kiểu ET_CAUHINHTHUOCKEDON.</param>
    /// <param name="token">Giá trị dấu vân tay đối chiếu dùng khi thực hiện thao tác; kiểu string?.</param>
    /// <param name="user">Ngữ cảnh định danh và quyền nghiệp vụ của người thực hiện; DAL có thể xác thực lại theo dữ liệu hiện tại.</param>
    /// <returns>Kết quả kiểu BanNhapHuongDanThuoc dùng cho bước xử lý tiếp theo.</returns>
    public BanNhapHuongDanThuoc LuuHuongDan(ET_CAUHINHTHUOCKEDON config,string? token,NguCanhNghiepVu user)=>ThucHien((db,tx)=>
    {
        ArgumentNullException.ThrowIfNull(config);RxLock(db,tx,"RHM:HuongDanThuoc:"+config.MaThuoc);var a=ConfigUser(db,tx,user,false);ActiveDrug(db,tx,config.MaThuoc);var old=ReadGuidance(db,tx,config.MaThuoc);
        if(config.TrangThai!="Nháp"||config.MaNguoiDuyet is not null||config.NgayDuyet is not null)throw RxError("TrangThai","Soạn nháp không nhận chữ ký client.");
        if(old is null){if(token is not null||config.PhienBan!=1)throw RxError("PhienBan","Hướng dẫn mới bắt đầu phiên bản 1.");}
        else{RxHash(token,RxFingerprint(old));if(config.PhienBan!=(old.TrangThai=="Nháp"?old.PhienBan:old.PhienBan+1))throw RxError("PhienBan","Bản đã duyệt/ngừng cần phiên bản nháp tiếp theo.");}
        if(config.MaNhomThuoc is not null&&db.ExecuteScalar<int>("SELECT COUNT(*) FROM dbo.NHOM_THUOC_KE_DON WITH(HOLDLOCK) WHERE MaNhomThuoc=@MaNhomThuoc AND TrangThai=N'Đang áp dụng'",config,tx)!=1)throw RxError("MaNhomThuoc","Chọn nhóm đang áp dụng.");
        KiemTra(db,tx,"CAU_HINH_THUOC_KE_DON",config,old,null);
        WithRxConfigActor(db,tx,a,()=>db.Execute(old is null?"INSERT dbo.CAU_HINH_THUOC_KE_DON(MaThuoc,MaNhomThuoc,DoiTuongApDung,DuongDung,LieuDungGoiY,HuongDanGoiY,PhienBan,TrangThai,MaNguoiDuyet,NgayDuyet) VALUES(@MaThuoc,@MaNhomThuoc,@DoiTuongApDung,@DuongDung,@LieuDungGoiY,@HuongDanGoiY,@PhienBan,@TrangThai,@MaNguoiDuyet,@NgayDuyet)":"UPDATE dbo.CAU_HINH_THUOC_KE_DON SET MaNhomThuoc=@MaNhomThuoc,DoiTuongApDung=@DoiTuongApDung,DuongDung=@DuongDung,LieuDungGoiY=@LieuDungGoiY,HuongDanGoiY=@HuongDanGoiY,PhienBan=@PhienBan,TrangThai=@TrangThai,MaNguoiDuyet=NULL,NgayDuyet=NULL WHERE MaThuoc=@MaThuoc",config,tx));
        var saved=ReadGuidance(db,tx,config.MaThuoc)!;return new BanNhapHuongDanThuoc(saved,RxFingerprint(saved));
    });
    /// <summary>
    /// Duyệt phiên bản hướng dẫn thuốc sau các bước kiểm tra dữ liệu và quyền.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>ThucHien</c>, <c>RxLock</c>, <c>ConfigUser</c>, <c>ActiveDrug</c>.</remarks>
    /// <param name="drug">Giá trị thuốc dùng khi thực hiện thao tác; kiểu string.</param>
    /// <param name="token">Giá trị dấu vân tay đối chiếu dùng khi thực hiện thao tác; kiểu string.</param>
    /// <param name="user">Ngữ cảnh định danh và quyền nghiệp vụ của người thực hiện; DAL có thể xác thực lại theo dữ liệu hiện tại.</param>
    /// <returns>Kết quả kiểu BanNhapHuongDanThuoc dùng cho bước xử lý tiếp theo.</returns>
    public BanNhapHuongDanThuoc DuyetHuongDan(string drug,string token,NguCanhNghiepVu user)=>ThucHien((db,tx)=>
    {
        RxLock(db,tx,"RHM:HuongDanThuoc:"+drug);var a=ConfigUser(db,tx,user,true);ActiveDrug(db,tx,drug);var c=ReadGuidance(db,tx,drug)??throw RxError("MaThuoc","Lưu nháp hướng dẫn trước khi duyệt.");RxHash(token,RxFingerprint(c));
        if(c.TrangThai!="Nháp"||new[]{c.DoiTuongApDung,c.DuongDung,c.LieuDungGoiY,c.HuongDanGoiY}.Any(string.IsNullOrWhiteSpace))throw RxError("HuongDan","Cần đối tượng, đường dùng, liều và hướng dẫn đầy đủ trong bản nháp.");
        WithRxConfigActor(db,tx,a,()=>db.Execute("UPDATE dbo.CAU_HINH_THUOC_KE_DON SET TrangThai=N'Đã duyệt',MaNguoiDuyet=@MaNV,NgayDuyet=SYSDATETIME() WHERE MaThuoc=@drug",new{a.MaNV,drug},tx));var saved=ReadGuidance(db,tx,drug)!;return new BanNhapHuongDanThuoc(saved,RxFingerprint(saved));
    });
    /// <summary>
    /// Đọc cấu hình hướng dẫn thuốc hiện tại và các dữ liệu phiên bản liên quan. Các truy vấn có gợi ý khóa giữ bản dữ liệu liên quan trong phạm vi giao dịch.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>db.QuerySingleOrDefault&lt;ET_CAUHINHTHUOCKEDON&gt;</c>.</remarks>
    /// <param name="db">Kết nối SQL đang dùng; các lệnh trong thao tác này thực thi trên kết nối đó.</param>
    /// <param name="tx">Giao dịch dùng chung; null nếu nơi gọi không truyền giao dịch. Quyền commit/rollback thuộc nơi quản lý giao dịch.</param>
    /// <param name="id">Giá trị mã dùng khi thực hiện thao tác; kiểu string.</param>
    /// <returns>Đối tượng kiểu ET_CAUHINHTHUOCKEDON được tìm hoặc tính toán; có thể null theo nhánh xử lý trong hàm.</returns>
    private static ET_CAUHINHTHUOCKEDON? ReadGuidance(IDbConnection db,IDbTransaction? tx,string id)=>db.QuerySingleOrDefault<ET_CAUHINHTHUOCKEDON>("SELECT * FROM dbo.CAU_HINH_THUOC_KE_DON WITH(HOLDLOCK) WHERE MaThuoc=@id",new{id},tx);
    /// <summary>
    /// Xác thực người dùng và yêu cầu quyền quản lý cấu hình hướng dẫn thuốc.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>DAL_QUYENNGHIEPVU.XacThuc</c>, <c>a.CoQuyen</c>, <c>DAL_QUYENNGHIEPVU.TuChoi</c>.</remarks>
    /// <param name="db">Kết nối SQL đang dùng; các lệnh trong thao tác này thực thi trên kết nối đó.</param>
    /// <param name="tx">Giao dịch dùng chung; null nếu nơi gọi không truyền giao dịch. Quyền commit/rollback thuộc nơi quản lý giao dịch.</param>
    /// <param name="user">Ngữ cảnh định danh và quyền nghiệp vụ của người thực hiện; DAL có thể xác thực lại theo dữ liệu hiện tại.</param>
    /// <param name="approve">Giá trị duyệt dùng khi thực hiện thao tác; kiểu bool.</param>
    /// <returns>Kết quả kiểu NguCanhNghiepVu dùng cho bước xử lý tiếp theo.</returns>
    private static NguCanhNghiepVu ConfigUser(IDbConnection db,IDbTransaction? tx,NguCanhNghiepVu user,bool approve)
    {var a=DAL_QUYENNGHIEPVU.XacThuc(db,tx,user);if(approve?!a.CoQuyen(QuyenLamViec.DuyetHuongDanThuoc):!a.LaAdmin&&!a.CoQuyen(QuyenLamViec.CauHinhHoTro))throw DAL_QUYENNGHIEPVU.TuChoi();return a;}
    /// <summary>
    /// Điều phối thao tác cấu hình hướng dẫn thuốc với ngữ cảnh người dùng đã xác thực.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>db.ExecuteScalar&lt;string?&gt;</c>, <c>db.Execute</c>, <c>action</c>, <c>SqlConnection.ClearPool</c>.</remarks>
    /// <param name="db">Kết nối SQL đang dùng; các lệnh trong thao tác này thực thi trên kết nối đó.</param>
    /// <param name="tx">Giao dịch dùng chung; null nếu nơi gọi không truyền giao dịch. Quyền commit/rollback thuộc nơi quản lý giao dịch.</param>
    /// <param name="a">Ngữ cảnh định danh và quyền nghiệp vụ của người thực hiện; DAL có thể xác thực lại theo dữ liệu hiện tại.</param>
    /// <param name="action">Giá trị action dùng khi thực hiện thao tác; kiểu Func&lt;T&gt;.</param>
    /// <returns>Kết quả kiểu T dùng cho bước xử lý tiếp theo.</returns>
    /// <typeparam name="T">Kiểu dữ liệu dùng cho thao tác tổng quát này.</typeparam>
    private static T WithRxConfigActor<T>(IDbConnection db,IDbTransaction? tx,NguCanhNghiepVu a,Func<T> action)
    {
        var prior=db.ExecuteScalar<string?>("SELECT CONVERT(nvarchar(128),SESSION_CONTEXT(N'RHM.MaTK'))",transaction:tx);var ok=false;
        try{db.Execute("EXEC sys.sp_set_session_context @key=N'RHM.MaTK',@value=@actor",new{actor=a.MaTK},tx);var result=action();ok=true;return result;}
        finally{try{db.Execute("EXEC sys.sp_set_session_context @key=N'RHM.MaTK',@value=@actor",new{actor=prior},tx?.Connection is null?null:tx);}catch{if(db is SqlConnection c){SqlConnection.ClearPool(c);c.Close();}if(ok)throw;}}
    }
}
