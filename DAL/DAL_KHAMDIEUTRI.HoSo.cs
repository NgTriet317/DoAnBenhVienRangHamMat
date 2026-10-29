using System.Data;
using Dapper;
using ET;

namespace DAL;

public sealed partial class DAL_KHAMDIEUTRI
{
    public DuLieuHoSoTheoQuyen LayDuLieuHoSo(NguCanhNghiepVu nguoiDung)
        =>LaySnapshot(nguoiDung,null);
    public DuLieuHoSoTheoQuyen LayDuLieuHoSoGiaoDien(NguCanhHoSo context,NguCanhNghiepVu actor)
        =>LaySnapshot(actor,context??throw new ArgumentNullException(nameof(context)));
    private DuLieuHoSoTheoQuyen LaySnapshot(NguCanhNghiepVu nguoiDung,NguCanhHoSo? context)
        => ThucHien((db, tx) =>
        {
            var user = DAL_QUYENNGHIEPVU.XacThuc(db, tx, nguoiDung);
            if (user.Quyen == QuyenLamViec.Khong && !user.LaAdmin) throw DAL_QUYENNGHIEPVU.TuChoi();
            var doctor = user.CoQuyen(QuyenLamViec.Kham);
            var reception = user.CoQuyen(QuyenLamViec.TiepNhan);
            var nurse = user.CoQuyen(QuyenLamViec.CapNhatKetQuaDuocGiao) && !doctor;
            var pharmacy = user.CoQuyen(QuyenLamViec.GiaoThuoc);
            var fullClinical = doctor || reception || user.LaAdmin;
            var finance = doctor || user.CoQuyen(QuyenLamViec.ThuTien) || pharmacy || user.LaAdmin;
            using var reader = db.QueryMultiple(SnapshotSql, new { user.MaNV, doctor, reception, nurse, pharmacy, fullClinical, finance, user.LaAdmin,ui=context is not null,patient=context?.MaBN,visit=context?.MaLS,patientLimit=context is null?int.MaxValue:context.MaBN is null?50:1,visitLimit=context is null?int.MaxValue:100,receiptLimit=context is null?int.MaxValue:150,appointmentLimit=context is null||context.MaBN is not null?int.MaxValue:100 }, tx);
            var data = new Dictionary<string, object[]>(StringComparer.Ordinal);
            void Add<T>(string table) => data.Add(table, reader.Read<T>().Cast<object>().ToArray());
            Add<ET_BENHNHAN>("BENH_NHAN");
            Add<ET_HOSOBENHAN>("HO_SO_BENH_AN");
            Add<ET_PHIEUKHAMBENH>("PHIEU_KHAM_BENH");
            Add<ET_LICHKHAM>("LICH_KHAM");
            Add<ET_LICHSUKHAMBENH>("LICH_SU_KHAM_BENH");
            Add<ET_TINHTRANGBENH>("TINH_TRANG_BENH");
            Add<ET_KEHOACHDIEUTRI>("KE_HOACH_DIEU_TRI");
            Add<ET_CHITIETKEHOACH>("CHI_TIET_KE_HOACH");
            Add<ET_DICHVUTHUCHIEN>("DICH_VU_THUC_HIEN");
            Add<ET_DONTHUOC>("DON_THUOC");
            Add<ET_CHITIETDONTHUOC>("CHI_TIET_DON_THUOC");
            Add<ET_HOADON>("HOA_DON");
            Add<ET_CHITIETHOADON>("CHI_TIET_HOA_DON");
            Add<ET_THANHTOAN>("THANH_TOAN");
            Add<ET_PHIEUXUAT>("PHIEU_XUAT");
            Add<ET_CHITIETPHIEUXUAT>("CHI_TIET_PHIEU_XUAT");
            Add<ET_NHANVIEN>("NHAN_VIEN"); Add<ET_PHONG>("PHONG"); Add<ET_CALAM>("CA_LAM");
            Add<ET_DICHVU>("DICH_VU"); Add<ET_THUOC>("THUOC"); Add<ET_HANGHOA>("HANG_HOA");
            Add<ET_KHO>("KHO"); Add<ET_LOHANGHOA>("LO_HANG_HOA");
            DAL_QUYENNGHIEPVU.GhiTraCuuAdmin(db, tx, user, "PHAM_VI_HO_SO", user.MaNV);
            return new DuLieuHoSoTheoQuyen(user, data);
        });

    private const string SnapshotSql = """
        DECLARE @bn TABLE(MaBN nvarchar(50) PRIMARY KEY);
        INSERT @bn SELECT TOP(@patientLimit) b.MaBN FROM dbo.BENH_NHAN b WHERE (@patient IS NULL OR b.MaBN=@patient) AND (@reception=1 OR @LaAdmin=1
          OR (@doctor=1 AND EXISTS(SELECT 1 FROM dbo.HO_SO_BENH_AN hs JOIN dbo.PHIEU_KHAM_BENH p ON p.MaHS=hs.MaHS WHERE hs.MaBN=b.MaBN AND p.MaBS=@MaNV AND p.TrangThai<>N'Hủy'))
          OR ((@pharmacy=1 OR @finance=1) AND @doctor=0 AND EXISTS(SELECT 1 FROM dbo.HO_SO_BENH_AN hs JOIN dbo.PHIEU_KHAM_BENH p ON p.MaHS=hs.MaHS JOIN dbo.LICH_SU_KHAM_BENH l ON l.MaPhieu=p.MaPhieu WHERE hs.MaBN=b.MaBN AND l.TrangThai=N'Đã xác nhận'))) ORDER BY b.MaBN;
        DECLARE @ls TABLE(MaLS nvarchar(50) PRIMARY KEY);
        INSERT @ls SELECT TOP(@visitLimit) l.MaLS FROM dbo.LICH_SU_KHAM_BENH l JOIN dbo.PHIEU_KHAM_BENH p ON p.MaPhieu=l.MaPhieu
          JOIN dbo.HO_SO_BENH_AN hs ON hs.MaHS=p.MaHS JOIN @bn b ON b.MaBN=hs.MaBN
          WHERE (@visit IS NULL OR l.MaLS=@visit) AND (@LaAdmin=1 OR (@doctor=1 AND l.MaBS=@MaNV AND p.MaBS=@MaNV) OR l.TrangThai=N'Đã xác nhận'
          OR (@nurse=1 AND EXISTS(SELECT 1 FROM dbo.DICH_VU_THUC_HIEN d WHERE d.MaLS=l.MaLS AND d.MaNVThucHien=@MaNV))) ORDER BY l.ThoiGianKham DESC,l.MaLS;
        DECLARE @kh TABLE(MaKHDT nvarchar(50) PRIMARY KEY);
        INSERT @kh SELECT k.MaKHDT FROM dbo.KE_HOACH_DIEU_TRI k JOIN @ls l ON l.MaLS=k.MaLS
          WHERE @fullClinical=1 AND (@LaAdmin=1 OR k.TrangThai<>N'Nháp' OR (@doctor=1 AND k.MaBS=@MaNV));
        DECLARE @dt TABLE(MaDT nvarchar(50) PRIMARY KEY);
        INSERT @dt SELECT d.MaDT FROM dbo.DON_THUOC d JOIN @ls x ON x.MaLS=d.MaLS
          JOIN dbo.LICH_SU_KHAM_BENH l ON l.MaLS=d.MaLS JOIN dbo.PHIEU_KHAM_BENH p ON p.MaPhieu=l.MaPhieu
          WHERE @LaAdmin=1 OR (@doctor=1 AND d.MaBS=@MaNV AND l.MaBS=@MaNV AND p.MaBS=@MaNV)
          OR (l.TrangThai=N'Đã xác nhận' AND d.TrangThai IN(N'Đã xác nhận',N'Ngừng áp dụng'));
        DECLARE @hd TABLE(MaHD nvarchar(50) PRIMARY KEY);
        INSERT @hd SELECT h.MaHD FROM dbo.HOA_DON h JOIN @ls l ON l.MaLS=h.MaLS WHERE @finance=1;
        DECLARE @px TABLE(MaPX nvarchar(50) PRIMARY KEY);
        INSERT @px SELECT p.MaPX FROM dbo.PHIEU_XUAT p JOIN @ls l ON l.MaLS=p.MaLS WHERE @finance=1;
        SELECT b.MaBN,b.HoTenBN,b.GioiTinh,b.NgaySinh,b.SDT,b.DiaChi,b.MaBHYT,CASE WHEN @fullClinical=1 THEN b.NhomMau END NhomMau,
          CASE WHEN @fullClinical=1 THEN b.TienSuBenh END TienSuBenh,CASE WHEN @fullClinical=1 OR @pharmacy=1 THEN b.DiUng END DiUng,b.TrangThai
          FROM dbo.BENH_NHAN b JOIN @bn x ON x.MaBN=b.MaBN;
        SELECT h.MaHS,h.MaBN,h.NgayLap,CASE WHEN @fullClinical=1 THEN h.LyDoKhamBanDau ELSE N'' END LyDoKhamBanDau,
          CASE WHEN @fullClinical=1 THEN h.GhiChu END GhiChu,h.TrangThai FROM dbo.HO_SO_BENH_AN h JOIN @bn b ON b.MaBN=h.MaBN;
        SELECT TOP(@receiptLimit) p.MaPhieu,p.MaHS,p.MaLich,p.MaPhong,p.MaBS,p.MaNVTiepNhan,p.MaDV,p.SoThuTu,p.ThoiGianTiepNhan,
          CASE WHEN @fullClinical=1 THEN p.LyDoKham ELSE N'' END LyDoKham,p.TrangThai,CASE WHEN @fullClinical=1 THEN p.GhiChu END GhiChu
          FROM dbo.PHIEU_KHAM_BENH p JOIN dbo.HO_SO_BENH_AN hs ON hs.MaHS=p.MaHS JOIN @bn b ON b.MaBN=hs.MaBN
          WHERE @reception=1 OR @LaAdmin=1 OR (@doctor=1 AND p.MaBS=@MaNV)
          OR EXISTS(SELECT 1 FROM dbo.LICH_SU_KHAM_BENH l JOIN @ls x ON x.MaLS=l.MaLS WHERE l.MaPhieu=p.MaPhieu)
          ORDER BY CASE WHEN EXISTS(SELECT 1 FROM dbo.LICH_SU_KHAM_BENH l JOIN @ls x ON x.MaLS=l.MaLS WHERE l.MaPhieu=p.MaPhieu) THEN 0 ELSE 1 END,p.ThoiGianTiepNhan DESC,p.MaPhieu;
        SELECT TOP(@appointmentLimit) k.* FROM dbo.LICH_KHAM k JOIN @bn b ON b.MaBN=k.MaBN WHERE @fullClinical=1 ORDER BY k.NgayGioKham DESC,k.MaLich;
        SELECT l.MaLS,l.MaPhieu,l.MaBS,l.MaPhong,l.ThoiGianKham,
          CASE WHEN @fullClinical=1 AND (@LaAdmin=1 OR l.TrangThai=N'Đã xác nhận' OR (@doctor=1 AND l.MaBS=@MaNV)) THEN l.TrieuChung ELSE N'' END TrieuChung,
          CASE WHEN @LaAdmin=1 OR ((@fullClinical=1 OR @pharmacy=1) AND l.TrangThai=N'Đã xác nhận') OR (@doctor=1 AND l.MaBS=@MaNV) THEN l.ChanDoan ELSE N'' END ChanDoan,
          CASE WHEN @fullClinical=1 AND (@LaAdmin=1 OR l.TrangThai=N'Đã xác nhận' OR (@doctor=1 AND l.MaBS=@MaNV)) THEN l.KeHoachXuLy ELSE N'' END KeHoachXuLy,
            l.TrangThai,l.NgayXacNhan,CASE WHEN @fullClinical=1 AND (@LaAdmin=1 OR l.TrangThai=N'Đã xác nhận' OR (@doctor=1 AND l.MaBS=@MaNV)) THEN l.GhiChu END GhiChu,
            CASE WHEN @fullClinical=1 AND (@LaAdmin=1 OR l.TrangThai=N'Đã xác nhận' OR (@doctor=1 AND l.MaBS=@MaNV)) THEN l.KhongKeThuoc ELSE 0 END KhongKeThuoc,
            CASE WHEN @fullClinical=1 AND (@LaAdmin=1 OR l.TrangThai=N'Đã xác nhận' OR (@doctor=1 AND l.MaBS=@MaNV)) THEN l.LyDoKhongKeThuoc END LyDoKhongKeThuoc
          FROM dbo.LICH_SU_KHAM_BENH l JOIN @ls x ON x.MaLS=l.MaLS;
        SELECT t.* FROM dbo.TINH_TRANG_BENH t JOIN @ls x ON x.MaLS=t.MaLS JOIN dbo.LICH_SU_KHAM_BENH l ON l.MaLS=t.MaLS
          WHERE @fullClinical=1 AND (@LaAdmin=1 OR l.TrangThai=N'Đã xác nhận' OR (@doctor=1 AND l.MaBS=@MaNV));
        SELECT k.* FROM dbo.KE_HOACH_DIEU_TRI k JOIN @kh x ON x.MaKHDT=k.MaKHDT;
        SELECT c.* FROM dbo.CHI_TIET_KE_HOACH c JOIN @kh x ON x.MaKHDT=c.MaKHDT;
        SELECT d.MaDVThucHien,d.MaLS,d.MaCTKH,d.MaDV,d.MaNVThucHien,d.ThoiGianThucHien,d.SoLuong,d.DonGia,d.ThanhTien,
          CASE WHEN @fullClinical=1 THEN d.KetQua END KetQua,d.TrangThai
          FROM dbo.DICH_VU_THUC_HIEN d JOIN @ls x ON x.MaLS=d.MaLS JOIN dbo.LICH_SU_KHAM_BENH l ON l.MaLS=d.MaLS
          JOIN dbo.PHIEU_KHAM_BENH p ON p.MaPhieu=l.MaPhieu
          WHERE @LaAdmin=1 OR (@nurse=1 AND d.MaNVThucHien=@MaNV) OR (@nurse=0 AND
            ((@doctor=1 AND l.MaBS=@MaNV AND p.MaBS=@MaNV) OR d.TrangThai=N'Đã xác nhận'));
        SELECT d.MaDT,d.MaLS,d.MaBS,d.NgayKe,CASE WHEN @fullClinical=1 OR @pharmacy=1 THEN d.LyDoThayThe END LyDoThayThe,
          d.TrangThai,d.MaDTThayThe,CASE WHEN @fullClinical=1 OR @pharmacy=1 THEN d.GhiChu END GhiChu FROM dbo.DON_THUOC d JOIN @dt x ON x.MaDT=d.MaDT;
        SELECT c.MaCTDT,c.MaDT,c.MaThuoc,CASE WHEN @fullClinical=1 OR @pharmacy=1 THEN c.LieuDung ELSE N'' END LieuDung,
            c.SoLuong,c.SoNgayDung,CASE WHEN @fullClinical=1 OR @pharmacy=1 THEN c.HuongDan ELSE N'' END HuongDan,
            CASE WHEN @fullClinical=1 OR @pharmacy=1 THEN c.DuongDung END DuongDung,
            c.TenThuocChot,c.HamLuongChot,c.DonViTinhChot,
            CASE WHEN @fullClinical=1 OR @pharmacy=1 THEN c.PhienBanHuongDan END PhienBanHuongDan
          FROM dbo.CHI_TIET_DON_THUOC c JOIN @dt x ON x.MaDT=c.MaDT;
        SELECT h.* FROM dbo.HOA_DON h JOIN @hd x ON x.MaHD=h.MaHD;
        SELECT c.* FROM dbo.CHI_TIET_HOA_DON c JOIN @hd x ON x.MaHD=c.MaHD;
        SELECT t.* FROM dbo.THANH_TOAN t JOIN @hd x ON x.MaHD=t.MaHD;
        SELECT p.* FROM dbo.PHIEU_XUAT p JOIN @px x ON x.MaPX=p.MaPX;
        SELECT c.* FROM dbo.CHI_TIET_PHIEU_XUAT c JOIN @px x ON x.MaPX=c.MaPX;
        SELECT MaNV,HoTen,MaChucVu,MaCK,NgayVaoLam,TrangThai FROM dbo.NHAN_VIEN;
        SELECT * FROM dbo.PHONG; SELECT * FROM dbo.CA_LAM; SELECT * FROM dbo.DICH_VU;
        SELECT * FROM dbo.THUOC; SELECT * FROM dbo.HANG_HOA; SELECT * FROM dbo.KHO; SELECT * FROM dbo.LO_HANG_HOA;
        """;
}
