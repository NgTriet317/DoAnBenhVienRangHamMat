using System;
using System.Collections.Generic;
using System.Data;
using Dapper;
using ET;

namespace DAL
{
    // Lưu đầu phiếu, từng lô MỚI và chi tiết trong một giao dịch SQL.
    public class DAL_NHAPKHO : dbconnect
    {
        public string TaoPhieu(string maHDN, string maKho, string maNV, DateTime ngayNhap,
            string lyDo, string? ghiChu, List<ET_DONGNHAPMOI> cacDong, bool ghiSo)
        {
            return ThucHien((connection, transaction) =>
            {
                var hoaDon = connection.QuerySingleOrDefault<ET_HOADONNHAP>(
                    "SELECT * FROM dbo.HOA_DON_NHAP WITH(UPDLOCK,HOLDLOCK) WHERE MaHDN=@MaHDN",
                    new { MaHDN = maHDN }, transaction);
                if (hoaDon == null || hoaDon.TrangThai == "Hủy")
                    throw new InvalidOperationException("Hóa đơn nhập không tồn tại hoặc đã bị hủy.");

                // Chặn lập phiếu từ bản ghi danh mục không còn tồn tại.
                if (connection.ExecuteScalar<int>("SELECT COUNT(1) FROM dbo.KHO WHERE MaKho=@MaKho",
                    new { MaKho = maKho }, transaction) == 0)
                    throw new InvalidOperationException("Kho tiếp nhận không tồn tại.");
                if (connection.ExecuteScalar<int>("SELECT COUNT(1) FROM dbo.NHAN_VIEN WHERE MaNV=@MaNV",
                    new { MaNV = maNV }, transaction) == 0)
                    throw new InvalidOperationException("Nhân viên lập phiếu không tồn tại.");

                ET_PHIEUNHAP phieu = new ET_PHIEUNHAP();
                phieu.MaHDN = maHDN;
                phieu.MaKho = maKho;
                phieu.MaNV = maNV;
                phieu.NgayNhap = ngayNhap;
                phieu.TrangThai = "Nháp";
                phieu.LyDo = lyDo;
                phieu.GhiChu = ghiChu;
                new DAL_PHIEUNHAP(transaction).themPhieuNhap(phieu);

                foreach (ET_DONGNHAPMOI dong in cacDong)
                {
                    if (connection.ExecuteScalar<int>(
                        "SELECT COUNT(1) FROM dbo.HANG_HOA WHERE MaHH=@MaHH",
                        new { dong.MaHH }, transaction) == 0)
                        throw new InvalidOperationException("Mặt hàng không tồn tại: " + dong.MaHH);

                    // Mỗi dòng đều phải tạo lô mới, không tái sử dụng MaLo của chứng từ khác.
                    if (connection.ExecuteScalar<int>(
                        "SELECT COUNT(1) FROM dbo.LO_HANG_HOA WITH(UPDLOCK,HOLDLOCK) WHERE MaKho=@MaKho AND MaHH=@MaHH AND SoLo=@SoLo",
                        new { MaKho = maKho, dong.MaHH, dong.SoLo }, transaction) > 0)
                        throw new InvalidOperationException("Số lô đã có trong kho cho mặt hàng này: " + dong.SoLo);

                    ET_LOHANGHOA lo = new ET_LOHANGHOA();
                    lo.MaHH = dong.MaHH;
                    lo.MaKho = maKho;
                    lo.MaNCC = hoaDon.MaNCC;
                    lo.SoLo = dong.SoLo;
                    lo.NgayNhap = ngayNhap;
                    lo.HanSuDung = dong.HanSuDung;
                    lo.TrangThai = "Chờ nhập";
                    new DAL_LOHANGHOA(transaction).themLoHangHoa(lo);

                    ET_CHITIETPHIEUNHAP chiTiet = new ET_CHITIETPHIEUNHAP();
                    chiTiet.MaPN = phieu.MaPN;
                    chiTiet.MaHH = dong.MaHH;
                    chiTiet.MaLo = lo.MaLo;
                    chiTiet.SoLuong = dong.SoLuong;
                    chiTiet.DonGia = dong.DonGia;
                    chiTiet.GhiChu = dong.GhiChu;
                    new DAL_CHITIETPHIEUNHAP(transaction).themChiTietPhieuNhap(chiTiet);
                }

                if (ghiSo)
                    DoiTrangThai(connection, transaction, phieu.MaPN);
                return phieu.MaPN;
            });
        }


        // Chỉ cho sửa phiếu nháp có các lô "Chờ nhập" riêng do luồng mới tạo ra.
        public void CapNhatPhieu(string maPN, string maHDN, string maKho, string maNV,
            DateTime ngayNhap, string lyDo, string? ghiChu, List<ET_DONGNHAPMOI> cacDong, bool ghiSo)
        {
            ThucHien((connection, transaction) =>
            {
                ET_PHIEUNHAP? hienTai = connection.QuerySingleOrDefault<ET_PHIEUNHAP>(
                    "SELECT * FROM dbo.PHIEU_NHAP WITH(UPDLOCK,HOLDLOCK) WHERE MaPN=@MaPN",
                    new { MaPN = maPN }, transaction);
                if (hienTai == null || hienTai.TrangThai != "Nháp")
                    throw new InvalidOperationException("Chỉ sửa được phiếu nhập còn Nháp.");
                ET_HOADONNHAP? hoaDon = connection.QuerySingleOrDefault<ET_HOADONNHAP>(
                    "SELECT * FROM dbo.HOA_DON_NHAP WITH(UPDLOCK,HOLDLOCK) WHERE MaHDN=@MaHDN",
                    new { MaHDN = maHDN }, transaction);
                if (hoaDon == null || hoaDon.TrangThai == "Hủy")
                    throw new InvalidOperationException("Hóa đơn nhập không hợp lệ.");

                int soDongKhongTheSua = connection.ExecuteScalar<int>(@"
                    SELECT COUNT(1) FROM dbo.CHI_TIET_PHIEU_NHAP c
                    LEFT JOIN dbo.LO_HANG_HOA l ON l.MaLo=c.MaLo
                    WHERE c.MaPN=@MaPN AND (c.MaLo IS NULL OR l.MaLo IS NULL
                        OR l.TrangThai<>N'Chờ nhập'
                        OR EXISTS(SELECT 1 FROM dbo.CHI_TIET_PHIEU_NHAP c2 WHERE c2.MaLo=c.MaLo AND c2.MaPN<>c.MaPN)
                        OR EXISTS(SELECT 1 FROM dbo.CHI_TIET_PHIEU_XUAT x WHERE x.MaLo=c.MaLo))",
                    new { MaPN = maPN }, transaction);
                if (soDongKhongTheSua > 0)
                    throw new InvalidOperationException("Phiếu dùng lô cũ hoặc lô đã được tham chiếu; không thể thay đổi an toàn.");
                List<string> loCu = new List<string>(connection.Query<string>(
                    "SELECT MaLo FROM dbo.CHI_TIET_PHIEU_NHAP WHERE MaPN=@MaPN",
                    new { MaPN = maPN }, transaction));

                // Xóa chi tiết trước, xóa lô chỉ khi chưa có chứng từ khác tham chiếu.
                connection.Execute("DELETE FROM dbo.CHI_TIET_PHIEU_NHAP WHERE MaPN=@MaPN",
                    new { MaPN = maPN }, transaction);
                foreach (string maLo in loCu)
                    connection.Execute("DELETE FROM dbo.LO_HANG_HOA WHERE MaLo=@MaLo",
                        new { MaLo = maLo }, transaction);

                int suaDau = connection.Execute(@"
                    UPDATE dbo.PHIEU_NHAP SET MaHDN=@MaHDN,MaKho=@MaKho,
                        NgayNhap=@NgayNhap,LyDo=@LyDo,GhiChu=@GhiChu
                    WHERE MaPN=@MaPN AND TrangThai=N'Nháp'",
                    new { MaPN = maPN, MaHDN = maHDN, MaKho = maKho,
                        MaNV = maNV, NgayNhap = ngayNhap, LyDo = lyDo, GhiChu = ghiChu }, transaction);
                if (suaDau != 1)
                    throw new InvalidOperationException("Phiếu đã được xử lý ở cửa sổ khác.");
                ET_PHIEUNHAP phieu = hienTai;
                phieu.MaHDN = maHDN;
                phieu.MaKho = maKho;
                phieu.MaNV = maNV;
                phieu.NgayNhap = ngayNhap;
                foreach (ET_DONGNHAPMOI dong in cacDong)
                {
                    if (connection.ExecuteScalar<int>(
                        "SELECT COUNT(1) FROM dbo.HANG_HOA WHERE MaHH=@MaHH",
                        new { dong.MaHH }, transaction) == 0)
                        throw new InvalidOperationException("Mặt hàng không tồn tại: " + dong.MaHH);

                    // Mỗi dòng đều phải tạo lô mới, không tái sử dụng MaLo của chứng từ khác.
                    if (connection.ExecuteScalar<int>(
                        "SELECT COUNT(1) FROM dbo.LO_HANG_HOA WITH(UPDLOCK,HOLDLOCK) WHERE MaKho=@MaKho AND MaHH=@MaHH AND SoLo=@SoLo",
                        new { MaKho = maKho, dong.MaHH, dong.SoLo }, transaction) > 0)
                        throw new InvalidOperationException("Số lô đã có trong kho cho mặt hàng này: " + dong.SoLo);

                    ET_LOHANGHOA lo = new ET_LOHANGHOA();
                    lo.MaHH = dong.MaHH;
                    lo.MaKho = maKho;
                    lo.MaNCC = hoaDon.MaNCC;
                    lo.SoLo = dong.SoLo;
                    lo.NgayNhap = ngayNhap;
                    lo.HanSuDung = dong.HanSuDung;
                    lo.TrangThai = "Chờ nhập";
                    new DAL_LOHANGHOA(transaction).themLoHangHoa(lo);

                    ET_CHITIETPHIEUNHAP chiTiet = new ET_CHITIETPHIEUNHAP();
                    chiTiet.MaPN = phieu.MaPN;
                    chiTiet.MaHH = dong.MaHH;
                    chiTiet.MaLo = lo.MaLo;
                    chiTiet.SoLuong = dong.SoLuong;
                    chiTiet.DonGia = dong.DonGia;
                    chiTiet.GhiChu = dong.GhiChu;
                    new DAL_CHITIETPHIEUNHAP(transaction).themChiTietPhieuNhap(chiTiet);
                }

                if (ghiSo) DoiTrangThai(connection, transaction, maPN);
                return true;
            });
        }

        public void GhiSo(string maPN)
        {
            ThucHien((connection, transaction) =>
            {
                DoiTrangThai(connection, transaction, maPN);
                return true;
            });
        }

        private static void DoiTrangThai(IDbConnection connection, IDbTransaction? transaction, string maPN)
        {
            string? trangThai = connection.QuerySingleOrDefault<string>(
                "SELECT TrangThai FROM dbo.PHIEU_NHAP WITH(UPDLOCK,HOLDLOCK) WHERE MaPN=@MaPN",
                new { MaPN = maPN }, transaction);
            if (trangThai != "Nháp")
                throw new InvalidOperationException("Chỉ ghi sổ phiếu còn ở trạng thái Nháp.");
            int soDong = connection.ExecuteScalar<int>(
                "SELECT COUNT(1) FROM dbo.CHI_TIET_PHIEU_NHAP WHERE MaPN=@MaPN",
                new { MaPN = maPN }, transaction);
            if (soDong == 0)
                throw new InvalidOperationException("Phiếu nhập không có vật tư.");
            int soDongSai = connection.ExecuteScalar<int>(@"
                SELECT COUNT(1) FROM dbo.CHI_TIET_PHIEU_NHAP c
                LEFT JOIN dbo.LO_HANG_HOA l ON l.MaLo=c.MaLo
                JOIN dbo.PHIEU_NHAP p ON p.MaPN=c.MaPN
                JOIN dbo.HOA_DON_NHAP h ON h.MaHDN=p.MaHDN
                WHERE c.MaPN=@MaPN AND (c.MaLo IS NULL OR l.MaLo IS NULL
                    OR l.MaHH<>c.MaHH OR l.MaKho<>p.MaKho OR l.MaNCC<>h.MaNCC
                    OR EXISTS(SELECT 1 FROM dbo.CHI_TIET_PHIEU_NHAP c2
                        WHERE c2.MaLo=c.MaLo AND c2.MaPN<>c.MaPN))",
                new { MaPN = maPN }, transaction);
            if (soDongSai > 0)
                throw new InvalidOperationException("Phiếu có lô không hợp lệ hoặc lô đã được sử dụng ở phiếu khác.");
            int hoaDonChuaXacNhan = connection.ExecuteScalar<int>(@"
                SELECT COUNT(1) FROM dbo.PHIEU_NHAP p
                JOIN dbo.HOA_DON_NHAP h ON h.MaHDN=p.MaHDN
                WHERE p.MaPN=@MaPN AND h.TrangThai<>N'Đã xác nhận'",
                new { MaPN = maPN }, transaction);
            if (hoaDonChuaXacNhan > 0)
                throw new InvalidOperationException("Hóa đơn nhà cung cấp chưa được xác nhận.");
            int loHetHan = connection.ExecuteScalar<int>(@"
                SELECT COUNT(1) FROM dbo.CHI_TIET_PHIEU_NHAP c
                JOIN dbo.LO_HANG_HOA l ON l.MaLo=c.MaLo
                WHERE c.MaPN=@MaPN AND (
                    (l.HanSuDung IS NOT NULL AND l.HanSuDung < CAST(GETDATE() AS date))
                    OR l.TrangThai NOT IN (N'Chờ nhập',N'Còn sử dụng'))",
                new { MaPN = maPN }, transaction);
            if (loHetHan > 0)
                throw new InvalidOperationException("Có lô đã hết hạn hoặc ngừng sử dụng. Không thể ghi sổ.");
            // Lô thuộc phiếu nháp chỉ trở thành lô sử dụng được khi phiếu ghi sổ.
            connection.Execute(@"
                UPDATE l SET TrangThai=N'Còn sử dụng'
                FROM dbo.LO_HANG_HOA l
                JOIN dbo.CHI_TIET_PHIEU_NHAP c ON c.MaLo=l.MaLo
                WHERE c.MaPN=@MaPN AND l.TrangThai=N'Chờ nhập'",
                new { MaPN = maPN }, transaction);
            // SQL trigger PHIEU_NHAP kiểm tra tổng tiền và khóa phiếu đã ghi sổ.
            int ketQua = connection.Execute(
                "UPDATE dbo.PHIEU_NHAP SET TrangThai=N'Đã ghi sổ' WHERE MaPN=@MaPN AND TrangThai=N'Nháp'",
                new { MaPN = maPN }, transaction);
            if (ketQua != 1)
                throw new InvalidOperationException("Phiếu nhập đã được người khác xử lý. Vui lòng tải lại.");
        }
    }
}
