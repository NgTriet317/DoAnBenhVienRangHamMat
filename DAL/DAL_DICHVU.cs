// AI hỗ trợ: bổ sung chú thích tiếng Việt;
using System.Data;
using Dapper;
using ET;

namespace DAL
{
    /// <summary>
    /// Tầng truy cập SQL cho dịch vụ trong bảng DICH_VU. Lưu danh mục, chuyên khoa, giá, thời gian dự kiến và ngày áp dụng của dịch vụ khám chữa bệnh. Dapper ánh xạ dòng SQL sang ET; ThucHien dùng giao dịch được truyền vào hoặc tự quản lý giao dịch cục bộ.
    /// </summary>
    public class DAL_DICHVU : dbconnect
    {
        /// <summary>
        /// Tạo DAL quản lý dịch vụ, chuyển giao dịch tùy chọn cho dbconnect để nhiều thao tác có thể dùng chung kết nối và phạm vi lưu.
        /// </summary>
        /// <param name="transaction">Giao dịch của nơi gọi; null để hạ tầng tham gia giao dịch môi trường hoặc tự mở giao dịch cục bộ. Nếu truyền vào, nơi gọi quản lý commit/rollback.</param>
        public DAL_DICHVU(IDbTransaction? transaction = null) : base(transaction) { }

        /// <summary>
        /// Đọc toàn bộ dịch vụ từ dbo.DICH_VU, Dapper ánh xạ từng dòng sang ET, sắp theo mã; ToArray nạp hết dữ liệu trước khi hạ tầng kết thúc phạm vi kết nối/giao dịch.
        /// </summary>
        /// <returns>Danh sách ET đã vật hóa; không có bản ghi thì trả danh sách rỗng.</returns>
        public IEnumerable<ET_DICHVU> layToanBoDichVu()
        {
            return ThucHien((connection, transaction) =>
                connection.Query<ET_DICHVU>("SELECT * FROM dbo.[DICH_VU] ORDER BY [MaDV]", transaction: transaction).ToArray());
        }

        /// <summary>
        /// Tìm dịch vụ theo khóa trong dbo.DICH_VU; mã được truyền bằng tham số SQL và QuerySingleOrDefault ánh xạ tối đa một bản ghi sang ET.
        /// </summary>
        /// <param name="ma">Mã khóa chính của bản ghi cần đọc, sửa hoặc xóa; truyền bằng tham số SQL.</param>
        /// <returns>ET của bản ghi tìm thấy, hoặc null nếu mã không tồn tại.</returns>
        public ET_DICHVU? layDichVuTheoMa(string ma)
        {
            return ThucHien((connection, transaction) =>
                connection.QuerySingleOrDefault<ET_DICHVU>("SELECT * FROM dbo.[DICH_VU] WHERE [MaDV]=@Ma", new { Ma = ma }, transaction));
        }

        /// <summary>
        /// Thêm dịch vụ: cấp mã có khóa ngay vào duLieu, chuẩn hóa và kiểm tra ET theo metadata SQL rồi INSERT có tham số vào dbo.DICH_VU trong cùng giao dịch.
        /// </summary>
        /// <param name="duLieu">ET chứa dữ liệu cần lưu; thêm mới sẽ được gán mã, và kiểm tra metadata có thể chuẩn hóa lại các giá trị ngay trên đối tượng.</param>
        /// <returns>true sau khi INSERT thành công; lỗi dữ liệu/SQL được phát ra bằng ngoại lệ. Giao dịch ngoài chỉ commit khi nơi gọi quyết định.</returns>
        public bool themDichVu(ET_DICHVU duLieu)
        {
            return ThucHien((connection, transaction) =>
            {
                duLieu.MaDV = TaoMa(connection, transaction, "DICH_VU", "MaDV");
                KiemTra<ET_DICHVU>(connection, transaction, "DICH_VU", duLieu, null, null);
                connection.Execute("INSERT INTO dbo.[DICH_VU] ([MaDV],[TenDV],[NhomDV],[MaCK],[DonGia],[ThoiGianDuKien],[MoTa],[NgayApDung],[TrangThai]) VALUES (@MaDV,@TenDV,@NhomDV,@MaCK,@DonGia,@ThoiGianDuKien,@MoTa,@NgayApDung,@TrangThai)", duLieu, transaction);
                return true;
            });
        }

        /// <summary>
        /// Sửa dịch vụ: đọc và giữ khóa UPDLOCK/HOLDLOCK, báo lỗi nếu bản không còn, chuẩn hóa ET và đối chiếu banGoc để phát hiện sửa đồng thời. Khi chỉ đổi trạng thái, chặn thay đổi các trường ngoài TrangThai/NgayXacNhan/NgayCapNhat/LyDoThayDoi, sau đó UPDATE có tham số trong cùng giao dịch.
        /// </summary>
        /// <param name="duLieu">ET chứa dữ liệu cần lưu; thêm mới sẽ được gán mã, và kiểm tra metadata có thể chuẩn hóa lại các giá trị ngay trên đối tượng.</param>
        /// <param name="banGoc">Bản ET đã đọc trước khi thao tác; có giá trị để so với SQL và chặn ghi đè/xóa khi bản đã thay đổi, null để bỏ đối chiếu bản gốc.</param>
        /// <param name="chiDoiTrangThai">true để cấm thay đổi trường nội dung ngoài nhóm trường trạng thái cho phép; không tự đảm bảo mọi chuyển trạng thái đều hợp lệ.</param>
        /// <returns>true khi UPDATE chạy thành công; bản bị mất/thay đổi hoặc dữ liệu sai gây ngoại lệ. Không trả false để biểu thị lỗi.</returns>
        public bool suaDichVu(ET_DICHVU duLieu, ET_DICHVU? banGoc = null, bool chiDoiTrangThai = false)
        {
            return ThucHien((connection, transaction) =>
            {
                var hienTai = connection.QuerySingleOrDefault<ET_DICHVU>(
                    "SELECT * FROM dbo.[DICH_VU] WITH(UPDLOCK,HOLDLOCK) WHERE [MaDV]=@Ma", new { Ma = duLieu.MaDV }, transaction)
                    ?? throw new InvalidOperationException("Bản ghi không còn tồn tại. Vui lòng tải lại.");
                KiemTra(connection, transaction, "DICH_VU", duLieu, hienTai, banGoc);
                if (chiDoiTrangThai)
                    foreach (var pair in GiaTri(duLieu))
                        if (pair.Key is not "TrangThai" and not "NgayXacNhan" and not "NgayCapNhat" and not "LyDoThayDoi"
                            && !Equals(pair.Value, GiaTri(hienTai).GetValueOrDefault(pair.Key)))
                            throw new InvalidOperationException("Thao tác đổi trạng thái không được thay đổi nội dung bản ghi.");
                connection.Execute("UPDATE dbo.[DICH_VU] SET [TenDV]=@TenDV,[NhomDV]=@NhomDV,[MaCK]=@MaCK,[DonGia]=@DonGia,[ThoiGianDuKien]=@ThoiGianDuKien,[MoTa]=@MoTa,[NgayApDung]=@NgayApDung,[TrangThai]=@TrangThai WHERE [MaDV]=@MaDV", duLieu, transaction);
                return true;
            });
        }

        /// <summary>
        /// Xóa dịch vụ: đọc bản hiện tại với UPDLOCK/HOLDLOCK, báo lỗi nếu đã mất, so banGoc khi có rồi DELETE theo mã trong dbo.DICH_VU; toàn bộ thao tác giữ cùng giao dịch và lỗi ràng buộc SQL được phát ra.
        /// </summary>
        /// <param name="ma">Mã khóa chính của bản ghi cần đọc, sửa hoặc xóa; truyền bằng tham số SQL.</param>
        /// <param name="banGoc">Bản ET đã đọc trước khi thao tác; có giá trị để so với SQL và chặn ghi đè/xóa khi bản đã thay đổi, null để bỏ đối chiếu bản gốc.</param>
        /// <returns>true khi DELETE chạy thành công; không tồn tại, xung đột hoặc vi phạm SQL gây ngoại lệ.</returns>
        public bool xoaDichVu(string ma, ET_DICHVU? banGoc = null)
        {
            return ThucHien((connection, transaction) =>
            {
                var hienTai = connection.QuerySingleOrDefault<ET_DICHVU>(
                    "SELECT * FROM dbo.[DICH_VU] WITH(UPDLOCK,HOLDLOCK) WHERE [MaDV]=@Ma", new { Ma = ma }, transaction)
                    ?? throw new InvalidOperationException("Bản ghi không còn tồn tại. Vui lòng tải lại.");
                KiemTra(connection, transaction, "DICH_VU", hienTai, hienTai, banGoc);
                connection.Execute("DELETE FROM dbo.[DICH_VU] WHERE [MaDV]=@Ma", new { Ma = ma }, transaction);
                return true;
            });
        }
    }
}
