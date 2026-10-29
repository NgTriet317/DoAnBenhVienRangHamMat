// AI hỗ trợ: bổ sung chú thích tiếng Việt;
using System.Data;
using Dapper;
using ET;

namespace DAL
{
    /// <summary>
    /// Tầng truy cập SQL cho lịch làm việc trong bảng LICH_LAM_VIEC. Lưu nhân viên, ngày, ca, phòng, hiện diện và giờ vào ra để tổ chức hoạt động khám. Dapper ánh xạ dòng SQL sang ET; ThucHien dùng giao dịch được truyền vào hoặc tự quản lý giao dịch cục bộ.
    /// </summary>
    public class DAL_LICHLAMVIEC : dbconnect
    {
        /// <summary>
        /// Tạo DAL quản lý lịch làm việc, chuyển giao dịch tùy chọn cho dbconnect để nhiều thao tác có thể dùng chung kết nối và phạm vi lưu.
        /// </summary>
        /// <param name = "transaction">Giao dịch của nơi gọi; null để hạ tầng tham gia giao dịch môi trường hoặc tự mở giao dịch cục bộ. Nếu truyền vào, nơi gọi quản lý commit/rollback.</param>
        public DAL_LICHLAMVIEC(IDbTransaction? transaction = null) : base(transaction)
        {
        }

        /// <summary>
        /// Đọc toàn bộ lịch làm việc từ dbo.LICH_LAM_VIEC, Dapper ánh xạ từng dòng sang ET, sắp theo mã; ToArray nạp hết dữ liệu trước khi hạ tầng kết thúc phạm vi kết nối/giao dịch.
        /// </summary>
        /// <returns>Danh sách ET đã vật hóa; không có bản ghi thì trả danh sách rỗng.</returns>
        public IEnumerable<ET_LICHLAMVIEC> layToanBoLichLamViec()
        {
            return ThucHien((connection, transaction) =>
            {
                return connection.Query<ET_LICHLAMVIEC>("SELECT * FROM dbo.[LICH_LAM_VIEC] ORDER BY [MaLLV]", transaction: transaction).ToArray();
            });
        }

        /// <summary>
        /// Tìm lịch làm việc theo khóa trong dbo.LICH_LAM_VIEC; mã được truyền bằng tham số SQL và QuerySingleOrDefault ánh xạ tối đa một bản ghi sang ET.
        /// </summary>
        /// <param name = "ma">Mã khóa chính của bản ghi cần đọc, sửa hoặc xóa; truyền bằng tham số SQL.</param>
        /// <returns>ET của bản ghi tìm thấy, hoặc null nếu mã không tồn tại.</returns>
        public ET_LICHLAMVIEC? layLichLamViecTheoMa(string ma)
        {
            return ThucHien((connection, transaction) =>
            {
                return connection.QuerySingleOrDefault<ET_LICHLAMVIEC>("SELECT * FROM dbo.[LICH_LAM_VIEC] WHERE [MaLLV]=@Ma", new { Ma = ma }, transaction);
            });
        }

        /// <summary>
        /// Thêm lịch làm việc: cấp mã có khóa ngay vào duLieu, chuẩn hóa và kiểm tra ET theo metadata SQL rồi INSERT có tham số vào dbo.LICH_LAM_VIEC trong cùng giao dịch.
        /// </summary>
        /// <param name = "duLieu">ET chứa dữ liệu cần lưu; thêm mới sẽ được gán mã, và kiểm tra metadata có thể chuẩn hóa lại các giá trị ngay trên đối tượng.</param>
        /// <returns>true sau khi INSERT thành công; lỗi dữ liệu/SQL được phát ra bằng ngoại lệ. Giao dịch ngoài chỉ commit khi nơi gọi quyết định.</returns>
        public bool themLichLamViec(ET_LICHLAMVIEC duLieu)
        {
            return ThucHien((connection, transaction) =>
            {
                KhoaPhanCong(connection, transaction);
                KiemTraKhongDangNghi(connection, transaction, duLieu.MaNV, duLieu.NgayLam);
                duLieu.MaLLV = TaoMa(connection, transaction, "LICH_LAM_VIEC", "MaLLV");
                KiemTra<ET_LICHLAMVIEC>(connection, transaction, "LICH_LAM_VIEC", duLieu, null, null);
                connection.Execute("""
                INSERT INTO dbo.[LICH_LAM_VIEC] ([MaLLV],[MaNV],[MaCa],
                    [NgayLam],[MaPhong],[TrangThai],
                    [TrangThaiHienDien],[GioVao],[GioRa],
                    [GhiChu])
                VALUES (@MaLLV,@MaNV,@MaCa,
                    @NgayLam,@MaPhong,@TrangThai,
                    @TrangThaiHienDien,@GioVao,@GioRa,
                    @GhiChu)
                """, duLieu, transaction);
                return true;
            });
        }

        /// <summary>
        /// Sửa lịch làm việc: đọc và giữ khóa UPDLOCK/HOLDLOCK, báo lỗi nếu bản không còn, chuẩn hóa ET và đối chiếu banGoc để phát hiện sửa đồng thời. Khi chỉ đổi trạng thái, chặn thay đổi các trường ngoài TrangThai/NgayXacNhan/NgayCapNhat/LyDoThayDoi, sau đó UPDATE có tham số trong cùng giao dịch.
        /// </summary>
        /// <param name = "duLieu">ET chứa dữ liệu cần lưu; thêm mới sẽ được gán mã, và kiểm tra metadata có thể chuẩn hóa lại các giá trị ngay trên đối tượng.</param>
        /// <param name = "banGoc">Bản ET đã đọc trước khi thao tác; có giá trị để so với SQL và chặn ghi đè/xóa khi bản đã thay đổi, null để bỏ đối chiếu bản gốc.</param>
        /// <param name = "chiDoiTrangThai">true để cấm thay đổi trường nội dung ngoài nhóm trường trạng thái cho phép; không tự đảm bảo mọi chuyển trạng thái đều hợp lệ.</param>
        /// <returns>true khi UPDATE chạy thành công; bản bị mất/thay đổi hoặc dữ liệu sai gây ngoại lệ. Không trả false để biểu thị lỗi.</returns>
        public bool suaLichLamViec(ET_LICHLAMVIEC duLieu, ET_LICHLAMVIEC? banGoc = null, bool chiDoiTrangThai = false)
        {
            return ThucHien((connection, transaction) =>
            {
                var hienTai = connection.QuerySingleOrDefault<ET_LICHLAMVIEC>("SELECT * FROM dbo.[LICH_LAM_VIEC] WITH(UPDLOCK,HOLDLOCK) WHERE [MaLLV]=@Ma", new { Ma = duLieu.MaLLV }, transaction);
                if (hienTai is null)
                {
                    throw new InvalidOperationException("Bản ghi không còn tồn tại. Vui lòng tải lại.");
                }

                KhoaPhanCong(connection, transaction);
                if (duLieu.TrangThai != "Hủy" && duLieu.TrangThai != "Từ chối" &&
                    duLieu.TrangThai != "Nghỉ phép" && duLieu.TrangThai != "Nghỉ việc")
                    KiemTraKhongDangNghi(connection, transaction, duLieu.MaNV, duLieu.NgayLam);
                KiemTra(connection, transaction, "LICH_LAM_VIEC", duLieu, hienTai, banGoc);
                if (chiDoiTrangThai)
                {
                    KiemTraChiDoiTrangThai(duLieu, hienTai);
                }

                connection.Execute("""
                UPDATE dbo.[LICH_LAM_VIEC]
                SET [MaNV]=@MaNV,[MaCa]=@MaCa,[NgayLam]=@NgayLam,
                    [MaPhong]=@MaPhong,[TrangThai]=@TrangThai,[TrangThaiHienDien]=@TrangThaiHienDien,
                    [GioVao]=@GioVao,[GioRa]=@GioRa,[GhiChu]=@GhiChu
                WHERE [MaLLV]=@MaLLV
                """, duLieu, transaction);
                return true;
            });
        }

        /// <summary>
        /// Xóa lịch làm việc: đọc bản hiện tại với UPDLOCK/HOLDLOCK, báo lỗi nếu đã mất, so banGoc khi có rồi DELETE theo mã trong dbo.LICH_LAM_VIEC; toàn bộ thao tác giữ cùng giao dịch và lỗi ràng buộc SQL được phát ra.
        /// </summary>
        /// <param name = "ma">Mã khóa chính của bản ghi cần đọc, sửa hoặc xóa; truyền bằng tham số SQL.</param>
        /// <param name = "banGoc">Bản ET đã đọc trước khi thao tác; có giá trị để so với SQL và chặn ghi đè/xóa khi bản đã thay đổi, null để bỏ đối chiếu bản gốc.</param>
        /// <returns>true khi DELETE chạy thành công; không tồn tại, xung đột hoặc vi phạm SQL gây ngoại lệ.</returns>
        public bool xoaLichLamViec(string ma, ET_LICHLAMVIEC? banGoc = null)
        {
            return ThucHien((connection, transaction) =>
            {
                KhoaPhanCong(connection, transaction);
                var hienTai = connection.QuerySingleOrDefault<ET_LICHLAMVIEC>("SELECT * FROM dbo.[LICH_LAM_VIEC] WITH(UPDLOCK,HOLDLOCK) WHERE [MaLLV]=@Ma", new { Ma = ma }, transaction);
                if (hienTai is null)
                {
                    throw new InvalidOperationException("Bản ghi không còn tồn tại. Vui lòng tải lại.");
                }

                KiemTra(connection, transaction, "LICH_LAM_VIEC", hienTai, hienTai, banGoc);
                connection.Execute("DELETE FROM dbo.[LICH_LAM_VIEC] WHERE [MaLLV]=@Ma", new { Ma = ma }, transaction);
                return true;
            });
        }
        private void KhoaPhanCong(IDbConnection connection, IDbTransaction? transaction)
        {
            int k = connection.ExecuteScalar<int>(@"DECLARE @r int;
                EXEC @r=sys.sp_getapplock @Resource=N'RHM:PhanCong',@LockMode='Exclusive',
                @LockOwner='Transaction',@LockTimeout=10000; SELECT @r",transaction:transaction);
            if (k<0) throw new InvalidOperationException("Lịch đang được cập nhật. Hãy thử lại.");
        }

        private void KiemTraKhongDangNghi(IDbConnection connection, IDbTransaction? transaction,
            string maNV, DateTime ngayLam)
        {
            int soDon = connection.ExecuteScalar<int>(@"SELECT COUNT(*) FROM dbo.DON_XIN_NGHI WITH(UPDLOCK,HOLDLOCK)
                WHERE MaNV=@MaNV AND TrangThaiDuyet=N'Đã duyệt' AND TuNgay<=@Ngay
                    AND (DenNgay>=@Ngay OR GhiChu LIKE N'%Loại đơn: Nghỉ việc%')",
                new { MaNV=maNV, Ngay=ngayLam.Date }, transaction);
            if (soDon>0)
                throw new InvalidOperationException("Nhân viên đang trong thời gian nghỉ được duyệt, không thể phân công ca làm.");
        }

        // Các hàm phục vụ đăng ký, duyệt ca và phân phòng theo tuần.
        public List<ET_LICHLAMVIEC> layLichTheoTuan(DateTime dauTuan)
        {
            return ThucHien((connection, transaction) => connection.Query<ET_LICHLAMVIEC>(
                "SELECT * FROM dbo.LICH_LAM_VIEC WHERE NgayLam >= @TuNgay AND NgayLam < @DenNgay",
                new { TuNgay = dauTuan.Date, DenNgay = dauTuan.Date.AddDays(7) }, transaction).ToList());
        }

        public bool dangKyCa(ET_LICHLAMVIEC lich)
        {
            return ThucHien((connection, transaction) =>
            {
                KhoaPhanCong(connection, transaction);
                KiemTraKhongDangNghi(connection, transaction, lich.MaNV, lich.NgayLam);
                lich.MaLLV = TaoMa(connection, transaction, "LICH_LAM_VIEC", "MaLLV");
                return connection.Execute(@"INSERT INTO dbo.LICH_LAM_VIEC
                    (MaLLV, MaNV, MaCa, NgayLam, MaPhong, TrangThai, TrangThaiHienDien, GioVao, GioRa, GhiChu)
                    SELECT @MaLLV, @MaNV, @MaCa, @NgayLam, NULL, N'Chờ xác nhận', @TrangThaiHienDien, NULL, NULL, @GhiChu
                    WHERE EXISTS (SELECT 1 FROM dbo.CA_THEO_NGAY WHERE NgayLam=@NgayLam AND MaCa=@MaCa)
                      AND NOT EXISTS (SELECT 1 FROM dbo.LICH_LAM_VIEC WITH(UPDLOCK,HOLDLOCK)
                                      WHERE MaNV=@MaNV AND MaCa=@MaCa AND NgayLam=@NgayLam
                                        AND TrangThai<>N'Từ chối')", lich, transaction) > 0;
            });
        }

        // Đăng ký nhanh các ca còn hợp lệ trong tuần, lưu trong một giao dịch.
        // Không thay đổi quy tắc nghỉ phép / nghỉ việc của hệ thống.
        public (int ThanhCong, int DaCo, int TrungGio, int DangNghi) dangKyTuan(string maNV, DateTime dauTuan)
        {
            return ThucHien((connection, transaction) =>
            {
                KhoaPhanCong(connection, transaction);
                int thanhCong = 0, daCo = 0, trungGio = 0, dangNghi = 0;
                var danhSachCa = connection.Query<ET_CA_THEO_NGAY>(@"
                    SELECT DISTINCT NgayLam, MaCa FROM dbo.CA_THEO_NGAY
                    WHERE NgayLam >= @TuNgay AND NgayLam < @DenNgay AND NgayLam >= @HomNay
                    ORDER BY NgayLam, MaCa",
                    new { TuNgay = dauTuan.Date, DenNgay = dauTuan.Date.AddDays(7), HomNay = DateTime.Today },
                    transaction).ToList();

                foreach (ET_CA_THEO_NGAY ca in danhSachCa)
                {
                    var thamSo = new { MaNV = maNV, ca.MaCa, NgayLam = ca.NgayLam.Date };
                    // Chỉ ca bị từ chối mới cho phép đăng ký lại.
                    if (connection.ExecuteScalar<int>(@"
                        SELECT COUNT(*) FROM dbo.LICH_LAM_VIEC WITH(UPDLOCK,HOLDLOCK)
                        WHERE MaNV=@MaNV AND MaCa=@MaCa AND NgayLam=@NgayLam
                          AND TrangThai<>N'Từ chối'", thamSo, transaction) > 0)
                    {
                        daCo++;
                        continue;
                    }

                    // Giữ đúng nguyên tắc đơn nghỉ đang sử dụng trong đăng ký một ca:
                    // nghỉ phép chỉ chặn trong khoảng ngày, nghỉ việc chặn từ ngày hiệu lực.
                    if (connection.ExecuteScalar<int>(@"
                        SELECT COUNT(*) FROM dbo.DON_XIN_NGHI WITH(UPDLOCK,HOLDLOCK)
                        WHERE MaNV=@MaNV AND TrangThaiDuyet=N'Đã duyệt' AND TuNgay<=@NgayLam
                          AND (DenNgay>=@NgayLam OR GhiChu LIKE N'%Loại đơn: Nghỉ việc%')",
                        thamSo, transaction) > 0)
                    {
                        dangNghi++;
                        continue;
                    }

                    // Tránh đăng ký nhiều ca giao nhau trong cùng ngày (cả ca đang chờ duyệt).
                    if (connection.ExecuteScalar<int>(@"
                        SELECT COUNT(*) FROM dbo.LICH_LAM_VIEC lich WITH(UPDLOCK,HOLDLOCK)
                        JOIN dbo.CA_LAM caKhac ON caKhac.MaCa=lich.MaCa
                        JOIN dbo.CA_LAM caMoi ON caMoi.MaCa=@MaCa
                        WHERE lich.MaNV=@MaNV AND lich.NgayLam=@NgayLam
                          AND lich.TrangThai IN (N'Chờ xác nhận',N'Đã xác nhận')
                          AND caKhac.GioBatDau < caMoi.GioKetThuc
                          AND caMoi.GioBatDau < caKhac.GioKetThuc", thamSo, transaction) > 0)
                    {
                        trungGio++;
                        continue;
                    }

                    string maLLV = TaoMa(connection, transaction, "LICH_LAM_VIEC", "MaLLV");
                    thanhCong += connection.Execute(@"
                        INSERT INTO dbo.LICH_LAM_VIEC
                            (MaLLV, MaNV, MaCa, NgayLam, MaPhong, TrangThai,
                             TrangThaiHienDien, GioVao, GioRa, GhiChu)
                        VALUES (@MaLLV, @MaNV, @MaCa, @NgayLam, NULL, N'Chờ xác nhận',
                                N'Chưa chấm công', NULL, NULL, NULL)",
                        new { MaLLV = maLLV, MaNV = maNV, ca.MaCa, NgayLam = ca.NgayLam.Date }, transaction);
                }
                return (thanhCong, daCo, trungGio, dangNghi);
            });
        }

        public bool duyetCa(string maLLV, string maPhong)
        {
            return ThucHien((connection, transaction) =>
            {
                KhoaPhanCong(connection, transaction);
                return connection.Execute(@"
                UPDATE lich
                SET lich.TrangThai=N'Đã xác nhận', lich.MaPhong=@MaPhong
                FROM dbo.LICH_LAM_VIEC lich
                JOIN dbo.CA_LAM ca ON ca.MaCa=lich.MaCa
                WHERE lich.MaLLV=@MaLLV AND lich.TrangThai=N'Chờ xác nhận'
                AND NOT EXISTS (SELECT 1 FROM dbo.DON_XIN_NGHI don WITH(UPDLOCK,HOLDLOCK)
                    WHERE don.MaNV=lich.MaNV AND don.TrangThaiDuyet=N'Đã duyệt'
                      AND don.TuNgay<=lich.NgayLam
                      AND (don.DenNgay>=lich.NgayLam OR don.GhiChu LIKE N'%Loại đơn: Nghỉ việc%'))
                AND EXISTS (SELECT 1 FROM dbo.PHONG WHERE MaPhong=@MaPhong AND TrangThai=N'Đang hoạt động')
                AND NOT EXISTS (
                    SELECT 1 FROM dbo.LICH_LAM_VIEC x WITH(UPDLOCK,HOLDLOCK)
                    JOIN dbo.CA_LAM caKhac ON caKhac.MaCa=x.MaCa
                    WHERE x.NgayLam=lich.NgayLam AND x.MaLLV<>lich.MaLLV
                      AND x.TrangThai=N'Đã xác nhận'
                      AND x.MaNV=lich.MaNV
                      AND caKhac.GioBatDau < ca.GioKetThuc
                      AND ca.GioBatDau < caKhac.GioKetThuc
                )", new { MaLLV=maLLV, MaPhong=maPhong }, transaction) > 0;
            });
        }

        public bool tuChoiCa(string maLLV, string lyDo)
        {
            return ThucHien((connection, transaction) => connection.Execute(
                "UPDATE dbo.LICH_LAM_VIEC SET TrangThai=N'Từ chối', GhiChu=@LyDo WHERE MaLLV=@MaLLV AND TrangThai=N'Chờ xác nhận'",
                new { MaLLV=maLLV, LyDo=lyDo }, transaction) > 0);
        }

        public bool huyDangKy(string maLLV, string maNV)
        {
            return ThucHien((connection, transaction) => connection.Execute(
                "DELETE FROM dbo.LICH_LAM_VIEC WHERE MaLLV=@MaLLV AND MaNV=@MaNV AND TrangThai=N'Chờ xác nhận'",
                new { MaLLV=maLLV, MaNV=maNV }, transaction) > 0);
        }

    }
}
