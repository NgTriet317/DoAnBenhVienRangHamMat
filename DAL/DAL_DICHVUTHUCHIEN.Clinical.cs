using ET;
using Dapper;
using System.Data;
namespace DAL;
/// <summary>
/// Tầng truy cập dữ liệu dịch vụ thực hiện, thực hiện truy vấn và kiểm tra trong phạm vi kết nối/giao dịch của dbconnect.
/// </summary>
public partial class DAL_DICHVUTHUCHIEN
{
 /// <summary>
 /// Câu lệnh SQL lần khám SQL dùng trong các thao tác của lớp; giá trị đầu vào được truyền theo cách nơi gọi quy định.
 /// </summary>
 private const string VisitSql="""
 SELECT l.MaLS,h.MaBN,b.HoTenBN,p.MaHS,l.MaBS,l.ThoiGianKham,l.TrangThai,p.MaBS BacSiPhieu,p.TrangThai TrangThaiPhieu
 FROM dbo.LICH_SU_KHAM_BENH l WITH(HOLDLOCK) JOIN dbo.PHIEU_KHAM_BENH p ON p.MaPhieu=l.MaPhieu
 JOIN dbo.HO_SO_BENH_AN h ON h.MaHS=p.MaHS JOIN dbo.BENH_NHAN b ON b.MaBN=h.MaBN
 """;
 /// <summary>
 /// Phạm vi đọc dữ liệu được giữ trong phạm vi lớp để các thao tác dùng lại.
 /// </summary>
 private const string ReadScope="""
 (@Admin=1 OR (@Doctor=1 AND (l.MaBS=@MaNV AND p.MaBS=@MaNV OR EXISTS(SELECT 1 FROM dbo.PHIEU_KHAM_BENH p2 JOIN dbo.HO_SO_BENH_AN h2 ON h2.MaHS=p2.MaHS WHERE p2.MaBS=@MaNV AND h2.MaBN=h.MaBN)))
 OR @Cash=1 OR (@Nurse=1 AND EXISTS(SELECT 1 FROM dbo.DICH_VU_THUC_HIEN s WHERE s.MaLS=l.MaLS AND s.MaNVThucHien=@MaNV)))
 """;
 /// <summary>
 /// Chuẩn bị hoặc kiểm tra các đối số dùng đọc dữ liệu dịch vụ thực hiện.
 /// </summary>
 /// <remarks>Các bước được phối hợp qua <c>a.CoQuyen</c>.</remarks>
 /// <param name="a">Ngữ cảnh định danh và quyền nghiệp vụ của người thực hiện; DAL có thể xác thực lại theo dữ liệu hiện tại.</param>
 /// <param name="id">Giá trị mã dùng khi thực hiện thao tác; kiểu string?.</param>
 /// <param name="patient">Giá trị bệnh nhân dùng khi thực hiện thao tác; kiểu string?.</param>
 /// <returns>Kết quả kiểu object dùng cho bước xử lý tiếp theo.</returns>
 private static object ReadArgs(NguCanhNghiepVu a,string? id=null,string? patient=null)=>new{id,patient,a.MaNV,Admin=a.LaAdmin,Doctor=a.CoQuyen(QuyenLamViec.Kham),Cash=a.CoQuyen(QuyenLamViec.ThuTien),Nurse=a.CoQuyen(QuyenLamViec.CapNhatKetQuaDuocGiao)};
 /// <summary>
 /// Đọc các lần khám có nguồn dịch vụ thực hiện thuộc phạm vi tra cứu của người dùng.
 /// </summary>
 /// <remarks>Các bước được phối hợp qua <c>ThucHien</c>, <c>DAL_QUYENNGHIEPVU.XacThuc</c>, <c>db.Query&lt;LuotThucHien&gt;</c>, <c>ReadArgs</c>.</remarks>
 /// <param name="user">Ngữ cảnh định danh và quyền nghiệp vụ của người thực hiện; DAL có thể xác thực lại theo dữ liệu hiện tại.</param>
 /// <param name="maBN">Mã bệnh nhân xác định đối tượng cần xử lý.</param>
 /// <returns>Tập dữ liệu kiểu IReadOnlyList&lt;LuotThucHien&gt; theo điều kiện và phạm vi xử lý của hàm.</returns>
 public IReadOnlyList<LuotThucHien> LayLuotThucHien(NguCanhNghiepVu user,string? maBN=null)=>ThucHien((db,tx)=>
 {
  var a=DAL_QUYENNGHIEPVU.XacThuc(db,tx,user);
  return (IReadOnlyList<LuotThucHien>)db.Query<LuotThucHien>("SELECT MaLS,MaBN,HoTenBN,MaHS,MaBS,ThoiGianKham,TrangThai FROM ("+VisitSql+" WHERE l.TrangThai=N'Đã xác nhận' AND "+ReadScope+" AND(@patient IS NULL OR h.MaBN=@patient)) r ORDER BY ThoiGianKham DESC,MaLS",ReadArgs(a,patient:maBN),tx).ToArray();
 });
 /// <summary>
 /// Nạp dữ liệu lần khám và các dịch vụ lịch sử để màn hình chỉ tra cứu.
 /// </summary>
 /// <remarks>Các bước được phối hợp qua <c>ThucHien</c>, <c>DAL_QUYENNGHIEPVU.XacThuc</c>, <c>db.QuerySingleOrDefault&lt;LuotThucHien&gt;</c>, <c>ReadArgs</c>.</remarks>
 /// <param name="maLS">Mã lần khám xác định đối tượng cần xử lý.</param>
 /// <param name="user">Ngữ cảnh định danh và quyền nghiệp vụ của người thực hiện; DAL có thể xác thực lại theo dữ liệu hiện tại.</param>
 /// <returns>Kết quả kiểu KhongGianThucHien dùng cho bước xử lý tiếp theo.</returns>
 public KhongGianThucHien LayKhongGianThucHien(string maLS,NguCanhNghiepVu user)=>ThucHien((db,tx)=>
 {
  var a=DAL_QUYENNGHIEPVU.XacThuc(db,tx,user);
  var visit=db.QuerySingleOrDefault<LuotThucHien>("SELECT MaLS,MaBN,HoTenBN,MaHS,MaBS,ThoiGianKham,TrangThai FROM ("+VisitSql+" WHERE l.MaLS=@id AND l.TrangThai=N'Đã xác nhận' AND "+ReadScope+") r",ReadArgs(a,maLS),tx)??throw DAL_QUYENNGHIEPVU.TuChoi();
  var clinical=a.LaAdmin||a.CoQuyen(QuyenLamViec.Kham)&&db.ExecuteScalar<int>("SELECT COUNT(*) FROM dbo.PHIEU_KHAM_BENH p JOIN dbo.HO_SO_BENH_AN h ON h.MaHS=p.MaHS WHERE p.MaBS=@MaNV AND h.MaBN=@MaBN",new{a.MaNV,visit.MaBN},tx)>0;
  var rows=db.Query<ET_DICHVUTHUCHIEN>("SELECT * FROM dbo.DICH_VU_THUC_HIEN WHERE MaLS=@maLS AND TrangThai=N'Đã xác nhận' AND(@clinical=1 OR @cash=1 OR MaNVThucHien=@MaNV) ORDER BY ThoiGianThucHien DESC,MaDVThucHien",new{maLS,clinical,cash=a.CoQuyen(QuyenLamViec.ThuTien),a.MaNV},tx).ToArray();
  foreach(var row in rows)if(!clinical&&!(a.CoQuyen(QuyenLamViec.CapNhatKetQuaDuocGiao)&&row.MaNVThucHien==a.MaNV))row.KetQua=null;
  var result=rows.Select(r=>new BanNhapThucHien(r,"",false,false,db.ExecuteScalar<int>("SELECT COUNT(*) FROM dbo.CHI_TIET_HOA_DON WHERE MaDVThucHien=@id",new{id=r.MaDVThucHien},tx)>0,null,
   db.QuerySingle<string>("SELECT TenDV FROM dbo.DICH_VU WHERE MaDV=@MaDV",r,tx),db.QuerySingle<string>("SELECT HoTen FROM dbo.NHAN_VIEN WHERE MaNV=@MaNVThucHien",r,tx))).ToArray();
  DAL_QUYENNGHIEPVU.GhiTraCuuAdmin(db,tx,a,"LICH_SU_KHAM_BENH",maLS);
  return new KhongGianThucHien(visit,false,[],result,[],[]);
 });
 /// <summary>
 /// Đọc thông tin dịch vụ thực tế theo mã trong phạm vi người dùng được truy cập.
 /// </summary>
 /// <remarks>Các bước được phối hợp qua <c>ThucHien</c>, <c>db.QuerySingleOrDefault&lt;string&gt;</c>, <c>DAL_QUYENNGHIEPVU.TuChoi</c>.</remarks>
 /// <param name="id">Giá trị mã dùng khi thực hiện thao tác; kiểu string.</param>
 /// <param name="user">Ngữ cảnh định danh và quyền nghiệp vụ của người thực hiện; DAL có thể xác thực lại theo dữ liệu hiện tại.</param>
 /// <returns>Kết quả kiểu BanNhapThucHien dùng cho bước xử lý tiếp theo.</returns>
 public BanNhapThucHien LayThucHien(string id,NguCanhNghiepVu user)=>ThucHien((db,tx)=>
 {var parent=db.QuerySingleOrDefault<string>("SELECT MaLS FROM dbo.DICH_VU_THUC_HIEN WHERE MaDVThucHien=@id",new{id},tx)??throw DAL_QUYENNGHIEPVU.TuChoi();return new DAL_DICHVUTHUCHIEN(tx).LayKhongGianThucHien(parent,user).ThucTe.SingleOrDefault(x=>x.DichVu.MaDVThucHien==id)??throw DAL_QUYENNGHIEPVU.TuChoi();});
 /// <summary>
 /// API tương thích cũ luôn từ chối thay đổi dịch vụ độc lập bằng LoiKhamException. Lịch sử dịch vụ chỉ đọc; ghi nhận thực hiện qua quy trình khám.
 /// </summary>
 /// <param name="request">Gói dữ liệu đầu vào cho thao tác; các trường được kiểm tra trong quá trình xử lý.</param>
 /// <param name="user">Ngữ cảnh định danh và quyền nghiệp vụ của người thực hiện; DAL có thể xác thực lại theo dữ liệu hiện tại.</param>
 /// <returns>Không trả kết quả; luôn phát ngoại lệ theo quy tắc của API này.</returns>
 public BanNhapThucHien LuuThucHien(YeuCauThucHienDichVu request,NguCanhNghiepVu user)=>throw new LoiKhamException("DichVu","Dịch vụ thực hiện chỉ xem lịch sử. Ghi nhận điều trị khi Hoàn tất khám.");
 /// <summary>
 /// API tương thích cũ luôn từ chối thay đổi dịch vụ độc lập bằng LoiKhamException. Lịch sử dịch vụ chỉ đọc; ghi nhận thực hiện qua quy trình khám.
 /// </summary>
 /// <param name="id">Giá trị mã dùng khi thực hiện thao tác; kiểu string.</param>
 /// <param name="hash">Giá trị mã băm dùng khi thực hiện thao tác; kiểu string.</param>
 /// <param name="user">Ngữ cảnh định danh và quyền nghiệp vụ của người thực hiện; DAL có thể xác thực lại theo dữ liệu hiện tại.</param>
 /// <returns>Không trả kết quả; luôn phát ngoại lệ theo quy tắc của API này.</returns>
 public BanNhapThucHien XacNhanThucHien(string id,string hash,NguCanhNghiepVu user)=>throw new LoiKhamException("DichVu","Không xác nhận dịch vụ ngoài luồng hoàn tất khám.");
 /// <summary>
 /// API tương thích cũ luôn từ chối thay đổi dịch vụ độc lập bằng LoiKhamException. Lịch sử dịch vụ chỉ đọc; ghi nhận thực hiện qua quy trình khám.
 /// </summary>
 /// <param name="id">Giá trị mã dùng khi thực hiện thao tác; kiểu string.</param>
 /// <param name="hash">Giá trị mã băm dùng khi thực hiện thao tác; kiểu string.</param>
 /// <param name="user">Ngữ cảnh định danh và quyền nghiệp vụ của người thực hiện; DAL có thể xác thực lại theo dữ liệu hiện tại.</param>
 /// <returns>Không trả kết quả; luôn phát ngoại lệ theo quy tắc của API này.</returns>
 public BanNhapThucHien HuyThucHien(string id,string hash,NguCanhNghiepVu user)=>throw new LoiKhamException("DichVu","Lịch sử dịch vụ không được sửa hoặc xóa.");
}
