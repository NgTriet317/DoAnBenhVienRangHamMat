// AI hỗ trợ: bổ sung chú thích tiếng Việt;
using System.Data;
using Dapper;
using ET;

namespace DAL
{
    /// <summary>
    /// Tầng truy cập SQL cho hàng hóa trong bảng HANG_HOA. Lưu danh mục hàng chung gồm tên, loại và đơn vị tính để thuốc, vật tư, thiết bị và lô hàng liên kết. Dapper ánh xạ dòng SQL sang ET; ThucHien dùng giao dịch được truyền vào hoặc tự quản lý giao dịch cục bộ.
    /// </summary>
    public class DAL_HANGHOA : dbconnect
    {
        /// <summary>
        /// Tạo DAL quản lý hàng hóa, chuyển giao dịch tùy chọn cho dbconnect để nhiều thao tác có thể dùng chung kết nối và phạm vi lưu.
        /// </summary>
        /// <param name = "transaction">Giao dịch của nơi gọi; null để hạ tầng tham gia giao dịch môi trường hoặc tự mở giao dịch cục bộ. Nếu truyền vào, nơi gọi quản lý commit/rollback.</param>
        public DAL_HANGHOA(IDbTransaction? transaction = null) : base(transaction)
        {
        }

        /// <summary>
        /// Đọc toàn bộ hàng hóa từ dbo.HANG_HOA, Dapper ánh xạ từng dòng sang ET, sắp theo mã; ToArray nạp hết dữ liệu trước khi hạ tầng kết thúc phạm vi kết nối/giao dịch.
        /// </summary>
        /// <returns>Danh sách ET đã vật hóa; không có bản ghi thì trả danh sách rỗng.</returns>
        public IEnumerable<ET_HANGHOA> layToanBoHangHoa()
        {
            return ThucHien((connection, transaction) =>
            {
                return connection.Query<ET_HANGHOA>("SELECT * FROM dbo.[HANG_HOA] ORDER BY [MaHH]", transaction: transaction).ToArray();
            });
        }

        // Danh mục dùng chung cho 3 tab, trả về DataTable để binding trực tiếp.
        public DataTable layDanhMucChiTiet()
        {
            return ThucHien((connection, transaction) =>
            {
                using var reader = connection.ExecuteReader(@"
                    SELECT h.MaHH, h.TenHH, h.MaLoaiHH, l.TenLoaiHH AS TenLoai,
                        h.DonViTinh, h.MoTa, h.TrangThai,
                        CAST(CASE WHEN h.TrangThai = N'Đang hoạt động' THEN 1 ELSE 0 END AS bit) AS DangHoatDong,
                        CASE
                            WHEN l.TenLoaiHH LIKE N'Thuốc%' OR h.MaLoaiHH IN ('LHH001','LHH002','LHH003') THEN 'Thuoc'
                            WHEN l.TenLoaiHH LIKE N'Thiết bị%' OR h.MaLoaiHH IN ('LHH008','LHH009','LHH010') THEN 'ThietBi'
                            WHEN l.TenLoaiHH LIKE N'Vật tư%' OR h.MaLoaiHH IN ('LHH004','LHH005','LHH006','LHH007') THEN 'VatTu'
                            ELSE ''
                        END AS NhomHang,
                        th.MaThuoc, v.MaVT, th.HamLuong,
                        COALESCE(th.MucTonToiThieu, v.MucTonToiThieu, 0) AS MucTonToiThieu,
                        COALESCE(th.DonGiaBan, 0) AS DonGiaBan
                    FROM dbo.HANG_HOA h
                    LEFT JOIN dbo.LOAI_HANG_HOA l ON h.MaLoaiHH = l.MaLoaiHH
                    LEFT JOIN dbo.THUOC th ON th.MaHH = h.MaHH
                    LEFT JOIN dbo.VAT_TU v ON v.MaHH = h.MaHH
                    ORDER BY h.TenHH", transaction: transaction);
                var bang = new DataTable();
                bang.Load(reader);
                return bang;
            });
        }

        // Lưu HANG_HOA và bản ghi THUOC/VAT_TU trong cùng một giao dịch.
        public bool luuDanhMucChiTiet(ET_HANGHOA duLieu, ET_HANGHOA? banGoc,
            string nhom, decimal mucTon, string hamLuong, decimal giaBan, bool dangThem)
        {
            return ThucHien((connection, transaction) =>
            {
                if (nhom != "Thuoc" && nhom != "VatTu" && nhom != "ThietBi")
                    throw new InvalidOperationException("Nhóm hàng hóa không hợp lệ.");
                if (mucTon < 0 || giaBan < 0)
                    throw new InvalidOperationException("Số lượng tối thiểu và đơn giá không được âm.");

                if (dangThem)
                {
                    duLieu.MaHH = TaoMa(connection, transaction, "HANG_HOA", "MaHH");
                    KiemTra<ET_HANGHOA>(connection, transaction, "HANG_HOA", duLieu, null, null);
                    connection.Execute(@"INSERT INTO dbo.HANG_HOA (MaHH,TenHH,MaLoaiHH,DonViTinh,MoTa,TrangThai)
                        VALUES (@MaHH,@TenHH,@MaLoaiHH,@DonViTinh,@MoTa,@TrangThai)", duLieu, transaction);
                }
                else
                {
                    var hienTai = connection.QuerySingleOrDefault<ET_HANGHOA>(
                        "SELECT * FROM dbo.HANG_HOA WITH(UPDLOCK,HOLDLOCK) WHERE MaHH=@MaHH",
                        new { duLieu.MaHH }, transaction);
                    if (hienTai == null)
                        throw new InvalidOperationException("Mặt hàng không còn tồn tại. Vui lòng tải lại.");
                    KiemTra(connection, transaction, "HANG_HOA", duLieu, hienTai, banGoc);
                    bool coThuoc = connection.ExecuteScalar<int>(
                        "SELECT COUNT(*) FROM dbo.THUOC WHERE MaHH=@MaHH", new { duLieu.MaHH }, transaction) > 0;
                    bool coVatTu = connection.ExecuteScalar<int>(
                        "SELECT COUNT(*) FROM dbo.VAT_TU WHERE MaHH=@MaHH", new { duLieu.MaHH }, transaction) > 0;
                    if ((coThuoc && nhom != "Thuoc") || (coVatTu && nhom != "VatTu"))
                        throw new InvalidOperationException("Không thể đổi mặt hàng đã đăng ký sang nhóm khác.");
                    connection.Execute(@"UPDATE dbo.HANG_HOA SET TenHH=@TenHH, MaLoaiHH=@MaLoaiHH,
                        DonViTinh=@DonViTinh, MoTa=@MoTa, TrangThai=@TrangThai WHERE MaHH=@MaHH",
                        duLieu, transaction);
                }

                if (nhom == "Thuoc")
                {
                    var cu = connection.QuerySingleOrDefault<ET_THUOC>(
                        "SELECT * FROM dbo.THUOC WITH(UPDLOCK,HOLDLOCK) WHERE MaHH=@MaHH",
                        new { duLieu.MaHH }, transaction);
                    var thuoc = new ET_THUOC(cu?.MaThuoc ?? "", duLieu.MaHH, hamLuong, mucTon, giaBan);
                    if (cu == null)
                    {
                        thuoc.MaThuoc = TaoMa(connection, transaction, "THUOC", "MaThuoc");
                        KiemTra<ET_THUOC>(connection, transaction, "THUOC", thuoc, null, null);
                        connection.Execute(@"INSERT INTO dbo.THUOC (MaThuoc,MaHH,HamLuong,MucTonToiThieu,DonGiaBan)
                            VALUES (@MaThuoc,@MaHH,@HamLuong,@MucTonToiThieu,@DonGiaBan)", thuoc, transaction);
                    }
                    else
                    {
                        KiemTra(connection, transaction, "THUOC", thuoc, cu, null);
                        connection.Execute(@"UPDATE dbo.THUOC SET HamLuong=@HamLuong,
                            MucTonToiThieu=@MucTonToiThieu, DonGiaBan=@DonGiaBan WHERE MaThuoc=@MaThuoc",
                            thuoc, transaction);
                    }
                }
                else if (nhom == "VatTu")
                {
                    var cu = connection.QuerySingleOrDefault<ET_VATTU>(
                        "SELECT * FROM dbo.VAT_TU WITH(UPDLOCK,HOLDLOCK) WHERE MaHH=@MaHH",
                        new { duLieu.MaHH }, transaction);
                    var vatTu = new ET_VATTU(cu?.MaVT ?? "", duLieu.MaHH, mucTon);
                    if (cu == null)
                    {
                        vatTu.MaVT = TaoMa(connection, transaction, "VAT_TU", "MaVT");
                        KiemTra<ET_VATTU>(connection, transaction, "VAT_TU", vatTu, null, null);
                        connection.Execute(@"INSERT INTO dbo.VAT_TU (MaVT,MaHH,MucTonToiThieu)
                            VALUES (@MaVT,@MaHH,@MucTonToiThieu)", vatTu, transaction);
                    }
                    else
                    {
                        KiemTra(connection, transaction, "VAT_TU", vatTu, cu, null);
                        connection.Execute(@"UPDATE dbo.VAT_TU SET MucTonToiThieu=@MucTonToiThieu WHERE MaVT=@MaVT",
                            vatTu, transaction);
                    }
                }
                return true;
            });
        }

        // Xóa chi tiết trước khi xóa HANG_HOA; có tham chiếu nhập/xuất thì SQL từ chối và rollback.
        public bool xoaDanhMucChiTiet(string ma, ET_HANGHOA? banGoc)
        {
            return ThucHien((connection, transaction) =>
            {
                var hienTai = connection.QuerySingleOrDefault<ET_HANGHOA>(
                    "SELECT * FROM dbo.HANG_HOA WITH(UPDLOCK,HOLDLOCK) WHERE MaHH=@Ma",
                    new { Ma = ma }, transaction);
                if (hienTai == null)
                    throw new InvalidOperationException("Mặt hàng không còn tồn tại. Vui lòng tải lại.");
                KiemTra(connection, transaction, "HANG_HOA", hienTai, hienTai, banGoc);
                connection.Execute("DELETE FROM dbo.THUOC WHERE MaHH=@Ma", new { Ma = ma }, transaction);
                connection.Execute("DELETE FROM dbo.VAT_TU WHERE MaHH=@Ma", new { Ma = ma }, transaction);
                connection.Execute("DELETE FROM dbo.HANG_HOA WHERE MaHH=@Ma", new { Ma = ma }, transaction);
                return true;
            });
        }

        /// <summary>
        /// Tìm hàng hóa theo khóa trong dbo.HANG_HOA; mã được truyền bằng tham số SQL và QuerySingleOrDefault ánh xạ tối đa một bản ghi sang ET.
        /// </summary>
        /// <param name = "ma">Mã khóa chính của bản ghi cần đọc, sửa hoặc xóa; truyền bằng tham số SQL.</param>
        /// <returns>ET của bản ghi tìm thấy, hoặc null nếu mã không tồn tại.</returns>
        public ET_HANGHOA? layHangHoaTheoMa(string ma)
        {
            return ThucHien((connection, transaction) =>
            {
                return connection.QuerySingleOrDefault<ET_HANGHOA>("SELECT * FROM dbo.[HANG_HOA] WHERE [MaHH]=@Ma", new { Ma = ma }, transaction);
            });
        }

        /// <summary>
        /// Thêm hàng hóa: cấp mã có khóa ngay vào duLieu, chuẩn hóa và kiểm tra ET theo metadata SQL rồi INSERT có tham số vào dbo.HANG_HOA trong cùng giao dịch.
        /// </summary>
        /// <param name = "duLieu">ET chứa dữ liệu cần lưu; thêm mới sẽ được gán mã, và kiểm tra metadata có thể chuẩn hóa lại các giá trị ngay trên đối tượng.</param>
        /// <returns>true sau khi INSERT thành công; lỗi dữ liệu/SQL được phát ra bằng ngoại lệ. Giao dịch ngoài chỉ commit khi nơi gọi quyết định.</returns>
        public bool themHangHoa(ET_HANGHOA duLieu)
        {
            return ThucHien((connection, transaction) =>
            {
                duLieu.MaHH = TaoMa(connection, transaction, "HANG_HOA", "MaHH");
                KiemTra<ET_HANGHOA>(connection, transaction, "HANG_HOA", duLieu, null, null);
                connection.Execute("INSERT INTO dbo.[HANG_HOA] ([MaHH],[TenHH],[MaLoaiHH],[DonViTinh],[MoTa],[TrangThai]) VALUES (@MaHH,@TenHH,@MaLoaiHH,@DonViTinh,@MoTa,@TrangThai)", duLieu, transaction);
                return true;
            });
        }

        /// <summary>
        /// Sửa hàng hóa: đọc và giữ khóa UPDLOCK/HOLDLOCK, báo lỗi nếu bản không còn, chuẩn hóa ET và đối chiếu banGoc để phát hiện sửa đồng thời. Khi chỉ đổi trạng thái, chặn thay đổi các trường ngoài TrangThai/NgayXacNhan/NgayCapNhat/LyDoThayDoi, sau đó UPDATE có tham số trong cùng giao dịch.
        /// </summary>
        /// <param name = "duLieu">ET chứa dữ liệu cần lưu; thêm mới sẽ được gán mã, và kiểm tra metadata có thể chuẩn hóa lại các giá trị ngay trên đối tượng.</param>
        /// <param name = "banGoc">Bản ET đã đọc trước khi thao tác; có giá trị để so với SQL và chặn ghi đè/xóa khi bản đã thay đổi, null để bỏ đối chiếu bản gốc.</param>
        /// <param name = "chiDoiTrangThai">true để cấm thay đổi trường nội dung ngoài nhóm trường trạng thái cho phép; không tự đảm bảo mọi chuyển trạng thái đều hợp lệ.</param>
        /// <returns>true khi UPDATE chạy thành công; bản bị mất/thay đổi hoặc dữ liệu sai gây ngoại lệ. Không trả false để biểu thị lỗi.</returns>
        public bool suaHangHoa(ET_HANGHOA duLieu, ET_HANGHOA? banGoc = null, bool chiDoiTrangThai = false)
        {
            return ThucHien((connection, transaction) =>
            {
                var hienTai = connection.QuerySingleOrDefault<ET_HANGHOA>("SELECT * FROM dbo.[HANG_HOA] WITH(UPDLOCK,HOLDLOCK) WHERE [MaHH]=@Ma", new { Ma = duLieu.MaHH }, transaction);
                if (hienTai is null)
                {
                    throw new InvalidOperationException("Bản ghi không còn tồn tại. Vui lòng tải lại.");
                }

                KiemTra(connection, transaction, "HANG_HOA", duLieu, hienTai, banGoc);
                if (chiDoiTrangThai)
                {
                    KiemTraChiDoiTrangThai(duLieu, hienTai);
                }

                connection.Execute("UPDATE dbo.[HANG_HOA] SET [TenHH]=@TenHH,[MaLoaiHH]=@MaLoaiHH,[DonViTinh]=@DonViTinh,[MoTa]=@MoTa,[TrangThai]=@TrangThai WHERE [MaHH]=@MaHH", duLieu, transaction);
                return true;
            });
        }

        /// <summary>
        /// Xóa hàng hóa: đọc bản hiện tại với UPDLOCK/HOLDLOCK, báo lỗi nếu đã mất, so banGoc khi có rồi DELETE theo mã trong dbo.HANG_HOA; toàn bộ thao tác giữ cùng giao dịch và lỗi ràng buộc SQL được phát ra.
        /// </summary>
        /// <param name = "ma">Mã khóa chính của bản ghi cần đọc, sửa hoặc xóa; truyền bằng tham số SQL.</param>
        /// <param name = "banGoc">Bản ET đã đọc trước khi thao tác; có giá trị để so với SQL và chặn ghi đè/xóa khi bản đã thay đổi, null để bỏ đối chiếu bản gốc.</param>
        /// <returns>true khi DELETE chạy thành công; không tồn tại, xung đột hoặc vi phạm SQL gây ngoại lệ.</returns>
        public bool xoaHangHoa(string ma, ET_HANGHOA? banGoc = null)
        {
            return ThucHien((connection, transaction) =>
            {
                var hienTai = connection.QuerySingleOrDefault<ET_HANGHOA>("SELECT * FROM dbo.[HANG_HOA] WITH(UPDLOCK,HOLDLOCK) WHERE [MaHH]=@Ma", new { Ma = ma }, transaction);
                if (hienTai is null)
                {
                    throw new InvalidOperationException("Bản ghi không còn tồn tại. Vui lòng tải lại.");
                }

                KiemTra(connection, transaction, "HANG_HOA", hienTai, hienTai, banGoc);
                connection.Execute("DELETE FROM dbo.[HANG_HOA] WHERE [MaHH]=@Ma", new { Ma = ma }, transaction);
                return true;
            });
        }
    }
}
