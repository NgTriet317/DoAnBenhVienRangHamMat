// AI hỗ trợ: bổ sung chú thích tiếng Việt;
using System.Data;
using Dapper;
using ET;

namespace DAL
{
    /// <summary>
    /// Tầng truy cập SQL cho bước kế hoạch điều trị trong bảng CHI_TIET_KE_HOACH. Lưu dịch vụ, lần thực hiện, lượng và giá dự kiến; bảo vệ bước đã có dịch vụ xác nhận hoặc liên kết thực hiện. Dapper ánh xạ dòng SQL sang ET; ThucHien dùng giao dịch được truyền vào hoặc tự quản lý giao dịch cục bộ.
    /// </summary>
    public class DAL_CHITIETKEHOACH : dbconnect
    {
        /// <summary>
        /// Tạo DAL quản lý bước kế hoạch điều trị, chuyển giao dịch tùy chọn cho dbconnect để nhiều thao tác có thể dùng chung kết nối và phạm vi lưu.
        /// </summary>
        /// <param name="transaction">Giao dịch của nơi gọi; null để hạ tầng tham gia giao dịch môi trường hoặc tự mở giao dịch cục bộ. Nếu truyền vào, nơi gọi quản lý commit/rollback.</param>
        public DAL_CHITIETKEHOACH(IDbTransaction? transaction = null) : base(transaction) { }

        /// <summary>
        /// Đọc toàn bộ bước kế hoạch điều trị từ dbo.CHI_TIET_KE_HOACH, Dapper ánh xạ từng dòng sang ET, sắp theo mã; ToArray nạp hết dữ liệu trước khi hạ tầng kết thúc phạm vi kết nối/giao dịch.
        /// </summary>
        /// <returns>Danh sách ET đã vật hóa; không có bản ghi thì trả danh sách rỗng.</returns>
        public IEnumerable<ET_CHITIETKEHOACH> layToanBoChiTietKeHoach()
        {
            return ThucHien((connection, transaction) =>
                connection.Query<ET_CHITIETKEHOACH>("SELECT * FROM dbo.[CHI_TIET_KE_HOACH] ORDER BY [MaCTKH]", transaction: transaction).ToArray());
        }

        /// <summary>
        /// Tìm bước kế hoạch điều trị theo khóa trong dbo.CHI_TIET_KE_HOACH; mã được truyền bằng tham số SQL và QuerySingleOrDefault ánh xạ tối đa một bản ghi sang ET.
        /// </summary>
        /// <param name="ma">Mã khóa chính của bản ghi cần đọc, sửa hoặc xóa; truyền bằng tham số SQL.</param>
        /// <returns>ET của bản ghi tìm thấy, hoặc null nếu mã không tồn tại.</returns>
        public ET_CHITIETKEHOACH? layChiTietKeHoachTheoMa(string ma)
        {
            return ThucHien((connection, transaction) =>
                connection.QuerySingleOrDefault<ET_CHITIETKEHOACH>("SELECT * FROM dbo.[CHI_TIET_KE_HOACH] WHERE [MaCTKH]=@Ma", new { Ma = ma }, transaction));
        }

        /// <summary>
        /// Thêm bước kế hoạch điều trị: cấp mã có khóa ngay vào duLieu, kiểm tra nghiệp vụ, chuẩn hóa và kiểm tra ET theo metadata SQL rồi INSERT có tham số vào dbo.CHI_TIET_KE_HOACH trong cùng giao dịch.
        /// </summary>
        /// <param name="duLieu">ET chứa dữ liệu cần lưu; thêm mới sẽ được gán mã, và kiểm tra metadata có thể chuẩn hóa lại các giá trị ngay trên đối tượng.</param>
        /// <returns>true sau khi INSERT thành công; lỗi dữ liệu/SQL được phát ra bằng ngoại lệ. Giao dịch ngoài chỉ commit khi nơi gọi quyết định.</returns>
        public bool themChiTietKeHoach(ET_CHITIETKEHOACH duLieu)
        {
            return ThucHien((connection, transaction) =>
            {
                duLieu.MaCTKH = TaoMa(connection, transaction, "CHI_TIET_KE_HOACH", "MaCTKH");
                KiemTraNghiepVu(connection, transaction, duLieu, null);
                KiemTra<ET_CHITIETKEHOACH>(connection, transaction, "CHI_TIET_KE_HOACH", duLieu, null, null);
                connection.Execute("INSERT INTO dbo.[CHI_TIET_KE_HOACH] ([MaCTKH],[MaKHDT],[MaDV],[LanThu],[SoLuongDuKien],[DonGiaDuKien],[TrangThai],[GhiChu]) VALUES (@MaCTKH,@MaKHDT,@MaDV,@LanThu,@SoLuongDuKien,@DonGiaDuKien,@TrangThai,@GhiChu)", duLieu, transaction);
                return true;
            });
        }

        /// <summary>
        /// Sửa bước kế hoạch điều trị: đọc và giữ khóa UPDLOCK/HOLDLOCK, báo lỗi nếu bản không còn, kiểm tra nghiệp vụ, chuẩn hóa ET và đối chiếu banGoc để phát hiện sửa đồng thời. Khi chỉ đổi trạng thái, chặn thay đổi các trường ngoài TrangThai/NgayXacNhan/NgayCapNhat/LyDoThayDoi, sau đó UPDATE có tham số trong cùng giao dịch.
        /// </summary>
        /// <param name="duLieu">ET chứa dữ liệu cần lưu; thêm mới sẽ được gán mã, và kiểm tra metadata có thể chuẩn hóa lại các giá trị ngay trên đối tượng.</param>
        /// <param name="banGoc">Bản ET đã đọc trước khi thao tác; có giá trị để so với SQL và chặn ghi đè/xóa khi bản đã thay đổi, null để bỏ đối chiếu bản gốc.</param>
        /// <param name="chiDoiTrangThai">true để cấm thay đổi trường nội dung ngoài nhóm trường trạng thái cho phép; không tự đảm bảo mọi chuyển trạng thái đều hợp lệ.</param>
        /// <returns>true khi UPDATE chạy thành công; bản bị mất/thay đổi hoặc dữ liệu sai gây ngoại lệ. Không trả false để biểu thị lỗi.</returns>
        public bool suaChiTietKeHoach(ET_CHITIETKEHOACH duLieu, ET_CHITIETKEHOACH? banGoc = null, bool chiDoiTrangThai = false)
        {
            return ThucHien((connection, transaction) =>
            {
                var hienTai = connection.QuerySingleOrDefault<ET_CHITIETKEHOACH>(
                    "SELECT * FROM dbo.[CHI_TIET_KE_HOACH] WITH(UPDLOCK,HOLDLOCK) WHERE [MaCTKH]=@Ma", new { Ma = duLieu.MaCTKH }, transaction)
                    ?? throw new InvalidOperationException("Bản ghi không còn tồn tại. Vui lòng tải lại.");
                KiemTraNghiepVu(connection, transaction, duLieu, hienTai, chiDoiTrangThai);
                KiemTra(connection, transaction, "CHI_TIET_KE_HOACH", duLieu, hienTai, banGoc);
                if (chiDoiTrangThai)
                    foreach (var pair in GiaTri(duLieu))
                        if (pair.Key is not "TrangThai" and not "NgayXacNhan" and not "NgayCapNhat" and not "LyDoThayDoi"
                            && !Equals(pair.Value, GiaTri(hienTai).GetValueOrDefault(pair.Key)))
                            throw new InvalidOperationException("Thao tác đổi trạng thái không được thay đổi nội dung bản ghi.");
                connection.Execute("UPDATE dbo.[CHI_TIET_KE_HOACH] SET [MaKHDT]=@MaKHDT,[MaDV]=@MaDV,[LanThu]=@LanThu,[SoLuongDuKien]=@SoLuongDuKien,[DonGiaDuKien]=@DonGiaDuKien,[TrangThai]=@TrangThai,[GhiChu]=@GhiChu WHERE [MaCTKH]=@MaCTKH", duLieu, transaction);
                return true;
            });
        }

        /// <summary>
        /// Xóa bước kế hoạch điều trị: đọc bản hiện tại với UPDLOCK/HOLDLOCK, báo lỗi nếu đã mất, kiểm tra nghiệp vụ, so banGoc khi có rồi DELETE theo mã trong dbo.CHI_TIET_KE_HOACH; toàn bộ thao tác giữ cùng giao dịch và lỗi ràng buộc SQL được phát ra.
        /// </summary>
        /// <param name="ma">Mã khóa chính của bản ghi cần đọc, sửa hoặc xóa; truyền bằng tham số SQL.</param>
        /// <param name="banGoc">Bản ET đã đọc trước khi thao tác; có giá trị để so với SQL và chặn ghi đè/xóa khi bản đã thay đổi, null để bỏ đối chiếu bản gốc.</param>
        /// <returns>true khi DELETE chạy thành công; không tồn tại, xung đột hoặc vi phạm SQL gây ngoại lệ.</returns>
        public bool xoaChiTietKeHoach(string ma, ET_CHITIETKEHOACH? banGoc = null)
        {
            return ThucHien((connection, transaction) =>
            {
                var hienTai = connection.QuerySingleOrDefault<ET_CHITIETKEHOACH>(
                    "SELECT * FROM dbo.[CHI_TIET_KE_HOACH] WITH(UPDLOCK,HOLDLOCK) WHERE [MaCTKH]=@Ma", new { Ma = ma }, transaction)
                    ?? throw new InvalidOperationException("Bản ghi không còn tồn tại. Vui lòng tải lại.");
                KiemTraNghiepVu(connection, transaction, hienTai, hienTai);
                KiemTra(connection, transaction, "CHI_TIET_KE_HOACH", hienTai, hienTai, banGoc);
                connection.Execute("DELETE FROM dbo.[CHI_TIET_KE_HOACH] WHERE [MaCTKH]=@Ma", new { Ma = ma }, transaction);
                return true;
            });
        }

        /// <summary>
        /// Xét trạng thái hiện tại, chặn sửa nội dung ngoài các trạng thái nháp và chặn quay về Nháp; bảo vệ bước có dịch vụ xác nhận, cấm đổi dịch vụ khi đã có thực hiện, yêu cầu kế hoạch cha còn Nháp và giữ nguyên liên kết kế hoạch.
        /// </summary>
        /// <param name="db">Tham số kết nối của chữ ký chung; phần thân kiểm tra nghiệp vụ này không dùng trực tiếp.</param>
        /// <param name="tx">Giao dịch đang dùng để đọc/ghi và giữ khóa đồng bộ; được truyền tiếp cho các DAL liên quan nếu có.</param>
        /// <param name="record">ET cần kiểm tra hoặc cập nhật, chứa các mã liên kết và trạng thái mới.</param>
        /// <param name="original">Bản hiện tại đã đọc từ SQL; null khi thêm mới, dùng để bảo vệ nội dung/trạng thái và liên kết đã lưu.</param>
        /// <param name="statusOnly">true khi nơi gọi chỉ đổi trạng thái; cờ ảnh hưởng việc chặn sửa nội dung ngoài trạng thái nháp, các giới hạn khác vẫn áp dụng.</param>
        private void KiemTraNghiepVu(IDbConnection db, IDbTransaction? tx, ET_CHITIETKEHOACH record, ET_CHITIETKEHOACH? original, bool statusOnly = false)
        {
            if (original is not null)
            {
                var state = original.TrangThai;
                var draft = state is null or "" or "Nháp" or "Chưa xác nhận" or "Chờ xác nhận" or "Đang lập" or "Dự kiến" or "Chờ thực hiện";
                if (!draft && (!statusOnly)) throw new InvalidOperationException("Bản ghi đã xác nhận hoặc đã hủy; hãy lập bản mới.");
                if (!draft && record.TrangThai == "Nháp") throw new InvalidOperationException("Không chuyển chứng từ đã xác nhận về nháp.");
                if (new DAL_DICHVUTHUCHIEN(tx).layTheoChiTietKeHoach(original.MaCTKH).Any(row => row.TrangThai == "Đã xác nhận")) throw new InvalidOperationException("Bước kế hoạch đã có dịch vụ xác nhận, không thể thay đổi.");
                if (new DAL_DICHVUTHUCHIEN(tx).layTheoChiTietKeHoach(original.MaCTKH).Any(row => row.MaDV != record.MaDV))
                    throw new InvalidOperationException("Bước kế hoạch đã có dịch vụ thực hiện, không thể đổi dịch vụ.");
            }
            var parent = new DAL_KEHOACHDIEUTRI(tx).layKeHoachDieuTriTheoMa(record.MaKHDT);
            if (parent is null || parent.TrangThai != "Nháp") throw new InvalidOperationException("Chỉ sửa nội dung trong bản nháp có liên kết hợp lệ.");
            if (original is not null && record.MaKHDT != original.MaKHDT) throw new InvalidOperationException("Không chuyển bản ghi đã lưu sang hồ sơ khác.");
        }
    }
}
