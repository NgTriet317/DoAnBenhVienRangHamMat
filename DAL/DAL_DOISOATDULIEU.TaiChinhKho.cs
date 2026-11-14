namespace DAL;

/// <summary>
/// Phần quy tắc đối soát hóa đơn, thanh toán và luồng nhập/xuất kho bằng truy vấn chỉ đọc.
/// </summary>
public sealed partial class DAL_DOISOATDULIEU
{
    /// <summary>
    /// Đăng ký kiểm tra tổng hóa đơn, thu ròng theo thời điểm và quan hệ nguồn phí; không so giá lịch sử với giá danh mục hiện tại.
    /// </summary>
    private static void FinanceRules(AddCheck add,Func<string,bool> has)
    {
        add("HD_TONG","SaiXacDinh","Tổng hóa đơn không bằng tổng chi tiết; không chỉnh tổng độc lập.","""
            SELECT N'HOA_DON' Bang,h.MaHD Khoa,h.MaLS LienQuan FROM dbo.HOA_DON h
            WHERE h.TongTien<>COALESCE((SELECT SUM(c.ThanhTien) FROM dbo.CHI_TIET_HOA_DON c WHERE c.MaHD=h.MaHD),0)
            """);
        add("HD_THU_RONG","SaiXacDinh","Thu ròng đã xác nhận âm hoặc vượt phải thu; không sửa khoản thanh toán cũ.","""
            SELECT N'HOA_DON' Bang,h.MaHD Khoa,h.MaLS LienQuan FROM dbo.HOA_DON h
            CROSS APPLY(SELECT COALESCE(SUM(CASE WHEN t.LoaiGiaoDich=N'Thu' THEN t.SoTien ELSE -t.SoTien END),0) Net FROM dbo.THANH_TOAN t WHERE t.MaHD=h.MaHD AND t.TrangThai=N'Đã xác nhận') q
            WHERE q.Net<0 OR q.Net>h.SoTienPhaiThu
            """);
        add("HD_THOIGIAN","CanDoiChieu","Hóa đơn xác nhận thiếu thời điểm hoặc không theo sau lần khám; cần đối chiếu chứng từ.","""
            SELECT N'HOA_DON' Bang,h.MaHD Khoa,h.MaLS LienQuan FROM dbo.HOA_DON h JOIN dbo.LICH_SU_KHAM_BENH l ON l.MaLS=h.MaLS
            WHERE h.TrangThai=N'Đã xác nhận' AND(h.NgayXacNhan IS NULL OR h.NgayXacNhan<h.NgayLap OR h.NgayLap<l.ThoiGianKham OR l.TrangThai<>N'Đã xác nhận')
            """);
        add("TT_NGUON","SaiXacDinh","Giao dịch thu/hoàn xác nhận trỏ hóa đơn chưa xác nhận.","""
            SELECT N'THANH_TOAN' Bang,t.MaTT Khoa,t.MaHD LienQuan FROM dbo.THANH_TOAN t JOIN dbo.HOA_DON h ON h.MaHD=t.MaHD
            WHERE t.TrangThai=N'Đã xác nhận' AND h.TrangThai<>N'Đã xác nhận'
            """);
        add("TT_THOIGIAN","CanDoiChieu","Thời điểm thu/hoàn trước xác nhận hóa đơn hoặc tương lai.","""
            SELECT N'THANH_TOAN' Bang,t.MaTT Khoa,t.MaHD LienQuan FROM dbo.THANH_TOAN t JOIN dbo.HOA_DON h ON h.MaHD=t.MaHD
            WHERE t.TrangThai=N'Đã xác nhận' AND(t.NgayThanhToan<h.NgayXacNhan OR t.NgayThanhToan>SYSDATETIME())
            """);
        add("TT_SO_DU_LICHSU","SaiXacDinh","Thu ròng từng thời điểm âm/vượt phải thu; gộp cùng timestamp vì không biết thứ tự nội bộ.","""
            SELECT N'THANH_TOAN' Bang,t.MaTT Khoa,t.MaHD LienQuan FROM dbo.THANH_TOAN t JOIN dbo.HOA_DON h ON h.MaHD=t.MaHD
            JOIN(SELECT MaHD,NgayThanhToan,SUM(Delta) OVER(PARTITION BY MaHD ORDER BY NgayThanhToan ROWS UNBOUNDED PRECEDING) Net FROM(
             SELECT MaHD,NgayThanhToan,SUM(CASE WHEN LoaiGiaoDich=N'Thu' THEN SoTien ELSE -SoTien END) Delta
             FROM dbo.THANH_TOAN WHERE TrangThai=N'Đã xác nhận' GROUP BY MaHD,NgayThanhToan) moments) b
             ON b.MaHD=t.MaHD AND b.NgayThanhToan=t.NgayThanhToan WHERE t.TrangThai=N'Đã xác nhận' AND(b.Net<0 OR b.Net>h.SoTienPhaiThu)
            """);
        add("HD_NGUON_DV","SaiXacDinh","Dòng dịch vụ không đúng lượt/trạng thái/thành tiền của nguồn đã chốt.","""
            SELECT N'CHI_TIET_HOA_DON' Bang,c.MaCTHD Khoa,c.MaHD+N' / '+c.MaDVThucHien LienQuan
            FROM dbo.CHI_TIET_HOA_DON c JOIN dbo.HOA_DON h ON h.MaHD=c.MaHD JOIN dbo.DICH_VU_THUC_HIEN s ON s.MaDVThucHien=c.MaDVThucHien
            WHERE s.MaLS<>h.MaLS OR s.TrangThai<>N'Đã xác nhận' OR s.ThanhTien<>c.ThanhTien
            """);
        add("HD_NGUON_THUOC","SaiXacDinh","Dòng thuốc lệch lượt/đơn/xuất/trạng thái/lượng/thời gian hoặc không bằng lượng nguồn nhân giá chốt; không so giá danh mục hiện tại.","""
            SELECT N'CHI_TIET_HOA_DON' Bang,c.MaCTHD Khoa,c.MaHD+N' / '+COALESCE(c.MaCTPX,c.MaCTDT) LienQuan
            FROM dbo.CHI_TIET_HOA_DON c JOIN dbo.HOA_DON h ON h.MaHD=c.MaHD
            LEFT JOIN dbo.CHI_TIET_PHIEU_XUAT x ON x.MaCTPX=c.MaCTPX LEFT JOIN dbo.PHIEU_XUAT p ON p.MaPX=x.MaPX
            LEFT JOIN dbo.CHI_TIET_DON_THUOC r ON r.MaCTDT=COALESCE(c.MaCTDT,x.MaCTDT) LEFT JOIN dbo.DON_THUOC d ON d.MaDT=r.MaDT
            LEFT JOIN dbo.THUOC t ON t.MaThuoc=r.MaThuoc
            WHERE(c.MaCTPX IS NOT NULL OR c.MaCTDT IS NOT NULL) AND(
             r.MaCTDT IS NULL OR c.DonGia IS NULL OR d.MaLS<>h.MaLS OR d.TrangThai<>N'Đã xác nhận' OR d.NgayKe>h.NgayLap
             OR(c.MaCTPX IS NOT NULL AND(x.MaCTPX IS NULL OR p.MaLS<>h.MaLS OR p.TrangThai<>N'Đã ghi sổ' OR x.MaHH<>t.MaHH
              OR c.ThanhTien<>CAST(ROUND(x.SoLuong*c.DonGia,2) AS decimal(18,2)) OR p.NgayXuat>COALESCE(h.NgayXacNhan,h.NgayLap)))
             OR(c.MaCTDT IS NOT NULL AND(h.TrangThai=N'Đã xác nhận' OR c.ThanhTien<>CAST(ROUND(r.SoLuong*c.DonGia,2) AS decimal(18,2)))))
            """);
        add("HD_TRUNG_NGUON_THUOC","SaiXacDinh","Cùng chi tiết đơn bị tính cả nguồn chờ cấp MaCTDT và nguồn đã xuất MaCTPX trên hóa đơn chưa hủy.","""
            SELECT N'CHI_TIET_HOA_DON' Bang,c.MaCTHD Khoa,c.MaHD+N' / '+c.MaCTDT LienQuan FROM dbo.CHI_TIET_HOA_DON c JOIN dbo.HOA_DON h ON h.MaHD=c.MaHD
            WHERE c.MaCTDT IS NOT NULL AND h.TrangThai<>N'Hủy' AND EXISTS(
             SELECT 1 FROM dbo.CHI_TIET_HOA_DON other JOIN dbo.HOA_DON bill ON bill.MaHD=other.MaHD
             JOIN dbo.CHI_TIET_PHIEU_XUAT x ON x.MaCTPX=other.MaCTPX
             WHERE x.MaCTDT=c.MaCTDT AND bill.TrangThai<>N'Hủy' AND other.MaCTHD<>c.MaCTHD)
            """);
        if(has("CHI_TIET_HOA_DON.MaPhieuPhuThu"))
            add("HD_PHUTHU","SaiXacDinh","Hóa đơn xác nhận dịch vụ phải có đúng một phụ thu30000 đúng phiếu; phiếu thường không có phụ thu.","""
                SELECT N'HOA_DON' Bang,h.MaHD Khoa,p.MaPhieu LienQuan FROM dbo.HOA_DON h JOIN dbo.LICH_SU_KHAM_BENH l ON l.MaLS=h.MaLS
                JOIN dbo.PHIEU_KHAM_BENH p ON p.MaPhieu=l.MaPhieu WHERE h.TrangThai=N'Đã xác nhận' AND(
                 (p.LoaiKham=N'Dịch vụ' AND(SELECT COUNT(*) FROM dbo.CHI_TIET_HOA_DON c WHERE c.MaHD=h.MaHD AND c.MaPhieuPhuThu=p.MaPhieu AND c.ThanhTien=30000)<>1)
                 OR EXISTS(SELECT 1 FROM dbo.CHI_TIET_HOA_DON c WHERE c.MaHD=h.MaHD AND c.MaPhieuPhuThu IS NOT NULL AND(c.MaPhieuPhuThu<>p.MaPhieu OR p.LoaiKham<>N'Dịch vụ' OR c.ThanhTien<>30000)))
                """);
    }
    /// <summary>
    /// Đăng ký kiểm tra tổng nhập, tồn hiện tại và tồn lịch sử cùng nguồn đơn thuốc, lô và thời điểm xuất.
    /// </summary>
    /// <remarks>Gộp biến động cùng thời điểm trước khi tính tồn lịch sử; kết quả báo vi phạm để đối chiếu, không tự bù tồn.</remarks>
    private static void InventoryRules(AddCheck add)
    {
        add("NHAP_TONG","SaiXacDinh","Tổng phiếu nhập không bằng chi tiết.","""
            SELECT N'PHIEU_NHAP' Bang,p.MaPN Khoa,p.MaHDN LienQuan FROM dbo.PHIEU_NHAP p
            WHERE p.TongTien<>COALESCE((SELECT SUM(c.ThanhTien) FROM dbo.CHI_TIET_PHIEU_NHAP c WHERE c.MaPN=p.MaPN),0)
            """);
        add("KHO_AM","SaiXacDinh","Tồn hiện tại theo hàng/kho/lô âm; không tự bù nhập kho.","""
            SELECT N'KHO' Bang,q.MaKho+N' / '+q.MaHH+N' / '+COALESCE(q.MaLo,N'[không lô]') Khoa,N'Nhập ghi sổ - xuất ghi sổ' LienQuan
            FROM(SELECT MaKho,MaHH,MaLo,SUM(SoLuong) Ton FROM(
             SELECT p.MaKho,c.MaHH,c.MaLo,c.SoLuong FROM dbo.CHI_TIET_PHIEU_NHAP c JOIN dbo.PHIEU_NHAP p ON p.MaPN=c.MaPN WHERE p.TrangThai=N'Đã ghi sổ'
             UNION ALL SELECT p.MaKho,c.MaHH,c.MaLo,-c.SoLuong FROM dbo.CHI_TIET_PHIEU_XUAT c JOIN dbo.PHIEU_XUAT p ON p.MaPX=c.MaPX WHERE p.TrangThai=N'Đã ghi sổ') s GROUP BY MaKho,MaHH,MaLo) q WHERE q.Ton<0
            """);
        add("KHO_AM_LICHSU","SaiXacDinh","PK dòng xuất ở mốc tồn lịch sử âm, dù tồn cuối đủ; gộp biến động cùng timestamp.","""
            SELECT N'CHI_TIET_PHIEU_XUAT' Bang,x.MaCTPX Khoa,p.MaPX+N' / '+p.MaKho+N' / '+x.MaHH+N' / '+COALESCE(x.MaLo,N'[không lô]') LienQuan
            FROM dbo.CHI_TIET_PHIEU_XUAT x JOIN dbo.PHIEU_XUAT p ON p.MaPX=x.MaPX
            JOIN(SELECT MaKho,MaHH,MaLo,Ngay,SUM(Delta) OVER(PARTITION BY MaKho,MaHH,MaLo ORDER BY Ngay ROWS UNBOUNDED PRECEDING) Ton FROM(
             SELECT MaKho,MaHH,MaLo,Ngay,SUM(Delta) Delta FROM(
              SELECT p.MaKho,c.MaHH,c.MaLo,p.NgayNhap Ngay,c.SoLuong Delta FROM dbo.CHI_TIET_PHIEU_NHAP c JOIN dbo.PHIEU_NHAP p ON p.MaPN=c.MaPN WHERE p.TrangThai=N'Đã ghi sổ'
              UNION ALL SELECT p.MaKho,c.MaHH,c.MaLo,p.NgayXuat,-c.SoLuong FROM dbo.CHI_TIET_PHIEU_XUAT c JOIN dbo.PHIEU_XUAT p ON p.MaPX=c.MaPX WHERE p.TrangThai=N'Đã ghi sổ') events
              GROUP BY MaKho,MaHH,MaLo,Ngay) moments) b ON b.MaKho=p.MaKho AND b.MaHH=x.MaHH AND(b.MaLo=x.MaLo OR(b.MaLo IS NULL AND x.MaLo IS NULL)) AND b.Ngay=p.NgayXuat
            WHERE p.TrangThai=N'Đã ghi sổ' AND b.Ton<0
            """);
        add("KHO_NGUON_THUOC","SaiXacDinh","Xuất lâm sàng ghi sổ sai đơn/lượt/mặt hàng hoặc đơn chưa xác nhận.","""
            SELECT N'CHI_TIET_PHIEU_XUAT' Bang,x.MaCTPX Khoa,p.MaPX+N' / '+p.MaLS LienQuan
            FROM dbo.CHI_TIET_PHIEU_XUAT x JOIN dbo.PHIEU_XUAT p ON p.MaPX=x.MaPX LEFT JOIN dbo.CHI_TIET_DON_THUOC c ON c.MaCTDT=x.MaCTDT
            LEFT JOIN dbo.DON_THUOC d ON d.MaDT=c.MaDT LEFT JOIN dbo.THUOC t ON t.MaThuoc=c.MaThuoc
            WHERE p.TrangThai=N'Đã ghi sổ' AND p.MaLS IS NOT NULL AND(c.MaCTDT IS NULL OR d.MaLS<>p.MaLS OR t.MaHH<>x.MaHH OR d.TrangThai<>N'Đã xác nhận')
            """);
        add("KHO_LO_THOIGIAN","CanDoiChieu","Xuất trước ngày nhập lô, trước ngày kê hoặc sau hạn dùng theo dữ liệu lô hiện có.","""
            SELECT N'CHI_TIET_PHIEU_XUAT' Bang,x.MaCTPX Khoa,p.MaPX+N' / '+x.MaLo LienQuan
            FROM dbo.CHI_TIET_PHIEU_XUAT x JOIN dbo.PHIEU_XUAT p ON p.MaPX=x.MaPX JOIN dbo.LO_HANG_HOA l ON l.MaLo=x.MaLo
            LEFT JOIN dbo.CHI_TIET_DON_THUOC c ON c.MaCTDT=x.MaCTDT LEFT JOIN dbo.DON_THUOC d ON d.MaDT=c.MaDT
            WHERE p.TrangThai=N'Đã ghi sổ' AND(p.NgayXuat<l.NgayNhap OR p.NgayXuat<d.NgayKe OR CONVERT(date,p.NgayXuat)>l.HanSuDung)
            """);
    }
}
