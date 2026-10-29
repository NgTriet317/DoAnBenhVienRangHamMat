// AI hỗ trợ: bổ sung chú thích tiếng Việt;
using System;
using System.Data;
using Dapper;
using ET;

namespace DAL
{
    /// <summary>
    /// Tầng truy cập SQL cho thiết bị trong bảng THIET_BI. Theo dõi serial, ngày mua, nhà cung cấp, phòng đặt, bảo hành và tình trạng thiết bị. Dapper ánh xạ dòng SQL sang ET; ThucHien dùng giao dịch được truyền vào hoặc tự quản lý giao dịch cục bộ.
    /// </summary>
    public class DAL_THIETBI : dbconnect
    {
        /// <summary>
        /// Tạo DAL quản lý thiết bị, chuyển giao dịch tùy chọn cho dbconnect để nhiều thao tác có thể dùng chung kết nối và phạm vi lưu.
        /// </summary>
        /// <param name = "transaction">Giao dịch của nơi gọi; null để hạ tầng tham gia giao dịch môi trường hoặc tự mở giao dịch cục bộ. Nếu truyền vào, nơi gọi quản lý commit/rollback.</param>
        public DAL_THIETBI(IDbTransaction? transaction = null) : base(transaction)
        {
        }

        /// <summary>
        /// Đọc toàn bộ thiết bị từ dbo.THIET_BI, Dapper ánh xạ từng dòng sang ET, sắp theo mã; ToArray nạp hết dữ liệu trước khi hạ tầng kết thúc phạm vi kết nối/giao dịch.
        /// </summary>
        /// <returns>Danh sách ET đã vật hóa; không có bản ghi thì trả danh sách rỗng.</returns>
        public IEnumerable<ET_THIETBI> layToanBoThietBi()
        {
            return ThucHien((connection, transaction) =>
            {
                return connection.Query<ET_THIETBI>("SELECT * FROM dbo.[THIET_BI] ORDER BY [MaTB]", transaction: transaction).ToArray();
            });
        }

        /// <summary>
        /// Tìm thiết bị theo khóa trong dbo.THIET_BI; mã được truyền bằng tham số SQL và QuerySingleOrDefault ánh xạ tối đa một bản ghi sang ET.
        /// </summary>
        /// <param name = "ma">Mã khóa chính của bản ghi cần đọc, sửa hoặc xóa; truyền bằng tham số SQL.</param>
        /// <returns>ET của bản ghi tìm thấy, hoặc null nếu mã không tồn tại.</returns>
        public ET_THIETBI? layThietBiTheoMa(string ma)
        {
            return ThucHien((connection, transaction) =>
            {
                return connection.QuerySingleOrDefault<ET_THIETBI>("SELECT * FROM dbo.[THIET_BI] WHERE [MaTB]=@Ma", new { Ma = ma }, transaction);
            });
        }

        /// <summary>
        /// Thêm thiết bị: cấp mã có khóa ngay vào duLieu, chuẩn hóa và kiểm tra ET theo metadata SQL rồi INSERT có tham số vào dbo.THIET_BI trong cùng giao dịch.
        /// </summary>
        /// <param name = "duLieu">ET chứa dữ liệu cần lưu; thêm mới sẽ được gán mã, và kiểm tra metadata có thể chuẩn hóa lại các giá trị ngay trên đối tượng.</param>
        /// <returns>true sau khi INSERT thành công; lỗi dữ liệu/SQL được phát ra bằng ngoại lệ. Giao dịch ngoài chỉ commit khi nơi gọi quyết định.</returns>
        public bool themThietBi(ET_THIETBI duLieu)
        {
            return ThucHien((connection, transaction) =>
            {
                duLieu.MaTB = TaoMa(connection, transaction, "THIET_BI", "MaTB");
                KiemTra<ET_THIETBI>(connection, transaction, "THIET_BI", duLieu, null, null);
                connection.Execute("""
                INSERT INTO dbo.[THIET_BI] ([MaTB],[MaHH],[SoSerial],
                    [NgayMua],[MaNCC],[MaPhong],
                    [TinhTrang],[HanBaoHanh],[TrangThai],
                    [GhiChu])
                VALUES (@MaTB,@MaHH,@SoSerial,
                    @NgayMua,@MaNCC,@MaPhong,
                    @TinhTrang,@HanBaoHanh,@TrangThai,
                    @GhiChu)
                """, duLieu, transaction);
                return true;
            });
        }

        /// <summary>
        /// Sửa thiết bị: đọc và giữ khóa UPDLOCK/HOLDLOCK, báo lỗi nếu bản không còn, chuẩn hóa ET và đối chiếu banGoc để phát hiện sửa đồng thời. Khi chỉ đổi trạng thái, chặn thay đổi các trường ngoài TrangThai/NgayXacNhan/NgayCapNhat/LyDoThayDoi, sau đó UPDATE có tham số trong cùng giao dịch.
        /// </summary>
        /// <param name = "duLieu">ET chứa dữ liệu cần lưu; thêm mới sẽ được gán mã, và kiểm tra metadata có thể chuẩn hóa lại các giá trị ngay trên đối tượng.</param>
        /// <param name = "banGoc">Bản ET đã đọc trước khi thao tác; có giá trị để so với SQL và chặn ghi đè/xóa khi bản đã thay đổi, null để bỏ đối chiếu bản gốc.</param>
        /// <param name = "chiDoiTrangThai">true để cấm thay đổi trường nội dung ngoài nhóm trường trạng thái cho phép; không tự đảm bảo mọi chuyển trạng thái đều hợp lệ.</param>
        /// <returns>true khi UPDATE chạy thành công; bản bị mất/thay đổi hoặc dữ liệu sai gây ngoại lệ. Không trả false để biểu thị lỗi.</returns>
        public bool suaThietBi(ET_THIETBI duLieu, ET_THIETBI? banGoc = null, bool chiDoiTrangThai = false)
        {
            return ThucHien((connection, transaction) =>
            {
                var hienTai = connection.QuerySingleOrDefault<ET_THIETBI>("SELECT * FROM dbo.[THIET_BI] WITH(UPDLOCK,HOLDLOCK) WHERE [MaTB]=@Ma", new { Ma = duLieu.MaTB }, transaction);
                if (hienTai is null)
                {
                    throw new InvalidOperationException("Bản ghi không còn tồn tại. Vui lòng tải lại.");
                }

                KiemTra(connection, transaction, "THIET_BI", duLieu, hienTai, banGoc);
                if (chiDoiTrangThai)
                {
                    KiemTraChiDoiTrangThai(duLieu, hienTai);
                }

                connection.Execute("""
                UPDATE dbo.[THIET_BI]
                SET [MaHH]=@MaHH,[SoSerial]=@SoSerial,[NgayMua]=@NgayMua,
                    [MaNCC]=@MaNCC,[MaPhong]=@MaPhong,[TinhTrang]=@TinhTrang,
                    [HanBaoHanh]=@HanBaoHanh,[TrangThai]=@TrangThai,[GhiChu]=@GhiChu
                WHERE [MaTB]=@MaTB
                """, duLieu, transaction);
                return true;
            });
        }

        /// <summary>
        /// Xóa thiết bị: đọc bản hiện tại với UPDLOCK/HOLDLOCK, báo lỗi nếu đã mất, so banGoc khi có rồi DELETE theo mã trong dbo.THIET_BI; toàn bộ thao tác giữ cùng giao dịch và lỗi ràng buộc SQL được phát ra.
        /// </summary>
        /// <param name = "ma">Mã khóa chính của bản ghi cần đọc, sửa hoặc xóa; truyền bằng tham số SQL.</param>
        /// <param name = "banGoc">Bản ET đã đọc trước khi thao tác; có giá trị để so với SQL và chặn ghi đè/xóa khi bản đã thay đổi, null để bỏ đối chiếu bản gốc.</param>
        /// <returns>true khi DELETE chạy thành công; không tồn tại, xung đột hoặc vi phạm SQL gây ngoại lệ.</returns>
        public bool xoaThietBi(string ma, ET_THIETBI? banGoc = null)
        {
            return ThucHien((connection, transaction) =>
            {
                var hienTai = connection.QuerySingleOrDefault<ET_THIETBI>("SELECT * FROM dbo.[THIET_BI] WITH(UPDLOCK,HOLDLOCK) WHERE [MaTB]=@Ma", new { Ma = ma }, transaction);
                if (hienTai is null)
                {
                    throw new InvalidOperationException("Bản ghi không còn tồn tại. Vui lòng tải lại.");
                }

                KiemTra(connection, transaction, "THIET_BI", hienTai, hienTai, banGoc);
                connection.Execute("DELETE FROM dbo.[THIET_BI] WHERE [MaTB]=@Ma", new { Ma = ma }, transaction);
                return true;
            });
        }
        // Chỉ cập nhật phòng và trạng thái nhận bàn giao của thiết bị đã có.
        public bool phanThietBiVaoPhong(string maTB, string? maPhong, ET_THIETBI banGoc)
        {
            return ThucHien((connection, transaction) =>
            {
                ET_THIETBI? hienTai = connection.QuerySingleOrDefault<ET_THIETBI>(
                    "SELECT * FROM dbo.[THIET_BI] WITH(UPDLOCK,HOLDLOCK) WHERE [MaTB]=@MaTB",
                    new { MaTB = maTB }, transaction);
                if (hienTai == null)
                    throw new InvalidOperationException("Thiết bị không còn tồn tại. Vui lòng tải lại.");

                // Chặn ghi đè nếu người khác vừa thay đổi phòng hay hồ sơ thiết bị.
                ET_THIETBI capNhat = new ET_THIETBI(hienTai.MaTB, hienTai.MaHH,
                    hienTai.SoSerial, hienTai.NgayMua, hienTai.MaNCC, maPhong,
                    hienTai.TinhTrang, hienTai.HanBaoHanh, hienTai.TrangThai, hienTai.GhiChu);

                if (maPhong != null)
                {
                    bool phongHoatDong = connection.ExecuteScalar<int>(
                        "SELECT COUNT(1) FROM dbo.[PHONG] WITH (HOLDLOCK) WHERE [MaPhong]=@MaPhong AND [TrangThai]=N'Đang hoạt động'",
                        new { MaPhong = maPhong }, transaction) > 0;
                    if (!phongHoatDong)
                        throw new InvalidOperationException("Phòng được chọn không tồn tại hoặc đã ngừng hoạt động.");

                    if (hienTai.TrangThai != "Chờ bàn giao" && hienTai.TrangThai != "Đang sử dụng")
                        throw new InvalidOperationException("Thiết bị đang ngừng sử dụng hoặc bảo trì, không thể phân phòng.");
                    if (hienTai.TinhTrang.Contains("Hỏng", StringComparison.OrdinalIgnoreCase)
                        || hienTai.TinhTrang.Contains("bảo trì", StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("Thiết bị đang hỏng hoặc bảo trì, không thể phân phòng.");

                    capNhat.TrangThai = "Đang sử dụng";
                }
                else if (hienTai.TrangThai == "Đang sử dụng" || hienTai.TrangThai == "Chờ bàn giao")
                {
                    capNhat.TrangThai = "Chờ bàn giao";
                }

                KiemTra(connection, transaction, "THIET_BI", capNhat, hienTai, banGoc);
                connection.Execute(@"
                    UPDATE dbo.[THIET_BI]
                    SET [MaPhong]=@MaPhong, [TrangThai]=@TrangThai
                    WHERE [MaTB]=@MaTB", capNhat, transaction);
                return true;
            });
        }

    }
}
