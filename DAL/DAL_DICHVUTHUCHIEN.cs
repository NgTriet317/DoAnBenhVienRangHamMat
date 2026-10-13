// AI hỗ trợ: bổ sung chú thích tiếng Việt;
using System.Data;
using Dapper;
using ET;

namespace DAL
{
    /// <summary>
    /// Tầng truy cập SQL cho dịch vụ đã thực hiện trong bảng DICH_VU_THUC_HIEN. Ghi nhận dịch vụ thực tế, nhân viên, số lượng, giá và kết quả của lần khám; kiểm tra bước kế hoạch và giữ nguyên dịch vụ đã xác nhận. Dapper ánh xạ dòng SQL sang ET; ThucHien dùng giao dịch được truyền vào hoặc tự quản lý giao dịch cục bộ.
    /// </summary>
    public class DAL_DICHVUTHUCHIEN : dbconnect
    {
        /// <summary>
        /// Tạo DAL quản lý dịch vụ đã thực hiện, chuyển giao dịch tùy chọn cho dbconnect để nhiều thao tác có thể dùng chung kết nối và phạm vi lưu.
        /// </summary>
        /// <param name="transaction">Giao dịch của nơi gọi; null để hạ tầng tham gia giao dịch môi trường hoặc tự mở giao dịch cục bộ. Nếu truyền vào, nơi gọi quản lý commit/rollback.</param>
        public DAL_DICHVUTHUCHIEN(IDbTransaction? transaction = null) : base(transaction) { }

        /// <summary>
        /// Đọc toàn bộ dịch vụ đã thực hiện từ dbo.DICH_VU_THUC_HIEN, Dapper ánh xạ từng dòng sang ET, sắp theo mã; ToArray nạp hết dữ liệu trước khi hạ tầng kết thúc phạm vi kết nối/giao dịch.
        /// </summary>
        /// <returns>Danh sách ET đã vật hóa; không có bản ghi thì trả danh sách rỗng.</returns>
        public IEnumerable<ET_DICHVUTHUCHIEN> layToanBoDichVuThucHien()
        {
            return ThucHien((connection, transaction) =>
                connection.Query<ET_DICHVUTHUCHIEN>("SELECT * FROM dbo.[DICH_VU_THUC_HIEN] ORDER BY [MaDVThucHien]", transaction: transaction).ToArray());
        }

        /// <summary>
        /// Tìm dịch vụ đã thực hiện theo khóa trong dbo.DICH_VU_THUC_HIEN; mã được truyền bằng tham số SQL và QuerySingleOrDefault ánh xạ tối đa một bản ghi sang ET.
        /// </summary>
        /// <param name="ma">Mã khóa chính của bản ghi cần đọc, sửa hoặc xóa; truyền bằng tham số SQL.</param>
        /// <returns>ET của bản ghi tìm thấy, hoặc null nếu mã không tồn tại.</returns>
        public ET_DICHVUTHUCHIEN? layDichVuThucHienTheoMa(string ma)
        {
            return ThucHien((connection, transaction) =>
                connection.QuerySingleOrDefault<ET_DICHVUTHUCHIEN>("SELECT * FROM dbo.[DICH_VU_THUC_HIEN] WHERE [MaDVThucHien]=@Ma", new { Ma = ma }, transaction));
        }

        /// <summary>
        /// Thêm dịch vụ đã thực hiện: cấp mã có khóa ngay vào duLieu, kiểm tra nghiệp vụ, chuẩn hóa và kiểm tra ET theo metadata SQL, kiểm tra liên kết rồi INSERT có tham số vào dbo.DICH_VU_THUC_HIEN trong cùng giao dịch.
        /// </summary>
        /// <param name="duLieu">ET chứa dữ liệu cần lưu; thêm mới sẽ được gán mã, và kiểm tra metadata có thể chuẩn hóa lại các giá trị ngay trên đối tượng.</param>
        /// <returns>true sau khi INSERT thành công; lỗi dữ liệu/SQL được phát ra bằng ngoại lệ. Giao dịch ngoài chỉ commit khi nơi gọi quyết định.</returns>
        public bool themDichVuThucHien(ET_DICHVUTHUCHIEN duLieu)
        {
            return ThucHien((connection, transaction) =>
            {
                duLieu.MaDVThucHien = TaoMa(connection, transaction, "DICH_VU_THUC_HIEN", "MaDVThucHien");
                KiemTraNghiepVu(connection, transaction, duLieu, null);
                KiemTra<ET_DICHVUTHUCHIEN>(connection, transaction, "DICH_VU_THUC_HIEN", duLieu, null, null);
                KiemTraLienKet(transaction, duLieu);
                connection.Execute("INSERT INTO dbo.[DICH_VU_THUC_HIEN] ([MaDVThucHien],[MaLS],[MaCTKH],[MaDV],[MaNVThucHien],[ThoiGianThucHien],[SoLuong],[DonGia],[KetQua],[TrangThai]) VALUES (@MaDVThucHien,@MaLS,@MaCTKH,@MaDV,@MaNVThucHien,@ThoiGianThucHien,@SoLuong,@DonGia,@KetQua,@TrangThai)", duLieu, transaction);
                return true;
            });
        }

        /// <summary>
        /// Sửa dịch vụ đã thực hiện: đọc và giữ khóa UPDLOCK/HOLDLOCK, báo lỗi nếu bản không còn, kiểm tra nghiệp vụ, chuẩn hóa ET và đối chiếu banGoc để phát hiện sửa đồng thời. Khi chỉ đổi trạng thái, chặn thay đổi các trường ngoài TrangThai/NgayXacNhan/NgayCapNhat/LyDoThayDoi; kiểm tra lại liên kết, sau đó UPDATE có tham số trong cùng giao dịch.
        /// </summary>
        /// <param name="duLieu">ET chứa dữ liệu cần lưu; thêm mới sẽ được gán mã, và kiểm tra metadata có thể chuẩn hóa lại các giá trị ngay trên đối tượng.</param>
        /// <param name="banGoc">Bản ET đã đọc trước khi thao tác; có giá trị để so với SQL và chặn ghi đè/xóa khi bản đã thay đổi, null để bỏ đối chiếu bản gốc.</param>
        /// <param name="chiDoiTrangThai">true để cấm thay đổi trường nội dung ngoài nhóm trường trạng thái cho phép; không tự đảm bảo mọi chuyển trạng thái đều hợp lệ.</param>
        /// <returns>true khi UPDATE chạy thành công; bản bị mất/thay đổi hoặc dữ liệu sai gây ngoại lệ. Không trả false để biểu thị lỗi.</returns>
        public bool suaDichVuThucHien(ET_DICHVUTHUCHIEN duLieu, ET_DICHVUTHUCHIEN? banGoc = null, bool chiDoiTrangThai = false)
        {
            return ThucHien((connection, transaction) =>
            {
                var hienTai = connection.QuerySingleOrDefault<ET_DICHVUTHUCHIEN>(
                    "SELECT * FROM dbo.[DICH_VU_THUC_HIEN] WITH(UPDLOCK,HOLDLOCK) WHERE [MaDVThucHien]=@Ma", new { Ma = duLieu.MaDVThucHien }, transaction)
                    ?? throw new InvalidOperationException("Bản ghi không còn tồn tại. Vui lòng tải lại.");
                KiemTraNghiepVu(connection, transaction, duLieu, hienTai, chiDoiTrangThai);
                KiemTra(connection, transaction, "DICH_VU_THUC_HIEN", duLieu, hienTai, banGoc);
                if (chiDoiTrangThai)
                    foreach (var pair in GiaTri(duLieu))
                        if (pair.Key is not "TrangThai" and not "NgayXacNhan" and not "NgayCapNhat" and not "LyDoThayDoi"
                            && !Equals(pair.Value, GiaTri(hienTai).GetValueOrDefault(pair.Key)))
                            throw new InvalidOperationException("Thao tác đổi trạng thái không được thay đổi nội dung bản ghi.");
                KiemTraLienKet(transaction, duLieu);
                connection.Execute("UPDATE dbo.[DICH_VU_THUC_HIEN] SET [MaLS]=@MaLS,[MaCTKH]=@MaCTKH,[MaDV]=@MaDV,[MaNVThucHien]=@MaNVThucHien,[ThoiGianThucHien]=@ThoiGianThucHien,[SoLuong]=@SoLuong,[DonGia]=@DonGia,[KetQua]=@KetQua,[TrangThai]=@TrangThai WHERE [MaDVThucHien]=@MaDVThucHien", duLieu, transaction);
                return true;
            });
        }

        /// <summary>
        /// Xóa dịch vụ đã thực hiện: đọc bản hiện tại với UPDLOCK/HOLDLOCK, báo lỗi nếu đã mất, kiểm tra nghiệp vụ, so banGoc khi có rồi DELETE theo mã trong dbo.DICH_VU_THUC_HIEN; toàn bộ thao tác giữ cùng giao dịch và lỗi ràng buộc SQL được phát ra.
        /// </summary>
        /// <param name="ma">Mã khóa chính của bản ghi cần đọc, sửa hoặc xóa; truyền bằng tham số SQL.</param>
        /// <param name="banGoc">Bản ET đã đọc trước khi thao tác; có giá trị để so với SQL và chặn ghi đè/xóa khi bản đã thay đổi, null để bỏ đối chiếu bản gốc.</param>
        /// <returns>true khi DELETE chạy thành công; không tồn tại, xung đột hoặc vi phạm SQL gây ngoại lệ.</returns>
        public bool xoaDichVuThucHien(string ma, ET_DICHVUTHUCHIEN? banGoc = null)
        {
            return ThucHien((connection, transaction) =>
            {
                var hienTai = connection.QuerySingleOrDefault<ET_DICHVUTHUCHIEN>(
                    "SELECT * FROM dbo.[DICH_VU_THUC_HIEN] WITH(UPDLOCK,HOLDLOCK) WHERE [MaDVThucHien]=@Ma", new { Ma = ma }, transaction)
                    ?? throw new InvalidOperationException("Bản ghi không còn tồn tại. Vui lòng tải lại.");
                KiemTraNghiepVu(connection, transaction, hienTai, hienTai);
                KiemTra(connection, transaction, "DICH_VU_THUC_HIEN", hienTai, hienTai, banGoc);
                connection.Execute("DELETE FROM dbo.[DICH_VU_THUC_HIEN] WHERE [MaDVThucHien]=@Ma", new { Ma = ma }, transaction);
                return true;
            });
        }

        /// <summary>
        /// Nếu có mã bước kế hoạch, đọc bước và kế hoạch cha, chặn bản đã Hủy, lần theo hồ sơ của hai lần khám để yêu cầu cùng bệnh nhân và kiểm tra mã dịch vụ khớp bước; không chọn bước thì bỏ qua liên kết này.
        /// </summary>
        /// <param name="tx">Giao dịch đang dùng để đọc/ghi và giữ khóa đồng bộ; được truyền tiếp cho các DAL liên quan nếu có.</param>
        /// <param name="record">ET cần kiểm tra hoặc cập nhật, chứa các mã liên kết và trạng thái mới.</param>
        private void KiemTraLienKet(IDbTransaction? tx, ET_DICHVUTHUCHIEN record)
        {
            if (string.IsNullOrEmpty(record.MaCTKH)) return;
            var step = new DAL_CHITIETKEHOACH(tx).layChiTietKeHoachTheoMa(record.MaCTKH)
                ?? throw new InvalidOperationException("Không tìm thấy bước kế hoạch.");
            var plan = new DAL_KEHOACHDIEUTRI(tx).layKeHoachDieuTriTheoMa(step.MaKHDT)
                ?? throw new InvalidOperationException("Không tìm thấy kế hoạch điều trị.");
            if (step.TrangThai == "Hủy" || plan.TrangThai == "Hủy")
                throw new InvalidOperationException("Kế hoạch hoặc bước điều trị đã hủy, không thể ghi nhận thực hiện.");
            var visits = new DAL_LICHSUKHAMBENH(tx);
            var patient = visits.layMaBenhNhan(plan.MaLS);
            if (patient.Length == 0 || patient != visits.layMaBenhNhan(record.MaLS))
                throw new InvalidOperationException("Bước kế hoạch thuộc bệnh nhân khác.");
            if (step.MaDV != record.MaDV) throw new InvalidOperationException("Dịch vụ thực hiện phải khớp bước kế hoạch đã chọn.");
        }

        /// <summary>
        /// Giữ nguyên mọi dịch vụ Đã xác nhận; ngoài nhóm trạng thái nháp chỉ cho thao tác trạng thái, cấm quay về Nháp và cấm chuyển dịch vụ đã lưu sang lần khám khác.
        /// </summary>
        /// <param name="db">Tham số kết nối của chữ ký chung; phần thân kiểm tra nghiệp vụ này không dùng trực tiếp.</param>
        /// <param name="tx">Tham số giao dịch của chữ ký chung; phần thân hàm này không truy vấn qua nó.</param>
        /// <param name="record">ET cần kiểm tra hoặc cập nhật, chứa các mã liên kết và trạng thái mới.</param>
        /// <param name="original">Bản hiện tại đã đọc từ SQL; null khi thêm mới, dùng để bảo vệ nội dung/trạng thái và liên kết đã lưu.</param>
        /// <param name="statusOnly">true khi nơi gọi chỉ đổi trạng thái; cờ ảnh hưởng việc chặn sửa nội dung ngoài trạng thái nháp, các giới hạn khác vẫn áp dụng.</param>
        private void KiemTraNghiepVu(IDbConnection db, IDbTransaction? tx, ET_DICHVUTHUCHIEN record, ET_DICHVUTHUCHIEN? original, bool statusOnly = false)
        {
            if (original is not null)
            {
                var state = original.TrangThai;
                var draft = state is null or "" or "Nháp" or "Chưa xác nhận" or "Chờ xác nhận" or "Đang lập" or "Dự kiến" or "Chờ thực hiện";
                if (state == "Đã xác nhận") throw new InvalidOperationException("Dịch vụ đã xác nhận được giữ nguyên.");
                if (!draft && (!statusOnly)) throw new InvalidOperationException("Bản ghi đã xác nhận hoặc đã hủy; hãy lập bản mới.");
                if (!draft && record.TrangThai == "Nháp") throw new InvalidOperationException("Không chuyển chứng từ đã xác nhận về nháp.");
            }
            if (original is not null && record.MaLS != original.MaLS) throw new InvalidOperationException("Không chuyển bản ghi đã lưu sang hồ sơ khác.");
        }
        /// <summary>
        /// Đọc các dịch vụ thực hiện gắn với một bước kế hoạch để kiểm tra bước đã có thực hiện hoặc đã xác nhận; vật hóa kết quả trước khi trả.
        /// </summary>
        /// <param name="maCTKH">Mã bước kế hoạch điều trị để tìm các dịch vụ đã liên kết thực hiện.</param>
        /// <returns>Danh sách ET dịch vụ thực hiện; rỗng nếu bước chưa có dịch vụ liên kết.</returns>
        public IEnumerable<ET_DICHVUTHUCHIEN> layTheoChiTietKeHoach(string maCTKH) => ThucHien((db, tx) =>
            db.Query<ET_DICHVUTHUCHIEN>("SELECT * FROM dbo.DICH_VU_THUC_HIEN WHERE MaCTKH=@maCTKH", new { maCTKH }, tx).ToArray());
    }
}
