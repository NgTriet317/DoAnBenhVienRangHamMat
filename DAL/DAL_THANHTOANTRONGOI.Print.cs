using Dapper;
using ET;
namespace DAL;

public sealed partial class DAL_THANHTOANTRONGOI
{
    public BanInHoaDon LayBanInHoaDon(string maHD,NguCanhNghiepVu user)=>ThucHien((db,tx)=>
    {
        var actor=DAL_QUYENNGHIEPVU.XacThuc(db,tx,user);DAL_QUYENNGHIEPVU.KiemTraMa(maHD);
        var context=db.QuerySingleOrDefault<InvoiceContext>("""
            SELECT bn.MaBN,bn.HoTenBN,n.HoTen NguoiLap,l.MaBS,p.MaBS BacSiPhieu,l.TrangThai TrangThaiLuot,
                CONVERT(bit,CASE WHEN EXISTS(SELECT 1 FROM dbo.PHIEU_KHAM_BENH own WITH(HOLDLOCK) WHERE own.MaHS=p.MaHS AND own.MaBS=@MaNV AND own.TrangThai<>N'Hủy') THEN 1 ELSE 0 END) DaPhuTrach
            FROM dbo.HOA_DON h WITH(HOLDLOCK) JOIN dbo.LICH_SU_KHAM_BENH l WITH(HOLDLOCK) ON l.MaLS=h.MaLS
            JOIN dbo.PHIEU_KHAM_BENH p WITH(HOLDLOCK) ON p.MaPhieu=l.MaPhieu JOIN dbo.HO_SO_BENH_AN hs ON hs.MaHS=p.MaHS
            JOIN dbo.BENH_NHAN bn ON bn.MaBN=hs.MaBN JOIN dbo.NHAN_VIEN n ON n.MaNV=h.MaNV WHERE h.MaHD=@maHD
            """,new{maHD,actor.MaNV},tx);
        if(context is null)throw DAL_QUYENNGHIEPVU.TuChoi();
        var h=db.QuerySingle<ET_HOADON>("SELECT * FROM dbo.HOA_DON WITH(HOLDLOCK) WHERE MaHD=@maHD",new{maHD},tx);
        var payments=db.Query<ET_THANHTOAN>("SELECT * FROM dbo.THANH_TOAN WITH(HOLDLOCK) WHERE MaHD=@maHD ORDER BY MaTT",new{maHD},tx).ToArray();
        var collected=payments.Where(t=>t.TrangThai=="Đã xác nhận").Sum(t=>t.LoaiGiaoDich=="Thu"?t.SoTien:-t.SoTien);
        var allowed=actor.LaAdmin || actor.CoQuyen(QuyenLamViec.ThuTien) ||
            actor.CoQuyen(QuyenLamViec.Kham)&&(context.MaBS==actor.MaNV&&context.BacSiPhieu==actor.MaNV || context.TrangThaiLuot=="Đã xác nhận"&&context.DaPhuTrach) ||
            actor.CoQuyen(QuyenLamViec.GiaoThuoc)&&h.TrangThai=="Đã xác nhận"&&collected==h.SoTienPhaiThu;
        if(!allowed)throw DAL_QUYENNGHIEPVU.TuChoi();
        if(h.PhienBanQuyTrinh==2)ReadPaymentResult(db,tx,h.MaHD,true); // Bản phát hành mới phải thu đủ.
        var details=db.Query<DongInHoaDon>("""
            SELECT c.MaCTHD,c.MaDVThucHien,c.MaCTPX,d.MaCTDT,d.MaDT,c.NoiDung,
                COALESCE(d.DonViTinhChot,hh.DonViTinh) DonViTinh,COALESCE(s.SoLuong,x.SoLuong,d.SoLuong) SoLuong,
                COALESCE(s.DonGia,c.DonGia) DonGia,c.ThanhTien,x.MaLo,l.SoLo
            FROM dbo.CHI_TIET_HOA_DON c WITH(HOLDLOCK)
            LEFT JOIN dbo.DICH_VU_THUC_HIEN s WITH(HOLDLOCK) ON s.MaDVThucHien=c.MaDVThucHien
            LEFT JOIN dbo.CHI_TIET_PHIEU_XUAT x WITH(HOLDLOCK) ON x.MaCTPX=c.MaCTPX
            LEFT JOIN dbo.CHI_TIET_DON_THUOC d WITH(HOLDLOCK) ON d.MaCTDT=COALESCE(c.MaCTDT,x.MaCTDT)
            LEFT JOIN dbo.HANG_HOA hh ON hh.MaHH=x.MaHH LEFT JOIN dbo.LO_HANG_HOA l ON l.MaLo=x.MaLo
            WHERE c.MaHD=@maHD ORDER BY c.MaCTHD
            """,new{maHD},tx).ToArray();
        var cash=payments.SingleOrDefault(t=>h.PhienBanQuyTrinh==2&&t.LoaiGiaoDich=="Thu"&&t.TrangThai=="Đã xác nhận");
        DAL_QUYENNGHIEPVU.GhiTraCuuAdmin(db,tx,actor,"HOA_DON",maHD);
        return new BanInHoaDon(h,context.MaBN,context.HoTenBN,context.NguoiLap,collected,h.SoTienPhaiThu-collected,details,
            h.PhienBanQuyTrinh==2?cash?.SoTienKhachDua??0:null,h.PhienBanQuyTrinh==2?cash?.TienThua??0:null,cash?.PhuongThuc,cash?.MaThamChieu);
    });
    private sealed class InvoiceContext
    {
        public string MaBN{get;set;}="";public string HoTenBN{get;set;}="";public string NguoiLap{get;set;}="";
        public string MaBS{get;set;}="";public string BacSiPhieu{get;set;}="";public string TrangThaiLuot{get;set;}="";public bool DaPhuTrach{get;set;}
    }
}
