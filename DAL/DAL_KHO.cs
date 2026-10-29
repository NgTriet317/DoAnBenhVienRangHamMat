// AI hỗ trợ: bổ sung chú thích tiếng Việt;
using System.Data;
using Dapper;
using ET;

namespace DAL
{
    /// <summary>
    /// Tầng truy cập SQL cho kho trong bảng KHO. Lưu loại kho và nhân viên phụ trách để nhập, xuất và tính tồn theo địa điểm. Dapper ánh xạ dòng SQL sang ET; ThucHien dùng giao dịch được truyền vào hoặc tự quản lý giao dịch cục bộ.
    /// </summary>
    public class DAL_KHO : dbconnect
    {
        /// <summary>
        /// Tạo DAL quản lý kho, chuyển giao dịch tùy chọn cho dbconnect để nhiều thao tác có thể dùng chung kết nối và phạm vi lưu.
        /// </summary>
        /// <param name = "transaction">Giao dịch của nơi gọi; null để hạ tầng tham gia giao dịch môi trường hoặc tự mở giao dịch cục bộ. Nếu truyền vào, nơi gọi quản lý commit/rollback.</param>
        public DAL_KHO(IDbTransaction? transaction = null) : base(transaction)
        {
        }

        /// <summary>
        /// Đọc toàn bộ kho từ dbo.KHO, Dapper ánh xạ từng dòng sang ET, sắp theo mã; ToArray nạp hết dữ liệu trước khi hạ tầng kết thúc phạm vi kết nối/giao dịch.
        /// </summary>
        /// <returns>Danh sách ET đã vật hóa; không có bản ghi thì trả danh sách rỗng.</returns>
        public IEnumerable<ET_KHO> layToanBoKho()
        {
            return ThucHien((connection, transaction) =>
            {
                return connection.Query<ET_KHO>("SELECT * FROM dbo.[KHO] ORDER BY [MaKho]", transaction: transaction).ToArray();
            });
        }

        /// <summary>
        /// Tìm kho theo khóa trong dbo.KHO; mã được truyền bằng tham số SQL và QuerySingleOrDefault ánh xạ tối đa một bản ghi sang ET.
        /// </summary>
        /// <param name = "ma">Mã khóa chính của bản ghi cần đọc, sửa hoặc xóa; truyền bằng tham số SQL.</param>
        /// <returns>ET của bản ghi tìm thấy, hoặc null nếu mã không tồn tại.</returns>
        public ET_KHO? layKhoTheoMa(string ma)
        {
            return ThucHien((connection, transaction) =>
            {
                return connection.QuerySingleOrDefault<ET_KHO>("SELECT * FROM dbo.[KHO] WHERE [MaKho]=@Ma", new { Ma = ma }, transaction);
            });
        }

        /// <summary>
        /// Thêm kho: cấp mã có khóa ngay vào duLieu, chuẩn hóa và kiểm tra ET theo metadata SQL rồi INSERT có tham số vào dbo.KHO trong cùng giao dịch.
        /// </summary>
        /// <param name = "duLieu">ET chứa dữ liệu cần lưu; thêm mới sẽ được gán mã, và kiểm tra metadata có thể chuẩn hóa lại các giá trị ngay trên đối tượng.</param>
        /// <returns>true sau khi INSERT thành công; lỗi dữ liệu/SQL được phát ra bằng ngoại lệ. Giao dịch ngoài chỉ commit khi nơi gọi quyết định.</returns>
        public bool themKho(ET_KHO duLieu)
        {
            return ThucHien((connection, transaction) =>
            {
                duLieu.MaKho = TaoMa(connection, transaction, "KHO", "MaKho");
                KiemTra<ET_KHO>(connection, transaction, "KHO", duLieu, null, null);
                connection.Execute("INSERT INTO dbo.[KHO] ([MaKho],[TenKho],[MaNV],[LoaiKho],[TrangThai]) VALUES (@MaKho,@TenKho,@MaNV,@LoaiKho,@TrangThai)", duLieu, transaction);
                return true;
            });
        }

        /// <summary>
        /// Sửa kho: đọc và giữ khóa UPDLOCK/HOLDLOCK, báo lỗi nếu bản không còn, chuẩn hóa ET và đối chiếu banGoc để phát hiện sửa đồng thời. Khi chỉ đổi trạng thái, chặn thay đổi các trường ngoài TrangThai/NgayXacNhan/NgayCapNhat/LyDoThayDoi, sau đó UPDATE có tham số trong cùng giao dịch.
        /// </summary>
        /// <param name = "duLieu">ET chứa dữ liệu cần lưu; thêm mới sẽ được gán mã, và kiểm tra metadata có thể chuẩn hóa lại các giá trị ngay trên đối tượng.</param>
        /// <param name = "banGoc">Bản ET đã đọc trước khi thao tác; có giá trị để so với SQL và chặn ghi đè/xóa khi bản đã thay đổi, null để bỏ đối chiếu bản gốc.</param>
        /// <param name = "chiDoiTrangThai">true để cấm thay đổi trường nội dung ngoài nhóm trường trạng thái cho phép; không tự đảm bảo mọi chuyển trạng thái đều hợp lệ.</param>
        /// <returns>true khi UPDATE chạy thành công; bản bị mất/thay đổi hoặc dữ liệu sai gây ngoại lệ. Không trả false để biểu thị lỗi.</returns>
        public bool suaKho(ET_KHO duLieu, ET_KHO? banGoc = null, bool chiDoiTrangThai = false)
        {
            return ThucHien((connection, transaction) =>
            {
                var hienTai = connection.QuerySingleOrDefault<ET_KHO>("SELECT * FROM dbo.[KHO] WITH(UPDLOCK,HOLDLOCK) WHERE [MaKho]=@Ma", new { Ma = duLieu.MaKho }, transaction);
                if (hienTai is null)
                {
                    throw new InvalidOperationException("Bản ghi không còn tồn tại. Vui lòng tải lại.");
                }

                KiemTra(connection, transaction, "KHO", duLieu, hienTai, banGoc);
                if (chiDoiTrangThai)
                {
                    KiemTraChiDoiTrangThai(duLieu, hienTai);
                }

                connection.Execute("UPDATE dbo.[KHO] SET [TenKho]=@TenKho,[MaNV]=@MaNV,[LoaiKho]=@LoaiKho,[TrangThai]=@TrangThai WHERE [MaKho]=@MaKho", duLieu, transaction);
                return true;
            });
        }

        /// <summary>
        /// Xóa kho: đọc bản hiện tại với UPDLOCK/HOLDLOCK, báo lỗi nếu đã mất, so banGoc khi có rồi DELETE theo mã trong dbo.KHO; toàn bộ thao tác giữ cùng giao dịch và lỗi ràng buộc SQL được phát ra.
        /// </summary>
        /// <param name = "ma">Mã khóa chính của bản ghi cần đọc, sửa hoặc xóa; truyền bằng tham số SQL.</param>
        /// <param name = "banGoc">Bản ET đã đọc trước khi thao tác; có giá trị để so với SQL và chặn ghi đè/xóa khi bản đã thay đổi, null để bỏ đối chiếu bản gốc.</param>
        /// <returns>true khi DELETE chạy thành công; không tồn tại, xung đột hoặc vi phạm SQL gây ngoại lệ.</returns>
        public bool xoaKho(string ma, ET_KHO? banGoc = null)
        {
            return ThucHien((connection, transaction) =>
            {
                var hienTai = connection.QuerySingleOrDefault<ET_KHO>("SELECT * FROM dbo.[KHO] WITH(UPDLOCK,HOLDLOCK) WHERE [MaKho]=@Ma", new { Ma = ma }, transaction);
                if (hienTai is null)
                {
                    throw new InvalidOperationException("Bản ghi không còn tồn tại. Vui lòng tải lại.");
                }

                KiemTra(connection, transaction, "KHO", hienTai, hienTai, banGoc);
                connection.Execute("DELETE FROM dbo.[KHO] WHERE [MaKho]=@Ma", new { Ma = ma }, transaction);
                return true;
            });
        }

        // Tổng tồn theo loại kho (thuốc, vật tư, thiết bị); mặt hàng chưa có lô vẫn hiện tồn bằng 0.
        public DataTable layTonKhoTheoKho(string maKho)
        {
            return ThucHien((connection, transaction) =>
            {
                using var reader = connection.ExecuteReader(@"
                    SELECT h.MaHH, h.TenHH,
                        COALESCE(lhh.TenLoaiHH, N'Chưa phân loại') AS LoaiHang,
                        h.DonViTinh,
                        COALESCE(v.MucTonToiThieu, th.MucTonToiThieu, 0) AS MucTonToiThieu,
                        COALESCE(SUM(t.SoLuongTon), 0) AS SoLuongTon,
                        CASE
                            WHEN COALESCE(SUM(t.SoLuongTon), 0) <= 0 THEN N'Hết hàng'
                            WHEN COALESCE(SUM(t.SoLuongTon), 0) <= COALESCE(v.MucTonToiThieu, th.MucTonToiThieu, 0) THEN N'Sắp hết'
                            ELSE N'Còn hàng'
                        END AS TrangThaiTon
                    FROM dbo.KHO k
                    CROSS JOIN dbo.HANG_HOA h
                    LEFT JOIN dbo.LOAI_HANG_HOA lhh ON lhh.MaLoaiHH = h.MaLoaiHH
                    LEFT JOIN dbo.VAT_TU v ON v.MaHH = h.MaHH
                    LEFT JOIN dbo.THUOC th ON th.MaHH = h.MaHH
                    LEFT JOIN dbo.VW_TON_KHO t ON t.MaKho = k.MaKho AND t.MaHH = h.MaHH
                        AND NOT EXISTS (
                            SELECT 1 FROM dbo.LO_HANG_HOA l
                            WHERE l.MaLo = t.MaLo AND l.MaKho = t.MaKho
                                AND l.TrangThai = N'Chờ nhập'
                        )
                    WHERE k.MaKho = @MaKho
                        AND ((k.LoaiKho = N'Thuốc' AND th.MaHH IS NOT NULL)
                             OR (k.LoaiKho = N'Vật tư' AND v.MaHH IS NOT NULL)
                             OR (k.LoaiKho = N'Thiết bị' AND
                                 (lhh.TenLoaiHH LIKE N'Thiết bị%'
                                  OR h.MaLoaiHH IN ('LHH008', 'LHH009', 'LHH010'))))
                    GROUP BY h.MaHH, h.TenHH, lhh.TenLoaiHH, h.DonViTinh,
                        v.MucTonToiThieu, th.MucTonToiThieu
                    ORDER BY h.TenHH",
                    new { MaKho = maKho }, transaction);
                var table = new DataTable();
                table.Load(reader);
                return table;
            });
        }

        // Dữ liệu từng lô để xem chi tiết mặt hàng trong kho.
        public DataTable layLoTonTheoKho(string maKho)
        {
            return ThucHien((connection, transaction) =>
            {
                using var reader = connection.ExecuteReader(@"
                    SELECT t.MaHH,
                        COALESCE(NULLIF(l.SoLo, N''), t.MaLo, N'Chưa gán lô') AS SoLo,
                        t.SoLuongTon AS SoLuongLo,
                        COALESCE(ncc.TenNCC, N'Chưa có nhà cung cấp') AS TenNCC,
                        CASE
                            WHEN t.SoLuongTon <= 0 OR l.HanSuDung IS NULL THEN N''
                            WHEN l.HanSuDung < CAST(GETDATE() AS date) THEN N'Đã hết hạn'
                            WHEN l.HanSuDung <= DATEADD(day, 30, CAST(GETDATE() AS date)) THEN N'Sắp hết hạn'
                            ELSE N''
                        END AS CanhBaoHan,
                        CASE
                            WHEN l.HanSuDung IS NULL THEN N'Không có'
                            WHEN l.HanSuDung < CAST(GETDATE() AS date)
                                THEN CONVERT(nvarchar(10), l.HanSuDung, 103) + N' (Hết hạn)'
                            ELSE CONVERT(nvarchar(10), l.HanSuDung, 103)
                        END AS HanDungHienThi
                    FROM dbo.VW_TON_KHO t
                    JOIN dbo.KHO k ON k.MaKho = t.MaKho
                    JOIN dbo.HANG_HOA h ON h.MaHH = t.MaHH
                    LEFT JOIN dbo.LOAI_HANG_HOA lhh ON lhh.MaLoaiHH = h.MaLoaiHH
                    LEFT JOIN dbo.LO_HANG_HOA l ON l.MaLo = t.MaLo AND l.MaKho = t.MaKho
                    LEFT JOIN dbo.NHA_CUNG_CAP ncc ON ncc.MaNCC = l.MaNCC
                    WHERE t.MaKho = @MaKho
                        AND (l.MaLo IS NULL OR l.TrangThai <> N'Chờ nhập')
                        AND ((k.LoaiKho = N'Thuốc'
                              AND EXISTS (SELECT 1 FROM dbo.THUOC th WHERE th.MaHH = t.MaHH))
                             OR (k.LoaiKho = N'Vật tư'
                              AND EXISTS (SELECT 1 FROM dbo.VAT_TU v WHERE v.MaHH = t.MaHH))
                             OR (k.LoaiKho = N'Thiết bị' AND
                                 (lhh.TenLoaiHH LIKE N'Thiết bị%'
                                  OR h.MaLoaiHH IN ('LHH008', 'LHH009', 'LHH010'))))
                    ORDER BY t.MaHH, l.HanSuDung, t.MaLo",
                    new { MaKho = maKho }, transaction);
                var table = new DataTable();
                table.Load(reader);
                return table;
            });
        }

        public ET_HANGHOA layTenHHTheoMa(string maHH)
        {
            using var connection = CreateConnection();

            return connection.Query<ET_HANGHOA>("SELECT * FROM dbo.[HANG_HOA] WHERE [MaHH]=@Ma", new {Ma = maHH}).FirstOrDefault();
        }

        public ET_LOAIHANGHOA layTenLHHTheoMa(string maLHH)
        {
            using var connection = CreateConnection();

            return connection.Query<ET_LOAIHANGHOA>("SELECT * from dbo.[LOAI_HANG_HOA] where [MaLoaiHH]=@ma", new { ma = maLHH }).FirstOrDefault();
        }
    }
}
