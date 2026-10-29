using ET;
using Dapper;
using System.Data;
namespace DAL;
public partial class DAL_DICHVUTHUCHIEN
{
 private const string VisitSql="""
 SELECT l.MaLS,h.MaBN,b.HoTenBN,p.MaHS,l.MaBS,l.ThoiGianKham,l.TrangThai,p.MaBS BacSiPhieu,p.TrangThai TrangThaiPhieu
 FROM dbo.LICH_SU_KHAM_BENH l WITH(HOLDLOCK) JOIN dbo.PHIEU_KHAM_BENH p ON p.MaPhieu=l.MaPhieu
 JOIN dbo.HO_SO_BENH_AN h ON h.MaHS=p.MaHS JOIN dbo.BENH_NHAN b ON b.MaBN=h.MaBN
 """;
 private const string ReadScope="""
 (@Admin=1 OR (@Doctor=1 AND (l.MaBS=@MaNV AND p.MaBS=@MaNV OR EXISTS(SELECT 1 FROM dbo.PHIEU_KHAM_BENH p2 JOIN dbo.HO_SO_BENH_AN h2 ON h2.MaHS=p2.MaHS WHERE p2.MaBS=@MaNV AND h2.MaBN=h.MaBN)))
 OR @Cash=1 OR (@Nurse=1 AND EXISTS(SELECT 1 FROM dbo.DICH_VU_THUC_HIEN s WHERE s.MaLS=l.MaLS AND s.MaNVThucHien=@MaNV)))
 """;
 private static object ReadArgs(NguCanhNghiepVu a,string? id=null,string? patient=null)=>new{id,patient,a.MaNV,Admin=a.LaAdmin,Doctor=a.CoQuyen(QuyenLamViec.Kham),Cash=a.CoQuyen(QuyenLamViec.ThuTien),Nurse=a.CoQuyen(QuyenLamViec.CapNhatKetQuaDuocGiao)};
 public IReadOnlyList<LuotThucHien> LayLuotThucHien(NguCanhNghiepVu user,string? maBN=null)=>ThucHien((db,tx)=>
 {
  var a=DAL_QUYENNGHIEPVU.XacThuc(db,tx,user);
  return (IReadOnlyList<LuotThucHien>)db.Query<LuotThucHien>("SELECT MaLS,MaBN,HoTenBN,MaHS,MaBS,ThoiGianKham,TrangThai FROM ("+VisitSql+" WHERE l.TrangThai=N'Đã xác nhận' AND "+ReadScope+" AND(@patient IS NULL OR h.MaBN=@patient)) r ORDER BY ThoiGianKham DESC,MaLS",ReadArgs(a,patient:maBN),tx).ToArray();
 });
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
 public BanNhapThucHien LayThucHien(string id,NguCanhNghiepVu user)=>ThucHien((db,tx)=>
 {var parent=db.QuerySingleOrDefault<string>("SELECT MaLS FROM dbo.DICH_VU_THUC_HIEN WHERE MaDVThucHien=@id",new{id},tx)??throw DAL_QUYENNGHIEPVU.TuChoi();return new DAL_DICHVUTHUCHIEN(tx).LayKhongGianThucHien(parent,user).ThucTe.SingleOrDefault(x=>x.DichVu.MaDVThucHien==id)??throw DAL_QUYENNGHIEPVU.TuChoi();});
 public BanNhapThucHien LuuThucHien(YeuCauThucHienDichVu request,NguCanhNghiepVu user)=>throw new LoiKhamException("DichVu","Dịch vụ thực hiện chỉ xem lịch sử. Ghi nhận điều trị khi Hoàn tất khám.");
 public BanNhapThucHien XacNhanThucHien(string id,string hash,NguCanhNghiepVu user)=>throw new LoiKhamException("DichVu","Không xác nhận dịch vụ ngoài luồng hoàn tất khám.");
 public BanNhapThucHien HuyThucHien(string id,string hash,NguCanhNghiepVu user)=>throw new LoiKhamException("DichVu","Lịch sử dịch vụ không được sửa hoặc xóa.");
}
