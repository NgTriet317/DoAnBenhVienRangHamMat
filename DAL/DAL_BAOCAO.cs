using Dapper;
using ET;

namespace DAL;

/// <summary>Các truy vấn SELECT báo cáo; dùng cấu hình kết nối database chung của dự án.</summary>
public sealed class DAL_BAOCAO : dbconnect
{
    /// <summary>
    /// Đọc báo cáo theo chuyên đề, thời gian và các bộ lọc được truyền vào; ánh xạ dữ liệu cho bước tổng hợp/hiển thị.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>MoKetNoi</c>, <c>connection.Query&lt;ET_DONGBAOCAO&gt;</c>, <c>denNgay.Date.AddDays</c>, <c>tuNgay.Date.AddDays</c>.</remarks>
    /// <param name="ma">Mã định danh của bản ghi cần xử lý.</param>
    /// <param name="tuNgay">Giá trị từ ngày dùng khi thực hiện thao tác; kiểu DateTime.</param>
    /// <param name="denNgay">Giá trị đến ngày dùng khi thực hiện thao tác; kiểu DateTime.</param>
    /// <param name="canhBao">Giá trị cảnh báo dùng khi thực hiện thao tác; kiểu int.</param>
    /// <returns>Tập dữ liệu kiểu ET_DONGBAOCAO[] theo điều kiện và phạm vi xử lý của hàm.</returns>
    public ET_DONGBAOCAO[] Doc(string ma, DateTime tuNgay, DateTime denNgay, int canhBao)
    {
        // Chỉ chọn SQL có sẵn; mã chuyên đề không được nối vào câu truy vấn.
        var sql = ma switch
        {
            "11" => """
                SELECT bl.NgayChot AS Ngay, bl.LuongTheoCa AS Mot, bl.TienHoaHong AS Hai,
                    bl.TongLuong AS Ba, bl.SoCaTinh AS Bon, bl.MaNV + N' · ' + nv.HoTen AS Nhom, bl.TongLuong AS GiaTriCoCau,
                    bl.MaBL AS Ma, nv.HoTen AS Ten, bl.MaNV AS PhanLoai, N'VNĐ' AS DonVi,
                    bl.NgayChot AS NgayChiTiet, bl.DenNgay AS DenNgayChiTiet,
                    bl.LuongTheoCa AS ChiTietMot, bl.TienHoaHong AS ChiTietHai,
                    bl.TongLuong AS ChiTietBa, bl.SoCaTinh AS ChiTietBon, bl.TrangThai,
                    N'Kỳ lương: ' + CONVERT(nvarchar(10), bl.TuNgay, 103) + N' – ' + CONVERT(nvarchar(10), bl.DenNgay, 103) AS GhiChu
                FROM dbo.BANG_LUONG bl
                JOIN dbo.NHAN_VIEN nv ON nv.MaNV = bl.MaNV
                WHERE bl.TrangThai = N'Đã chốt' AND bl.NgayChot >= @TuNgay AND bl.NgayChot < @DenExclusive
                ORDER BY bl.NgayChot, bl.MaBL
                """,
            "13" => """
                SELECT pn.NgayNhap AS Ngay, CAST(1 AS decimal(18,2)) AS Mot, CAST(0 AS decimal(18,2)) AS Hai,
                    ct.ThanhTien AS Ba, CAST(0 AS decimal(18,2)) AS Bon, N'Dòng nhập ghi sổ' AS Nhom, 1 AS GiaTriCoCau,
                    hh.MaHH AS Ma, hh.TenHH AS Ten, hh.MaLoaiHH AS PhanLoai, hh.DonViTinh AS DonVi,
                    pn.NgayNhap AS NgayChiTiet, ct.SoLuong AS ChiTietMot,
                    CAST(0 AS decimal(18,2)) AS ChiTietHai, ct.ThanhTien AS ChiTietBa,
                    pn.TrangThai, N'Phiếu nhập: ' + pn.MaPN AS GhiChu
                FROM dbo.CHI_TIET_PHIEU_NHAP ct
                JOIN dbo.PHIEU_NHAP pn ON pn.MaPN = ct.MaPN
                JOIN dbo.HANG_HOA hh ON hh.MaHH = ct.MaHH
                WHERE pn.TrangThai = N'Đã ghi sổ' AND pn.NgayNhap >= @TuNgay AND pn.NgayNhap < @DenExclusive
                UNION ALL
                SELECT px.NgayXuat, 0, 1, 0, 0, N'Dòng xuất ghi sổ', 1,
                    hh.MaHH, hh.TenHH, hh.MaLoaiHH, hh.DonViTinh, px.NgayXuat,
                    0, ct.SoLuong, 0, px.TrangThai, N'Phiếu xuất: ' + px.MaPX
                FROM dbo.CHI_TIET_PHIEU_XUAT ct
                JOIN dbo.PHIEU_XUAT px ON px.MaPX = ct.MaPX
                JOIN dbo.HANG_HOA hh ON hh.MaHH = ct.MaHH
                WHERE px.TrangThai = N'Đã ghi sổ' AND px.NgayXuat >= @TuNgay AND px.NgayXuat < @DenExclusive
                """,
            "14" => """
                SELECT @TuNgay AS Ngay, 1 AS Mot,
                    CASE WHEN lo.HanSuDung < @TuNgay THEN 1 ELSE 0 END AS Hai,
                    CASE WHEN lo.HanSuDung >= @TuNgay AND lo.HanSuDung <= @CanhBaoDen THEN 1 ELSE 0 END AS Ba,
                    CASE WHEN EXISTS (SELECT 1 FROM dbo.THUOC t WHERE t.MaHH = lo.MaHH)
                        AND lo.HanSuDung <= @CanhBaoDen THEN 1 ELSE 0 END AS Bon,
                    CASE WHEN lo.HanSuDung < @TuNgay THEN N'Đã quá hạn'
                         WHEN lo.HanSuDung <= @CanhBaoDen THEN N'Sắp đến hạn' ELSE N'Ngoài ngưỡng cảnh báo' END AS Nhom,
                    1 AS GiaTriCoCau, lo.MaLo AS Ma, hh.TenHH AS Ten, lo.MaHH AS PhanLoai, hh.DonViTinh AS DonVi,
                    lo.HanSuDung AS NgayChiTiet, lo.NgayNhap AS DenNgayChiTiet,
                    DATEDIFF(day, @TuNgay, lo.HanSuDung) AS ChiTietMot,
                    lo.TrangThai,
                    N'Số lô: ' + ISNULL(lo.SoLo, N'—') + N' · Kho: ' + lo.MaKho AS GhiChu
                FROM dbo.LO_HANG_HOA lo
                JOIN dbo.HANG_HOA hh ON hh.MaHH = lo.MaHH
                WHERE lo.HanSuDung IS NOT NULL AND lo.NgayNhap < @DenExclusive
                    AND lo.TrangThai NOT IN (N'Hủy', N'Đã hủy')
                ORDER BY lo.HanSuDung, lo.MaLo
                """,
            "18" => """
                SELECT d.NgayGui AS Ngay, 1 AS Mot,
                    CASE WHEN d.TrangThaiDuyet = N'Đã duyệt' THEN 1 ELSE 0 END AS Hai,
                    CASE WHEN d.TrangThaiDuyet = N'Đã duyệt' AND ISNULL(d.GhiChu,N'') NOT LIKE N'%Loại đơn: Nghỉ việc%'
                        THEN DATEDIFF(day, d.TuNgay, d.DenNgay) + 1 ELSE 0 END AS Ba,
                    thay.SoCa AS Bon, d.TrangThaiDuyet AS Nhom, 1 AS GiaTriCoCau,
                    d.MaDonNghi AS Ma, nv.HoTen AS Ten, d.MaNV AS PhanLoai, N'ngày lịch' AS DonVi,
                    d.TuNgay AS NgayChiTiet, d.DenNgay AS DenNgayChiTiet,
                    CASE WHEN d.TrangThaiDuyet = N'Đã duyệt' AND ISNULL(d.GhiChu,N'') NOT LIKE N'%Loại đơn: Nghỉ việc%'
                        THEN DATEDIFF(day, d.TuNgay, d.DenNgay) + 1 ELSE 0 END AS ChiTietMot,
                    thay.SoCa AS ChiTietHai, d.TrangThaiDuyet AS TrangThai,
                    N'Gửi: ' + CONVERT(nvarchar(10), d.NgayGui, 103) +
                        CASE WHEN ISNULL(d.GhiChu,N'') LIKE N'%Loại đơn: Nghỉ việc%' THEN N' · Nghỉ việc: không tính số ngày' ELSE N'' END AS GhiChu
                FROM dbo.DON_XIN_NGHI d
                JOIN dbo.NHAN_VIEN nv ON nv.MaNV = d.MaNV
                OUTER APPLY (SELECT COUNT(*) AS SoCa FROM dbo.PHAN_CONG_THAY_THE pc
                    WHERE pc.MaDonNghi = d.MaDonNghi AND pc.TrangThai = N'Đã xác nhận') thay
                WHERE d.NgayGui >= @TuNgay AND d.NgayGui < @DenExclusive
                ORDER BY d.NgayGui, d.MaDonNghi
                """,
            _ => throw new ArgumentException("Không có nguồn báo cáo cho mã chuyên đề này.", nameof(ma))
        };
        using var connection = MoKetNoi();
        return connection.Query<ET_DONGBAOCAO>(sql, new
        {
            TuNgay = tuNgay.Date,
            DenExclusive = denNgay.Date.AddDays(1),
            CanhBaoDen = tuNgay.Date.AddDays(canhBao)
        }).ToArray();
    }
}
