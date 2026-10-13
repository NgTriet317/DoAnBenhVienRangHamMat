// AI hỗ trợ: bổ sung chú thích tiếng Việt;
using System.Data;
using Dapper;
using ET;

namespace DAL
{
    /// <summary>
    /// Tầng truy cập SQL cho phiếu khám bệnh trong bảng PHIEU_KHAM_BENH. Tiếp nhận lượt khám theo hồ sơ và lịch hẹn; kiểm tra đúng bệnh nhân, số thứ tự và tránh tiếp nhận trùng lịch còn hiệu lực. Dapper ánh xạ dòng SQL sang ET; ThucHien dùng giao dịch được truyền vào hoặc tự quản lý giao dịch cục bộ.
    /// </summary>
    public class DAL_PHIEUKHAMBENH : dbconnect
    {
        /// <summary>
        /// Tạo DAL quản lý phiếu khám bệnh, chuyển giao dịch tùy chọn cho dbconnect để nhiều thao tác có thể dùng chung kết nối và phạm vi lưu.
        /// </summary>
        /// <param name="transaction">Giao dịch của nơi gọi; null để hạ tầng tham gia giao dịch môi trường hoặc tự mở giao dịch cục bộ. Nếu truyền vào, nơi gọi quản lý commit/rollback.</param>
        public DAL_PHIEUKHAMBENH(IDbTransaction? transaction = null) : base(transaction) { }

        /// <summary>
        /// Đọc toàn bộ phiếu khám bệnh từ dbo.PHIEU_KHAM_BENH, Dapper ánh xạ từng dòng sang ET, sắp theo mã; ToArray nạp hết dữ liệu trước khi hạ tầng kết thúc phạm vi kết nối/giao dịch.
        /// </summary>
        /// <returns>Danh sách ET đã vật hóa; không có bản ghi thì trả danh sách rỗng.</returns>
        public IEnumerable<ET_PHIEUKHAMBENH> layToanBoPhieuKhamBenh()
        {
            return ThucHien((connection, transaction) =>
                connection.Query<ET_PHIEUKHAMBENH>("SELECT * FROM dbo.[PHIEU_KHAM_BENH] ORDER BY [MaPhieu]", transaction: transaction).ToArray());
        }

        /// <summary>
        /// Tìm phiếu khám bệnh theo khóa trong dbo.PHIEU_KHAM_BENH; mã được truyền bằng tham số SQL và QuerySingleOrDefault ánh xạ tối đa một bản ghi sang ET.
        /// </summary>
        /// <param name="ma">Mã khóa chính của bản ghi cần đọc, sửa hoặc xóa; truyền bằng tham số SQL.</param>
        /// <returns>ET của bản ghi tìm thấy, hoặc null nếu mã không tồn tại.</returns>
        public ET_PHIEUKHAMBENH? layPhieuKhamBenhTheoMa(string ma)
        {
            return ThucHien((connection, transaction) =>
                connection.QuerySingleOrDefault<ET_PHIEUKHAMBENH>("SELECT * FROM dbo.[PHIEU_KHAM_BENH] WHERE [MaPhieu]=@Ma", new { Ma = ma }, transaction));
        }

        /// <summary>
        /// Thêm phiếu khám bệnh: cấp mã có khóa ngay vào duLieu, kiểm tra nghiệp vụ, chuẩn hóa và kiểm tra ET theo metadata SQL, kiểm tra liên kết rồi INSERT có tham số vào dbo.PHIEU_KHAM_BENH trong cùng giao dịch.
        /// </summary>
        /// <param name="duLieu">ET chứa dữ liệu cần lưu; thêm mới sẽ được gán mã, và kiểm tra metadata có thể chuẩn hóa lại các giá trị ngay trên đối tượng.</param>
        /// <returns>true sau khi INSERT thành công; lỗi dữ liệu/SQL được phát ra bằng ngoại lệ. Giao dịch ngoài chỉ commit khi nơi gọi quyết định.</returns>
        public bool themPhieuKhamBenh(ET_PHIEUKHAMBENH duLieu)
        {
            return ThucHien((connection, transaction) =>
            {
                duLieu.MaPhieu = TaoMa(connection, transaction, "PHIEU_KHAM_BENH", "MaPhieu");
                KiemTraNghiepVu(connection, transaction, duLieu, null);
                KiemTra<ET_PHIEUKHAMBENH>(connection, transaction, "PHIEU_KHAM_BENH", duLieu, null, null);
                KiemTraLienKet(connection, transaction, duLieu);
                connection.Execute("INSERT INTO dbo.[PHIEU_KHAM_BENH] ([MaPhieu],[MaHS],[MaLich],[MaPhong],[MaBS],[MaNVTiepNhan],[MaDV],[SoThuTu],[ThoiGianTiepNhan],[LyDoKham],[TrangThai],[GhiChu]) VALUES (@MaPhieu,@MaHS,@MaLich,@MaPhong,@MaBS,@MaNVTiepNhan,@MaDV,@SoThuTu,@ThoiGianTiepNhan,@LyDoKham,@TrangThai,@GhiChu)", duLieu, transaction);
                return true;
            });
        }

        /// <summary>
        /// Sửa phiếu khám bệnh: đọc và giữ khóa UPDLOCK/HOLDLOCK, báo lỗi nếu bản không còn, kiểm tra nghiệp vụ, chuẩn hóa ET và đối chiếu banGoc để phát hiện sửa đồng thời. Khi chỉ đổi trạng thái, chặn thay đổi các trường ngoài TrangThai/NgayXacNhan/NgayCapNhat/LyDoThayDoi; kiểm tra lại liên kết, sau đó UPDATE có tham số trong cùng giao dịch.
        /// </summary>
        /// <param name="duLieu">ET chứa dữ liệu cần lưu; thêm mới sẽ được gán mã, và kiểm tra metadata có thể chuẩn hóa lại các giá trị ngay trên đối tượng.</param>
        /// <param name="banGoc">Bản ET đã đọc trước khi thao tác; có giá trị để so với SQL và chặn ghi đè/xóa khi bản đã thay đổi, null để bỏ đối chiếu bản gốc.</param>
        /// <param name="chiDoiTrangThai">true để cấm thay đổi trường nội dung ngoài nhóm trường trạng thái cho phép; không tự đảm bảo mọi chuyển trạng thái đều hợp lệ.</param>
        /// <returns>true khi UPDATE chạy thành công; bản bị mất/thay đổi hoặc dữ liệu sai gây ngoại lệ. Không trả false để biểu thị lỗi.</returns>
        public bool suaPhieuKhamBenh(ET_PHIEUKHAMBENH duLieu, ET_PHIEUKHAMBENH? banGoc = null, bool chiDoiTrangThai = false)
        {
            return ThucHien((connection, transaction) =>
            {
                var hienTai = connection.QuerySingleOrDefault<ET_PHIEUKHAMBENH>(
                    "SELECT * FROM dbo.[PHIEU_KHAM_BENH] WITH(UPDLOCK,HOLDLOCK) WHERE [MaPhieu]=@Ma", new { Ma = duLieu.MaPhieu }, transaction)
                    ?? throw new InvalidOperationException("Bản ghi không còn tồn tại. Vui lòng tải lại.");
                KiemTraNghiepVu(connection, transaction, duLieu, hienTai, chiDoiTrangThai);
                KiemTra(connection, transaction, "PHIEU_KHAM_BENH", duLieu, hienTai, banGoc);
                if (chiDoiTrangThai)
                    foreach (var pair in GiaTri(duLieu))
                        if (pair.Key is not "TrangThai" and not "NgayXacNhan" and not "NgayCapNhat" and not "LyDoThayDoi"
                            && !Equals(pair.Value, GiaTri(hienTai).GetValueOrDefault(pair.Key)))
                            throw new InvalidOperationException("Thao tác đổi trạng thái không được thay đổi nội dung bản ghi.");
                KiemTraLienKet(connection, transaction, duLieu);
                connection.Execute("UPDATE dbo.[PHIEU_KHAM_BENH] SET [MaHS]=@MaHS,[MaLich]=@MaLich,[MaPhong]=@MaPhong,[MaBS]=@MaBS,[MaNVTiepNhan]=@MaNVTiepNhan,[MaDV]=@MaDV,[SoThuTu]=@SoThuTu,[ThoiGianTiepNhan]=@ThoiGianTiepNhan,[LyDoKham]=@LyDoKham,[TrangThai]=@TrangThai,[GhiChu]=@GhiChu WHERE [MaPhieu]=@MaPhieu", duLieu, transaction);
                return true;
            });
        }

        /// <summary>
        /// Xóa phiếu khám bệnh: đọc bản hiện tại với UPDLOCK/HOLDLOCK, báo lỗi nếu đã mất, kiểm tra nghiệp vụ, so banGoc khi có rồi DELETE theo mã trong dbo.PHIEU_KHAM_BENH; toàn bộ thao tác giữ cùng giao dịch và lỗi ràng buộc SQL được phát ra.
        /// </summary>
        /// <param name="ma">Mã khóa chính của bản ghi cần đọc, sửa hoặc xóa; truyền bằng tham số SQL.</param>
        /// <param name="banGoc">Bản ET đã đọc trước khi thao tác; có giá trị để so với SQL và chặn ghi đè/xóa khi bản đã thay đổi, null để bỏ đối chiếu bản gốc.</param>
        /// <returns>true khi DELETE chạy thành công; không tồn tại, xung đột hoặc vi phạm SQL gây ngoại lệ.</returns>
        public bool xoaPhieuKhamBenh(string ma, ET_PHIEUKHAMBENH? banGoc = null)
        {
            return ThucHien((connection, transaction) =>
            {
                var hienTai = connection.QuerySingleOrDefault<ET_PHIEUKHAMBENH>(
                    "SELECT * FROM dbo.[PHIEU_KHAM_BENH] WITH(UPDLOCK,HOLDLOCK) WHERE [MaPhieu]=@Ma", new { Ma = ma }, transaction)
                    ?? throw new InvalidOperationException("Bản ghi không còn tồn tại. Vui lòng tải lại.");
                KiemTraNghiepVu(connection, transaction, hienTai, hienTai);
                KiemTra(connection, transaction, "PHIEU_KHAM_BENH", hienTai, hienTai, banGoc);
                connection.Execute("DELETE FROM dbo.[PHIEU_KHAM_BENH] WHERE [MaPhieu]=@Ma", new { Ma = ma }, transaction);
                return true;
            });
        }

        /// <summary>
        /// Đọc phiếu khám rồi hồ sơ bệnh án trong cùng giao dịch để xác định bệnh nhân; thiếu phiếu hoặc hồ sơ thì trả chuỗi rỗng.
        /// </summary>
        /// <param name="maPhieu">Mã phiếu khám để lần theo hồ sơ bệnh án và tìm bệnh nhân.</param>
        /// <returns>Mã bệnh nhân, hoặc chuỗi rỗng nếu không xác định được từ chuỗi liên kết.</returns>
        public string layMaBenhNhan(string maPhieu) => ThucHien((db, tx) =>
        {
            var receipt = new DAL_PHIEUKHAMBENH(tx).layPhieuKhamBenhTheoMa(maPhieu);
            return receipt is null ? "" : new DAL_HOSOBENHAN(tx).layHoSoBenhAnTheoMa(receipt.MaHS)?.MaBN ?? "";
        });

        /// <summary>
        /// Yêu cầu số thứ tự dương; nếu có mã lịch thì kiểm tra lịch/hồ sơ cùng bệnh nhân, lịch chưa hủy hoặc đổi. Đếm phiếu còn hiệu lực với UPDLOCK/HOLDLOCK để chặn tiếp nhận trùng; nếu không liên kết lịch thì kết thúc sau kiểm tra số thứ tự.
        /// </summary>
        /// <param name="db">Kết nối SQL đang mở của giao dịch do nơi gọi quản lý.</param>
        /// <param name="tx">Giao dịch đang dùng để đọc/ghi và giữ khóa đồng bộ; được truyền tiếp cho các DAL liên quan nếu có.</param>
        /// <param name="record">ET cần kiểm tra hoặc cập nhật, chứa các mã liên kết và trạng thái mới.</param>
        private void KiemTraLienKet(IDbConnection db, IDbTransaction? tx, ET_PHIEUKHAMBENH record)
        {
            if (record.SoThuTu <= 0) throw new InvalidOperationException("Số thứ tự phải là số nguyên lớn hơn 0.");
            if (string.IsNullOrEmpty(record.MaLich)) return;
            var appointment = new DAL_LICHKHAM(tx).layLichKhamTheoMa(record.MaLich)
                ?? throw new InvalidOperationException("Không tìm thấy lịch hẹn.");
            var history = new DAL_HOSOBENHAN(tx).layHoSoBenhAnTheoMa(record.MaHS)
                ?? throw new InvalidOperationException("Không tìm thấy hồ sơ bệnh án.");
            if (appointment.MaBN != history.MaBN) throw new InvalidOperationException("Lịch hẹn không thuộc bệnh nhân của hồ sơ này.");
            if (appointment.TrangThai is "Hủy" or "Đã đổi") throw new InvalidOperationException("Lịch hẹn đã hủy hoặc đã đổi. Chọn lịch hẹn còn hiệu lực.");
            if (db.ExecuteScalar<int>("SELECT COUNT(*) FROM dbo.PHIEU_KHAM_BENH WITH(UPDLOCK,HOLDLOCK) WHERE MaLich=@MaLich AND MaPhieu<>@MaPhieu AND TrangThai<>N'Hủy'", record, tx) > 0)
                throw new InvalidOperationException("Lịch hẹn đã được tiếp nhận. Hãy mở lượt tiếp nhận hiện có.");
        }

        /// <summary>
        /// Chặn sửa nội dung ngoài nhóm trạng thái nháp, chặn quay về Nháp và giữ nguyên hồ sơ bệnh án của phiếu đã lưu.
        /// </summary>
        /// <param name="db">Tham số kết nối của chữ ký chung; phần thân kiểm tra nghiệp vụ này không dùng trực tiếp.</param>
        /// <param name="tx">Tham số giao dịch của chữ ký chung; phần thân hàm này không truy vấn qua nó.</param>
        /// <param name="record">ET cần kiểm tra hoặc cập nhật, chứa các mã liên kết và trạng thái mới.</param>
        /// <param name="original">Bản hiện tại đã đọc từ SQL; null khi thêm mới, dùng để bảo vệ nội dung/trạng thái và liên kết đã lưu.</param>
        /// <param name="statusOnly">true khi nơi gọi chỉ đổi trạng thái; cờ ảnh hưởng việc chặn sửa nội dung ngoài trạng thái nháp, các giới hạn khác vẫn áp dụng.</param>
        private void KiemTraNghiepVu(IDbConnection db, IDbTransaction? tx, ET_PHIEUKHAMBENH record, ET_PHIEUKHAMBENH? original, bool statusOnly = false)
        {
            if (original is not null)
            {
                var state = original.TrangThai;
                var draft = state is null or "" or "Nháp" or "Chưa xác nhận" or "Chờ xác nhận" or "Đang lập" or "Dự kiến" or "Chờ thực hiện";
                if (!draft && (!statusOnly)) throw new InvalidOperationException("Bản ghi đã xác nhận hoặc đã hủy; hãy lập bản mới.");
                if (!draft && record.TrangThai == "Nháp") throw new InvalidOperationException("Không chuyển chứng từ đã xác nhận về nháp.");
            }
            if (original is not null && record.MaHS != original.MaHS) throw new InvalidOperationException("Không chuyển bản ghi đã lưu sang hồ sơ khác.");
        }
    }
}
