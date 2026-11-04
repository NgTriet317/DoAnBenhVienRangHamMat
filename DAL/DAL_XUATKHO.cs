using System;
using System.Collections.Generic;
using System.Data;
using Dapper;
using ET;

namespace DAL
{
    // Lưu phiếu xuất nội bộ và các dòng hàng trong cùng một giao dịch Serializable.
    // Không dùng đường này để cấp thuốc theo đơn khám (MaLS / MaCTDT).
    public class DAL_XUATKHO : dbconnect
    {
        /// <summary>
        /// Đọc tồn từng lô trong kho để người dùng chọn nguồn xuất phù hợp.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>ThucHien</c>, <c>connection.Query&lt;ET_TONLOXUAT&gt;</c>.</remarks>
        /// <param name="maKho">Mã kho xác định đối tượng cần xử lý.</param>
        /// <returns>Tập dữ liệu kiểu List&lt;ET_TONLOXUAT&gt; theo điều kiện và phạm vi xử lý của hàm.</returns>
        public List<ET_TONLOXUAT> LayTonTheoLo(string maKho)
        {
            return ThucHien((connection, transaction) =>
                connection.Query<ET_TONLOXUAT>(@"
                    SELECT l.MaLo,l.MaHH,COALESCE(l.SoLo,l.MaLo) SoLo,
                        h.TenHH,h.DonViTinh,l.HanSuDung,
                        COALESCE(n.SoLuong,0)-COALESCE(x.SoLuong,0) SoLuongTon
                    FROM dbo.LO_HANG_HOA l
                    JOIN dbo.HANG_HOA h ON h.MaHH=l.MaHH
                    OUTER APPLY (SELECT SUM(c.SoLuong) SoLuong FROM dbo.CHI_TIET_PHIEU_NHAP c
                        JOIN dbo.PHIEU_NHAP p ON p.MaPN=c.MaPN
                        WHERE c.MaLo=l.MaLo AND p.MaKho=@MaKho AND p.TrangThai=N'Đã ghi sổ') n
                    OUTER APPLY (SELECT SUM(c.SoLuong) SoLuong FROM dbo.CHI_TIET_PHIEU_XUAT c
                        JOIN dbo.PHIEU_XUAT p ON p.MaPX=c.MaPX
                        WHERE c.MaLo=l.MaLo AND p.MaKho=@MaKho AND p.TrangThai=N'Đã ghi sổ') x
                    WHERE l.MaKho=@MaKho AND l.TrangThai=N'Còn sử dụng'
                        AND (EXISTS(SELECT 1 FROM dbo.VAT_TU v WHERE v.MaHH=l.MaHH)
                             OR EXISTS(SELECT 1 FROM dbo.THUOC t WHERE t.MaHH=l.MaHH))
                        AND l.NgayNhap<=SYSDATETIME()
                        AND (l.HanSuDung IS NULL OR l.HanSuDung>=CAST(SYSDATETIME() AS date))
                        AND COALESCE(n.SoLuong,0)>COALESCE(x.SoLuong,0)
                    ORDER BY h.TenHH,l.HanSuDung,l.SoLo",
                    new { MaKho = maKho }, transaction).ToList());
        }

        /// <summary>
        /// Kiểm tra và lưu đầu phiếu cùng các dòng hàng; nội dung được ghi theo trạng thái và quyền hiện tại. Các truy vấn có gợi ý khóa giữ bản dữ liệu liên quan trong phạm vi giao dịch.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>ThucHien</c>, <c>connection.ExecuteScalar&lt;int&gt;</c>, <c>connection.QuerySingleOrDefault&lt;ET_PHIEUXUAT&gt;</c>, <c>connection.Execute</c>.</remarks>
        /// <param name="maPX">Mã phiếu xuất xác định đối tượng cần xử lý.</param>
        /// <param name="maKho">Mã kho xác định đối tượng cần xử lý.</param>
        /// <param name="maNV">Mã nhân viên xác định đối tượng cần xử lý.</param>
        /// <param name="maKhoa">Mã khóa xác định đối tượng cần xử lý.</param>
        /// <param name="maPhong">Mã phòng xác định đối tượng cần xử lý.</param>
        /// <param name="ngayXuat">Giá trị ngày xuất dùng khi thực hiện thao tác; kiểu DateTime.</param>
        /// <param name="lyDo">Giá trị lý do dùng khi thực hiện thao tác; kiểu string.</param>
        /// <param name="ghiChu">Giá trị ghi chú dùng khi thực hiện thao tác; kiểu string?.</param>
        /// <param name="dong">Giá trị dòng dùng khi thực hiện thao tác; kiểu List&lt;ET_DONGXUATMOI&gt;.</param>
        /// <param name="ghiSo">Giá trị ghi sổ dùng khi thực hiện thao tác; kiểu bool.</param>
        /// <returns>Chuỗi kết quả của thao tác lưu phiếu.</returns>
        public string LuuPhieu(string? maPX, string maKho, string maNV, string? maKhoa,
            string? maPhong, DateTime ngayXuat, string lyDo, string? ghiChu,
            List<ET_DONGXUATMOI> dong, bool ghiSo)
        {
            return ThucHien((connection, transaction) =>
            {
                if (connection.ExecuteScalar<int>(
                    "SELECT COUNT(1) FROM dbo.KHO WITH(UPDLOCK,HOLDLOCK) WHERE MaKho=@Ma AND TrangThai=N'Đang hoạt động'",
                    new { Ma = maKho }, transaction) != 1)
                    throw new InvalidOperationException("Kho xuất không tồn tại hoặc không còn hoạt động.");
                if (connection.ExecuteScalar<int>(
                    "SELECT COUNT(1) FROM dbo.NHAN_VIEN WHERE MaNV=@Ma AND TrangThai=N'Đang làm việc'",
                    new { Ma = maNV }, transaction) != 1)
                    throw new InvalidOperationException("Người lập phiếu không còn ở trạng thái Đang làm việc.");
                if (maKhoa != null && connection.ExecuteScalar<int>(
                    "SELECT COUNT(1) FROM dbo.KHOA WHERE MaKhoa=@Ma", new { Ma = maKhoa }, transaction) != 1)
                    throw new InvalidOperationException("Khoa nhận không tồn tại.");
                if (maPhong != null && connection.ExecuteScalar<int>(
                    "SELECT COUNT(1) FROM dbo.PHONG WHERE MaPhong=@Ma", new { Ma = maPhong }, transaction) != 1)
                    throw new InvalidOperationException("Phòng nhận không tồn tại.");

                if (!string.IsNullOrWhiteSpace(maPX))
                {
                    // Không cho sửa phiếu đã ghi sổ hoặc phiếu liên kết cấp thuốc.
                    ET_PHIEUXUAT? phieuCu = connection.QuerySingleOrDefault<ET_PHIEUXUAT>(
                        "SELECT * FROM dbo.PHIEU_XUAT WITH(UPDLOCK,HOLDLOCK) WHERE MaPX=@Ma",
                        new { Ma = maPX }, transaction);
                    if (phieuCu == null || phieuCu.TrangThai != "Nháp" || phieuCu.MaLS != null)
                        throw new InvalidOperationException("Chỉ sửa được phiếu xuất nội bộ còn Nháp.");
                    if (connection.ExecuteScalar<int>(
                        "SELECT COUNT(1) FROM dbo.CHI_TIET_PHIEU_XUAT WHERE MaPX=@Ma AND MaCTDT IS NOT NULL",
                        new { Ma = maPX }, transaction) != 0)
                        throw new InvalidOperationException("Phiếu đã liên kết đơn thuốc không thể sửa tại đây.");
                    // Trigger chỉ cho xóa chi tiết của phiếu Nháp.
                    connection.Execute("DELETE FROM dbo.CHI_TIET_PHIEU_XUAT WHERE MaPX=@Ma",
                        new { Ma = maPX }, transaction);
                    int soDong = connection.Execute(@"
                        UPDATE dbo.PHIEU_XUAT SET MaKho=@MaKho,MaNV=@MaNV,MaKhoa=@MaKhoa,
                            MaPhong=@MaPhong,MaLS=NULL,NgayXuat=@NgayXuat,LyDo=@LyDo,GhiChu=@GhiChu
                        WHERE MaPX=@MaPX AND TrangThai=N'Nháp'",
                        new { MaPX = maPX, MaKho = maKho, MaNV = maNV, MaKhoa = maKhoa,
                            MaPhong = maPhong, NgayXuat = ngayXuat, LyDo = lyDo, GhiChu = ghiChu }, transaction);
                    if (soDong != 1) throw new InvalidOperationException("Phiếu đã thay đổi, hãy tải lại.");
                }
                else
                {
                    maPX = TaoMa(connection, transaction, "PHIEU_XUAT", "MaPX");
                    connection.Execute(@"
                        INSERT INTO dbo.PHIEU_XUAT
                          (MaPX,MaKho,MaNV,MaKhoa,MaPhong,MaLS,NgayXuat,LyDo,TrangThai,GhiChu)
                        VALUES(@MaPX,@MaKho,@MaNV,@MaKhoa,@MaPhong,NULL,@NgayXuat,@LyDo,N'Nháp',@GhiChu)",
                        new { MaPX = maPX, MaKho = maKho, MaNV = maNV, MaKhoa = maKhoa,
                            MaPhong = maPhong, NgayXuat = ngayXuat, LyDo = lyDo, GhiChu = ghiChu }, transaction);
                }

                foreach (ET_DONGXUATMOI d in dong)
                {
                    // Luôn yêu cầu lô cụ thể và đúng hàng / đúng kho, kể cả lúc lưu nháp.
                    if (connection.ExecuteScalar<int>(@"
                        SELECT COUNT(1) FROM dbo.LO_HANG_HOA l
                        WHERE l.MaLo=@MaLo AND l.MaHH=@MaHH AND l.MaKho=@MaKho",
                        new { d.MaLo, d.MaHH, MaKho = maKho }, transaction) != 1)
                        throw new InvalidOperationException("Lô hàng không thuộc kho/mặt hàng đang xuất.");
                    ET_CHITIETPHIEUXUAT ct = new ET_CHITIETPHIEUXUAT();
                    ct.MaPX = maPX;
                    ct.MaHH = d.MaHH;
                    ct.MaLo = d.MaLo;
                    ct.MaCTDT = null;
                    ct.SoLuong = d.SoLuong;
                    ct.GhiChu = d.GhiChu;
                    DAL_CHITIETPHIEUXUAT.ThemChiTiet(connection, transaction, ct);
                }
                if (ghiSo) GhiSoNoiBo(connection, transaction, maPX);
                return maPX;
            });
        }

        /// <summary>
        /// Kiểm tra phiếu và ghi sổ nhập/xuất để các thay đổi tồn kho có hiệu lực theo nghiệp vụ.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>ThucHien</c>, <c>GhiSoNoiBo</c>.</remarks>
        /// <param name="maPX">Mã phiếu xuất xác định đối tượng cần xử lý.</param>
        public void GhiSo(string maPX)
        {
            ThucHien((connection, transaction) =>
            {
                GhiSoNoiBo(connection, transaction, maPX);
                return true;
            });
        }

        /// <summary>
        /// Thực hiện phần ghi sổ phiếu xuất trong phạm vi giao dịch đang được điều phối. Các truy vấn có gợi ý khóa giữ bản dữ liệu liên quan trong phạm vi giao dịch.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>connection.QuerySingleOrDefault&lt;ET_PHIEUXUAT&gt;</c>, <c>connection.ExecuteScalar&lt;int&gt;</c>, <c>connection.Query&lt;ET_CHITIETPHIEUXUAT&gt;</c>, <c>maHang.Contains</c>.</remarks>
        /// <param name="connection">Kết nối SQL đang dùng; các lệnh trong thao tác này thực thi trên kết nối đó.</param>
        /// <param name="transaction">Giao dịch dùng chung; null nếu nơi gọi không truyền giao dịch. Quyền commit/rollback thuộc nơi quản lý giao dịch.</param>
        /// <param name="maPX">Mã phiếu xuất xác định đối tượng cần xử lý.</param>
        private static void GhiSoNoiBo(IDbConnection connection, IDbTransaction? transaction, string maPX)
        {
            ET_PHIEUXUAT? p = connection.QuerySingleOrDefault<ET_PHIEUXUAT>(
                "SELECT * FROM dbo.PHIEU_XUAT WITH(UPDLOCK,HOLDLOCK) WHERE MaPX=@MaPX",
                new { MaPX = maPX }, transaction);
            if (p == null || p.TrangThai != "Nháp" || p.MaLS != null)
                throw new InvalidOperationException("Chỉ ghi sổ phiếu xuất nội bộ còn Nháp.");
            if (connection.ExecuteScalar<int>(
                "SELECT COUNT(1) FROM dbo.CHI_TIET_PHIEU_XUAT WHERE MaPX=@MaPX AND MaCTDT IS NOT NULL",
                new { MaPX = maPX }, transaction) != 0)
                throw new InvalidOperationException("Phiếu cấp thuốc theo đơn phải xử lý tại chức năng cấp thuốc.");

            List<ET_CHITIETPHIEUXUAT> cacDong = connection.Query<ET_CHITIETPHIEUXUAT>(
                "SELECT * FROM dbo.CHI_TIET_PHIEU_XUAT WHERE MaPX=@MaPX",
                new { MaPX = maPX }, transaction).ToList();
            if (cacDong.Count == 0) throw new InvalidOperationException("Phiếu xuất không có vật tư.");
            if (p.NgayXuat > DateTime.Now)
                throw new InvalidOperationException("Ngày xuất không được lớn hơn hiện tại.");
            if (connection.ExecuteScalar<int>(
                "SELECT COUNT(1) FROM dbo.KHO WHERE MaKho=@Ma AND TrangThai=N'Đang hoạt động'",
                new { Ma = p.MaKho }, transaction) != 1)
                throw new InvalidOperationException("Kho xuất không còn hoạt động.");

            // Khóa mặt hàng theo thứ tự mã để tránh ghi sổ hai phiếu xuất cùng tồn.
            List<string> maHang = new List<string>();
            foreach (ET_CHITIETPHIEUXUAT d in cacDong)
                if (!maHang.Contains(d.MaHH)) maHang.Add(d.MaHH);
            maHang.Sort(StringComparer.Ordinal);
            foreach (string ma in maHang)
            {
                if (connection.ExecuteScalar<string?>(
                    "SELECT MaHH FROM dbo.HANG_HOA WITH(UPDLOCK,HOLDLOCK) WHERE MaHH=@Ma",
                    new { Ma = ma }, transaction) == null)
                    throw new InvalidOperationException("Mặt hàng không còn tồn tại.");
            }
            Dictionary<string, decimal> tongTheoLo = new Dictionary<string, decimal>();
            foreach (ET_CHITIETPHIEUXUAT d in cacDong)
            {
                if (string.IsNullOrWhiteSpace(d.MaLo) || d.SoLuong <= 0)
                    throw new InvalidOperationException("Mỗi dòng phải có lô và số lượng hợp lệ.");
                if (!tongTheoLo.ContainsKey(d.MaLo)) tongTheoLo[d.MaLo] = 0;
                tongTheoLo[d.MaLo] += d.SoLuong;
            }
            foreach (KeyValuePair<string, decimal> item in tongTheoLo)
            {
                string maLo = item.Key;
                string? maHH = connection.ExecuteScalar<string?>(@"
                    SELECT MaHH FROM dbo.LO_HANG_HOA WITH(UPDLOCK,HOLDLOCK)
                    WHERE MaLo=@MaLo AND MaKho=@MaKho AND TrangThai=N'Còn sử dụng'
                      AND NgayNhap<=@NgayXuat
                      AND (HanSuDung IS NULL OR HanSuDung>=CAST(@NgayXuat AS date))",
                    new { MaLo = maLo, MaKho = p.MaKho, NgayXuat = p.NgayXuat }, transaction);
                bool saiMaHang = false;
                foreach (ET_CHITIETPHIEUXUAT d in cacDong)
                    if (d.MaLo == maLo && d.MaHH != maHH) saiMaHang = true;
                if (maHH == null || saiMaHang)
                    throw new InvalidOperationException("Lô không thuộc kho, đã hết hạn hoặc chưa được nhập chính thức.");

                decimal ton = connection.ExecuteScalar<decimal>(@"
                    SELECT COALESCE((SELECT SUM(c.SoLuong) FROM dbo.CHI_TIET_PHIEU_NHAP c
                        JOIN dbo.PHIEU_NHAP pn ON pn.MaPN=c.MaPN
                        WHERE c.MaLo=@MaLo AND pn.MaKho=@MaKho AND pn.TrangThai=N'Đã ghi sổ'),0)
                       - COALESCE((SELECT SUM(c.SoLuong) FROM dbo.CHI_TIET_PHIEU_XUAT c
                        JOIN dbo.PHIEU_XUAT px ON px.MaPX=c.MaPX
                        WHERE c.MaLo=@MaLo AND px.MaKho=@MaKho AND px.TrangThai=N'Đã ghi sổ'),0)",
                    new { MaLo = maLo, MaKho = p.MaKho }, transaction);
                if (item.Value > ton)
                    throw new InvalidOperationException("Không đủ tồn cho lô " + maLo + ". Tồn hiện tại: " + ton.ToString("N3"));
            }
            // Trigger TR_PX_GhiSoDuDon tiếp tục kiểm tra tại SQL trước khi commit.
            if (connection.Execute(@"
                UPDATE dbo.PHIEU_XUAT SET TrangThai=N'Đã ghi sổ'
                WHERE MaPX=@MaPX AND TrangThai=N'Nháp'",
                new { MaPX = maPX }, transaction) != 1)
                throw new InvalidOperationException("Phiếu đã thay đổi, hãy tải lại.");
        }
    }
}
