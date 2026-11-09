using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Text;
using Dapper;
using ET;

namespace DAL
{
    // Lưu đầu phiếu, từng lô MỚI và chi tiết trong một giao dịch SQL.
    public class DAL_NHAPKHO : dbconnect
    {
        /// <summary>
        /// Tạo phiếu nhập và các dòng hàng sau khi kiểm tra đầu vào, nguồn hóa đơn và các liên kết. Các truy vấn có gợi ý khóa giữ bản dữ liệu liên quan trong phạm vi giao dịch.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>ThucHien</c>, <c>connection.QuerySingleOrDefault&lt;ET_HOADONNHAP&gt;</c>, <c>connection.ExecuteScalar&lt;int&gt;</c>, <c>DoiTrangThai</c>.</remarks>
        /// <param name="maHDN">Mã hóa đơn nhập xác định đối tượng cần xử lý.</param>
        /// <param name="maKho">Mã kho xác định đối tượng cần xử lý.</param>
        /// <param name="maNV">Mã nhân viên xác định đối tượng cần xử lý.</param>
        /// <param name="ngayNhap">Giá trị ngày nhập dùng khi thực hiện thao tác; kiểu DateTime.</param>
        /// <param name="lyDo">Giá trị lý do dùng khi thực hiện thao tác; kiểu string.</param>
        /// <param name="ghiChu">Giá trị ghi chú dùng khi thực hiện thao tác; kiểu string?.</param>
        /// <param name="cacDong">Giá trị các dòng dùng khi thực hiện thao tác; kiểu List&lt;ET_DONGNHAPMOI&gt;.</param>
        /// <param name="ghiSo">Giá trị ghi sổ dùng khi thực hiện thao tác; kiểu bool.</param>
        /// <returns>Chuỗi kết quả của thao tác tạo phiếu.</returns>
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

        /// <summary>
        /// Kiểm tra phiếu và ghi sổ nhập/xuất để các thay đổi tồn kho có hiệu lực theo nghiệp vụ.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>ThucHien</c>, <c>DoiTrangThai</c>.</remarks>
        /// <param name="maPN">Mã phiếu nhập xác định đối tượng cần xử lý.</param>
        public void GhiSo(string maPN)
        {
            ThucHien((connection, transaction) =>
            {
                DoiTrangThai(connection, transaction, maPN);
                return true;
            });
        }

        /// <summary>
        /// Chuyển trạng thái phiếu theo điều kiện của bản hiện tại và dữ liệu đã đối chiếu. Các truy vấn có gợi ý khóa giữ bản dữ liệu liên quan trong phạm vi giao dịch.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>connection.QuerySingleOrDefault&lt;string&gt;</c>, <c>connection.ExecuteScalar&lt;int&gt;</c>, <c>connection.Execute</c>, <c>TaoThietBiTuLo</c>.</remarks>
        /// <param name="connection">Kết nối SQL đang dùng; các lệnh trong thao tác này thực thi trên kết nối đó.</param>
        /// <param name="transaction">Giao dịch dùng chung; null nếu nơi gọi không truyền giao dịch. Quyền commit/rollback thuộc nơi quản lý giao dịch.</param>
        /// <param name="maPN">Mã phiếu nhập xác định đối tượng cần xử lý.</param>
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

            // Tạo tài sản ngay trong giao dịch ghi sổ, rollback toàn bộ nếu sinh serial thất bại.
            // Phiếu nháp không có thiết bị; ghi sổ lại không thể sinh trùng do trạng thái đã khóa.
            TaoThietBiTuLo(connection, transaction, maPN);
        }

        /// <summary>
        /// Tạo các thiết bị/serial từ dòng nhập thiết bị theo lô trong cùng giao dịch ghi sổ. Các truy vấn có gợi ý khóa giữ bản dữ liệu liên quan trong phạm vi giao dịch. Dùng application lock sở hữu bởi giao dịch; từ chối khi không lấy được khóa trong thời gian chờ.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>connection.Query&lt;ET_CHITIETPHIEUNHAP&gt;</c>, <c>decimal.Truncate</c>, <c>connection.QuerySingle&lt;ET_HANGHOA&gt;</c>, <c>connection.QuerySingle&lt;ET_LOHANGHOA&gt;</c>.</remarks>
        /// <param name="connection">Kết nối SQL đang dùng; các lệnh trong thao tác này thực thi trên kết nối đó.</param>
        /// <param name="transaction">Giao dịch dùng chung; null nếu nơi gọi không truyền giao dịch. Quyền commit/rollback thuộc nơi quản lý giao dịch.</param>
        /// <param name="maPN">Mã phiếu nhập xác định đối tượng cần xử lý.</param>
        private static void TaoThietBiTuLo(IDbConnection connection, IDbTransaction? transaction, string maPN)
        {
            List<ET_CHITIETPHIEUNHAP> cacDong = new List<ET_CHITIETPHIEUNHAP>(connection.Query<ET_CHITIETPHIEUNHAP>(@"
                SELECT c.* FROM dbo.CHI_TIET_PHIEU_NHAP c
                JOIN dbo.HANG_HOA h ON h.MaHH=c.MaHH
                JOIN dbo.LOAI_HANG_HOA lh ON lh.MaLoaiHH=h.MaLoaiHH
                WHERE c.MaPN=@MaPN AND
                    (lh.TenLoaiHH LIKE N'%Thiết bị%'
                     OR h.MaLoaiHH IN ('LHH008','LHH009','LHH010'))
                ORDER BY c.MaLo", new { MaPN = maPN }, transaction));

            foreach (ET_CHITIETPHIEUNHAP dong in cacDong)
            {
                // Thiết bị là tài sản riêng lẻ; không thể tạo một phần của thiết bị.
                if (dong.SoLuong <= 0 || dong.SoLuong != decimal.Truncate(dong.SoLuong)
                    || dong.SoLuong > int.MaxValue)
                    throw new InvalidOperationException("Số lượng lô thiết bị phải là số nguyên dương.");

                ET_HANGHOA hang = connection.QuerySingle<ET_HANGHOA>(
                    "SELECT * FROM dbo.HANG_HOA WHERE MaHH=@MaHH", new { dong.MaHH }, transaction);
                ET_LOHANGHOA lo = connection.QuerySingle<ET_LOHANGHOA>(
                    "SELECT * FROM dbo.LO_HANG_HOA WHERE MaLo=@MaLo", new { dong.MaLo }, transaction);
                string tienTo = LayChuCaiDau(hang.TenHH);

                for (int stt = 0; stt < (int)dong.SoLuong; stt++)
                {
                    // Serial duy nhất trên TOÀN BỘ bảng THIET_BI, không khởi động lại ở từng lô.
                    // Giữ khóa trong giao dịch để hai người ghi sổ cùng loại không nhận cùng số.
                    int ketQuaKhoa = connection.ExecuteScalar<int>(@"
                        DECLARE @ketQua int;
                        EXEC @ketQua=sys.sp_getapplock
                            @Resource=@Resource, @LockMode='Exclusive',
                            @LockOwner='Transaction', @LockTimeout=10000;
                        SELECT @ketQua;",
                        new { Resource = "RHM:Serial:" + tienTo }, transaction);
                    if (ketQuaKhoa < 0)
                        throw new InvalidOperationException("Không thể cấp serial thiết bị lúc này. Vui lòng thử lại.");

                    long soThuTu = connection.ExecuteScalar<long>(@"
                        SELECT COALESCE(MAX(TRY_CONVERT(bigint,
                            SUBSTRING(SoSerial, LEN(@TienTo) + 1, 255))), 0)
                        FROM dbo.THIET_BI WITH (UPDLOCK,HOLDLOCK)
                        WHERE SoSerial LIKE @Mau",
                        new { TienTo = tienTo, Mau = tienTo + "%" }, transaction);
                    if (soThuTu == long.MaxValue)
                        throw new InvalidOperationException("Đã hết số thứ tự serial thiết bị.");

                    ET_THIETBI thietBi = new ET_THIETBI();
                    thietBi.MaHH = dong.MaHH;
                    thietBi.SoSerial = tienTo + (soThuTu + 1).ToString("D3", CultureInfo.InvariantCulture);
                    thietBi.NgayMua = lo.NgayNhap.Date;
                    thietBi.MaNCC = lo.MaNCC;
                    thietBi.MaPhong = null;
                    thietBi.TinhTrang = "Đang hoạt động";
                    thietBi.HanBaoHanh = null;
                    thietBi.TrangThai = "Chờ bàn giao";
                    thietBi.GhiChu = "Tạo tự động khi ghi sổ phiếu " + maPN
                        + "; Mã lô: " + dong.MaLo + "; Số lô: " + lo.SoLo;
                    new DAL_THIETBI(transaction).themThietBi(thietBi);
                }
            }
        }

        /// <summary>
        /// Lấy các chữ cái đầu của tên để tạo phần nhận diện theo quy tắc mã đang áp dụng.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>tenThietBi.Replace</c>, <c>CharUnicodeInfo.GetUnicodeCategory</c>, <c>char.IsLetter</c>, <c>chuDau.Append</c>.</remarks>
        /// <param name="tenThietBi">Giá trị tên thiết bị dùng khi thực hiện thao tác; kiểu string.</param>
        /// <returns>Chuỗi kết quả của thao tác lấy chú cài đầu.</returns>
        private static string LayChuCaiDau(string tenThietBi)
        {
            // VD: "Máy đo huyết áp" -> MDHA, "Máy X-quang" -> MXQ.
            string tenKhongDau = tenThietBi.Replace('Đ', 'D').Replace('đ', 'd')
                .Normalize(NormalizationForm.FormD);
            StringBuilder chuDau = new StringBuilder();
            bool dauTu = true;
            foreach (char kyTu in tenKhongDau)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(kyTu) == UnicodeCategory.NonSpacingMark)
                    continue;
                if (char.IsLetter(kyTu))
                {
                    if (dauTu)
                        chuDau.Append(char.ToUpperInvariant(kyTu));
                    dauTu = false;
                }
                else if (!char.IsDigit(kyTu))
                    dauTu = true;
            }
            if (chuDau.Length == 0)
                throw new InvalidOperationException("Tên thiết bị không có chữ cái để tạo serial.");
            if (chuDau.Length > 235)
                throw new InvalidOperationException("Tên thiết bị quá dài để tạo serial.");
            return chuDau.ToString();
        }
    }
}
