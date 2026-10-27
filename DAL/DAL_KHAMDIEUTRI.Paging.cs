using System.Globalization;
using Dapper;
using ET;
namespace DAL;
public sealed partial class DAL_KHAMDIEUTRI
{
    private const string PatientScopeSql="""
        WITH patients AS (SELECT b.* FROM dbo.BENH_NHAN b WHERE @reception=1 OR @LaAdmin=1
          OR (@doctor=1 AND EXISTS(SELECT 1 FROM dbo.HO_SO_BENH_AN hs JOIN dbo.PHIEU_KHAM_BENH p ON p.MaHS=hs.MaHS WHERE hs.MaBN=b.MaBN AND p.MaBS=@MaNV AND p.TrangThai<>N'Hủy'))
          OR (@finance=1 AND @doctor=0 AND EXISTS(SELECT 1 FROM dbo.HO_SO_BENH_AN hs JOIN dbo.PHIEU_KHAM_BENH p ON p.MaHS=hs.MaHS JOIN dbo.LICH_SU_KHAM_BENH l ON l.MaPhieu=p.MaPhieu WHERE hs.MaBN=b.MaBN AND l.TrangThai=N'Đã xác nhận')))
        """;
    private TrangHoSo<T> ReadPage<T>(BoLocDanhSachHoSo filter,NguCanhNghiepVu actor,string source,string fields,string order,Action<System.Data.IDbConnection,System.Data.IDbTransaction?,T[]>? project=null,bool prescriptionHistory=false)=>ThucHien((db,tx)=>
    {
        var user=DAL_QUYENNGHIEPVU.XacThuc(db,tx,actor);
        if(user.Quyen==QuyenLamViec.Khong&&!user.LaAdmin||filter is null||filter.Trang<1||filter.KichThuocTrang is <1 or >100||filter.TuKhoa?.Length>255||filter.MaBN?.Length>50||filter.MaLS?.Length>50||filter.TrangThai?.Length>50||filter.GioiTinh?.Length>20||filter.TuNgay>filter.DenNgay||filter.DenNgay?.Date==DateTime.MaxValue.Date)
            throw new InvalidOperationException("Bộ lọc không hợp lệ; mỗi trang từ 1 đến 100 dòng.");
        var doctor=user.CoQuyen(QuyenLamViec.Kham);var reception=user.CoQuyen(QuyenLamViec.TiepNhan);var finance=doctor||user.CoQuyen(QuyenLamViec.ThuTien)||user.CoQuyen(QuyenLamViec.GiaoThuoc)||user.LaAdmin;
        if(prescriptionHistory&&!doctor&&!reception&&!user.CoQuyen(QuyenLamViec.GiaoThuoc)&&!user.LaAdmin)throw DAL_QUYENNGHIEPVU.TuChoi();
        var term=filter.TuKhoa?.Trim()??"";var search="%"+term.Replace("~","~~").Replace("%","~%").Replace("_","~_").Replace("[","~[")+"%";
        var parsed=DateTime.TryParseExact(term,["dd/MM/yyyy","d/M/yyyy","yyyy-MM-dd"],CultureInfo.InvariantCulture,DateTimeStyles.None,out var birth);
        var p=new{user.MaNV,user.LaAdmin,doctor,reception,finance,fullClinical=doctor||reception||user.LaAdmin,nurse=user.CoQuyen(QuyenLamViec.CapNhatKetQuaDuocGiao),onlyMine=filter.ChiCuaToi,term,search,birth=parsed?(DateTime?)birth:null,patient=filter.MaBN,visit=filter.MaLS,status=string.IsNullOrWhiteSpace(filter.TrangThai)?null:filter.TrangThai,gender=string.IsNullOrWhiteSpace(filter.GioiTinh)?null:filter.GioiTinh,from=filter.TuNgay?.Date,end=filter.DenNgay?.Date.AddDays(1),offset=((long)filter.Trang-1)*filter.KichThuocTrang,size=filter.KichThuocTrang};
        using var reader=db.QueryMultiple(PatientScopeSql+", matches AS ("+source+") SELECT COUNT(*) FROM matches OPTION(RECOMPILE);"+PatientScopeSql+", matches AS ("+source+") SELECT "+fields+" FROM matches ORDER BY "+order+" OFFSET @offset ROWS FETCH NEXT @size ROWS ONLY OPTION(RECOMPILE)",p,tx);
        var total=reader.ReadSingle<int>();var rows=reader.Read<T>().ToArray();
        reader.Dispose();project?.Invoke(db,tx,rows);
        DAL_QUYENNGHIEPVU.GhiTraCuuAdmin(db,tx,user,"TRANG_HO_SO",typeof(T).Name);
        return new TrangHoSo<T>(rows,total,filter.Trang,filter.KichThuocTrang);
    });
    public TrangHoSo<DongBenhNhanDanhSach> LayTrangBenhNhan(BoLocDanhSachHoSo filter,NguCanhNghiepVu actor)=>ReadPage<DongBenhNhanDanhSach>(filter,actor,"""
        SELECT b.MaBN,b.HoTenBN,b.NgaySinh,b.GioiTinh,b.SDT,b.DiaChi,b.TrangThai,
         (SELECT MAX(l.ThoiGianKham) FROM dbo.HO_SO_BENH_AN hs JOIN dbo.PHIEU_KHAM_BENH p ON p.MaHS=hs.MaHS JOIN dbo.LICH_SU_KHAM_BENH l ON l.MaPhieu=p.MaPhieu
          WHERE hs.MaBN=b.MaBN AND (l.TrangThai=N'Đã xác nhận' OR (@doctor=1 AND l.MaBS=@MaNV AND p.MaBS=@MaNV))) LanKhamGanNhat
        FROM patients b WHERE (@status IS NULL OR b.TrangThai=@status) AND (@gender IS NULL OR b.GioiTinh=@gender)
          AND (@patient IS NULL OR b.MaBN=@patient) AND (@term=N'' OR b.MaBN LIKE @search ESCAPE N'~' OR b.HoTenBN LIKE @search ESCAPE N'~' OR b.SDT LIKE @search ESCAPE N'~' OR b.NgaySinh=@birth)
        ""","*","HoTenBN,MaBN");
    public TrangHoSo<DongLanKhamDanhSach> LayTrangLanKham(BoLocDanhSachHoSo filter,NguCanhNghiepVu actor)=>ReadPage<DongLanKhamDanhSach>(filter,actor,"""
        SELECT l.MaLS,l.MaPhieu,l.MaBS,l.MaPhong,l.ThoiGianKham,l.TrangThai,l.NgayXacNhan,b.MaBN,b.HoTenBN,n.HoTen TenBS,
          CASE WHEN @fullClinical=1 AND(l.TrangThai=N'Đã xác nhận' OR(@doctor=1 AND l.MaBS=@MaNV AND p.MaBS=@MaNV)) THEN l.ChanDoan ELSE N'' END ChanDoan,CASE WHEN p.MaLich IS NULL THEN N'Trực tiếp' ELSE N'Lịch hẹn #'+p.MaLich END NguonTiepNhan
        FROM dbo.LICH_SU_KHAM_BENH l JOIN dbo.PHIEU_KHAM_BENH p ON p.MaPhieu=l.MaPhieu JOIN dbo.HO_SO_BENH_AN hs ON hs.MaHS=p.MaHS JOIN patients b ON b.MaBN=hs.MaBN JOIN dbo.NHAN_VIEN n ON n.MaNV=l.MaBS
        WHERE ((@doctor=1 AND l.MaBS=@MaNV AND p.MaBS=@MaNV) OR l.TrangThai=N'Đã xác nhận' OR (@nurse=1 AND EXISTS(SELECT 1 FROM dbo.DICH_VU_THUC_HIEN d WHERE d.MaLS=l.MaLS AND d.MaNVThucHien=@MaNV)))
          AND (@doctor=0 OR l.MaBS=@MaNV OR ((@onlyMine=0 OR @visit=l.MaLS) AND l.TrangThai=N'Đã xác nhận'))
          AND (@status IS NULL OR l.TrangThai=@status) AND (@patient IS NULL OR b.MaBN=@patient) AND (@visit IS NULL OR l.MaLS=@visit)
          AND (@from IS NULL OR l.ThoiGianKham>=@from) AND (@end IS NULL OR l.ThoiGianKham<@end)
          AND (@term=N'' OR l.MaLS LIKE @search ESCAPE N'~' OR b.MaBN LIKE @search ESCAPE N'~' OR b.HoTenBN LIKE @search ESCAPE N'~' OR (@fullClinical=1 AND(l.TrangThai=N'Đã xác nhận' OR(@doctor=1 AND l.MaBS=@MaNV AND p.MaBS=@MaNV)) AND l.ChanDoan LIKE @search ESCAPE N'~') OR (@finance=1 AND EXISTS(SELECT 1 FROM dbo.HOA_DON h WHERE h.MaLS=l.MaLS AND h.MaHD LIKE @search ESCAPE N'~')))
        ""","*","ThoiGianKham DESC,MaLS");
    public TrangHoSo<DongLichSuDon> LayTrangLichSuDon(BoLocDanhSachHoSo filter,NguCanhNghiepVu actor)=>ReadPage<DongLichSuDon>(filter,actor,"""
        SELECT d.*,b.MaBN,b.HoTenBN,b.SDT,b.NgaySinh,l.ThoiGianKham,l.ChanDoan,n.HoTen TenBS
        FROM dbo.DON_THUOC d JOIN dbo.LICH_SU_KHAM_BENH l ON l.MaLS=d.MaLS JOIN dbo.PHIEU_KHAM_BENH p ON p.MaPhieu=l.MaPhieu JOIN dbo.HO_SO_BENH_AN hs ON hs.MaHS=p.MaHS JOIN patients b ON b.MaBN=hs.MaBN JOIN dbo.NHAN_VIEN n ON n.MaNV=d.MaBS
        WHERE ((@doctor=1 AND d.MaBS=@MaNV AND l.MaBS=@MaNV AND p.MaBS=@MaNV) OR (l.TrangThai=N'Đã xác nhận' AND d.TrangThai IN(N'Đã xác nhận',N'Ngừng áp dụng')))
          AND (@patient IS NULL OR b.MaBN=@patient) AND (@visit IS NULL OR d.MaLS=@visit) AND (@status IS NULL OR d.TrangThai=@status)
          AND (@term=N'' OR d.MaDT LIKE @search ESCAPE N'~' OR b.MaBN LIKE @search ESCAPE N'~' OR b.HoTenBN LIKE @search ESCAPE N'~' OR b.SDT LIKE @search ESCAPE N'~' OR n.HoTen LIKE @search ESCAPE N'~')
        ""","*","NgayKe DESC,MaDT",(db,tx,rows)=>
        {
            if(rows.Length==0)return;
            var lines=db.Query<ET_CHITIETDONTHUOC>("SELECT * FROM dbo.CHI_TIET_DON_THUOC WHERE MaDT IN @ids ORDER BY MaDT,MaCTDT",new{ids=rows.Select(r=>r.MaDT).ToArray()},tx).ToLookup(d=>d.MaDT);
            foreach(var row in rows)row.CacDong=lines[row.MaDT].ToArray();
        },true);
    public IReadOnlyList<string> LayMaLuotHoaDon(string? maLS,NguCanhNghiepVu actor)=>ThucHien((db,tx)=>
    {
        var user=DAL_QUYENNGHIEPVU.XacThuc(db,tx,actor);if(maLS is not null)DAL_QUYENNGHIEPVU.KiemTraMa(maLS);
        if(user.Quyen==QuyenLamViec.Khong&&!user.LaAdmin)throw DAL_QUYENNGHIEPVU.TuChoi();
        var doctor=user.CoQuyen(QuyenLamViec.Kham);var reception=user.CoQuyen(QuyenLamViec.TiepNhan);var finance=doctor||user.CoQuyen(QuyenLamViec.ThuTien)||user.CoQuyen(QuyenLamViec.GiaoThuoc)||user.LaAdmin;
        var rows=db.Query<string>(PatientScopeSql+"""
            SELECT DISTINCT h.MaLS FROM dbo.HOA_DON h JOIN dbo.LICH_SU_KHAM_BENH l ON l.MaLS=h.MaLS
            JOIN dbo.PHIEU_KHAM_BENH p ON p.MaPhieu=l.MaPhieu JOIN dbo.HO_SO_BENH_AN hs ON hs.MaHS=p.MaHS JOIN patients b ON b.MaBN=hs.MaBN
            WHERE @finance=1 AND (@visit IS NULL OR h.MaLS=@visit)
              AND ((@doctor=1 AND l.MaBS=@MaNV AND p.MaBS=@MaNV) OR l.TrangThai=N'Đã xác nhận')
            ORDER BY h.MaLS OPTION(RECOMPILE)
            """,new{user.MaNV,user.LaAdmin,doctor,reception,finance,visit=maLS},tx).ToArray();
        DAL_QUYENNGHIEPVU.GhiTraCuuAdmin(db,tx,user,"DANH_SACH_HOA_DON",maLS??user.MaNV);return (IReadOnlyList<string>)rows;
    });
}
