namespace DAL;

/// <summary>
/// Phần quy tắc đối soát liên kết, trạng thái và chất lượng mã của quy trình khám; chỉ đăng ký truy vấn đọc.
/// </summary>
public sealed partial class DAL_DOISOATDULIEU
{
    /// <summary>
    /// Đăng ký các kiểm tra lâm sàng theo schema hiện có, giữ riêng lỗi quan hệ và dữ liệu cần đối chiếu lịch sử.
    /// </summary>
    private static void ClinicalRules(AddCheck add,Func<string,bool> has)
    {
        add("BN_NGAY_SINH","CanDoiChieu","Ngày sinh tương lai hoặc tuổi trên120; không tự sửa hoặc suy đoán ngày sinh.","""
            SELECT N'BENH_NHAN' Bang,MaBN Khoa,N'NgaySinh' LienQuan FROM dbo.BENH_NHAN
            WHERE NgaySinh>CONVERT(date,SYSDATETIME()) OR NgaySinh<DATEADD(year,-120,CONVERT(date,SYSDATETIME()))
            """);
        add("BN_NGHI_TRUNG","CanDoiChieu","Trùng tên/ngày sinh/số điện thoại không rỗng chỉ là nghi vấn; không gộp hay ẩn hồ sơ.","""
            SELECT N'BENH_NHAN' Bang,a.MaBN Khoa,b.MaBN LienQuan FROM dbo.BENH_NHAN a JOIN dbo.BENH_NHAN b ON a.MaBN<b.MaBN
             AND a.HoTenBN=b.HoTenBN AND a.NgaySinh=b.NgaySinh AND a.SDT=b.SDT WHERE NULLIF(LTRIM(RTRIM(a.SDT)),N'') IS NOT NULL
            """);
        add("HS_THOIGIAN","CanDoiChieu","Ngày lập hồ sơ trước ngày sinh hoặc tương lai; cần kiểm nguồn hồ sơ.","""
            SELECT N'HO_SO_BENH_AN' Bang,h.MaHS Khoa,h.MaBN LienQuan FROM dbo.HO_SO_BENH_AN h JOIN dbo.BENH_NHAN b ON b.MaBN=h.MaBN
            WHERE h.NgayLap<CONVERT(date,b.NgaySinh) OR h.NgayLap>SYSDATETIME()
            """);
        add("LS_THOIGIAN","CanDoiChieu","Thứ tự tiếp nhận/khám/xác nhận chưa hợp lý hoặc lượt xác nhận thiếu thời điểm; không tự hoàn tất lượt.","""
            SELECT N'LICH_SU_KHAM_BENH' Bang,l.MaLS Khoa,l.MaPhieu LienQuan FROM dbo.LICH_SU_KHAM_BENH l JOIN dbo.PHIEU_KHAM_BENH p ON p.MaPhieu=l.MaPhieu
            WHERE l.ThoiGianKham<p.ThoiGianTiepNhan OR l.ThoiGianKham>SYSDATETIME()
             OR(l.TrangThai=N'Đã xác nhận' AND(l.NgayXacNhan IS NULL OR l.NgayXacNhan<l.ThoiGianKham OR l.NgayXacNhan>SYSDATETIME()))
            """);
        add("LS_BACSI","CanDoiChieu","Bác sĩ phiếu/lượt khác nhau hoặc chức vụ/chuyên khoa hiện tại thiếu; danh mục hiện tại không chứng minh lịch sử sai.","""
            SELECT N'LICH_SU_KHAM_BENH' Bang,l.MaLS Khoa,l.MaBS+N' / '+p.MaBS LienQuan
            FROM dbo.LICH_SU_KHAM_BENH l JOIN dbo.PHIEU_KHAM_BENH p ON p.MaPhieu=l.MaPhieu JOIN dbo.NHAN_VIEN n ON n.MaNV=l.MaBS
            WHERE l.MaBS<>p.MaBS OR n.MaChucVu<>N'CV002' OR n.MaCK IS NULL
            """);
        add("NS_HIEN_TAI","CanDoiChieu","Nhân viên/tài khoản bác sĩ hiện không hoạt động; không suy ra lượt khám trong quá khứ sai.","""
            SELECT N'LICH_SU_KHAM_BENH' Bang,l.MaLS Khoa,l.MaBS LienQuan FROM dbo.LICH_SU_KHAM_BENH l JOIN dbo.NHAN_VIEN n ON n.MaNV=l.MaBS
            WHERE n.TrangThai<>N'Đang làm việc' OR NOT EXISTS(SELECT 1 FROM dbo.TAI_KHOAN t WHERE t.MaNV=n.MaNV AND t.TrangThai=N'Đang hoạt động')
            """);
        add("BENH_THIEU_MA","CanDoiChieu","Dòng tình trạng của lượt xác nhận thiếu mã; báo chất lượng, không đoán mã từ chẩn đoán.","""
            SELECT N'TINH_TRANG_BENH' Bang,t.MaTTB Khoa,t.MaLS LienQuan FROM dbo.TINH_TRANG_BENH t JOIN dbo.LICH_SU_KHAM_BENH l ON l.MaLS=t.MaLS
            WHERE l.TrangThai=N'Đã xác nhận' AND t.MaDMTT IS NULL
            """);
        add("BENH_KHONG_GHI_NHAN","CanDoiChieu","Lượt xác nhận không có dòng tình trạng có mã; không coi là không có bệnh.","""
            SELECT N'LICH_SU_KHAM_BENH' Bang,l.MaLS Khoa,l.MaPhieu LienQuan FROM dbo.LICH_SU_KHAM_BENH l
            WHERE l.TrangThai=N'Đã xác nhận' AND NOT EXISTS(SELECT 1 FROM dbo.TINH_TRANG_BENH t WHERE t.MaLS=l.MaLS AND t.MaDMTT IS NOT NULL)
            """);
        add("BENH_TRUNG_MA","CanDoiChieu","Cùng mã tình trạng lặp trên lượt; báo từng PK, thống kê vẫn phải DISTINCT MaLS/MaDMTT.","""
            SELECT N'TINH_TRANG_BENH' Bang,t.MaTTB Khoa,t.MaLS+N' / '+t.MaDMTT LienQuan FROM dbo.TINH_TRANG_BENH t
            JOIN dbo.LICH_SU_KHAM_BENH l ON l.MaLS=t.MaLS WHERE l.TrangThai=N'Đã xác nhận' AND t.MaDMTT IS NOT NULL
             AND EXISTS(SELECT 1 FROM dbo.TINH_TRANG_BENH x WHERE x.MaLS=t.MaLS AND x.MaDMTT=t.MaDMTT AND x.MaTTB<>t.MaTTB)
            """);
        add("DV_HIEULUC","CanDoiChieu","Dịch vụ thực hiện trước ngày áp dụng danh mục hiện tại; cần giá/nguồn lịch sử, không hồi tố giá.","""
            SELECT N'DICH_VU_THUC_HIEN' Bang,s.MaDVThucHien Khoa,s.MaLS+N' / '+s.MaDV LienQuan
            FROM dbo.DICH_VU_THUC_HIEN s JOIN dbo.DICH_VU d ON d.MaDV=s.MaDV
            WHERE s.TrangThai=N'Đã xác nhận' AND CONVERT(date,s.ThoiGianThucHien)<d.NgayApDung
            """);
        if(has("PHIEU_KHAM_BENH.LoaiKham"))
            add("TN_DICHVU_LICH","CanDoiChieu","Phiếu dịch vụ chưa đối chiếu được ca/phòng/hiện diện ở thời điểm tiếp nhận; lịch hiện tại có thể đã thay đổi.","""
                SELECT N'PHIEU_KHAM_BENH' Bang,p.MaPhieu Khoa,p.MaBS+N' / '+p.MaPhong LienQuan FROM dbo.PHIEU_KHAM_BENH p
                WHERE p.LoaiKham=N'Dịch vụ' AND p.TrangThai<>N'Hủy' AND NOT EXISTS(
                  SELECT 1 FROM dbo.LICH_LAM_VIEC v JOIN dbo.CA_LAM c ON c.MaCa=v.MaCa WHERE v.MaNV=p.MaBS AND v.MaPhong=p.MaPhong
                   AND v.TrangThai IN(N'Đã xác nhận',N'Đang hoạt động') AND v.TrangThaiHienDien=N'Có mặt'
                   AND p.ThoiGianTiepNhan>=DATEADD(second,DATEDIFF(second,CONVERT(time,'00:00'),c.GioBatDau),CONVERT(datetime2,v.NgayLam))
                   AND p.ThoiGianTiepNhan<DATEADD(day,CASE WHEN c.GioKetThuc<=c.GioBatDau THEN 1 ELSE 0 END,DATEADD(second,DATEDIFF(second,CONVERT(time,'00:00'),c.GioKetThuc),CONVERT(datetime2,v.NgayLam))))
                """);
    }
}
