// AI hỗ trợ: bổ sung chú thích tiếng Việt;
using System.Data;
using Dapper;
using ET;

namespace DAL
{
    /// <summary>
    /// Tầng truy cập SQL cho lần khám trong bảng LICH_SU_KHAM_BENH. Lưu triệu chứng, chẩn đoán và kế hoạch xử lý từ phiếu khám; đặt ngày xác nhận và giữ nguyên bản đã rời trạng thái nháp. Dapper ánh xạ dòng SQL sang ET; ThucHien dùng giao dịch được truyền vào hoặc tự quản lý giao dịch cục bộ.
    /// </summary>
    public class DAL_LICHSUKHAMBENH : dbconnect
    {
        /// <summary>
        /// Tạo DAL quản lý lần khám, chuyển giao dịch tùy chọn cho dbconnect để nhiều thao tác có thể dùng chung kết nối và phạm vi lưu.
        /// </summary>
        /// <param name = "transaction">Giao dịch của nơi gọi; null để hạ tầng tham gia giao dịch môi trường hoặc tự mở giao dịch cục bộ. Nếu truyền vào, nơi gọi quản lý commit/rollback.</param>
        public DAL_LICHSUKHAMBENH(IDbTransaction? transaction = null) : base(transaction)
        {
        }

        /// <summary>
        /// Đọc toàn bộ lần khám từ dbo.LICH_SU_KHAM_BENH, Dapper ánh xạ từng dòng sang ET, sắp theo mã; ToArray nạp hết dữ liệu trước khi hạ tầng kết thúc phạm vi kết nối/giao dịch.
        /// </summary>
        /// <returns>Danh sách ET đã vật hóa; không có bản ghi thì trả danh sách rỗng.</returns>
        public IEnumerable<ET_LICHSUKHAMBENH> layToanBoLichSuKhamBenh()
        {
            return ThucHien((connection, transaction) =>
            {
                return connection.Query<ET_LICHSUKHAMBENH>("SELECT * FROM dbo.[LICH_SU_KHAM_BENH] ORDER BY [MaLS]", transaction: transaction).ToArray();
            });
        }

        /// <summary>
        /// Tìm lần khám theo khóa trong dbo.LICH_SU_KHAM_BENH; mã được truyền bằng tham số SQL và QuerySingleOrDefault ánh xạ tối đa một bản ghi sang ET.
        /// </summary>
        /// <param name = "ma">Mã khóa chính của bản ghi cần đọc, sửa hoặc xóa; truyền bằng tham số SQL.</param>
        /// <returns>ET của bản ghi tìm thấy, hoặc null nếu mã không tồn tại.</returns>
        public ET_LICHSUKHAMBENH? layLichSuKhamBenhTheoMa(string ma)
        {
            return ThucHien((connection, transaction) =>
            {
                return connection.QuerySingleOrDefault<ET_LICHSUKHAMBENH>("SELECT * FROM dbo.[LICH_SU_KHAM_BENH] WHERE [MaLS]=@Ma", new { Ma = ma }, transaction);
            });
        }

        /// <summary>
        /// Thêm lần khám: cấp mã có khóa ngay vào duLieu, kiểm tra nghiệp vụ, chuẩn hóa và kiểm tra ET theo metadata SQL rồi INSERT có tham số vào dbo.LICH_SU_KHAM_BENH trong cùng giao dịch.
        /// </summary>
        /// <param name = "duLieu">ET chứa dữ liệu cần lưu; thêm mới sẽ được gán mã, và kiểm tra metadata có thể chuẩn hóa lại các giá trị ngay trên đối tượng.</param>
        /// <returns>true sau khi INSERT thành công; lỗi dữ liệu/SQL được phát ra bằng ngoại lệ. Giao dịch ngoài chỉ commit khi nơi gọi quyết định.</returns>
        public bool themLichSuKhamBenh(ET_LICHSUKHAMBENH duLieu)
        {
            return ThucHien((connection, transaction) =>
            {
                duLieu.MaLS = TaoMa(connection, transaction, "LICH_SU_KHAM_BENH", "MaLS");
                KiemTraNghiepVu(duLieu, null);
                KiemTra<ET_LICHSUKHAMBENH>(connection, transaction, "LICH_SU_KHAM_BENH", duLieu, null, null);
                connection.Execute("""
                    INSERT INTO dbo.[LICH_SU_KHAM_BENH] ([MaLS],[MaPhieu],[MaBS],
                        [MaPhong],[ThoiGianKham],[TrieuChung],
                        [ChanDoan],[KeHoachXuLy],[TrangThai],
                        [NgayXacNhan],[GhiChu])
                    VALUES (@MaLS,@MaPhieu,
                        @MaBS,@MaPhong,@ThoiGianKham,
                        @TrieuChung,@ChanDoan,@KeHoachXuLy,
                        @TrangThai,@NgayXacNhan,@GhiChu)
                    """, duLieu, transaction);
                return true;
            });
        }

        /// <summary>
        /// Sửa lần khám: đọc và giữ khóa UPDLOCK/HOLDLOCK, báo lỗi nếu bản không còn, kiểm tra nghiệp vụ, chuẩn hóa ET và đối chiếu banGoc để phát hiện sửa đồng thời. Khi chỉ đổi trạng thái, chặn thay đổi các trường ngoài TrangThai/NgayXacNhan/NgayCapNhat/LyDoThayDoi, sau đó UPDATE có tham số trong cùng giao dịch.
        /// </summary>
        /// <param name = "duLieu">ET chứa dữ liệu cần lưu; thêm mới sẽ được gán mã, và kiểm tra metadata có thể chuẩn hóa lại các giá trị ngay trên đối tượng.</param>
        /// <param name = "banGoc">Bản ET đã đọc trước khi thao tác; có giá trị để so với SQL và chặn ghi đè/xóa khi bản đã thay đổi, null để bỏ đối chiếu bản gốc.</param>
        /// <param name = "chiDoiTrangThai">true để cấm thay đổi trường nội dung ngoài nhóm trường trạng thái cho phép; không tự đảm bảo mọi chuyển trạng thái đều hợp lệ.</param>
        /// <returns>true khi UPDATE chạy thành công; bản bị mất/thay đổi hoặc dữ liệu sai gây ngoại lệ. Không trả false để biểu thị lỗi.</returns>
        public bool suaLichSuKhamBenh(ET_LICHSUKHAMBENH duLieu, ET_LICHSUKHAMBENH? banGoc = null, bool chiDoiTrangThai = false)
        {
            return ThucHien((connection, transaction) =>
            {
                var hienTai = connection.QuerySingleOrDefault<ET_LICHSUKHAMBENH>("SELECT * FROM dbo.[LICH_SU_KHAM_BENH] WITH(UPDLOCK,HOLDLOCK) WHERE [MaLS]=@Ma", new { Ma = duLieu.MaLS }, transaction);
                if (hienTai is null)
                {
                    throw new InvalidOperationException("Bản ghi không còn tồn tại. Vui lòng tải lại.");
                }

                KiemTraNghiepVu(duLieu, hienTai);
                KiemTra(connection, transaction, "LICH_SU_KHAM_BENH", duLieu, hienTai, banGoc);
                if (chiDoiTrangThai)
                {
                    KiemTraChiDoiTrangThai(duLieu, hienTai);
                }

                connection.Execute("""
                    UPDATE dbo.[LICH_SU_KHAM_BENH]
                    SET [MaPhieu]=@MaPhieu,[MaBS]=@MaBS,[MaPhong]=@MaPhong,
                        [ThoiGianKham]=@ThoiGianKham,[TrieuChung]=@TrieuChung,[ChanDoan]=@ChanDoan,
                        [KeHoachXuLy]=@KeHoachXuLy,[TrangThai]=@TrangThai,[NgayXacNhan]=@NgayXacNhan,
                        [GhiChu]=@GhiChu
                    WHERE [MaLS]=@MaLS
                    """, duLieu, transaction);
                return true;
            });
        }

        /// <summary>
        /// Xóa lần khám: đọc bản hiện tại với UPDLOCK/HOLDLOCK, báo lỗi nếu đã mất, kiểm tra nghiệp vụ, so banGoc khi có rồi DELETE theo mã trong dbo.LICH_SU_KHAM_BENH; toàn bộ thao tác giữ cùng giao dịch và lỗi ràng buộc SQL được phát ra.
        /// </summary>
        /// <param name = "ma">Mã khóa chính của bản ghi cần đọc, sửa hoặc xóa; truyền bằng tham số SQL.</param>
        /// <param name = "banGoc">Bản ET đã đọc trước khi thao tác; có giá trị để so với SQL và chặn ghi đè/xóa khi bản đã thay đổi, null để bỏ đối chiếu bản gốc.</param>
        /// <returns>true khi DELETE chạy thành công; không tồn tại, xung đột hoặc vi phạm SQL gây ngoại lệ.</returns>
        public bool xoaLichSuKhamBenh(string ma, ET_LICHSUKHAMBENH? banGoc = null)
        {
            return ThucHien((connection, transaction) =>
            {
                var hienTai = connection.QuerySingleOrDefault<ET_LICHSUKHAMBENH>("SELECT * FROM dbo.[LICH_SU_KHAM_BENH] WITH(UPDLOCK,HOLDLOCK) WHERE [MaLS]=@Ma", new { Ma = ma }, transaction);
                if (hienTai is null)
                {
                    throw new InvalidOperationException("Bản ghi không còn tồn tại. Vui lòng tải lại.");
                }

                KiemTraNghiepVu(hienTai, hienTai);
                KiemTra(connection, transaction, "LICH_SU_KHAM_BENH", hienTai, hienTai, banGoc);
                connection.Execute("DELETE FROM dbo.[LICH_SU_KHAM_BENH] WHERE [MaLS]=@Ma", new { Ma = ma }, transaction);
                return true;
            });
        }

        /// <summary>
        /// Đọc lần khám rồi gọi DAL phiếu khám trong cùng giao dịch để lần theo hồ sơ và xác định bệnh nhân; thiếu liên kết thì trả chuỗi rỗng.
        /// </summary>
        /// <param name = "maLS">Mã lần khám liên kết; dùng để xác định bệnh nhân hoặc nguồn thu hợp lệ.</param>
        /// <returns>Mã bệnh nhân, hoặc chuỗi rỗng nếu không xác định được từ chuỗi liên kết.</returns>
        public string layMaBenhNhan(string maLS)
        {
            return ThucHien((db, tx) =>
            {
                var visit = new DAL_LICHSUKHAMBENH(tx).layLichSuKhamBenhTheoMa(maLS);
                return visit is null ? "" : new DAL_PHIEUKHAMBENH(tx).layMaBenhNhan(visit.MaPhieu);
            });
        }

        /// <summary>
        /// Đặt NgayXacNhan khi trạng thái mới là Đã xác nhận, xóa ngày khi chưa xác nhận; chặn sửa/xóa bản hiện tại ngoài nhóm trạng thái nháp và cấm chuyển sang phiếu khám khác.
        /// </summary>
        /// <param name = "duLieu">ET cần kiểm tra hoặc cập nhật, chứa các mã liên kết và trạng thái mới.</param>
        /// <param name = "banHienTai">Bản hiện tại đã đọc từ SQL; null khi thêm mới, dùng để bảo vệ nội dung/trạng thái và liên kết đã lưu.</param>
        private void KiemTraNghiepVu(ET_LICHSUKHAMBENH duLieu, ET_LICHSUKHAMBENH? banHienTai)
        {
            duLieu.NgayXacNhan = duLieu.TrangThai == "Đã xác nhận" ? duLieu.NgayXacNhan ?? DateTime.Now : null;
            if (banHienTai is not null)
            {
                var trangThaiCu = banHienTai.TrangThai;
                var laBanNhap = trangThaiCu is null or "" or "Nháp" or "Chưa xác nhận" or "Chờ xác nhận" or "Đang lập" or "Dự kiến" or "Chờ thực hiện";
                if (!laBanNhap)
                {
                    throw new InvalidOperationException("Bản ghi đã xác nhận; hãy lập bản mới.");
                }

                if (!laBanNhap && duLieu.TrangThai == "Nháp")
                {
                    throw new InvalidOperationException("Không chuyển chứng từ đã xác nhận về nháp.");
                }
            }

            if (banHienTai is not null && duLieu.MaPhieu != banHienTai.MaPhieu)
            {
                throw new InvalidOperationException("Không chuyển bản ghi đã lưu sang hồ sơ khác.");
            }
        }
    }
}
