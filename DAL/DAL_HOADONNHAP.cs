// AI hỗ trợ: bổ sung chú thích tiếng Việt;
using System.Data;
using Dapper;
using ET;

namespace DAL
{
    /// <summary>
    /// Tầng truy cập SQL cho hóa đơn nhập trong bảng HOA_DON_NHAP. Lưu chứng từ nhà cung cấp, nhân viên lập và tổng tiền; bảo vệ nội dung chứng từ đã rời trạng thái nháp. Dapper ánh xạ dòng SQL sang ET; ThucHien dùng giao dịch được truyền vào hoặc tự quản lý giao dịch cục bộ.
    /// </summary>
    public class DAL_HOADONNHAP : dbconnect
    {
        /// <summary>
        /// Tạo DAL quản lý hóa đơn nhập, chuyển giao dịch tùy chọn cho dbconnect để nhiều thao tác có thể dùng chung kết nối và phạm vi lưu.
        /// </summary>
        /// <param name = "transaction">Giao dịch của nơi gọi; null để hạ tầng tham gia giao dịch môi trường hoặc tự mở giao dịch cục bộ. Nếu truyền vào, nơi gọi quản lý commit/rollback.</param>
        public DAL_HOADONNHAP(IDbTransaction? transaction = null) : base(transaction)
        {
        }

        /// <summary>
        /// Đọc toàn bộ hóa đơn nhập từ dbo.HOA_DON_NHAP, Dapper ánh xạ từng dòng sang ET, sắp theo mã; ToArray nạp hết dữ liệu trước khi hạ tầng kết thúc phạm vi kết nối/giao dịch.
        /// </summary>
        /// <returns>Danh sách ET đã vật hóa; không có bản ghi thì trả danh sách rỗng.</returns>
        public IEnumerable<ET_HOADONNHAP> layToanBoHoaDonNhap()
        {
            return ThucHien((connection, transaction) =>
            {
                return connection.Query<ET_HOADONNHAP>("SELECT * FROM dbo.[HOA_DON_NHAP] ORDER BY [MaHDN]", transaction: transaction).ToArray();
            });
        }

        /// <summary>
        /// Tìm hóa đơn nhập theo khóa trong dbo.HOA_DON_NHAP; mã được truyền bằng tham số SQL và QuerySingleOrDefault ánh xạ tối đa một bản ghi sang ET.
        /// </summary>
        /// <param name = "ma">Mã khóa chính của bản ghi cần đọc, sửa hoặc xóa; truyền bằng tham số SQL.</param>
        /// <returns>ET của bản ghi tìm thấy, hoặc null nếu mã không tồn tại.</returns>
        public ET_HOADONNHAP? layHoaDonNhapTheoMa(string ma)
        {
            return ThucHien((connection, transaction) =>
            {
                return connection.QuerySingleOrDefault<ET_HOADONNHAP>("SELECT * FROM dbo.[HOA_DON_NHAP] WHERE [MaHDN]=@Ma", new { Ma = ma }, transaction);
            });
        }

        /// <summary>
        /// Thêm hóa đơn nhập: cấp mã có khóa ngay vào duLieu, kiểm tra nghiệp vụ, chuẩn hóa và kiểm tra ET theo metadata SQL rồi INSERT có tham số vào dbo.HOA_DON_NHAP trong cùng giao dịch.
        /// </summary>
        /// <param name = "duLieu">ET chứa dữ liệu cần lưu; thêm mới sẽ được gán mã, và kiểm tra metadata có thể chuẩn hóa lại các giá trị ngay trên đối tượng.</param>
        /// <returns>true sau khi INSERT thành công; lỗi dữ liệu/SQL được phát ra bằng ngoại lệ. Giao dịch ngoài chỉ commit khi nơi gọi quyết định.</returns>
        public bool themHoaDonNhap(ET_HOADONNHAP duLieu)
        {
            return ThucHien((connection, transaction) =>
            {
                duLieu.MaHDN = TaoMa(connection, transaction, "HOA_DON_NHAP", "MaHDN");
                KiemTraNghiepVu(null);
                KiemTra<ET_HOADONNHAP>(connection, transaction, "HOA_DON_NHAP", duLieu, null, null);
                connection.Execute("""
                INSERT INTO dbo.[HOA_DON_NHAP] ([MaHDN],[SoHoaDonNCC],[MaNCC],
                    [MaNV],[NgayHoaDon],[TongTien],
                    [TrangThai],[GhiChu])
                VALUES (@MaHDN,@SoHoaDonNCC,
                    @MaNCC,@MaNV,@NgayHoaDon,
                    @TongTien,@TrangThai,@GhiChu)
                """, duLieu, transaction);
                return true;
            });
        }

        /// <summary>
        /// Sửa hóa đơn nhập: đọc và giữ khóa UPDLOCK/HOLDLOCK, báo lỗi nếu bản không còn, kiểm tra nghiệp vụ, chuẩn hóa ET và đối chiếu banGoc để phát hiện sửa đồng thời. Khi chỉ đổi trạng thái, chặn thay đổi các trường ngoài TrangThai/NgayXacNhan/NgayCapNhat/LyDoThayDoi, sau đó UPDATE có tham số trong cùng giao dịch.
        /// </summary>
        /// <param name = "duLieu">ET chứa dữ liệu cần lưu; thêm mới sẽ được gán mã, và kiểm tra metadata có thể chuẩn hóa lại các giá trị ngay trên đối tượng.</param>
        /// <param name = "banGoc">Bản ET đã đọc trước khi thao tác; có giá trị để so với SQL và chặn ghi đè/xóa khi bản đã thay đổi, null để bỏ đối chiếu bản gốc.</param>
        /// <param name = "chiDoiTrangThai">true để cấm thay đổi trường nội dung ngoài nhóm trường trạng thái cho phép; không tự đảm bảo mọi chuyển trạng thái đều hợp lệ.</param>
        /// <returns>true khi UPDATE chạy thành công; bản bị mất/thay đổi hoặc dữ liệu sai gây ngoại lệ. Không trả false để biểu thị lỗi.</returns>
        public bool suaHoaDonNhap(ET_HOADONNHAP duLieu, ET_HOADONNHAP? banGoc = null, bool chiDoiTrangThai = false)
        {
            return ThucHien((connection, transaction) =>
            {
                var hienTai = connection.QuerySingleOrDefault<ET_HOADONNHAP>("SELECT * FROM dbo.[HOA_DON_NHAP] WITH(UPDLOCK,HOLDLOCK) WHERE [MaHDN]=@Ma", new { Ma = duLieu.MaHDN }, transaction);
                if (hienTai is null)
                {
                    throw new InvalidOperationException("Bản ghi không còn tồn tại. Vui lòng tải lại.");
                }

                KiemTraNghiepVu(hienTai);
                KiemTra(connection, transaction, "HOA_DON_NHAP", duLieu, hienTai, banGoc);
                if (chiDoiTrangThai)
                {
                    KiemTraChiDoiTrangThai(duLieu, hienTai);
                }

                connection.Execute("""
                UPDATE dbo.[HOA_DON_NHAP]
                SET [SoHoaDonNCC]=@SoHoaDonNCC,[MaNCC]=@MaNCC,[MaNV]=@MaNV,
                    [NgayHoaDon]=@NgayHoaDon,[TongTien]=@TongTien,[TrangThai]=@TrangThai,
                    [GhiChu]=@GhiChu
                WHERE [MaHDN]=@MaHDN
                """, duLieu, transaction);
                return true;
            });
        }

        /// <summary>
        /// Xóa hóa đơn nhập: đọc bản hiện tại với UPDLOCK/HOLDLOCK, báo lỗi nếu đã mất, kiểm tra nghiệp vụ, so banGoc khi có rồi DELETE theo mã trong dbo.HOA_DON_NHAP; toàn bộ thao tác giữ cùng giao dịch và lỗi ràng buộc SQL được phát ra.
        /// </summary>
        /// <param name = "ma">Mã khóa chính của bản ghi cần đọc, sửa hoặc xóa; truyền bằng tham số SQL.</param>
        /// <param name = "banGoc">Bản ET đã đọc trước khi thao tác; có giá trị để so với SQL và chặn ghi đè/xóa khi bản đã thay đổi, null để bỏ đối chiếu bản gốc.</param>
        /// <returns>true khi DELETE chạy thành công; không tồn tại, xung đột hoặc vi phạm SQL gây ngoại lệ.</returns>
        public bool xoaHoaDonNhap(string ma, ET_HOADONNHAP? banGoc = null)
        {
            return ThucHien((connection, transaction) =>
            {
                var hienTai = connection.QuerySingleOrDefault<ET_HOADONNHAP>("SELECT * FROM dbo.[HOA_DON_NHAP] WITH(UPDLOCK,HOLDLOCK) WHERE [MaHDN]=@Ma", new { Ma = ma }, transaction);
                if (hienTai is null)
                {
                    throw new InvalidOperationException("Bản ghi không còn tồn tại. Vui lòng tải lại.");
                }

                KiemTraNghiepVu(hienTai);
                KiemTra(connection, transaction, "HOA_DON_NHAP", hienTai, hienTai, banGoc);
                connection.Execute("DELETE FROM dbo.[HOA_DON_NHAP] WHERE [MaHDN]=@Ma", new { Ma = ma }, transaction);
                return true;
            });
        }

        /// <summary>
        /// Khi bản hiện tại tồn tại, yêu cầu nó còn Nháp trước mọi sửa hoặc xóa; hàm không truy vấn SQL và không kiểm tra dữ liệu bản thêm mới.
        /// </summary>
        /// <param name = "banHienTai">Bản hiện tại đã đọc từ SQL; null khi thêm mới, dùng để bảo vệ nội dung/trạng thái và liên kết đã lưu.</param>
        private void KiemTraNghiepVu(ET_HOADONNHAP? banHienTai)
        {
            if (banHienTai is not null && banHienTai.TrangThai != "Nháp")
            {
                throw new InvalidOperationException("Chứng từ đã xác nhận hoặc đã hủy được giữ nguyên.");
            }
        }
    }
}
