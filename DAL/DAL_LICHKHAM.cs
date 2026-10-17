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
        /// <param name = "transaction">Giao dịch của nơi gọi; null để hạ tầng tham gia giao dịch môi trường hoặc tự mở giao dịch cục bộ. Nếu truyền vào, nơi gọi quản lý commit/rollback.</param>
        public DAL_LICHKHAM(IDbTransaction? transaction = null) : base(transaction)
        {
        }

        /// <summary>
        /// Đọc toàn bộ lịch khám từ dbo.LICH_KHAM, Dapper ánh xạ từng dòng sang ET, sắp theo mã; ToArray nạp hết dữ liệu trước khi hạ tầng kết thúc phạm vi kết nối/giao dịch.
        /// </summary>
        /// <returns>Danh sách ET đã vật hóa; không có bản ghi thì trả danh sách rỗng.</returns>
        public IEnumerable<ET_LICHKHAM> layToanBoLichKham()
        {
            return ThucHien((connection, transaction) =>
            {
                return connection.Query<ET_LICHKHAM>("SELECT * FROM dbo.[LICH_KHAM] ORDER BY [MaLich]", transaction: transaction).ToArray();
            });
        }

        /// <summary>
        /// Tìm lịch khám theo khóa trong dbo.LICH_KHAM; mã được truyền bằng tham số SQL và QuerySingleOrDefault ánh xạ tối đa một bản ghi sang ET.
        /// </summary>
        /// <param name = "ma">Mã khóa chính của bản ghi cần đọc, sửa hoặc xóa; truyền bằng tham số SQL.</param>
        /// <returns>ET của bản ghi tìm thấy, hoặc null nếu mã không tồn tại.</returns>
        public ET_LICHKHAM? layLichKhamTheoMa(string ma)
        {
            return ThucHien((connection, transaction) =>
            {
                return connection.QuerySingleOrDefault<ET_LICHKHAM>("SELECT * FROM dbo.[LICH_KHAM] WHERE [MaLich]=@Ma", new { Ma = ma }, transaction);
            });
        }

        // Truy vấn riêng để hiển thị tên; các mã gốc vẫn dùng khi lưu.
        private const string sqlHienThi = @"SELECT l.*,
            ISNULL(bn.HoTenBN,N'Chưa có thông tin') TenBenhNhan,
            ISNULL(nv.HoTen,N'Chưa có thông tin') TenBacSi,
            ISNULL(p.TenPhong,N'Chưa phân phòng') TenPhong,
            ISNULL(dv.TenDV,N'Chưa chọn dịch vụ') TenDichVu,
            COALESCE(NULLIF(LTRIM(RTRIM(ca.GhiChu)),N''),
                CONVERT(nvarchar(5),ca.GioBatDau,108)+N' – '+CONVERT(nvarchar(5),ca.GioKetThuc,108)) TenCa
            FROM dbo.LICH_KHAM l
            LEFT JOIN dbo.BENH_NHAN bn ON bn.MaBN=l.MaBN
            LEFT JOIN dbo.NHAN_VIEN nv ON nv.MaNV=l.MaBS
            LEFT JOIN dbo.PHONG p ON p.MaPhong=l.MaPhong
            LEFT JOIN dbo.DICH_VU dv ON dv.MaDV=l.MaDV
            LEFT JOIN dbo.CA_LAM ca ON ca.MaCa=l.MaCa";

        public IEnumerable<ET_LICHKHAM_HIENTHI> layLichKhamHienThi()
        {
            return ThucHien((connection, transaction) =>
                connection.Query<ET_LICHKHAM_HIENTHI>(sqlHienThi + " ORDER BY l.NgayGioKham,l.MaLich",
                    transaction: transaction).ToArray());
        }

        public ET_LICHKHAM_HIENTHI? layLichKhamHienThiTheoMa(string ma)
        {
            return ThucHien((connection, transaction) =>
                connection.QuerySingleOrDefault<ET_LICHKHAM_HIENTHI>(
                    sqlHienThi + " WHERE l.MaLich=@Ma", new { Ma=ma },transaction));
        }

        /// <summary>
        /// Thêm lịch khám: cấp mã có khóa ngay vào duLieu, kiểm tra nghiệp vụ, chuẩn hóa và kiểm tra ET theo metadata SQL rồi INSERT có tham số vào dbo.LICH_KHAM trong cùng giao dịch.
        /// </summary>
        /// <param name = "duLieu">ET chứa dữ liệu cần lưu; thêm mới sẽ được gán mã, và kiểm tra metadata có thể chuẩn hóa lại các giá trị ngay trên đối tượng.</param>
        /// <returns>true sau khi INSERT thành công; lỗi dữ liệu/SQL được phát ra bằng ngoại lệ. Giao dịch ngoài chỉ commit khi nơi gọi quyết định.</returns>
        public bool themLichKham(ET_LICHKHAM duLieu)
        {
            return ThucHien((connection, transaction) =>
            {
                KhoaPhanCong(connection, transaction);
                duLieu.MaLich = TaoMa(connection, transaction, "LICH_KHAM", "MaLich");
                KiemTraNghiepVu(duLieu, null);
                KiemTra<ET_LICHKHAM>(connection, transaction, "LICH_KHAM", duLieu, null, null);
                KiemTraNghiVaDongBoCa(connection, transaction, duLieu);
                connection.Execute("""
                    INSERT INTO dbo.[LICH_KHAM] ([MaLich],[MaBN],[MaBS],
                        [MaCa],[MaPhong],[MaDV],
                        [NgayGioKham],[ThoiLuongDuKien],[UuTien],
                        [LyDoKham],[LyDoThayDoi],[TrangThai],
                        [GhiChu],[NgayTao],[NgayCapNhat])
                    VALUES (@MaLich,
                        @MaBN,@MaBS,@MaCa,
                        @MaPhong,@MaDV,@NgayGioKham,
                        @ThoiLuongDuKien,@UuTien,@LyDoKham,
                        @LyDoThayDoi,@TrangThai,@GhiChu,
                        @NgayTao,@NgayCapNhat)
                    """, duLieu, transaction);
                return true;
            });
        }

        /// <summary>
        /// Sửa lịch khám: đọc và giữ khóa UPDLOCK/HOLDLOCK, báo lỗi nếu bản không còn, kiểm tra nghiệp vụ, chuẩn hóa ET và đối chiếu banGoc để phát hiện sửa đồng thời. Khi chỉ đổi trạng thái, chặn thay đổi các trường ngoài TrangThai/NgayXacNhan/NgayCapNhat/LyDoThayDoi, sau đó UPDATE có tham số trong cùng giao dịch.
        /// </summary>
        /// <param name = "duLieu">ET chứa dữ liệu cần lưu; thêm mới sẽ được gán mã, và kiểm tra metadata có thể chuẩn hóa lại các giá trị ngay trên đối tượng.</param>
        /// <param name = "banGoc">Bản ET đã đọc trước khi thao tác; có giá trị để so với SQL và chặn ghi đè/xóa khi bản đã thay đổi, null để bỏ đối chiếu bản gốc.</param>
        /// <param name = "chiDoiTrangThai">true để cấm thay đổi trường nội dung ngoài nhóm trường trạng thái cho phép; không tự đảm bảo mọi chuyển trạng thái đều hợp lệ.</param>
        /// <returns>true khi UPDATE chạy thành công; bản bị mất/thay đổi hoặc dữ liệu sai gây ngoại lệ. Không trả false để biểu thị lỗi.</returns>
        public bool suaLichKham(ET_LICHKHAM duLieu, ET_LICHKHAM? banGoc = null, bool chiDoiTrangThai = false)
        {
            return ThucHien((connection, transaction) =>
            {
                KhoaPhanCong(connection, transaction);
                var hienTai = connection.QuerySingleOrDefault<ET_LICHKHAM>("SELECT * FROM dbo.[LICH_KHAM] WITH(UPDLOCK,HOLDLOCK) WHERE [MaLich]=@Ma", new { Ma = duLieu.MaLich }, transaction);
                if (hienTai is null)
                {
                    throw new InvalidOperationException("Bản ghi không còn tồn tại. Vui lòng tải lại.");
                }

                KiemTraNghiepVu(duLieu, hienTai, chiDoiTrangThai);
                KiemTra(connection, transaction, "LICH_KHAM", duLieu, hienTai, banGoc);
                if (chiDoiTrangThai)
                {
                    KiemTraChiDoiTrangThai(duLieu, hienTai);
                }
                KiemTraNghiVaDongBoCa(connection, transaction, duLieu);

                connection.Execute("""
                    UPDATE dbo.[LICH_KHAM]
                    SET [MaBN]=@MaBN,[MaBS]=@MaBS,[MaCa]=@MaCa,
                        [MaPhong]=@MaPhong,[MaDV]=@MaDV,[NgayGioKham]=@NgayGioKham,
                        [ThoiLuongDuKien]=@ThoiLuongDuKien,[UuTien]=@UuTien,[LyDoKham]=@LyDoKham,
                        [LyDoThayDoi]=@LyDoThayDoi,[TrangThai]=@TrangThai,[GhiChu]=@GhiChu,
                        [NgayTao]=@NgayTao,[NgayCapNhat]=@NgayCapNhat
                    WHERE [MaLich]=@MaLich
                    """, duLieu, transaction);
                return true;
            });
        }

        /// <summary>
        /// Xóa lịch khám: đọc bản hiện tại với UPDLOCK/HOLDLOCK, báo lỗi nếu đã mất, kiểm tra nghiệp vụ, so banGoc khi có rồi DELETE theo mã trong dbo.LICH_KHAM; toàn bộ thao tác giữ cùng giao dịch và lỗi ràng buộc SQL được phát ra.
        /// </summary>
        /// <param name = "ma">Mã khóa chính của bản ghi cần đọc, sửa hoặc xóa; truyền bằng tham số SQL.</param>
        /// <param name = "banGoc">Bản ET đã đọc trước khi thao tác; có giá trị để so với SQL và chặn ghi đè/xóa khi bản đã thay đổi, null để bỏ đối chiếu bản gốc.</param>
        /// <returns>true khi DELETE chạy thành công; không tồn tại, xung đột hoặc vi phạm SQL gây ngoại lệ.</returns>
        public bool xoaLichKham(string ma, ET_LICHKHAM? banGoc = null)
        {
            return ThucHien((connection, transaction) =>
            {
                var hienTai = connection.QuerySingleOrDefault<ET_LICHKHAM>("SELECT * FROM dbo.[LICH_KHAM] WITH(UPDLOCK,HOLDLOCK) WHERE [MaLich]=@Ma", new { Ma = ma }, transaction);
                if (hienTai is null)
                {
                    throw new InvalidOperationException("Bản ghi không còn tồn tại. Vui lòng tải lại.");
                }

                KiemTraNghiepVu(hienTai, hienTai);
                KiemTra(connection, transaction, "LICH_KHAM", hienTai, hienTai, banGoc);
                connection.Execute("DELETE FROM dbo.[LICH_KHAM] WHERE [MaLich]=@Ma", new { Ma = ma }, transaction);
                return true;
            });
        }


        // Dùng chung khóa nghiệp vụ với duyệt nghỉ và phân người thay thế.
        private void KhoaPhanCong(IDbConnection connection, IDbTransaction? transaction)
        {
            int ketQua = connection.ExecuteScalar<int>(@"DECLARE @k int;
                EXEC @k=sys.sp_getapplock @Resource=N'RHM:PhanCong',@LockMode='Exclusive',
                    @LockOwner='Transaction',@LockTimeout=10000; SELECT @k",transaction:transaction);
            if (ketQua<0) throw new InvalidOperationException("Lịch đang được thay đổi bởi người khác. Vui lòng thử lại.");
        }

        private void KiemTraNghiVaDongBoCa(IDbConnection connection, IDbTransaction? transaction, ET_LICHKHAM lich)
        {
            // Lịch đã hủy/hoàn thành không tự tạo ca làm mới.
            if (lich.TrangThai=="Hủy" || lich.TrangThai=="Đã hủy" || lich.TrangThai=="Đã đổi" ||
                lich.TrangThai=="Hoàn thành" || lich.TrangThai=="Đã khám") return;

            int nhanVien = connection.ExecuteScalar<int>(
                "SELECT COUNT(*) FROM dbo.NHAN_VIEN WHERE MaNV=@MaNV AND TrangThai=N'Đang làm việc'",
                new { MaNV=lich.MaBS },transaction);
            if (nhanVien==0) throw new InvalidOperationException("Bác sĩ không còn ở trạng thái đang làm việc.");
            int nghi = connection.ExecuteScalar<int>(@"SELECT COUNT(*) FROM dbo.DON_XIN_NGHI WITH(UPDLOCK,HOLDLOCK)
                WHERE MaNV=@MaNV AND TrangThaiDuyet=N'Đã duyệt' AND TuNgay<=@Ngay
                  AND (DenNgay>=@Ngay OR GhiChu LIKE N'%Loại đơn: Nghỉ việc%')",
                new { MaNV=lich.MaBS,Ngay=lich.NgayGioKham.Date },transaction);
            if (nghi>0) throw new InvalidOperationException("Bác sĩ đã được duyệt nghỉ trong ngày này, không thể phân lịch khám.");

            ET_CALAM? ca = connection.QuerySingleOrDefault<ET_CALAM>(
                "SELECT * FROM dbo.CA_LAM WHERE MaCa=@MaCa", new { lich.MaCa },transaction);
            if (ca==null) throw new InvalidOperationException("Ca khám không tồn tại.");
            TimeSpan batDau = lich.NgayGioKham.TimeOfDay;
            TimeSpan ketThuc = batDau.Add(TimeSpan.FromMinutes(lich.ThoiLuongDuKien));
            if (batDau<ca.GioBatDau || ketThuc>ca.GioKetThuc)
                throw new InvalidOperationException("Giờ khám nằm ngoài thời gian của ca làm.");

            // Luôn kiểm tra xung đột ở DAL, không chỉ kiểm tra trong giao diện.
            int trung = connection.ExecuteScalar<int>(@"SELECT COUNT(*) FROM dbo.LICH_KHAM WITH(UPDLOCK,HOLDLOCK)
                WHERE MaLich<>@MaLich AND TrangThai NOT IN (N'Hủy',N'Đã hủy',N'Đã đổi',N'Hoàn thành',N'Đã khám')
                  AND NgayGioKham<DATEADD(minute,@ThoiLuong,@Ngay)
                  AND DATEADD(minute,ThoiLuongDuKien,NgayGioKham)>@Ngay
                  AND (MaBS=@MaBS OR MaPhong=@MaPhong OR MaBN=@MaBN)",
                new { lich.MaLich,Ngay=lich.NgayGioKham,ThoiLuong=lich.ThoiLuongDuKien,
                    lich.MaBS,lich.MaPhong,lich.MaBN },transaction);
            if (trung>0) throw new InvalidOperationException("Bác sĩ, bệnh nhân hoặc phòng có lịch khám trùng giờ.");

            // Khi phân lịch khám trong tương lai, đồng bộ nhân viên vào Ca làm.
            if (lich.NgayGioKham.Date<DateTime.Today) return;
            var cacCa = connection.Query<ET_LICHLAMVIEC>(@"SELECT * FROM dbo.LICH_LAM_VIEC WITH(UPDLOCK,HOLDLOCK)
                WHERE MaNV=@MaNV AND NgayLam=@Ngay AND MaCa=@MaCa
                  AND TrangThai NOT IN (N'Từ chối',N'Hủy',N'Nghỉ phép',N'Nghỉ việc')",
                new { MaNV=lich.MaBS,Ngay=lich.NgayGioKham.Date,lich.MaCa },transaction).ToList();
            bool daCoCa = false;
            foreach (ET_LICHLAMVIEC llv in cacCa)
            {
                if (llv.TrangThai!="Đã xác nhận")
                    throw new InvalidOperationException("Bác sĩ có ca đang chờ xác nhận/thay thế. Quản lý cần xử lý trong Ca làm trước.");
                if (llv.MaPhong!=lich.MaPhong)
                    throw new InvalidOperationException("Bác sĩ đã được phân ca ở phòng khác. Hãy chọn đúng phòng hoặc điều chỉnh Ca làm.");
                daCoCa = true;
            }
            if (daCoCa) return;
            // Bác sĩ không được có hai ca khác nhau trùng khung giờ.
            int trungCa = connection.ExecuteScalar<int>(@"SELECT COUNT(*)
                FROM dbo.LICH_LAM_VIEC l WITH(UPDLOCK,HOLDLOCK)
                JOIN dbo.CA_LAM caKhac ON caKhac.MaCa=l.MaCa
                WHERE l.MaNV=@MaNV AND l.NgayLam=@Ngay
                  AND l.TrangThai IN (N'Đã xác nhận',N'Chờ xác nhận',N'Dự kiến thay thế')
                  AND caKhac.GioBatDau<@GioKetThuc AND @GioBatDau<caKhac.GioKetThuc",
                new { MaNV=lich.MaBS,Ngay=lich.NgayGioKham.Date,
                    GioBatDau=ca.GioBatDau,GioKetThuc=ca.GioKetThuc },transaction);
            if (trungCa>0) throw new InvalidOperationException("Bác sĩ có một ca làm khác trùng giờ.");
            connection.Execute(@"IF NOT EXISTS (SELECT 1 FROM dbo.CA_THEO_NGAY WITH(UPDLOCK,HOLDLOCK)
                    WHERE NgayLam=@Ngay AND MaCa=@MaCa)
                INSERT INTO dbo.CA_THEO_NGAY (NgayLam,MaCa) VALUES (@Ngay,@MaCa)",
                new { Ngay=lich.NgayGioKham.Date,lich.MaCa },transaction);
            string maLLV = TaoMa(connection,transaction,"LICH_LAM_VIEC","MaLLV");
            connection.Execute(@"INSERT INTO dbo.LICH_LAM_VIEC
                (MaLLV,MaNV,MaCa,NgayLam,MaPhong,TrangThai,TrangThaiHienDien,GioVao,GioRa,GhiChu)
                VALUES (@MaLLV,@MaNV,@MaCa,@Ngay,@MaPhong,N'Đã xác nhận',N'Chưa vào ca',NULL,NULL,
                    N'Tạo tự động từ phân công lịch khám')",
                new { MaLLV=maLLV,MaNV=lich.MaBS,lich.MaCa,Ngay=lich.NgayGioKham.Date,lich.MaPhong },transaction);
        }

        // Đồng bộ dữ liệu lịch khám cũ trước khi xử lý các đơn nghỉ đã có lịch.
        public int dongBoLichKhamVaoCaLam()
        {
            using var connection = MoKetNoi();
            using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
            try
            {
                KhoaPhanCong(connection,transaction);
                var lich = connection.Query<ET_LICHKHAM>(@"SELECT * FROM dbo.LICH_KHAM
                    WHERE CONVERT(date,NgayGioKham)>=CONVERT(date,GETDATE())
                    AND TrangThai NOT IN (N'Hủy',N'Đã hủy',N'Đã đổi',N'Hoàn thành',N'Đã khám')
                    ORDER BY NgayGioKham",transaction:transaction).ToList();
                int dem = 0;
                foreach (ET_LICHKHAM item in lich)
                {
                    int daCo = connection.ExecuteScalar<int>(@"SELECT COUNT(*) FROM dbo.LICH_LAM_VIEC
                        WHERE MaNV=@MaNV AND MaCa=@MaCa AND NgayLam=@Ngay AND TrangThai=N'Đã xác nhận'
                          AND MaPhong=@MaPhong",
                        new { MaNV=item.MaBS,item.MaCa,Ngay=item.NgayGioKham.Date,item.MaPhong },transaction);
                    if (daCo>0) continue;
                    KiemTraNghiVaDongBoCa(connection,transaction,item);
                    dem++;
                }
                transaction.Commit();
                return dem;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        /// <summary>
        /// Chặn sửa nội dung ngoài nhóm trạng thái nháp và chặn quay về Nháp; nếu lịch đã Hủy, Đã đổi hoặc Hoàn thành thì chặn cả thao tác chuyển trạng thái.
        /// </summary>
        /// <param name = "duLieu">ET cần kiểm tra hoặc cập nhật, chứa các mã liên kết và trạng thái mới.</param>
        /// <param name = "banHienTai">Bản hiện tại đã đọc từ SQL; null khi thêm mới, dùng để bảo vệ nội dung/trạng thái và liên kết đã lưu.</param>
        /// <param name = "chiDoiTrangThai">true khi nơi gọi chỉ đổi trạng thái; cờ ảnh hưởng việc chặn sửa nội dung ngoài trạng thái nháp, các giới hạn khác vẫn áp dụng.</param>
        private void KiemTraNghiepVu(ET_LICHKHAM duLieu, ET_LICHKHAM? banHienTai, bool chiDoiTrangThai = false)
        {
            if (banHienTai is not null)
            {
                var trangThaiCu = banHienTai.TrangThai;
                var laBanNhap = trangThaiCu is null or "" or "Nháp" or "Chưa xác nhận" or "Chờ xác nhận" or "Đang lập" or "Dự kiến" or "Chờ thực hiện";
                if (!laBanNhap && (!chiDoiTrangThai))
                {
                    throw new InvalidOperationException("Bản ghi đã xác nhận hoặc đã hủy; hãy lập bản mới.");
                }

                if (!laBanNhap && duLieu.TrangThai == "Nháp")
                {
                    throw new InvalidOperationException("Không chuyển chứng từ đã xác nhận về nháp.");
                }

                if (trangThaiCu is "Hủy" or "Đã đổi" or "Hoàn thành")
                {
                    throw new InvalidOperationException("Lịch đã kết thúc, hãy lập lịch mới.");
                }
            }
        }
    }
}
