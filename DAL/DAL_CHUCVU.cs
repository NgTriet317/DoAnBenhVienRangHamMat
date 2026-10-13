// AI hỗ trợ: bổ sung chú thích tiếng Việt;
using System.Data;
using Dapper;
using ET;

namespace DAL
{
    /// <summary>
    /// Tầng truy cập SQL cho chức vụ trong bảng CHUC_VU. Quản lý tên và mô tả chức vụ để hồ sơ nhân viên tham chiếu. Dapper ánh xạ dòng SQL sang ET; ThucHien dùng giao dịch được truyền vào hoặc tự quản lý giao dịch cục bộ.
    /// </summary>
    public class DAL_CHUCVU : dbconnect
    {
        /// <summary>
        /// Tạo DAL quản lý chức vụ, chuyển giao dịch tùy chọn cho dbconnect để nhiều thao tác có thể dùng chung kết nối và phạm vi lưu.
        /// </summary>
        /// <param name = "transaction">Giao dịch của nơi gọi; null để hạ tầng tham gia giao dịch môi trường hoặc tự mở giao dịch cục bộ. Nếu truyền vào, nơi gọi quản lý commit/rollback.</param>
        public DAL_CHUCVU(IDbTransaction? transaction = null) : base(transaction)
        {
        }

        /// <summary>
        /// Đọc toàn bộ chức vụ từ dbo.CHUC_VU, Dapper ánh xạ từng dòng sang ET, sắp theo mã; ToArray nạp hết dữ liệu trước khi hạ tầng kết thúc phạm vi kết nối/giao dịch.
        /// </summary>
        /// <returns>Danh sách ET đã vật hóa; không có bản ghi thì trả danh sách rỗng.</returns>
        public IEnumerable<ET_CHUCVU> layToanBoChucVu()
        {
            return ThucHien((connection, transaction) =>
            {
                return connection.Query<ET_CHUCVU>("SELECT * FROM dbo.[CHUC_VU] ORDER BY [MaChucVu]", transaction: transaction).ToArray();
            });
        }

        /// <summary>
        /// Tìm chức vụ theo khóa trong dbo.CHUC_VU; mã được truyền bằng tham số SQL và QuerySingleOrDefault ánh xạ tối đa một bản ghi sang ET.
        /// </summary>
        /// <param name = "ma">Mã khóa chính của bản ghi cần đọc, sửa hoặc xóa; truyền bằng tham số SQL.</param>
        /// <returns>ET của bản ghi tìm thấy, hoặc null nếu mã không tồn tại.</returns>
        public ET_CHUCVU? layChucVuTheoMa(string ma)
        {
            return ThucHien((connection, transaction) =>
            {
                return connection.QuerySingleOrDefault<ET_CHUCVU>("SELECT * FROM dbo.[CHUC_VU] WHERE [MaChucVu]=@Ma", new { Ma = ma }, transaction);
            });
        }

        /// <summary>
        /// Thêm chức vụ: cấp mã có khóa ngay vào duLieu, chuẩn hóa và kiểm tra ET theo metadata SQL rồi INSERT có tham số vào dbo.CHUC_VU trong cùng giao dịch.
        /// </summary>
        /// <param name = "duLieu">ET chứa dữ liệu cần lưu; thêm mới sẽ được gán mã, và kiểm tra metadata có thể chuẩn hóa lại các giá trị ngay trên đối tượng.</param>
        /// <returns>true sau khi INSERT thành công; lỗi dữ liệu/SQL được phát ra bằng ngoại lệ. Giao dịch ngoài chỉ commit khi nơi gọi quyết định.</returns>
        public bool themChucVu(ET_CHUCVU duLieu)
        {
            return ThucHien((connection, transaction) =>
            {
                duLieu.MaChucVu = TaoMa(connection, transaction, "CHUC_VU", "MaChucVu");
                KiemTra<ET_CHUCVU>(connection, transaction, "CHUC_VU", duLieu, null, null);
                connection.Execute("INSERT INTO dbo.[CHUC_VU] ([MaChucVu],[TenChucVu],[MoTa]) VALUES (@MaChucVu,@TenChucVu,@MoTa)", duLieu, transaction);
                return true;
            });
        }

        /// <summary>
        /// Sửa chức vụ: đọc và giữ khóa UPDLOCK/HOLDLOCK, báo lỗi nếu bản không còn, chuẩn hóa ET và đối chiếu banGoc để phát hiện sửa đồng thời. Khi chỉ đổi trạng thái, chặn thay đổi các trường ngoài TrangThai/NgayXacNhan/NgayCapNhat/LyDoThayDoi, sau đó UPDATE có tham số trong cùng giao dịch.
        /// </summary>
        /// <param name = "duLieu">ET chứa dữ liệu cần lưu; thêm mới sẽ được gán mã, và kiểm tra metadata có thể chuẩn hóa lại các giá trị ngay trên đối tượng.</param>
        /// <param name = "banGoc">Bản ET đã đọc trước khi thao tác; có giá trị để so với SQL và chặn ghi đè/xóa khi bản đã thay đổi, null để bỏ đối chiếu bản gốc.</param>
        /// <param name = "chiDoiTrangThai">true để cấm thay đổi trường nội dung ngoài nhóm trường trạng thái cho phép; không tự đảm bảo mọi chuyển trạng thái đều hợp lệ.</param>
        /// <returns>true khi UPDATE chạy thành công; bản bị mất/thay đổi hoặc dữ liệu sai gây ngoại lệ. Không trả false để biểu thị lỗi.</returns>
        public bool suaChucVu(ET_CHUCVU duLieu, ET_CHUCVU? banGoc = null, bool chiDoiTrangThai = false)
        {
            return ThucHien((connection, transaction) =>
            {
                var hienTai = connection.QuerySingleOrDefault<ET_CHUCVU>("SELECT * FROM dbo.[CHUC_VU] WITH(UPDLOCK,HOLDLOCK) WHERE [MaChucVu]=@Ma", new { Ma = duLieu.MaChucVu }, transaction);
                if (hienTai is null)
                {
                    throw new InvalidOperationException("Bản ghi không còn tồn tại. Vui lòng tải lại.");
                }

                KiemTra(connection, transaction, "CHUC_VU", duLieu, hienTai, banGoc);
                if (chiDoiTrangThai)
                {
                    KiemTraChiDoiTrangThai(duLieu, hienTai);
                }

                connection.Execute("UPDATE dbo.[CHUC_VU] SET [TenChucVu]=@TenChucVu,[MoTa]=@MoTa WHERE [MaChucVu]=@MaChucVu", duLieu, transaction);
                return true;
            });
        }

        /// <summary>
        /// Xóa chức vụ: đọc bản hiện tại với UPDLOCK/HOLDLOCK, báo lỗi nếu đã mất, so banGoc khi có rồi DELETE theo mã trong dbo.CHUC_VU; toàn bộ thao tác giữ cùng giao dịch và lỗi ràng buộc SQL được phát ra.
        /// </summary>
        /// <param name = "ma">Mã khóa chính của bản ghi cần đọc, sửa hoặc xóa; truyền bằng tham số SQL.</param>
        /// <param name = "banGoc">Bản ET đã đọc trước khi thao tác; có giá trị để so với SQL và chặn ghi đè/xóa khi bản đã thay đổi, null để bỏ đối chiếu bản gốc.</param>
        /// <returns>true khi DELETE chạy thành công; không tồn tại, xung đột hoặc vi phạm SQL gây ngoại lệ.</returns>
        public bool xoaChucVu(string ma, ET_CHUCVU? banGoc = null)
        {
            return ThucHien((connection, transaction) =>
            {
                var hienTai = connection.QuerySingleOrDefault<ET_CHUCVU>("SELECT * FROM dbo.[CHUC_VU] WITH(UPDLOCK,HOLDLOCK) WHERE [MaChucVu]=@Ma", new { Ma = ma }, transaction);
                if (hienTai is null)
                {
                    throw new InvalidOperationException("Bản ghi không còn tồn tại. Vui lòng tải lại.");
                }

                KiemTra(connection, transaction, "CHUC_VU", hienTai, hienTai, banGoc);
                connection.Execute("DELETE FROM dbo.[CHUC_VU] WHERE [MaChucVu]=@Ma", new { Ma = ma }, transaction);
                return true;
            });
        }

        /// <summary>
        /// Tên gọi tương thích của layToanBoChucVu; trả toàn bộ chức vụ theo mã bằng cùng luồng truy vấn.
        /// </summary>
        /// <returns>Danh sách chức vụ đã nạp đầy đủ, hoặc danh sách rỗng.</returns>
        public IEnumerable<ET_CHUCVU> layToanBoCV()
        {
            return layToanBoChucVu();
        }

        /// <summary>
        /// Tên gọi tương thích của suaChucVu; cập nhật chức vụ qua khóa và kiểm tra metadata, không truyền bản gốc để so sửa đồng thời.
        /// </summary>
        /// <param name = "record">ET cần kiểm tra hoặc cập nhật, chứa các mã liên kết và trạng thái mới.</param>
        /// <returns>true khi cập nhật thành công; lỗi kiểm tra hoặc SQL được phát lại bằng ngoại lệ.</returns>
        public bool capNhatChucVu(ET_CHUCVU record)
        {
            return suaChucVu(record);
        }
    }
}
