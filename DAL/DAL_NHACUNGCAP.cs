// AI hỗ trợ: bổ sung chú thích tiếng Việt;
using System.Data;
using Dapper;
using ET;

namespace DAL
{
    /// <summary>
    /// Tầng truy cập SQL cho nhà cung cấp trong bảng NHA_CUNG_CAP. Lưu thông tin liên hệ và trạng thái đối tác để hóa đơn nhập, lô hàng và thiết bị tham chiếu. Dapper ánh xạ dòng SQL sang ET; ThucHien dùng giao dịch được truyền vào hoặc tự quản lý giao dịch cục bộ.
    /// </summary>
    public class DAL_NHACUNGCAP : dbconnect
    {
        /// <summary>
        /// Tạo DAL quản lý nhà cung cấp, chuyển giao dịch tùy chọn cho dbconnect để nhiều thao tác có thể dùng chung kết nối và phạm vi lưu.
        /// </summary>
        /// <param name="transaction">Giao dịch của nơi gọi; null để hạ tầng tham gia giao dịch môi trường hoặc tự mở giao dịch cục bộ. Nếu truyền vào, nơi gọi quản lý commit/rollback.</param>
        public DAL_NHACUNGCAP(IDbTransaction? transaction = null) : base(transaction) { }
        /// <summary>
        /// Đọc toàn bộ nhà cung cấp từ dbo.NHA_CUNG_CAP, Dapper ánh xạ từng dòng sang ET, sắp theo mã; ToArray nạp hết dữ liệu trước khi hạ tầng kết thúc phạm vi kết nối/giao dịch.
        /// </summary>
        /// <returns>Danh sách ET đã vật hóa; không có bản ghi thì trả danh sách rỗng.</returns>
        public IEnumerable<ET_NHACUNGCAP> layToanBoNhaCungCap() => ThucHien((connection, transaction) =>
            connection.Query<ET_NHACUNGCAP>("SELECT * FROM dbo.[NHA_CUNG_CAP] ORDER BY [MaNCC]", transaction: transaction).ToArray());
        /// <summary>
        /// Tìm nhà cung cấp theo khóa trong dbo.NHA_CUNG_CAP; mã được truyền bằng tham số SQL và QuerySingleOrDefault ánh xạ tối đa một bản ghi sang ET.
        /// </summary>
        /// <param name="ma">Mã khóa chính của bản ghi cần đọc, sửa hoặc xóa; truyền bằng tham số SQL.</param>
        /// <returns>ET của bản ghi tìm thấy, hoặc null nếu mã không tồn tại.</returns>
        public ET_NHACUNGCAP? layNhaCungCapTheoMa(string ma) => ThucHien((connection, transaction) =>
            connection.QuerySingleOrDefault<ET_NHACUNGCAP>("SELECT * FROM dbo.[NHA_CUNG_CAP] WHERE [MaNCC]=@Ma", new { Ma = ma }, transaction));
        /// <summary>
        /// Thêm nhà cung cấp: cấp mã có khóa ngay vào duLieu, chuẩn hóa và kiểm tra ET theo metadata SQL rồi INSERT có tham số vào dbo.NHA_CUNG_CAP trong cùng giao dịch.
        /// </summary>
        /// <param name="duLieu">ET chứa dữ liệu cần lưu; thêm mới sẽ được gán mã, và kiểm tra metadata có thể chuẩn hóa lại các giá trị ngay trên đối tượng.</param>
        /// <returns>true sau khi INSERT thành công; lỗi dữ liệu/SQL được phát ra bằng ngoại lệ. Giao dịch ngoài chỉ commit khi nơi gọi quyết định.</returns>
        public bool themNhaCungCap(ET_NHACUNGCAP duLieu) => ThucHien((connection, transaction) =>
        {
            duLieu.MaNCC = TaoMa(connection, transaction, "NHA_CUNG_CAP", "MaNCC");

            KiemTra<ET_NHACUNGCAP>(connection, transaction, "NHA_CUNG_CAP", duLieu, null, null);
            connection.Execute("INSERT INTO dbo.[NHA_CUNG_CAP] ([MaNCC],[TenNCC],[SDT],[Email],[DiaChi],[NguoiLienHe],[TrangThai]) VALUES (@MaNCC,@TenNCC,@SDT,@Email,@DiaChi,@NguoiLienHe,@TrangThai)", duLieu, transaction);
            return true;
        });
        /// <summary>
        /// Sửa nhà cung cấp: đọc và giữ khóa UPDLOCK/HOLDLOCK, báo lỗi nếu bản không còn, chuẩn hóa ET và đối chiếu banGoc để phát hiện sửa đồng thời. Khi chỉ đổi trạng thái, chặn thay đổi các trường ngoài TrangThai/NgayXacNhan/NgayCapNhat/LyDoThayDoi, sau đó UPDATE có tham số trong cùng giao dịch.
        /// </summary>
        /// <param name="duLieu">ET chứa dữ liệu cần lưu; thêm mới sẽ được gán mã, và kiểm tra metadata có thể chuẩn hóa lại các giá trị ngay trên đối tượng.</param>
        /// <param name="banGoc">Bản ET đã đọc trước khi thao tác; có giá trị để so với SQL và chặn ghi đè/xóa khi bản đã thay đổi, null để bỏ đối chiếu bản gốc.</param>
        /// <param name="chiDoiTrangThai">true để cấm thay đổi trường nội dung ngoài nhóm trường trạng thái cho phép; không tự đảm bảo mọi chuyển trạng thái đều hợp lệ.</param>
        /// <returns>true khi UPDATE chạy thành công; bản bị mất/thay đổi hoặc dữ liệu sai gây ngoại lệ. Không trả false để biểu thị lỗi.</returns>
        public bool suaNhaCungCap(ET_NHACUNGCAP duLieu, ET_NHACUNGCAP? banGoc = null, bool chiDoiTrangThai = false) => ThucHien((connection, transaction) =>
        {
            var hienTai = connection.QuerySingleOrDefault<ET_NHACUNGCAP>("SELECT * FROM dbo.[NHA_CUNG_CAP] WITH(UPDLOCK,HOLDLOCK) WHERE [MaNCC]=@Ma", new { Ma = duLieu.MaNCC }, transaction)
                ?? throw new InvalidOperationException("Bản ghi không còn tồn tại. Vui lòng tải lại.");

            KiemTra(connection, transaction, "NHA_CUNG_CAP", duLieu, hienTai, banGoc);
                if (chiDoiTrangThai)
                    foreach (var pair in GiaTri(duLieu))
                        if (pair.Key is not "TrangThai" and not "NgayXacNhan" and not "NgayCapNhat" and not "LyDoThayDoi"
                            && !Equals(pair.Value, GiaTri(hienTai).GetValueOrDefault(pair.Key)))
                            throw new InvalidOperationException("Thao tác đổi trạng thái không được thay đổi nội dung bản ghi.");
            connection.Execute("UPDATE dbo.[NHA_CUNG_CAP] SET [TenNCC]=@TenNCC,[SDT]=@SDT,[Email]=@Email,[DiaChi]=@DiaChi,[NguoiLienHe]=@NguoiLienHe,[TrangThai]=@TrangThai WHERE [MaNCC]=@MaNCC", duLieu, transaction);
            return true;
        });
        /// <summary>
        /// Xóa nhà cung cấp: đọc bản hiện tại với UPDLOCK/HOLDLOCK, báo lỗi nếu đã mất, so banGoc khi có rồi DELETE theo mã trong dbo.NHA_CUNG_CAP; toàn bộ thao tác giữ cùng giao dịch và lỗi ràng buộc SQL được phát ra.
        /// </summary>
        /// <param name="ma">Mã khóa chính của bản ghi cần đọc, sửa hoặc xóa; truyền bằng tham số SQL.</param>
        /// <param name="banGoc">Bản ET đã đọc trước khi thao tác; có giá trị để so với SQL và chặn ghi đè/xóa khi bản đã thay đổi, null để bỏ đối chiếu bản gốc.</param>
        /// <returns>true khi DELETE chạy thành công; không tồn tại, xung đột hoặc vi phạm SQL gây ngoại lệ.</returns>
        public bool xoaNhaCungCap(string ma, ET_NHACUNGCAP? banGoc = null) => ThucHien((connection, transaction) =>
        {
            var hienTai = connection.QuerySingleOrDefault<ET_NHACUNGCAP>("SELECT * FROM dbo.[NHA_CUNG_CAP] WITH(UPDLOCK,HOLDLOCK) WHERE [MaNCC]=@Ma", new { Ma = ma }, transaction)
                ?? throw new InvalidOperationException("Bản ghi không còn tồn tại. Vui lòng tải lại.");

            KiemTra(connection, transaction, "NHA_CUNG_CAP", hienTai, hienTai, banGoc);
            connection.Execute("DELETE FROM dbo.[NHA_CUNG_CAP] WHERE [MaNCC]=@Ma", new { Ma = ma }, transaction);
            return true;
        });


    }
}