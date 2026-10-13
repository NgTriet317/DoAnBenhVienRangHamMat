// AI hỗ trợ: bổ sung chú thích tiếng Việt;
using System.Data;
using Dapper;
using ET;

namespace DAL
{
    /// <summary>
    /// Tầng truy cập SQL cho dòng thuốc trong đơn trong bảng CHI_TIET_DON_THUOC. Lưu thuốc, liều dùng, số lượng, số ngày và hướng dẫn; nội dung chỉ sửa được khi đơn cha còn nháp. Dapper ánh xạ dòng SQL sang ET; ThucHien dùng giao dịch được truyền vào hoặc tự quản lý giao dịch cục bộ.
    /// </summary>
    public class DAL_CHITIETDONTHUOC : dbconnect
    {
        /// <summary>
        /// Tạo DAL quản lý dòng thuốc trong đơn, chuyển giao dịch tùy chọn cho dbconnect để nhiều thao tác có thể dùng chung kết nối và phạm vi lưu.
        /// </summary>
        /// <param name="transaction">Giao dịch của nơi gọi; null để hạ tầng tham gia giao dịch môi trường hoặc tự mở giao dịch cục bộ. Nếu truyền vào, nơi gọi quản lý commit/rollback.</param>
        public DAL_CHITIETDONTHUOC(IDbTransaction? transaction = null) : base(transaction) { }

        /// <summary>
        /// Đọc toàn bộ dòng thuốc trong đơn từ dbo.CHI_TIET_DON_THUOC, Dapper ánh xạ từng dòng sang ET, sắp theo mã; ToArray nạp hết dữ liệu trước khi hạ tầng kết thúc phạm vi kết nối/giao dịch.
        /// </summary>
        /// <returns>Danh sách ET đã vật hóa; không có bản ghi thì trả danh sách rỗng.</returns>
        public IEnumerable<ET_CHITIETDONTHUOC> layToanBoChiTietDonThuoc()
        {
            return ThucHien((connection, transaction) =>
                connection.Query<ET_CHITIETDONTHUOC>("SELECT * FROM dbo.[CHI_TIET_DON_THUOC] ORDER BY [MaCTDT]", transaction: transaction).ToArray());
        }

        /// <summary>
        /// Tìm dòng thuốc trong đơn theo khóa trong dbo.CHI_TIET_DON_THUOC; mã được truyền bằng tham số SQL và QuerySingleOrDefault ánh xạ tối đa một bản ghi sang ET.
        /// </summary>
        /// <param name="ma">Mã khóa chính của bản ghi cần đọc, sửa hoặc xóa; truyền bằng tham số SQL.</param>
        /// <returns>ET của bản ghi tìm thấy, hoặc null nếu mã không tồn tại.</returns>
        public ET_CHITIETDONTHUOC? layChiTietDonThuocTheoMa(string ma)
        {
            return ThucHien((connection, transaction) =>
                connection.QuerySingleOrDefault<ET_CHITIETDONTHUOC>("SELECT * FROM dbo.[CHI_TIET_DON_THUOC] WHERE [MaCTDT]=@Ma", new { Ma = ma }, transaction));
        }

        /// <summary>
        /// Thêm dòng thuốc trong đơn: cấp mã có khóa ngay vào duLieu, kiểm tra nghiệp vụ, chuẩn hóa và kiểm tra ET theo metadata SQL rồi INSERT có tham số vào dbo.CHI_TIET_DON_THUOC trong cùng giao dịch.
        /// </summary>
        /// <param name="duLieu">ET chứa dữ liệu cần lưu; thêm mới sẽ được gán mã, và kiểm tra metadata có thể chuẩn hóa lại các giá trị ngay trên đối tượng.</param>
        /// <returns>true sau khi INSERT thành công; lỗi dữ liệu/SQL được phát ra bằng ngoại lệ. Giao dịch ngoài chỉ commit khi nơi gọi quyết định.</returns>
        public bool themChiTietDonThuoc(ET_CHITIETDONTHUOC duLieu)
        {
            return ThucHien((connection, transaction) =>
            {
                duLieu.MaCTDT = TaoMa(connection, transaction, "CHI_TIET_DON_THUOC", "MaCTDT");
                KiemTraNghiepVu(connection, transaction, duLieu, null);
                KiemTra<ET_CHITIETDONTHUOC>(connection, transaction, "CHI_TIET_DON_THUOC", duLieu, null, null);
                connection.Execute("INSERT INTO dbo.[CHI_TIET_DON_THUOC] ([MaCTDT],[MaDT],[MaThuoc],[LieuDung],[SoLuong],[SoNgayDung],[HuongDan]) VALUES (@MaCTDT,@MaDT,@MaThuoc,@LieuDung,@SoLuong,@SoNgayDung,@HuongDan)", duLieu, transaction);
                return true;
            });
        }

        /// <summary>
        /// Sửa dòng thuốc trong đơn: đọc và giữ khóa UPDLOCK/HOLDLOCK, báo lỗi nếu bản không còn, kiểm tra nghiệp vụ, chuẩn hóa ET và đối chiếu banGoc để phát hiện sửa đồng thời. Khi chỉ đổi trạng thái, chặn thay đổi các trường ngoài TrangThai/NgayXacNhan/NgayCapNhat/LyDoThayDoi, sau đó UPDATE có tham số trong cùng giao dịch.
        /// </summary>
        /// <param name="duLieu">ET chứa dữ liệu cần lưu; thêm mới sẽ được gán mã, và kiểm tra metadata có thể chuẩn hóa lại các giá trị ngay trên đối tượng.</param>
        /// <param name="banGoc">Bản ET đã đọc trước khi thao tác; có giá trị để so với SQL và chặn ghi đè/xóa khi bản đã thay đổi, null để bỏ đối chiếu bản gốc.</param>
        /// <param name="chiDoiTrangThai">true để cấm thay đổi trường nội dung ngoài nhóm trường trạng thái cho phép; không tự đảm bảo mọi chuyển trạng thái đều hợp lệ.</param>
        /// <returns>true khi UPDATE chạy thành công; bản bị mất/thay đổi hoặc dữ liệu sai gây ngoại lệ. Không trả false để biểu thị lỗi.</returns>
        public bool suaChiTietDonThuoc(ET_CHITIETDONTHUOC duLieu, ET_CHITIETDONTHUOC? banGoc = null, bool chiDoiTrangThai = false)
        {
            return ThucHien((connection, transaction) =>
            {
                var hienTai = connection.QuerySingleOrDefault<ET_CHITIETDONTHUOC>(
                    "SELECT * FROM dbo.[CHI_TIET_DON_THUOC] WITH(UPDLOCK,HOLDLOCK) WHERE [MaCTDT]=@Ma", new { Ma = duLieu.MaCTDT }, transaction)
                    ?? throw new InvalidOperationException("Bản ghi không còn tồn tại. Vui lòng tải lại.");
                KiemTraNghiepVu(connection, transaction, duLieu, hienTai, chiDoiTrangThai);
                KiemTra(connection, transaction, "CHI_TIET_DON_THUOC", duLieu, hienTai, banGoc);
                if (chiDoiTrangThai)
                    foreach (var pair in GiaTri(duLieu))
                        if (pair.Key is not "TrangThai" and not "NgayXacNhan" and not "NgayCapNhat" and not "LyDoThayDoi"
                            && !Equals(pair.Value, GiaTri(hienTai).GetValueOrDefault(pair.Key)))
                            throw new InvalidOperationException("Thao tác đổi trạng thái không được thay đổi nội dung bản ghi.");
                connection.Execute("UPDATE dbo.[CHI_TIET_DON_THUOC] SET [MaDT]=@MaDT,[MaThuoc]=@MaThuoc,[LieuDung]=@LieuDung,[SoLuong]=@SoLuong,[SoNgayDung]=@SoNgayDung,[HuongDan]=@HuongDan WHERE [MaCTDT]=@MaCTDT", duLieu, transaction);
                return true;
            });
        }

        /// <summary>
        /// Xóa dòng thuốc trong đơn: đọc bản hiện tại với UPDLOCK/HOLDLOCK, báo lỗi nếu đã mất, kiểm tra nghiệp vụ, so banGoc khi có rồi DELETE theo mã trong dbo.CHI_TIET_DON_THUOC; toàn bộ thao tác giữ cùng giao dịch và lỗi ràng buộc SQL được phát ra.
        /// </summary>
        /// <param name="ma">Mã khóa chính của bản ghi cần đọc, sửa hoặc xóa; truyền bằng tham số SQL.</param>
        /// <param name="banGoc">Bản ET đã đọc trước khi thao tác; có giá trị để so với SQL và chặn ghi đè/xóa khi bản đã thay đổi, null để bỏ đối chiếu bản gốc.</param>
        /// <returns>true khi DELETE chạy thành công; không tồn tại, xung đột hoặc vi phạm SQL gây ngoại lệ.</returns>
        public bool xoaChiTietDonThuoc(string ma, ET_CHITIETDONTHUOC? banGoc = null)
        {
            return ThucHien((connection, transaction) =>
            {
                var hienTai = connection.QuerySingleOrDefault<ET_CHITIETDONTHUOC>(
                    "SELECT * FROM dbo.[CHI_TIET_DON_THUOC] WITH(UPDLOCK,HOLDLOCK) WHERE [MaCTDT]=@Ma", new { Ma = ma }, transaction)
                    ?? throw new InvalidOperationException("Bản ghi không còn tồn tại. Vui lòng tải lại.");
                KiemTraNghiepVu(connection, transaction, hienTai, hienTai);
                KiemTra(connection, transaction, "CHI_TIET_DON_THUOC", hienTai, hienTai, banGoc);
                connection.Execute("DELETE FROM dbo.[CHI_TIET_DON_THUOC] WHERE [MaCTDT]=@Ma", new { Ma = ma }, transaction);
                return true;
            });
        }

        /// <summary>
        /// Đọc đơn cha trong giao dịch, yêu cầu đơn tồn tại và còn Nháp; cấm chuyển dòng đã lưu sang đơn khác.
        /// </summary>
        /// <param name="db">Tham số kết nối của chữ ký chung; phần thân kiểm tra nghiệp vụ này không dùng trực tiếp.</param>
        /// <param name="tx">Giao dịch đang dùng để đọc/ghi và giữ khóa đồng bộ; được truyền tiếp cho các DAL liên quan nếu có.</param>
        /// <param name="record">ET cần kiểm tra hoặc cập nhật, chứa các mã liên kết và trạng thái mới.</param>
        /// <param name="original">Bản hiện tại đã đọc từ SQL; null khi thêm mới, dùng để bảo vệ nội dung/trạng thái và liên kết đã lưu.</param>
        /// <param name="statusOnly">Tham số của chữ ký kiểm tra chung; phần thân hàm này không sử dụng cờ, các điều kiện vẫn được kiểm tra như bình thường.</param>
        private void KiemTraNghiepVu(IDbConnection db, IDbTransaction? tx, ET_CHITIETDONTHUOC record, ET_CHITIETDONTHUOC? original, bool statusOnly = false)
        {
            var parent = new DAL_DONTHUOC(tx).layDonThuocTheoMa(record.MaDT);
            if (parent is null || parent.TrangThai != "Nháp") throw new InvalidOperationException("Chỉ sửa nội dung trong bản nháp có liên kết hợp lệ.");
            if (original is not null && record.MaDT != original.MaDT) throw new InvalidOperationException("Không chuyển bản ghi đã lưu sang hồ sơ khác.");
        }
    }
}
