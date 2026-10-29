using Dapper;
using ET;

namespace DAL;

public sealed partial class DAL_KHAMDIEUTRI
{
    // Phiếu tiếp nhận là nguồn công việc; lịch sử khám chỉ được tạo khi bác sĩ bắt đầu.
    private const string QueueFrom = """
        FROM dbo.PHIEU_KHAM_BENH p
        JOIN dbo.HO_SO_BENH_AN hs ON hs.MaHS=p.MaHS
        JOIN dbo.BENH_NHAN b ON b.MaBN=hs.MaBN
        JOIN dbo.NHAN_VIEN n ON n.MaNV=p.MaBS
        JOIN dbo.PHONG ph ON ph.MaPhong=p.MaPhong
        LEFT JOIN dbo.DICH_VU d ON d.MaDV=p.MaDV
        LEFT JOIN dbo.LICH_SU_KHAM_BENH l ON l.MaPhieu=p.MaPhieu
        WHERE p.TrangThai IN(N'Chờ khám',N'Đang khám') AND (l.MaLS IS NULL OR l.TrangThai=N'Nháp')
          AND (@LaAdmin=1 OR (p.MaBS=@MaNV AND (l.MaLS IS NULL OR l.MaBS=@MaNV)))
          AND (@doctor IS NULL OR p.MaBS=@doctor)
          AND (@patient IS NULL OR b.MaBN=@patient) AND (@receipt IS NULL OR p.MaPhieu=@receipt)
          AND (@from IS NULL OR p.ThoiGianTiepNhan>=@from) AND (@end IS NULL OR p.ThoiGianTiepNhan<@end)
          AND (@status IS NULL OR p.TrangThai=@status)
          AND (@term=N'' OR p.MaPhieu LIKE @search ESCAPE N'~' OR b.MaBN LIKE @search ESCAPE N'~'
              OR b.HoTenBN LIKE @search ESCAPE N'~' OR n.HoTen LIKE @search ESCAPE N'~')
        """;

    public TrangHoSo<HangDoiKhamItem> LayTrangHangDoi(BoLocHangDoi boLoc,NguCanhNghiepVu nguoiDung)
        => ThucHien((db,tx) =>
        {
            var user=DAL_QUYENNGHIEPVU.XacThuc(db,tx,nguoiDung);
            if(!user.CoQuyen(QuyenLamViec.Kham))throw DAL_QUYENNGHIEPVU.TuChoi();
            if(boLoc is null || boLoc.Trang<1 || boLoc.KichThuocTrang is <1 or >100 ||
                boLoc.TuKhoa?.Length>255 || boLoc.MaBN?.Length>50 || boLoc.MaBS?.Length>50 || boLoc.MaPhieu?.Length>50 ||
                boLoc.TrangThai is not (null or "" or "Chờ khám" or "Đang khám") ||
                boLoc.TuNgay?.Date>boLoc.DenNgay?.Date || boLoc.DenNgay?.Date==DateTime.MaxValue.Date)
                throw new InvalidOperationException("Bộ lọc hàng chờ không hợp lệ; mỗi trang từ1 đến100 dòng.");
            var doctor=string.IsNullOrWhiteSpace(boLoc.MaBS)?null:boLoc.MaBS.Trim();
            if(!user.LaAdmin && doctor is not null && doctor!=user.MaNV)throw DAL_QUYENNGHIEPVU.TuChoi();
            var term=boLoc.TuKhoa?.Trim()??"";
            var search="%"+term.Replace("~","~~").Replace("%","~%").Replace("_","~_").Replace("[","~[")+"%";
            var args=new{user.MaNV,user.LaAdmin,doctor,patient=string.IsNullOrWhiteSpace(boLoc.MaBN)?null:boLoc.MaBN,
                receipt=string.IsNullOrWhiteSpace(boLoc.MaPhieu)?null:boLoc.MaPhieu,from=boLoc.TuNgay?.Date,end=boLoc.DenNgay?.Date.AddDays(1),
                status=string.IsNullOrEmpty(boLoc.TrangThai)?null:boLoc.TrangThai,term,search,
                offset=((long)boLoc.Trang-1)*boLoc.KichThuocTrang,size=boLoc.KichThuocTrang};
            using var reader=db.QueryMultiple("SELECT COUNT(*) "+QueueFrom+";"+"""
                SELECT p.MaPhieu,l.MaLS,p.MaHS,hs.MaBN,b.HoTenBN,b.NgaySinh,p.MaBS,p.MaPhong,p.MaDV,
                    p.SoThuTu,p.ThoiGianTiepNhan,p.TrangThai,p.LyDoKham,n.HoTen TenBS,ph.TenPhong,d.TenDV
                """+"\n"+QueueFrom+"\n"+"""
                ORDER BY p.ThoiGianTiepNhan,p.SoThuTu,p.MaPhieu
                OFFSET @offset ROWS FETCH NEXT @size ROWS ONLY;
                """,args,tx);
            var count=reader.ReadSingle<int>();var rows=reader.Read<HangDoiKhamItem>().ToArray();reader.Dispose();
            DAL_QUYENNGHIEPVU.GhiTraCuuAdmin(db,tx,user,"HANG_CHO_KHAM",doctor??user.MaNV);
            return new TrangHoSo<HangDoiKhamItem>(rows,count,boLoc.Trang,boLoc.KichThuocTrang);
        });
}
