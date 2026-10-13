// AI hỗ trợ: bổ sung chú thích tiếng Việt;
using System.Data;
using Dapper;
using ET;

namespace DAL
{
    /// <summary>
    /// Tầng truy cập SQL cho vai trò trong bảng VAI_TRO. Lưu tên và mô tả vai trò để tài khoản tham chiếu phạm vi chức năng. Dapper ánh xạ dòng SQL sang ET; ThucHien dùng giao dịch được truyền vào hoặc tự quản lý giao dịch cục bộ.
    /// </summary>
    public class DAL_MAVAITRO : dbconnect
    {
        /// <summary>
        /// Tạo DAL quản lý vai trò, chuyển giao dịch tùy chọn cho dbconnect để nhiều thao tác có thể dùng chung kết nối và phạm vi lưu.
        /// </summary>
        /// <param name="transaction">Giao dịch của nơi gọi; null để hạ tầng tham gia giao dịch môi trường hoặc tự mở giao dịch cục bộ. Nếu truyền vào, nơi gọi quản lý commit/rollback.</param>
        public DAL_MAVAITRO(IDbTransaction? transaction = null) : base(transaction) { }
        /// <summary>
        /// Đọc toàn bộ vai trò từ dbo.VAI_TRO, Dapper ánh xạ từng dòng sang ET, sắp theo mã; ToArray nạp hết dữ liệu trước khi hạ tầng kết thúc phạm vi kết nối/giao dịch.
        /// </summary>
        /// <returns>Danh sách ET đã vật hóa; không có bản ghi thì trả danh sách rỗng.</returns>
        public IEnumerable<ET_MAVAITRO> layToanBoMaVaiTro() => ThucHien((connection, transaction) =>
            connection.Query<ET_MAVAITRO>("SELECT * FROM dbo.[VAI_TRO] ORDER BY [MaVaiTro]", transaction: transaction).ToArray());
        /// <summary>
        /// Tìm vai trò theo khóa trong dbo.VAI_TRO; mã được truyền bằng tham số SQL và QuerySingleOrDefault ánh xạ tối đa một bản ghi sang ET.
        /// </summary>
        /// <param name="ma">Mã khóa chính của bản ghi cần đọc, sửa hoặc xóa; truyền bằng tham số SQL.</param>
        /// <returns>ET của bản ghi tìm thấy, hoặc null nếu mã không tồn tại.</returns>
        public ET_MAVAITRO? layMaVaiTroTheoMa(string ma) => ThucHien((connection, transaction) =>
            connection.QuerySingleOrDefault<ET_MAVAITRO>("SELECT * FROM dbo.[VAI_TRO] WHERE [MaVaiTro]=@Ma", new { Ma = ma }, transaction));
        /// <summary>
        /// Thêm vai trò: cấp mã có khóa ngay vào duLieu, chuẩn hóa và kiểm tra ET theo metadata SQL rồi INSERT có tham số vào dbo.VAI_TRO trong cùng giao dịch.
        /// </summary>
        /// <param name="duLieu">ET chứa dữ liệu cần lưu; thêm mới sẽ được gán mã, và kiểm tra metadata có thể chuẩn hóa lại các giá trị ngay trên đối tượng.</param>
        /// <returns>true sau khi INSERT thành công; lỗi dữ liệu/SQL được phát ra bằng ngoại lệ. Giao dịch ngoài chỉ commit khi nơi gọi quyết định.</returns>
        public bool themMaVaiTro(ET_MAVAITRO duLieu) => ThucHien((connection, transaction) =>
        {
            duLieu.MaVaiTro = TaoMa(connection, transaction, "VAI_TRO", "MaVaiTro");

            KiemTra<ET_MAVAITRO>(connection, transaction, "VAI_TRO", duLieu, null, null);
            connection.Execute("INSERT INTO dbo.[VAI_TRO] ([MaVaiTro],[TenVaiTro],[MoTa]) VALUES (@MaVaiTro,@TenVaiTro,@MoTa)", duLieu, transaction);
            return true;
        });
        /// <summary>
        /// Sửa vai trò: đọc và giữ khóa UPDLOCK/HOLDLOCK, báo lỗi nếu bản không còn, chuẩn hóa ET và đối chiếu banGoc để phát hiện sửa đồng thời. Khi chỉ đổi trạng thái, chặn thay đổi các trường ngoài TrangThai/NgayXacNhan/NgayCapNhat/LyDoThayDoi, sau đó UPDATE có tham số trong cùng giao dịch.
        /// </summary>
        /// <param name="duLieu">ET chứa dữ liệu cần lưu; thêm mới sẽ được gán mã, và kiểm tra metadata có thể chuẩn hóa lại các giá trị ngay trên đối tượng.</param>
        /// <param name="banGoc">Bản ET đã đọc trước khi thao tác; có giá trị để so với SQL và chặn ghi đè/xóa khi bản đã thay đổi, null để bỏ đối chiếu bản gốc.</param>
        /// <param name="chiDoiTrangThai">true để cấm thay đổi trường nội dung ngoài nhóm trường trạng thái cho phép; không tự đảm bảo mọi chuyển trạng thái đều hợp lệ.</param>
        /// <returns>true khi UPDATE chạy thành công; bản bị mất/thay đổi hoặc dữ liệu sai gây ngoại lệ. Không trả false để biểu thị lỗi.</returns>
        public bool suaMaVaiTro(ET_MAVAITRO duLieu, ET_MAVAITRO? banGoc = null, bool chiDoiTrangThai = false) => ThucHien((connection, transaction) =>
        {
            var hienTai = connection.QuerySingleOrDefault<ET_MAVAITRO>("SELECT * FROM dbo.[VAI_TRO] WITH(UPDLOCK,HOLDLOCK) WHERE [MaVaiTro]=@Ma", new { Ma = duLieu.MaVaiTro }, transaction)
                ?? throw new InvalidOperationException("Bản ghi không còn tồn tại. Vui lòng tải lại.");

            KiemTra(connection, transaction, "VAI_TRO", duLieu, hienTai, banGoc);
                if (chiDoiTrangThai)
                    foreach (var pair in GiaTri(duLieu))
                        if (pair.Key is not "TrangThai" and not "NgayXacNhan" and not "NgayCapNhat" and not "LyDoThayDoi"
                            && !Equals(pair.Value, GiaTri(hienTai).GetValueOrDefault(pair.Key)))
                            throw new InvalidOperationException("Thao tác đổi trạng thái không được thay đổi nội dung bản ghi.");
            connection.Execute("UPDATE dbo.[VAI_TRO] SET [TenVaiTro]=@TenVaiTro,[MoTa]=@MoTa WHERE [MaVaiTro]=@MaVaiTro", duLieu, transaction);
            return true;
        });
        /// <summary>
        /// Xóa vai trò: đọc bản hiện tại với UPDLOCK/HOLDLOCK, báo lỗi nếu đã mất, so banGoc khi có rồi DELETE theo mã trong dbo.VAI_TRO; toàn bộ thao tác giữ cùng giao dịch và lỗi ràng buộc SQL được phát ra.
        /// </summary>
        /// <param name="ma">Mã khóa chính của bản ghi cần đọc, sửa hoặc xóa; truyền bằng tham số SQL.</param>
        /// <param name="banGoc">Bản ET đã đọc trước khi thao tác; có giá trị để so với SQL và chặn ghi đè/xóa khi bản đã thay đổi, null để bỏ đối chiếu bản gốc.</param>
        /// <returns>true khi DELETE chạy thành công; không tồn tại, xung đột hoặc vi phạm SQL gây ngoại lệ.</returns>
        public bool xoaMaVaiTro(string ma, ET_MAVAITRO? banGoc = null) => ThucHien((connection, transaction) =>
        {
            var hienTai = connection.QuerySingleOrDefault<ET_MAVAITRO>("SELECT * FROM dbo.[VAI_TRO] WITH(UPDLOCK,HOLDLOCK) WHERE [MaVaiTro]=@Ma", new { Ma = ma }, transaction)
                ?? throw new InvalidOperationException("Bản ghi không còn tồn tại. Vui lòng tải lại.");

            KiemTra(connection, transaction, "VAI_TRO", hienTai, hienTai, banGoc);
            connection.Execute("DELETE FROM dbo.[VAI_TRO] WHERE [MaVaiTro]=@Ma", new { Ma = ma }, transaction);
            return true;
        });


    }
}