// AI hỗ trợ: bổ sung chú thích tiếng Việt;
using System.Data;
using Dapper;
using ET;

namespace DAL
{
    /// <summary>
    /// Tầng truy cập SQL cho đợt bảo trì thiết bị trong bảng BAO_TRI_THIET_BI. Theo dõi thiết bị, nhân viên phụ trách, khoảng thời gian, nội dung, chi phí và lịch bảo trì tiếp theo. Dapper ánh xạ dòng SQL sang ET; ThucHien dùng giao dịch được truyền vào hoặc tự quản lý giao dịch cục bộ.
    /// </summary>
    public class DAL_BAOTRITHIETBI : dbconnect
    {
        /// <summary>
        /// Tạo DAL quản lý đợt bảo trì thiết bị, chuyển giao dịch tùy chọn cho dbconnect để nhiều thao tác có thể dùng chung kết nối và phạm vi lưu.
        /// </summary>
        /// <param name = "transaction">Giao dịch của nơi gọi; null để hạ tầng tham gia giao dịch môi trường hoặc tự mở giao dịch cục bộ. Nếu truyền vào, nơi gọi quản lý commit/rollback.</param>
        public DAL_BAOTRITHIETBI(IDbTransaction? transaction = null) : base(transaction)
        {
        }

        /// <summary>
        /// Đọc toàn bộ đợt bảo trì thiết bị từ dbo.BAO_TRI_THIET_BI, Dapper ánh xạ từng dòng sang ET, sắp theo mã; ToArray nạp hết dữ liệu trước khi hạ tầng kết thúc phạm vi kết nối/giao dịch.
        /// </summary>
        /// <returns>Danh sách ET đã vật hóa; không có bản ghi thì trả danh sách rỗng.</returns>
        public IEnumerable<ET_BAOTRITHIETBI> layToanBoBaoTriThietBi()
        {
            return ThucHien((connection, transaction) =>
            {
                return connection.Query<ET_BAOTRITHIETBI>("SELECT * FROM dbo.[BAO_TRI_THIET_BI] ORDER BY [MaBT]", transaction: transaction).ToArray();
            });
        }

        /// <summary>
        /// Tìm đợt bảo trì thiết bị theo khóa trong dbo.BAO_TRI_THIET_BI; mã được truyền bằng tham số SQL và QuerySingleOrDefault ánh xạ tối đa một bản ghi sang ET.
        /// </summary>
        /// <param name = "ma">Mã khóa chính của bản ghi cần đọc, sửa hoặc xóa; truyền bằng tham số SQL.</param>
        /// <returns>ET của bản ghi tìm thấy, hoặc null nếu mã không tồn tại.</returns>
        public ET_BAOTRITHIETBI? layBaoTriThietBiTheoMa(string ma)
        {
            return ThucHien((connection, transaction) =>
            {
                return connection.QuerySingleOrDefault<ET_BAOTRITHIETBI>("SELECT * FROM dbo.[BAO_TRI_THIET_BI] WHERE [MaBT]=@Ma", new { Ma = ma }, transaction);
            });
        }

        /// <summary>
        /// Thêm đợt bảo trì thiết bị: cấp mã có khóa ngay vào duLieu, chuẩn hóa và kiểm tra ET theo metadata SQL rồi INSERT có tham số vào dbo.BAO_TRI_THIET_BI trong cùng giao dịch.
        /// </summary>
        /// <param name = "duLieu">ET chứa dữ liệu cần lưu; thêm mới sẽ được gán mã, và kiểm tra metadata có thể chuẩn hóa lại các giá trị ngay trên đối tượng.</param>
        /// <returns>true sau khi INSERT thành công; lỗi dữ liệu/SQL được phát ra bằng ngoại lệ. Giao dịch ngoài chỉ commit khi nơi gọi quyết định.</returns>
        public bool themBaoTriThietBi(ET_BAOTRITHIETBI duLieu)
        {
            return ThucHien((connection, transaction) =>
            {
                duLieu.MaBT = TaoMa(connection, transaction, "BAO_TRI_THIET_BI", "MaBT");
                KiemTra<ET_BAOTRITHIETBI>(connection, transaction, "BAO_TRI_THIET_BI", duLieu, null, null);
                KiemTraNghiepVu(duLieu);
                connection.Execute("""
                INSERT INTO dbo.[BAO_TRI_THIET_BI] ([MaBT],[MaTB],[MaNV],
                    [NgayBatDau],[NgayKetThuc],[NoiDung],
                    [ChiPhi],[NgayBaoTriTiepTheo],[TrangThai],
                    [GhiChu])
                VALUES (@MaBT,@MaTB,@MaNV,
                    @NgayBatDau,@NgayKetThuc,@NoiDung,
                    @ChiPhi,@NgayBaoTriTiepTheo,@TrangThai,
                    @GhiChu)
                """, duLieu, transaction);
                return true;
            });
        }

        /// <summary>
        /// Sửa đợt bảo trì thiết bị: đọc và giữ khóa UPDLOCK/HOLDLOCK, báo lỗi nếu bản không còn, chuẩn hóa ET và đối chiếu banGoc để phát hiện sửa đồng thời. Khi chỉ đổi trạng thái, chặn thay đổi các trường ngoài TrangThai/NgayXacNhan/NgayCapNhat/LyDoThayDoi, sau đó UPDATE có tham số trong cùng giao dịch.
        /// </summary>
        /// <param name = "duLieu">ET chứa dữ liệu cần lưu; thêm mới sẽ được gán mã, và kiểm tra metadata có thể chuẩn hóa lại các giá trị ngay trên đối tượng.</param>
        /// <param name = "banGoc">Bản ET đã đọc trước khi thao tác; có giá trị để so với SQL và chặn ghi đè/xóa khi bản đã thay đổi, null để bỏ đối chiếu bản gốc.</param>
        /// <param name = "chiDoiTrangThai">true để cấm thay đổi trường nội dung ngoài nhóm trường trạng thái cho phép; không tự đảm bảo mọi chuyển trạng thái đều hợp lệ.</param>
        /// <returns>true khi UPDATE chạy thành công; bản bị mất/thay đổi hoặc dữ liệu sai gây ngoại lệ. Không trả false để biểu thị lỗi.</returns>
        public bool suaBaoTriThietBi(ET_BAOTRITHIETBI duLieu, ET_BAOTRITHIETBI? banGoc = null, bool chiDoiTrangThai = false)
        {
            return ThucHien((connection, transaction) =>
            {
                var hienTai = connection.QuerySingleOrDefault<ET_BAOTRITHIETBI>("SELECT * FROM dbo.[BAO_TRI_THIET_BI] WITH(UPDLOCK,HOLDLOCK) WHERE [MaBT]=@Ma", new { Ma = duLieu.MaBT }, transaction);
                if (hienTai is null)
                {
                    throw new InvalidOperationException("Bản ghi không còn tồn tại. Vui lòng tải lại.");
                }

                KiemTra(connection, transaction, "BAO_TRI_THIET_BI", duLieu, hienTai, banGoc);
                if (hienTai.TrangThai is not "Nháp" and not "Đang thực hiện")
                    throw new InvalidOperationException("Lần bảo trì đã kết thúc được giữ nguyên. Hãy tạo lần bảo trì mới.");
                KiemTraNghiepVu(duLieu);
                if (chiDoiTrangThai)
                {
                    KiemTraChiDoiTrangThai(duLieu, hienTai);
                }

                connection.Execute("""
                UPDATE dbo.[BAO_TRI_THIET_BI]
                SET [MaTB]=@MaTB,[MaNV]=@MaNV,[NgayBatDau]=@NgayBatDau,
                    [NgayKetThuc]=@NgayKetThuc,[NoiDung]=@NoiDung,[ChiPhi]=@ChiPhi,
                    [NgayBaoTriTiepTheo]=@NgayBaoTriTiepTheo,[TrangThai]=@TrangThai,[GhiChu]=@GhiChu
                WHERE [MaBT]=@MaBT
                """, duLieu, transaction);
                return true;
            });
        }

        /// <summary>
        /// Xóa đợt bảo trì thiết bị: đọc bản hiện tại với UPDLOCK/HOLDLOCK, báo lỗi nếu đã mất, so banGoc khi có rồi DELETE theo mã trong dbo.BAO_TRI_THIET_BI; toàn bộ thao tác giữ cùng giao dịch và lỗi ràng buộc SQL được phát ra.
        /// </summary>
        /// <param name = "ma">Mã khóa chính của bản ghi cần đọc, sửa hoặc xóa; truyền bằng tham số SQL.</param>
        /// <param name = "banGoc">Bản ET đã đọc trước khi thao tác; có giá trị để so với SQL và chặn ghi đè/xóa khi bản đã thay đổi, null để bỏ đối chiếu bản gốc.</param>
        /// <returns>true khi DELETE chạy thành công; không tồn tại, xung đột hoặc vi phạm SQL gây ngoại lệ.</returns>
        public bool xoaBaoTriThietBi(string ma, ET_BAOTRITHIETBI? banGoc = null)
        {
            return ThucHien((connection, transaction) =>
            {
                var hienTai = connection.QuerySingleOrDefault<ET_BAOTRITHIETBI>("SELECT * FROM dbo.[BAO_TRI_THIET_BI] WITH(UPDLOCK,HOLDLOCK) WHERE [MaBT]=@Ma", new { Ma = ma }, transaction);
                if (hienTai is null)
                {
                    throw new InvalidOperationException("Bản ghi không còn tồn tại. Vui lòng tải lại.");
                }

                KiemTra(connection, transaction, "BAO_TRI_THIET_BI", hienTai, hienTai, banGoc);
                if (hienTai.TrangThai != "Nháp")
                    throw new InvalidOperationException("Chỉ xóa lần bảo trì nháp. Lần đang thực hiện cần chuyển sang Hủy để giữ lịch sử.");
                connection.Execute("DELETE FROM dbo.[BAO_TRI_THIET_BI] WHERE [MaBT]=@Ma", new { Ma = ma }, transaction);
                return true;
            });
        }

        /// <summary>Kiểm tra trước khi ghi, dùng chung cho thêm/sửa và mọi nơi gọi BUS.</summary>
        private static void KiemTraNghiepVu(ET_BAOTRITHIETBI duLieu)
        {
            if (string.IsNullOrWhiteSpace(duLieu.MaTB) || string.IsNullOrWhiteSpace(duLieu.MaNV) || string.IsNullOrWhiteSpace(duLieu.NoiDung))
                throw new InvalidOperationException("Vui lòng chọn thiết bị, nhân viên phụ trách và nhập nội dung bảo trì.");
            if (duLieu.TrangThai is not "Nháp" and not "Đang thực hiện" and not "Hoàn thành" and not "Đã xác nhận" and not "Hủy")
                throw new InvalidOperationException("Trạng thái bảo trì không hợp lệ.");
            if (duLieu.ChiPhi < 0)
                throw new InvalidOperationException("Chi phí bảo trì không được âm.");
            if (duLieu.NgayKetThuc < duLieu.NgayBatDau)
                throw new InvalidOperationException("Ngày kết thúc không được trước ngày bắt đầu.");
            if (duLieu.NgayBaoTriTiepTheo.HasValue && duLieu.NgayBaoTriTiepTheo.Value.Date <= (duLieu.NgayKetThuc ?? duLieu.NgayBatDau).Date)
                throw new InvalidOperationException("Ngày bảo trì tiếp theo phải sau ngày kết thúc hoặc ngày bắt đầu.");
            if ((duLieu.TrangThai is "Hoàn thành" or "Đã xác nhận") && duLieu.NgayKetThuc is null)
                throw new InvalidOperationException("Vui lòng nhập ngày kết thúc khi hoàn thành bảo trì.");
        }
    }
}
