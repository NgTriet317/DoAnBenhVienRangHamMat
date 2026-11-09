using System.Data;
using Dapper;
using ET;
namespace DAL;

/// <summary>
/// Tầng truy cập dữ liệu khám điều trị, thực hiện truy vấn và kiểm tra trong phạm vi kết nối/giao dịch của dbconnect.
/// </summary>
public sealed partial class DAL_KHAMDIEUTRI
{
    // Hoàn tất là xác nhận của bác sĩ rằng toàn bộ dịch vụ trong các kế hoạch đã duyệt đã làm xong.
    private static void ChotDichVuDieuTri(IDbConnection db,IDbTransaction? tx,VisitRead visit,DateTime now)
    {
        if(db.ExecuteScalar<int>("SELECT COUNT(*) FROM dbo.HOA_DON WITH(UPDLOCK,HOLDLOCK) WHERE MaLS=@MaLS",visit,tx)>0)
            throw KhamError("HoaDon","Lượt đã có hóa đơn; đối chiếu trước khi hoàn tất.");
        var rows=db.Query<BuocChot>("""
            SELECT c.MaCTKH,c.MaDV,c.SoLuongDuKien,c.DonGiaDuKien,c.NoiDungBuoc
            FROM dbo.KE_HOACH_DIEU_TRI k WITH(UPDLOCK,HOLDLOCK)
            JOIN dbo.CHI_TIET_KE_HOACH c WITH(UPDLOCK,HOLDLOCK) ON c.MaKHDT=k.MaKHDT
            WHERE k.MaLS=@MaLS AND k.TrangThai IN(N'Đã xác nhận',N'Đang thực hiện',N'Đã hoàn thành') AND c.TrangThai<>N'Hủy'
            ORDER BY c.MaCTKH
            """,visit,tx).ToArray();
        foreach(var step in rows)
        {
            var used=db.ExecuteScalar<decimal>("SELECT COALESCE(SUM(SoLuong),0) FROM dbo.DICH_VU_THUC_HIEN WITH(UPDLOCK,HOLDLOCK) WHERE MaCTKH=@MaCTKH AND TrangThai=N'Đã xác nhận'",step,tx);
            if(used>step.SoLuongDuKien)throw KhamError("DichVu","Lượng đã thực hiện vượt kế hoạch; cần đối chiếu.");
            if(used<step.SoLuongDuKien)
            {
                ActiveService(db,tx,step.MaDV);
                InsertFinishedService(db,tx,visit,now,step.MaDV,step.MaCTKH,step.SoLuongDuKien-used,step.DonGiaDuKien,
                    "Bác sĩ xác nhận hoàn tất theo kế hoạch: "+step.NoiDungBuoc);
            }
            db.Execute("UPDATE dbo.CHI_TIET_KE_HOACH SET TrangThai=N'Đã hoàn thành' WHERE MaCTKH=@MaCTKH",step,tx);
        }
        var consultation=db.QuerySingleOrDefault<ET_DICHVU>("SELECT d.* FROM dbo.PHIEU_KHAM_BENH p WITH(HOLDLOCK) JOIN dbo.DICH_VU d WITH(HOLDLOCK) ON d.MaDV=p.MaDV WHERE p.MaPhieu=@MaPhieu",visit,tx)
            ??throw KhamError("DichVu","Phiếu tiếp nhận cần dịch vụ khám hợp lệ trước khi hoàn tất.");
        if(db.ExecuteScalar<int>("SELECT COUNT(*) FROM dbo.DICH_VU_THUC_HIEN WITH(HOLDLOCK) WHERE MaLS=@MaLS AND MaDV=@MaDV AND TrangThai=N'Đã xác nhận'",new{visit.MaLS,consultation.MaDV},tx)==0)
            InsertFinishedService(db,tx,visit,now,consultation.MaDV,null,1,consultation.DonGia,"Hoàn tất dịch vụ khám theo phiếu tiếp nhận.");
        db.Execute("UPDATE dbo.KE_HOACH_DIEU_TRI SET TrangThai=N'Đã hoàn thành' WHERE MaLS=@MaLS AND TrangThai IN(N'Đã xác nhận',N'Đang thực hiện')",visit,tx);
    }
    /// <summary>
    /// Ghi một dịch vụ thực tế đã hoàn tất từ nguồn kế hoạch/khám được xác nhận.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>TaoMa</c>, <c>db.Execute</c>.</remarks>
    /// <param name="db">Kết nối SQL đang dùng; các lệnh trong thao tác này thực thi trên kết nối đó.</param>
    /// <param name="tx">Giao dịch dùng chung; null nếu nơi gọi không truyền giao dịch. Quyền commit/rollback thuộc nơi quản lý giao dịch.</param>
    /// <param name="visit">Giá trị lần khám dùng khi thực hiện thao tác; kiểu VisitRead.</param>
    /// <param name="now">Giá trị thời điểm hiện tại dùng khi thực hiện thao tác; kiểu DateTime.</param>
    /// <param name="service">Giá trị dịch vụ dùng khi thực hiện thao tác; kiểu string.</param>
    /// <param name="step">Giá trị bước dùng khi thực hiện thao tác; kiểu string?.</param>
    /// <param name="qty">Giá trị số lượng dùng khi thực hiện thao tác; kiểu decimal.</param>
    /// <param name="price">Giá trị đơn giá dùng khi thực hiện thao tác; kiểu decimal.</param>
    /// <param name="result">Giá trị kết quả dùng khi thực hiện thao tác; kiểu string.</param>
    private static void InsertFinishedService(IDbConnection db,IDbTransaction? tx,VisitRead visit,DateTime now,string service,string? step,decimal qty,decimal price,string result)
    {
        var id=TaoMa(db,tx,"DICH_VU_THUC_HIEN","MaDVThucHien");
        db.Execute("""
            INSERT dbo.DICH_VU_THUC_HIEN(MaDVThucHien,MaLS,MaCTKH,MaDV,MaNVThucHien,ThoiGianThucHien,SoLuong,DonGia,KetQua,TrangThai)
            VALUES(@id,@MaLS,@step,@service,@MaBS,@now,@qty,@price,@result,N'Đã xác nhận')
            """,new{id,visit.MaLS,visit.MaBS,step,service,now,qty,price,result},tx);
    }
    /// <summary>
    /// Mô hình và thao tác hỗ trợ bước chốt dùng bởi các thành phần liên quan.
    /// </summary>
    private sealed class BuocChot
    {
        /// <summary>
        /// Mã dòng chi tiết kế hoạch điều trị.
        /// </summary>
        public string MaCTKH{get;set;}="";
        /// <summary>
        /// Mã dịch vụ.
        /// </summary>
        public string MaDV{get;set;}="";
        /// <summary>
        /// Số lượng dự kiến của bước kế hoạch.
        /// </summary>
        public decimal SoLuongDuKien{get;set;}
        /// <summary>
        /// Đơn giá dự kiến của bước kế hoạch.
        /// </summary>
        public decimal DonGiaDuKien{get;set;}
        /// <summary>
        /// Nội dung bước. Có thể để null khi dữ liệu chưa được cung cấp.
        /// </summary>
        public string? NoiDungBuoc{get;set;}}
}
