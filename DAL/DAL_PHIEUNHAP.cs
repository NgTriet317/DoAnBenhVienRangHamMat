// AI hỗ trợ: bổ sung chú thích tiếng Việt;
using System.Data;
using Dapper;
using ET;

namespace DAL
{
    /// <summary>
    /// Tầng truy cập SQL cho phiếu nhập kho trong bảng PHIEU_NHAP. Lưu đầu phiếu theo hóa đơn nhập, kho và nhân viên; chỉ chỉnh chứng từ khi còn nháp. Dapper ánh xạ dòng SQL sang ET; ThucHien dùng giao dịch được truyền vào hoặc tự quản lý giao dịch cục bộ.
    /// </summary>
    public class DAL_PHIEUNHAP : dbconnect
    {
        /// <summary>
        /// Tạo DAL quản lý phiếu nhập kho, chuyển giao dịch tùy chọn cho dbconnect để nhiều thao tác có thể dùng chung kết nối và phạm vi lưu.
        /// </summary>
        /// <param name="transaction">Giao dịch của nơi gọi; null để hạ tầng tham gia giao dịch môi trường hoặc tự mở giao dịch cục bộ. Nếu truyền vào, nơi gọi quản lý commit/rollback.</param>
        public DAL_PHIEUNHAP(IDbTransaction? transaction = null) : base(transaction) { }
        /// <summary>
        /// Đọc toàn bộ phiếu nhập kho từ dbo.PHIEU_NHAP, Dapper ánh xạ từng dòng sang ET, sắp theo mã; ToArray nạp hết dữ liệu trước khi hạ tầng kết thúc phạm vi kết nối/giao dịch.
        /// </summary>
        /// <returns>Danh sách ET đã vật hóa; không có bản ghi thì trả danh sách rỗng.</returns>
        public IEnumerable<ET_PHIEUNHAP> layToanBoPhieuNhap() => ThucHien((connection, transaction) =>
            connection.Query<ET_PHIEUNHAP>("SELECT * FROM dbo.[PHIEU_NHAP] ORDER BY [MaPN]", transaction: transaction).ToArray());
        /// <summary>
        /// Tìm phiếu nhập kho theo khóa trong dbo.PHIEU_NHAP; mã được truyền bằng tham số SQL và QuerySingleOrDefault ánh xạ tối đa một bản ghi sang ET.
        /// </summary>
        /// <param name="ma">Mã khóa chính của bản ghi cần đọc, sửa hoặc xóa; truyền bằng tham số SQL.</param>
        /// <returns>ET của bản ghi tìm thấy, hoặc null nếu mã không tồn tại.</returns>
        public ET_PHIEUNHAP? layPhieuNhapTheoMa(string ma) => ThucHien((connection, transaction) =>
            connection.QuerySingleOrDefault<ET_PHIEUNHAP>("SELECT * FROM dbo.[PHIEU_NHAP] WHERE [MaPN]=@Ma", new { Ma = ma }, transaction));
        /// <summary>
        /// Thêm phiếu nhập kho: cấp mã có khóa ngay vào duLieu, kiểm tra nghiệp vụ, chuẩn hóa và kiểm tra ET theo metadata SQL rồi INSERT có tham số vào dbo.PHIEU_NHAP trong cùng giao dịch.
        /// </summary>
        /// <param name="duLieu">ET chứa dữ liệu cần lưu; thêm mới sẽ được gán mã, và kiểm tra metadata có thể chuẩn hóa lại các giá trị ngay trên đối tượng.</param>
        /// <returns>true sau khi INSERT thành công; lỗi dữ liệu/SQL được phát ra bằng ngoại lệ. Giao dịch ngoài chỉ commit khi nơi gọi quyết định.</returns>
        public bool themPhieuNhap(ET_PHIEUNHAP duLieu) => ThucHien((connection, transaction) =>
        {
            duLieu.MaPN = TaoMa(connection, transaction, "PHIEU_NHAP", "MaPN");
            KiemTraNghiepVu(transaction, duLieu, null);
            KiemTra<ET_PHIEUNHAP>(connection, transaction, "PHIEU_NHAP", duLieu, null, null);
            connection.Execute("INSERT INTO dbo.[PHIEU_NHAP] ([MaPN],[MaHDN],[MaKho],[MaNV],[NgayNhap],[TrangThai],[LyDo],[GhiChu]) VALUES (@MaPN,@MaHDN,@MaKho,@MaNV,@NgayNhap,@TrangThai,@LyDo,@GhiChu)", duLieu, transaction);
            return true;
        });
        /// <summary>
        /// Sửa phiếu nhập kho: đọc và giữ khóa UPDLOCK/HOLDLOCK, báo lỗi nếu bản không còn, kiểm tra nghiệp vụ, chuẩn hóa ET và đối chiếu banGoc để phát hiện sửa đồng thời. Khi chỉ đổi trạng thái, chặn thay đổi các trường ngoài TrangThai/NgayXacNhan/NgayCapNhat/LyDoThayDoi, sau đó UPDATE có tham số trong cùng giao dịch.
        /// </summary>
        /// <param name="duLieu">ET chứa dữ liệu cần lưu; thêm mới sẽ được gán mã, và kiểm tra metadata có thể chuẩn hóa lại các giá trị ngay trên đối tượng.</param>
        /// <param name="banGoc">Bản ET đã đọc trước khi thao tác; có giá trị để so với SQL và chặn ghi đè/xóa khi bản đã thay đổi, null để bỏ đối chiếu bản gốc.</param>
        /// <param name="chiDoiTrangThai">true để cấm thay đổi trường nội dung ngoài nhóm trường trạng thái cho phép; không tự đảm bảo mọi chuyển trạng thái đều hợp lệ.</param>
        /// <returns>true khi UPDATE chạy thành công; bản bị mất/thay đổi hoặc dữ liệu sai gây ngoại lệ. Không trả false để biểu thị lỗi.</returns>
        public bool suaPhieuNhap(ET_PHIEUNHAP duLieu, ET_PHIEUNHAP? banGoc = null, bool chiDoiTrangThai = false) => ThucHien((connection, transaction) =>
        {
            var hienTai = connection.QuerySingleOrDefault<ET_PHIEUNHAP>("SELECT * FROM dbo.[PHIEU_NHAP] WITH(UPDLOCK,HOLDLOCK) WHERE [MaPN]=@Ma", new { Ma = duLieu.MaPN }, transaction)
                ?? throw new InvalidOperationException("Bản ghi không còn tồn tại. Vui lòng tải lại.");
            KiemTraNghiepVu(transaction, duLieu, hienTai);
            KiemTra(connection, transaction, "PHIEU_NHAP", duLieu, hienTai, banGoc);
            if (chiDoiTrangThai)
                foreach (var pair in GiaTri(duLieu))
                    if (pair.Key is not "TrangThai" and not "NgayXacNhan" and not "NgayCapNhat" and not "LyDoThayDoi"
                        && !Equals(pair.Value, GiaTri(hienTai).GetValueOrDefault(pair.Key)))
                        throw new InvalidOperationException("Thao tác đổi trạng thái không được thay đổi nội dung bản ghi.");
            connection.Execute("UPDATE dbo.[PHIEU_NHAP] SET [MaHDN]=@MaHDN,[MaKho]=@MaKho,[MaNV]=@MaNV,[NgayNhap]=@NgayNhap,[TrangThai]=@TrangThai,[LyDo]=@LyDo,[GhiChu]=@GhiChu WHERE [MaPN]=@MaPN", duLieu, transaction);
            return true;
        });
        /// <summary>
        /// Xóa phiếu nhập kho: đọc bản hiện tại với UPDLOCK/HOLDLOCK, báo lỗi nếu đã mất, kiểm tra nghiệp vụ, so banGoc khi có rồi DELETE theo mã trong dbo.PHIEU_NHAP; toàn bộ thao tác giữ cùng giao dịch và lỗi ràng buộc SQL được phát ra.
        /// </summary>
        /// <param name="ma">Mã khóa chính của bản ghi cần đọc, sửa hoặc xóa; truyền bằng tham số SQL.</param>
        /// <param name="banGoc">Bản ET đã đọc trước khi thao tác; có giá trị để so với SQL và chặn ghi đè/xóa khi bản đã thay đổi, null để bỏ đối chiếu bản gốc.</param>
        /// <returns>true khi DELETE chạy thành công; không tồn tại, xung đột hoặc vi phạm SQL gây ngoại lệ.</returns>
        public bool xoaPhieuNhap(string ma, ET_PHIEUNHAP? banGoc = null) => ThucHien((connection, transaction) =>
        {
            var hienTai = connection.QuerySingleOrDefault<ET_PHIEUNHAP>("SELECT * FROM dbo.[PHIEU_NHAP] WITH(UPDLOCK,HOLDLOCK) WHERE [MaPN]=@Ma", new { Ma = ma }, transaction)
                ?? throw new InvalidOperationException("Bản ghi không còn tồn tại. Vui lòng tải lại.");
            KiemTraNghiepVu(transaction, hienTai, hienTai);
            KiemTra(connection, transaction, "PHIEU_NHAP", hienTai, hienTai, banGoc);
            connection.Execute("DELETE FROM dbo.[PHIEU_NHAP] WHERE [MaPN]=@Ma", new { Ma = ma }, transaction);
            return true;
        });
        /// <summary>
        /// Khi bản hiện tại tồn tại, yêu cầu nó còn Nháp trước mọi sửa hoặc xóa; hàm không truy vấn SQL và không kiểm tra dữ liệu bản thêm mới.
        /// </summary>
        /// <param name="tx">Tham số giao dịch của chữ ký chung; phần thân hàm này không truy vấn qua nó.</param>
        /// <param name="record">Bản dữ liệu của chữ ký chung; hàm hiện chỉ xét trạng thái original.</param>
        /// <param name="original">Bản hiện tại đã đọc từ SQL; null khi thêm mới, dùng để bảo vệ nội dung/trạng thái và liên kết đã lưu.</param>
        private void KiemTraNghiepVu(IDbTransaction? tx, ET_PHIEUNHAP record, ET_PHIEUNHAP? original)
        {
            if (original is not null && original.TrangThai != "Nháp")
                throw new InvalidOperationException("Chứng từ đã xác nhận hoặc đã hủy được giữ nguyên.");
        }

    }
}
