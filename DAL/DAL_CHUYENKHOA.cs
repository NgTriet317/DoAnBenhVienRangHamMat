// AI hỗ trợ: bổ sung chú thích tiếng Việt;
using System.Data;
using Dapper;
using ET;

namespace DAL
{
    /// <summary>
    /// Tầng truy cập SQL cho chuyên khoa trong bảng CHUYEN_KHOA. Lưu chuyên khoa thuộc khoa để nhân viên và dịch vụ tham chiếu. Dapper ánh xạ dòng SQL sang ET; ThucHien dùng giao dịch được truyền vào hoặc tự quản lý giao dịch cục bộ.
    /// </summary>
    public class DAL_CHUYENKHOA : dbconnect
    {
        /// <summary>
        /// Tạo DAL quản lý chuyên khoa, chuyển giao dịch tùy chọn cho dbconnect để nhiều thao tác có thể dùng chung kết nối và phạm vi lưu.
        /// </summary>
        /// <param name = "transaction">Giao dịch của nơi gọi; null để hạ tầng tham gia giao dịch môi trường hoặc tự mở giao dịch cục bộ. Nếu truyền vào, nơi gọi quản lý commit/rollback.</param>
        public DAL_CHUYENKHOA(IDbTransaction? transaction = null) : base(transaction)
        {
        }

        /// <summary>
        /// Đọc toàn bộ chuyên khoa từ dbo.CHUYEN_KHOA, Dapper ánh xạ từng dòng sang ET, sắp theo mã; ToArray nạp hết dữ liệu trước khi hạ tầng kết thúc phạm vi kết nối/giao dịch.
        /// </summary>
        /// <returns>Danh sách ET đã vật hóa; không có bản ghi thì trả danh sách rỗng.</returns>
        public IEnumerable<ET_CHUYENKHOA> layToanBoChuyenKhoa()
        {
            return ThucHien((connection, transaction) =>
            {
                return connection.Query<ET_CHUYENKHOA>("SELECT * FROM dbo.[CHUYEN_KHOA] ORDER BY [MaCK]", transaction: transaction).ToArray();
            });
        }

        /// <summary>
        /// Tìm chuyên khoa theo khóa trong dbo.CHUYEN_KHOA; mã được truyền bằng tham số SQL và QuerySingleOrDefault ánh xạ tối đa một bản ghi sang ET.
        /// </summary>
        /// <param name = "ma">Mã khóa chính của bản ghi cần đọc, sửa hoặc xóa; truyền bằng tham số SQL.</param>
        /// <returns>ET của bản ghi tìm thấy, hoặc null nếu mã không tồn tại.</returns>
        public ET_CHUYENKHOA? layChuyenKhoaTheoMa(string ma)
        {
            return ThucHien((connection, transaction) =>
            {
                return connection.QuerySingleOrDefault<ET_CHUYENKHOA>("SELECT * FROM dbo.[CHUYEN_KHOA] WHERE [MaCK]=@Ma", new { Ma = ma }, transaction);
            });
        }

        /// <summary>
        /// Thêm chuyên khoa: cấp mã có khóa ngay vào duLieu, chuẩn hóa và kiểm tra ET theo metadata SQL rồi INSERT có tham số vào dbo.CHUYEN_KHOA trong cùng giao dịch.
        /// </summary>
        /// <param name = "duLieu">ET chứa dữ liệu cần lưu; thêm mới sẽ được gán mã, và kiểm tra metadata có thể chuẩn hóa lại các giá trị ngay trên đối tượng.</param>
        /// <returns>true sau khi INSERT thành công; lỗi dữ liệu/SQL được phát ra bằng ngoại lệ. Giao dịch ngoài chỉ commit khi nơi gọi quyết định.</returns>
        public bool themChuyenKhoa(ET_CHUYENKHOA duLieu)
        {
            return ThucHien((connection, transaction) =>
            {
                duLieu.MaCK = TaoMa(connection, transaction, "CHUYEN_KHOA", "MaCK");
                KiemTra<ET_CHUYENKHOA>(connection, transaction, "CHUYEN_KHOA", duLieu, null, null);
                connection.Execute("INSERT INTO dbo.[CHUYEN_KHOA] ([MaCK],[MaKhoa],[TenCK],[MoTa],[TrangThai]) VALUES (@MaCK,@MaKhoa,@TenCK,@MoTa,@TrangThai)", duLieu, transaction);
                return true;
            });
        }

        /// <summary>
        /// Sửa chuyên khoa: đọc và giữ khóa UPDLOCK/HOLDLOCK, báo lỗi nếu bản không còn, chuẩn hóa ET và đối chiếu banGoc để phát hiện sửa đồng thời. Khi chỉ đổi trạng thái, chặn thay đổi các trường ngoài TrangThai/NgayXacNhan/NgayCapNhat/LyDoThayDoi, sau đó UPDATE có tham số trong cùng giao dịch.
        /// </summary>
        /// <param name = "duLieu">ET chứa dữ liệu cần lưu; thêm mới sẽ được gán mã, và kiểm tra metadata có thể chuẩn hóa lại các giá trị ngay trên đối tượng.</param>
        /// <param name = "banGoc">Bản ET đã đọc trước khi thao tác; có giá trị để so với SQL và chặn ghi đè/xóa khi bản đã thay đổi, null để bỏ đối chiếu bản gốc.</param>
        /// <param name = "chiDoiTrangThai">true để cấm thay đổi trường nội dung ngoài nhóm trường trạng thái cho phép; không tự đảm bảo mọi chuyển trạng thái đều hợp lệ.</param>
        /// <returns>true khi UPDATE chạy thành công; bản bị mất/thay đổi hoặc dữ liệu sai gây ngoại lệ. Không trả false để biểu thị lỗi.</returns>
        public bool suaChuyenKhoa(ET_CHUYENKHOA duLieu, ET_CHUYENKHOA? banGoc = null, bool chiDoiTrangThai = false)
        {
            return ThucHien((connection, transaction) =>
            {
                var hienTai = connection.QuerySingleOrDefault<ET_CHUYENKHOA>("SELECT * FROM dbo.[CHUYEN_KHOA] WITH(UPDLOCK,HOLDLOCK) WHERE [MaCK]=@Ma", new { Ma = duLieu.MaCK }, transaction);
                if (hienTai is null)
                {
                    throw new InvalidOperationException("Bản ghi không còn tồn tại. Vui lòng tải lại.");
                }

                KiemTra(connection, transaction, "CHUYEN_KHOA", duLieu, hienTai, banGoc);
                if (chiDoiTrangThai)
                {
                    KiemTraChiDoiTrangThai(duLieu, hienTai);
                }

                connection.Execute("UPDATE dbo.[CHUYEN_KHOA] SET [MaKhoa]=@MaKhoa,[TenCK]=@TenCK,[MoTa]=@MoTa,[TrangThai]=@TrangThai WHERE [MaCK]=@MaCK", duLieu, transaction);
                return true;
            });
        }

        /// <summary>
        /// Xóa chuyên khoa: đọc bản hiện tại với UPDLOCK/HOLDLOCK, báo lỗi nếu đã mất, so banGoc khi có rồi DELETE theo mã trong dbo.CHUYEN_KHOA; toàn bộ thao tác giữ cùng giao dịch và lỗi ràng buộc SQL được phát ra.
        /// </summary>
        /// <param name = "ma">Mã khóa chính của bản ghi cần đọc, sửa hoặc xóa; truyền bằng tham số SQL.</param>
        /// <param name = "banGoc">Bản ET đã đọc trước khi thao tác; có giá trị để so với SQL và chặn ghi đè/xóa khi bản đã thay đổi, null để bỏ đối chiếu bản gốc.</param>
        /// <returns>true khi DELETE chạy thành công; không tồn tại, xung đột hoặc vi phạm SQL gây ngoại lệ.</returns>
        public bool xoaChuyenKhoa(string ma, ET_CHUYENKHOA? banGoc = null)
        {
            return ThucHien((connection, transaction) =>
            {
                var hienTai = connection.QuerySingleOrDefault<ET_CHUYENKHOA>("SELECT * FROM dbo.[CHUYEN_KHOA] WITH(UPDLOCK,HOLDLOCK) WHERE [MaCK]=@Ma", new { Ma = ma }, transaction);
                if (hienTai is null)
                {
                    throw new InvalidOperationException("Bản ghi không còn tồn tại. Vui lòng tải lại.");
                }

                KiemTra(connection, transaction, "CHUYEN_KHOA", hienTai, hienTai, banGoc);
                connection.Execute("DELETE FROM dbo.[CHUYEN_KHOA] WHERE [MaCK]=@Ma", new { Ma = ma }, transaction);
                return true;
            });
        }

        /// <summary>
        /// Tên gọi tương thích của layToanBoChuyenKhoa; trả toàn bộ chuyên khoa theo mã bằng cùng luồng truy vấn.
        /// </summary>
        /// <returns>Danh sách chuyên khoa đã nạp đầy đủ, hoặc danh sách rỗng.</returns>
        public IEnumerable<ET_CHUYENKHOA> layToanBoCK()
        {
            return layToanBoChuyenKhoa();
        }

        /// <summary>
        /// Tên gọi tương thích của suaChuyenKhoa; cập nhật chuyên khoa qua khóa và kiểm tra metadata, không truyền bản gốc để so sửa đồng thời.
        /// </summary>
        /// <param name = "record">ET cần kiểm tra hoặc cập nhật, chứa các mã liên kết và trạng thái mới.</param>
        /// <returns>true khi cập nhật thành công; lỗi kiểm tra hoặc SQL được phát lại bằng ngoại lệ.</returns>
        public bool capNhatChuyenKhoa(ET_CHUYENKHOA record)
        {
            return suaChuyenKhoa(record);
        }
    }
}
