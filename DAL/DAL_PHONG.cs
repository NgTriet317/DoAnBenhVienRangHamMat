// AI hỗ trợ: bổ sung chú thích tiếng Việt;
using System.Data;
using Dapper;
using ET;

namespace DAL
{
    /// <summary>
    /// Tầng truy cập SQL cho phòng trong bảng PHONG. Lưu phòng thuộc khoa, loại phòng và tình trạng để phân lịch và tiếp nhận khám. Dapper ánh xạ dòng SQL sang ET; ThucHien dùng giao dịch được truyền vào hoặc tự quản lý giao dịch cục bộ.
    /// </summary>
    public class DAL_PHONG : dbconnect
    {
        /// <summary>
        /// Tạo DAL quản lý phòng, chuyển giao dịch tùy chọn cho dbconnect để nhiều thao tác có thể dùng chung kết nối và phạm vi lưu.
        /// </summary>
        /// <param name="transaction">Giao dịch của nơi gọi; null để hạ tầng tham gia giao dịch môi trường hoặc tự mở giao dịch cục bộ. Nếu truyền vào, nơi gọi quản lý commit/rollback.</param>
        public DAL_PHONG(IDbTransaction? transaction = null) : base(transaction) { }

        /// <summary>
        /// Đọc toàn bộ phòng từ dbo.PHONG, Dapper ánh xạ từng dòng sang ET, sắp theo mã; ToArray nạp hết dữ liệu trước khi hạ tầng kết thúc phạm vi kết nối/giao dịch.
        /// </summary>
        /// <returns>Danh sách ET đã vật hóa; không có bản ghi thì trả danh sách rỗng.</returns>
        public IEnumerable<ET_PHONG> layToanBoPhong()
        {
            return ThucHien((connection, transaction) =>
                connection.Query<ET_PHONG>("SELECT * FROM dbo.[PHONG] ORDER BY [MaPhong]", transaction: transaction).ToArray());
        }

        /// <summary>
        /// Tìm phòng theo khóa trong dbo.PHONG; mã được truyền bằng tham số SQL và QuerySingleOrDefault ánh xạ tối đa một bản ghi sang ET.
        /// </summary>
        /// <param name="ma">Mã khóa chính của bản ghi cần đọc, sửa hoặc xóa; truyền bằng tham số SQL.</param>
        /// <returns>ET của bản ghi tìm thấy, hoặc null nếu mã không tồn tại.</returns>
        public ET_PHONG? layPhongTheoMa(string ma)
        {
            return ThucHien((connection, transaction) =>
                connection.QuerySingleOrDefault<ET_PHONG>("SELECT * FROM dbo.[PHONG] WHERE [MaPhong]=@Ma", new { Ma = ma }, transaction));
        }

        /// <summary>
        /// Thêm phòng: cấp mã có khóa ngay vào duLieu, chuẩn hóa và kiểm tra ET theo metadata SQL rồi INSERT có tham số vào dbo.PHONG trong cùng giao dịch.
        /// </summary>
        /// <param name="duLieu">ET chứa dữ liệu cần lưu; thêm mới sẽ được gán mã, và kiểm tra metadata có thể chuẩn hóa lại các giá trị ngay trên đối tượng.</param>
        /// <returns>true sau khi INSERT thành công; lỗi dữ liệu/SQL được phát ra bằng ngoại lệ. Giao dịch ngoài chỉ commit khi nơi gọi quyết định.</returns>
        public bool themPhong(ET_PHONG duLieu)
        {
            return ThucHien((connection, transaction) =>
            {
                duLieu.MaPhong = TaoMa(connection, transaction, "PHONG", "MaPhong");
                KiemTra<ET_PHONG>(connection, transaction, "PHONG", duLieu, null, null);
                connection.Execute("INSERT INTO dbo.[PHONG] ([MaPhong],[TenPhong],[MaKhoa],[LoaiPhong],[TinhTrang],[TrangThai]) VALUES (@MaPhong,@TenPhong,@MaKhoa,@LoaiPhong,@TinhTrang,@TrangThai)", duLieu, transaction);
                return true;
            });
        }

        /// <summary>
        /// Sửa phòng: đọc và giữ khóa UPDLOCK/HOLDLOCK, báo lỗi nếu bản không còn, chuẩn hóa ET và đối chiếu banGoc để phát hiện sửa đồng thời. Khi chỉ đổi trạng thái, chặn thay đổi các trường ngoài TrangThai/NgayXacNhan/NgayCapNhat/LyDoThayDoi, sau đó UPDATE có tham số trong cùng giao dịch.
        /// </summary>
        /// <param name="duLieu">ET chứa dữ liệu cần lưu; thêm mới sẽ được gán mã, và kiểm tra metadata có thể chuẩn hóa lại các giá trị ngay trên đối tượng.</param>
        /// <param name="banGoc">Bản ET đã đọc trước khi thao tác; có giá trị để so với SQL và chặn ghi đè/xóa khi bản đã thay đổi, null để bỏ đối chiếu bản gốc.</param>
        /// <param name="chiDoiTrangThai">true để cấm thay đổi trường nội dung ngoài nhóm trường trạng thái cho phép; không tự đảm bảo mọi chuyển trạng thái đều hợp lệ.</param>
        /// <returns>true khi UPDATE chạy thành công; bản bị mất/thay đổi hoặc dữ liệu sai gây ngoại lệ. Không trả false để biểu thị lỗi.</returns>
        public bool suaPhong(ET_PHONG duLieu, ET_PHONG? banGoc = null, bool chiDoiTrangThai = false)
        {
            return ThucHien((connection, transaction) =>
            {
                var hienTai = connection.QuerySingleOrDefault<ET_PHONG>(
                    "SELECT * FROM dbo.[PHONG] WITH(UPDLOCK,HOLDLOCK) WHERE [MaPhong]=@Ma", new { Ma = duLieu.MaPhong }, transaction)
                    ?? throw new InvalidOperationException("Bản ghi không còn tồn tại. Vui lòng tải lại.");
                KiemTra(connection, transaction, "PHONG", duLieu, hienTai, banGoc);
                if (chiDoiTrangThai)
                    foreach (var pair in GiaTri(duLieu))
                        if (pair.Key is not "TrangThai" and not "NgayXacNhan" and not "NgayCapNhat" and not "LyDoThayDoi"
                            && !Equals(pair.Value, GiaTri(hienTai).GetValueOrDefault(pair.Key)))
                            throw new InvalidOperationException("Thao tác đổi trạng thái không được thay đổi nội dung bản ghi.");
                connection.Execute("UPDATE dbo.[PHONG] SET [TenPhong]=@TenPhong,[MaKhoa]=@MaKhoa,[LoaiPhong]=@LoaiPhong,[TinhTrang]=@TinhTrang,[TrangThai]=@TrangThai WHERE [MaPhong]=@MaPhong", duLieu, transaction);
                return true;
            });
        }

        /// <summary>
        /// Xóa phòng: đọc bản hiện tại với UPDLOCK/HOLDLOCK, báo lỗi nếu đã mất, so banGoc khi có rồi DELETE theo mã trong dbo.PHONG; toàn bộ thao tác giữ cùng giao dịch và lỗi ràng buộc SQL được phát ra.
        /// </summary>
        /// <param name="ma">Mã khóa chính của bản ghi cần đọc, sửa hoặc xóa; truyền bằng tham số SQL.</param>
        /// <param name="banGoc">Bản ET đã đọc trước khi thao tác; có giá trị để so với SQL và chặn ghi đè/xóa khi bản đã thay đổi, null để bỏ đối chiếu bản gốc.</param>
        /// <returns>true khi DELETE chạy thành công; không tồn tại, xung đột hoặc vi phạm SQL gây ngoại lệ.</returns>
        public bool xoaPhong(string ma, ET_PHONG? banGoc = null)
        {
            return ThucHien((connection, transaction) =>
            {
                var hienTai = connection.QuerySingleOrDefault<ET_PHONG>(
                    "SELECT * FROM dbo.[PHONG] WITH(UPDLOCK,HOLDLOCK) WHERE [MaPhong]=@Ma", new { Ma = ma }, transaction)
                    ?? throw new InvalidOperationException("Bản ghi không còn tồn tại. Vui lòng tải lại.");
                KiemTra(connection, transaction, "PHONG", hienTai, hienTai, banGoc);
                connection.Execute("DELETE FROM dbo.[PHONG] WHERE [MaPhong]=@Ma", new { Ma = ma }, transaction);
                return true;
            });
        }
    }
}
