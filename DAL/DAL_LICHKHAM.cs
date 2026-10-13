// AI hỗ trợ: bổ sung chú thích tiếng Việt;
using System.Data;
using Dapper;
using ET;

namespace DAL
{
    /// <summary>
    /// Tầng truy cập SQL cho lịch khám trong bảng LICH_KHAM. Lưu bệnh nhân, bác sĩ, ca, phòng, dịch vụ và thời điểm hẹn; giữ nguyên lịch đã hủy, đã đổi hoặc hoàn thành. Dapper ánh xạ dòng SQL sang ET; ThucHien dùng giao dịch được truyền vào hoặc tự quản lý giao dịch cục bộ.
    /// </summary>
    public class DAL_LICHKHAM : dbconnect
    {
        /// <summary>
        /// Tạo DAL quản lý lịch khám, chuyển giao dịch tùy chọn cho dbconnect để nhiều thao tác có thể dùng chung kết nối và phạm vi lưu.
        /// </summary>
        /// <param name="transaction">Giao dịch của nơi gọi; null để hạ tầng tham gia giao dịch môi trường hoặc tự mở giao dịch cục bộ. Nếu truyền vào, nơi gọi quản lý commit/rollback.</param>
        public DAL_LICHKHAM(IDbTransaction? transaction = null) : base(transaction) { }

        /// <summary>
        /// Đọc toàn bộ lịch khám từ dbo.LICH_KHAM, Dapper ánh xạ từng dòng sang ET, sắp theo mã; ToArray nạp hết dữ liệu trước khi hạ tầng kết thúc phạm vi kết nối/giao dịch.
        /// </summary>
        /// <returns>Danh sách ET đã vật hóa; không có bản ghi thì trả danh sách rỗng.</returns>
        public IEnumerable<ET_LICHKHAM> layToanBoLichKham()
        {
            return ThucHien((connection, transaction) =>
                connection.Query<ET_LICHKHAM>("SELECT * FROM dbo.[LICH_KHAM] ORDER BY [MaLich]", transaction: transaction).ToArray());
        }

        /// <summary>
        /// Tìm lịch khám theo khóa trong dbo.LICH_KHAM; mã được truyền bằng tham số SQL và QuerySingleOrDefault ánh xạ tối đa một bản ghi sang ET.
        /// </summary>
        /// <param name="ma">Mã khóa chính của bản ghi cần đọc, sửa hoặc xóa; truyền bằng tham số SQL.</param>
        /// <returns>ET của bản ghi tìm thấy, hoặc null nếu mã không tồn tại.</returns>
        public ET_LICHKHAM? layLichKhamTheoMa(string ma)
        {
            return ThucHien((connection, transaction) =>
                connection.QuerySingleOrDefault<ET_LICHKHAM>("SELECT * FROM dbo.[LICH_KHAM] WHERE [MaLich]=@Ma", new { Ma = ma }, transaction));
        }

        /// <summary>
        /// Thêm lịch khám: cấp mã có khóa ngay vào duLieu, kiểm tra nghiệp vụ, chuẩn hóa và kiểm tra ET theo metadata SQL rồi INSERT có tham số vào dbo.LICH_KHAM trong cùng giao dịch.
        /// </summary>
        /// <param name="duLieu">ET chứa dữ liệu cần lưu; thêm mới sẽ được gán mã, và kiểm tra metadata có thể chuẩn hóa lại các giá trị ngay trên đối tượng.</param>
        /// <returns>true sau khi INSERT thành công; lỗi dữ liệu/SQL được phát ra bằng ngoại lệ. Giao dịch ngoài chỉ commit khi nơi gọi quyết định.</returns>
        public bool themLichKham(ET_LICHKHAM duLieu)
        {
            return ThucHien((connection, transaction) =>
            {
                duLieu.MaLich = TaoMa(connection, transaction, "LICH_KHAM", "MaLich");
                KiemTraNghiepVu(connection, transaction, duLieu, null);
                KiemTra<ET_LICHKHAM>(connection, transaction, "LICH_KHAM", duLieu, null, null);
                connection.Execute("INSERT INTO dbo.[LICH_KHAM] ([MaLich],[MaBN],[MaBS],[MaCa],[MaPhong],[MaDV],[NgayGioKham],[ThoiLuongDuKien],[UuTien],[LyDoKham],[LyDoThayDoi],[TrangThai],[GhiChu],[NgayTao],[NgayCapNhat]) VALUES (@MaLich,@MaBN,@MaBS,@MaCa,@MaPhong,@MaDV,@NgayGioKham,@ThoiLuongDuKien,@UuTien,@LyDoKham,@LyDoThayDoi,@TrangThai,@GhiChu,@NgayTao,@NgayCapNhat)", duLieu, transaction);
                return true;
            });
        }

        /// <summary>
        /// Sửa lịch khám: đọc và giữ khóa UPDLOCK/HOLDLOCK, báo lỗi nếu bản không còn, kiểm tra nghiệp vụ, chuẩn hóa ET và đối chiếu banGoc để phát hiện sửa đồng thời. Khi chỉ đổi trạng thái, chặn thay đổi các trường ngoài TrangThai/NgayXacNhan/NgayCapNhat/LyDoThayDoi, sau đó UPDATE có tham số trong cùng giao dịch.
        /// </summary>
        /// <param name="duLieu">ET chứa dữ liệu cần lưu; thêm mới sẽ được gán mã, và kiểm tra metadata có thể chuẩn hóa lại các giá trị ngay trên đối tượng.</param>
        /// <param name="banGoc">Bản ET đã đọc trước khi thao tác; có giá trị để so với SQL và chặn ghi đè/xóa khi bản đã thay đổi, null để bỏ đối chiếu bản gốc.</param>
        /// <param name="chiDoiTrangThai">true để cấm thay đổi trường nội dung ngoài nhóm trường trạng thái cho phép; không tự đảm bảo mọi chuyển trạng thái đều hợp lệ.</param>
        /// <returns>true khi UPDATE chạy thành công; bản bị mất/thay đổi hoặc dữ liệu sai gây ngoại lệ. Không trả false để biểu thị lỗi.</returns>
        public bool suaLichKham(ET_LICHKHAM duLieu, ET_LICHKHAM? banGoc = null, bool chiDoiTrangThai = false)
        {
            return ThucHien((connection, transaction) =>
            {
                var hienTai = connection.QuerySingleOrDefault<ET_LICHKHAM>(
                    "SELECT * FROM dbo.[LICH_KHAM] WITH(UPDLOCK,HOLDLOCK) WHERE [MaLich]=@Ma", new { Ma = duLieu.MaLich }, transaction)
                    ?? throw new InvalidOperationException("Bản ghi không còn tồn tại. Vui lòng tải lại.");
                KiemTraNghiepVu(connection, transaction, duLieu, hienTai, chiDoiTrangThai);
                KiemTra(connection, transaction, "LICH_KHAM", duLieu, hienTai, banGoc);
                if (chiDoiTrangThai)
                    foreach (var pair in GiaTri(duLieu))
                        if (pair.Key is not "TrangThai" and not "NgayXacNhan" and not "NgayCapNhat" and not "LyDoThayDoi"
                            && !Equals(pair.Value, GiaTri(hienTai).GetValueOrDefault(pair.Key)))
                            throw new InvalidOperationException("Thao tác đổi trạng thái không được thay đổi nội dung bản ghi.");
                connection.Execute("UPDATE dbo.[LICH_KHAM] SET [MaBN]=@MaBN,[MaBS]=@MaBS,[MaCa]=@MaCa,[MaPhong]=@MaPhong,[MaDV]=@MaDV,[NgayGioKham]=@NgayGioKham,[ThoiLuongDuKien]=@ThoiLuongDuKien,[UuTien]=@UuTien,[LyDoKham]=@LyDoKham,[LyDoThayDoi]=@LyDoThayDoi,[TrangThai]=@TrangThai,[GhiChu]=@GhiChu,[NgayTao]=@NgayTao,[NgayCapNhat]=@NgayCapNhat WHERE [MaLich]=@MaLich", duLieu, transaction);
                return true;
            });
        }

        /// <summary>
        /// Xóa lịch khám: đọc bản hiện tại với UPDLOCK/HOLDLOCK, báo lỗi nếu đã mất, kiểm tra nghiệp vụ, so banGoc khi có rồi DELETE theo mã trong dbo.LICH_KHAM; toàn bộ thao tác giữ cùng giao dịch và lỗi ràng buộc SQL được phát ra.
        /// </summary>
        /// <param name="ma">Mã khóa chính của bản ghi cần đọc, sửa hoặc xóa; truyền bằng tham số SQL.</param>
        /// <param name="banGoc">Bản ET đã đọc trước khi thao tác; có giá trị để so với SQL và chặn ghi đè/xóa khi bản đã thay đổi, null để bỏ đối chiếu bản gốc.</param>
        /// <returns>true khi DELETE chạy thành công; không tồn tại, xung đột hoặc vi phạm SQL gây ngoại lệ.</returns>
        public bool xoaLichKham(string ma, ET_LICHKHAM? banGoc = null)
        {
            return ThucHien((connection, transaction) =>
            {
                var hienTai = connection.QuerySingleOrDefault<ET_LICHKHAM>(
                    "SELECT * FROM dbo.[LICH_KHAM] WITH(UPDLOCK,HOLDLOCK) WHERE [MaLich]=@Ma", new { Ma = ma }, transaction)
                    ?? throw new InvalidOperationException("Bản ghi không còn tồn tại. Vui lòng tải lại.");
                KiemTraNghiepVu(connection, transaction, hienTai, hienTai);
                KiemTra(connection, transaction, "LICH_KHAM", hienTai, hienTai, banGoc);
                connection.Execute("DELETE FROM dbo.[LICH_KHAM] WHERE [MaLich]=@Ma", new { Ma = ma }, transaction);
                return true;
            });
        }

        /// <summary>
        /// Chặn sửa nội dung ngoài nhóm trạng thái nháp và chặn quay về Nháp; nếu lịch đã Hủy, Đã đổi hoặc Hoàn thành thì chặn cả thao tác chuyển trạng thái.
        /// </summary>
        /// <param name="db">Tham số kết nối của chữ ký chung; phần thân kiểm tra nghiệp vụ này không dùng trực tiếp.</param>
        /// <param name="tx">Tham số giao dịch của chữ ký chung; phần thân hàm này không truy vấn qua nó.</param>
        /// <param name="record">ET cần kiểm tra hoặc cập nhật, chứa các mã liên kết và trạng thái mới.</param>
        /// <param name="original">Bản hiện tại đã đọc từ SQL; null khi thêm mới, dùng để bảo vệ nội dung/trạng thái và liên kết đã lưu.</param>
        /// <param name="statusOnly">true khi nơi gọi chỉ đổi trạng thái; cờ ảnh hưởng việc chặn sửa nội dung ngoài trạng thái nháp, các giới hạn khác vẫn áp dụng.</param>
        private void KiemTraNghiepVu(IDbConnection db, IDbTransaction? tx, ET_LICHKHAM record, ET_LICHKHAM? original, bool statusOnly = false)
        {
            if (original is not null)
            {
                var state = original.TrangThai;
                var draft = state is null or "" or "Nháp" or "Chưa xác nhận" or "Chờ xác nhận" or "Đang lập" or "Dự kiến" or "Chờ thực hiện";
                if (!draft && (!statusOnly)) throw new InvalidOperationException("Bản ghi đã xác nhận hoặc đã hủy; hãy lập bản mới.");
                if (!draft && record.TrangThai == "Nháp") throw new InvalidOperationException("Không chuyển chứng từ đã xác nhận về nháp.");
                if (state is "Hủy" or "Đã đổi" or "Hoàn thành") throw new InvalidOperationException("Lịch đã kết thúc, hãy lập lịch mới.");
            }
        }
    }
}
