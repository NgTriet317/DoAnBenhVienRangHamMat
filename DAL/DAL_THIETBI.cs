// AI hỗ trợ: bổ sung chú thích tiếng Việt;
using System;
using System.Data;
using Dapper;
using ET;

namespace DAL
{
    /// <summary>
    /// Tầng truy cập SQL cho thiết bị trong bảng THIET_BI. Theo dõi serial, ngày mua, nhà cung cấp, phòng đặt, bảo hành và tình trạng thiết bị. Dapper ánh xạ dòng SQL sang ET; ThucHien dùng giao dịch được truyền vào hoặc tự quản lý giao dịch cục bộ.
    /// </summary>
    public class DAL_THIETBI : dbconnect
    {
        /// <summary>
        /// Tạo DAL quản lý thiết bị, chuyển giao dịch tùy chọn cho dbconnect để nhiều thao tác có thể dùng chung kết nối và phạm vi lưu.
        /// </summary>
        /// <param name = "transaction">Giao dịch của nơi gọi; null để hạ tầng tham gia giao dịch môi trường hoặc tự mở giao dịch cục bộ. Nếu truyền vào, nơi gọi quản lý commit/rollback.</param>
        public DAL_THIETBI(IDbTransaction? transaction = null) : base(transaction)
        {
        }

        /// <summary>
        /// Đọc toàn bộ thiết bị từ dbo.THIET_BI, Dapper ánh xạ từng dòng sang ET, sắp theo mã; ToArray nạp hết dữ liệu trước khi hạ tầng kết thúc phạm vi kết nối/giao dịch.
        /// </summary>
        /// <returns>Danh sách ET đã vật hóa; không có bản ghi thì trả danh sách rỗng.</returns>
        public IEnumerable<ET_THIETBI> layToanBoThietBi()
        {
            return ThucHien((connection, transaction) =>
            {
                return connection.Query<ET_THIETBI>("SELECT * FROM dbo.[THIET_BI] ORDER BY [MaTB]", transaction: transaction).ToArray();
            });
        }

        /// <summary>
        /// Tìm thiết bị theo khóa trong dbo.THIET_BI; mã được truyền bằng tham số SQL và QuerySingleOrDefault ánh xạ tối đa một bản ghi sang ET.
        /// </summary>
        /// <param name = "ma">Mã khóa chính của bản ghi cần đọc, sửa hoặc xóa; truyền bằng tham số SQL.</param>
        /// <returns>ET của bản ghi tìm thấy, hoặc null nếu mã không tồn tại.</returns>
        public ET_THIETBI? layThietBiTheoMa(string ma)
        {
            return ThucHien((connection, transaction) =>
            {
                return connection.QuerySingleOrDefault<ET_THIETBI>("SELECT * FROM dbo.[THIET_BI] WHERE [MaTB]=@Ma", new { Ma = ma }, transaction);
            });
        }

        /// <summary>
        /// Thêm thiết bị: cấp mã có khóa ngay vào duLieu, chuẩn hóa và kiểm tra ET theo metadata SQL rồi INSERT có tham số vào dbo.THIET_BI trong cùng giao dịch.
        /// </summary>
        /// <param name = "duLieu">ET chứa dữ liệu cần lưu; thêm mới sẽ được gán mã, và kiểm tra metadata có thể chuẩn hóa lại các giá trị ngay trên đối tượng.</param>
        /// <returns>true sau khi INSERT thành công; lỗi dữ liệu/SQL được phát ra bằng ngoại lệ. Giao dịch ngoài chỉ commit khi nơi gọi quyết định.</returns>
        public bool themThietBi(ET_THIETBI duLieu)
        {
            return ThucHien((connection, transaction) =>
            {
                duLieu.MaTB = TaoMa(connection, transaction, "THIET_BI", "MaTB");
                duLieu.TinhTrang = "Đang hoạt động";
                KiemTra<ET_THIETBI>(connection, transaction, "THIET_BI", duLieu, null, null);
                connection.Execute("""
                INSERT INTO dbo.[THIET_BI] ([MaTB],[MaHH],[SoSerial],
                    [NgayMua],[MaNCC],[MaPhong],
                    [TinhTrang],[HanBaoHanh],[TrangThai],
                    [GhiChu])
                VALUES (@MaTB,@MaHH,@SoSerial,
                    @NgayMua,@MaNCC,@MaPhong,
                    @TinhTrang,@HanBaoHanh,@TrangThai,
                    @GhiChu)
                """, duLieu, transaction);
                return true;
            });
        }

        /// <summary>
        /// Sửa thiết bị: đọc và giữ khóa UPDLOCK/HOLDLOCK, báo lỗi nếu bản không còn, chuẩn hóa ET và đối chiếu banGoc để phát hiện sửa đồng thời. Khi chỉ đổi trạng thái, chặn thay đổi các trường ngoài TrangThai/NgayXacNhan/NgayCapNhat/LyDoThayDoi, sau đó UPDATE có tham số trong cùng giao dịch.
        /// </summary>
        /// <param name = "duLieu">ET chứa dữ liệu cần lưu; thêm mới sẽ được gán mã, và kiểm tra metadata có thể chuẩn hóa lại các giá trị ngay trên đối tượng.</param>
        /// <param name = "banGoc">Bản ET đã đọc trước khi thao tác; có giá trị để so với SQL và chặn ghi đè/xóa khi bản đã thay đổi, null để bỏ đối chiếu bản gốc.</param>
        /// <param name = "chiDoiTrangThai">true để cấm thay đổi trường nội dung ngoài nhóm trường trạng thái cho phép; không tự đảm bảo mọi chuyển trạng thái đều hợp lệ.</param>
        /// <returns>true khi UPDATE chạy thành công; bản bị mất/thay đổi hoặc dữ liệu sai gây ngoại lệ. Không trả false để biểu thị lỗi.</returns>
        public bool suaThietBi(ET_THIETBI duLieu, ET_THIETBI? banGoc = null, bool chiDoiTrangThai = false)
        {
            return ThucHien((connection, transaction) =>
            {
                var hienTai = connection.QuerySingleOrDefault<ET_THIETBI>("SELECT * FROM dbo.[THIET_BI] WITH(UPDLOCK,HOLDLOCK) WHERE [MaTB]=@Ma", new { Ma = duLieu.MaTB }, transaction);
                if (hienTai is null)
                {
                    throw new InvalidOperationException("Bản ghi không còn tồn tại. Vui lòng tải lại.");
                }

                // Không thay đổi tình trạng vật lý qua hàm sửa chung để tránh khôi phục thiết bị đã ngưng.
                if ((duLieu.SoSerial != hienTai.SoSerial || duLieu.MaHH != hienTai.MaHH)
                    && connection.ExecuteScalar<int>("SELECT COUNT(*) FROM dbo.BAO_TRI_THIET_BI WITH(HOLDLOCK) WHERE MaTB=@ma",new{ma=hienTai.MaTB},transaction)>0)
                    throw new InvalidOperationException("Thiết bị đã có lịch sử bảo trì; không thay serial hoặc loại hàng của tài sản.");
                if (duLieu.TinhTrang != hienTai.TinhTrang)
                    throw new InvalidOperationException("Chỉ được đổi tình trạng qua nút Hư hại hoặc Ngưng sử dụng.");
                if (duLieu.MaPhong != hienTai.MaPhong)
                    throw new InvalidOperationException("Chỉ có thể thay đổi phòng qua chức năng phân phòng hoặc thu hồi thiết bị.");

                KiemTra(connection, transaction, "THIET_BI", duLieu, hienTai, banGoc);
                if (chiDoiTrangThai)
                {
                    KiemTraChiDoiTrangThai(duLieu, hienTai);
                }

                connection.Execute("""
                UPDATE dbo.[THIET_BI]
                SET [MaHH]=@MaHH,[SoSerial]=@SoSerial,[NgayMua]=@NgayMua,
                    [MaNCC]=@MaNCC,[MaPhong]=@MaPhong,[TinhTrang]=@TinhTrang,
                    [HanBaoHanh]=@HanBaoHanh,[TrangThai]=@TrangThai,[GhiChu]=@GhiChu
                WHERE [MaTB]=@MaTB
                """, duLieu, transaction);
                return true;
            });
        }

        /// <summary>
        /// Xóa thiết bị: đọc bản hiện tại với UPDLOCK/HOLDLOCK, báo lỗi nếu đã mất, so banGoc khi có rồi DELETE theo mã trong dbo.THIET_BI; toàn bộ thao tác giữ cùng giao dịch và lỗi ràng buộc SQL được phát ra.
        /// </summary>
        /// <param name = "ma">Mã khóa chính của bản ghi cần đọc, sửa hoặc xóa; truyền bằng tham số SQL.</param>
        /// <param name = "banGoc">Bản ET đã đọc trước khi thao tác; có giá trị để so với SQL và chặn ghi đè/xóa khi bản đã thay đổi, null để bỏ đối chiếu bản gốc.</param>
        /// <returns>true khi DELETE chạy thành công; không tồn tại, xung đột hoặc vi phạm SQL gây ngoại lệ.</returns>
        public bool xoaThietBi(string ma, ET_THIETBI? banGoc = null)
        {
            return ThucHien((connection, transaction) =>
            {
                var hienTai = connection.QuerySingleOrDefault<ET_THIETBI>("SELECT * FROM dbo.[THIET_BI] WITH(UPDLOCK,HOLDLOCK) WHERE [MaTB]=@Ma", new { Ma = ma }, transaction);
                if (hienTai is null)
                {
                    throw new InvalidOperationException("Bản ghi không còn tồn tại. Vui lòng tải lại.");
                }

                KiemTra(connection, transaction, "THIET_BI", hienTai, hienTai, banGoc);
                connection.Execute("DELETE FROM dbo.[THIET_BI] WHERE [MaTB]=@Ma", new { Ma = ma }, transaction);
                return true;
            });
        }
        // Chỉ cập nhật tình trạng vật lý, không làm thay đổi TrangThai quản lý.
        public bool capNhatTinhTrangThietBi(string maTB, string tinhTrangMoi, ET_THIETBI banGoc)
        {
            return ThucHien((connection, transaction) =>
            {
                if (tinhTrangMoi != "Hư hại" && tinhTrangMoi != "Ngưng sử dụng")
                    throw new InvalidOperationException("Tình trạng thiết bị không hợp lệ.");

                ET_THIETBI? hienTai = connection.QuerySingleOrDefault<ET_THIETBI>(
                    "SELECT * FROM dbo.[THIET_BI] WITH(UPDLOCK,HOLDLOCK) WHERE [MaTB]=@MaTB",
                    new { MaTB = maTB }, transaction);
                if (hienTai == null)
                    throw new InvalidOperationException("Thiết bị không còn tồn tại. Vui lòng tải lại.");

                // Cả dữ liệu cũ 'Ngừng sử dụng' và dữ liệu mới đều không thể khôi phục.
                if (hienTai.TinhTrang == "Ngưng sử dụng" || hienTai.TinhTrang == "Ngừng sử dụng"
                    || hienTai.TrangThai == "Ngưng sử dụng" || hienTai.TrangThai == "Ngừng sử dụng")
                    throw new InvalidOperationException("Thiết bị đã ngưng sử dụng vĩnh viễn, không thể thay đổi tình trạng.");
                if (hienTai.TinhTrang == tinhTrangMoi)
                    throw new InvalidOperationException("Thiết bị đã ở tình trạng này.");

                if(connection.ExecuteScalar<int>("SELECT COUNT(*) FROM dbo.BAO_TRI_THIET_BI WITH(HOLDLOCK) WHERE MaTB=@ma AND TrangThai IN(N'Chưa xác nhận',N'Đã xác nhận',N'Đang thực hiện')",new{ma=maTB},transaction)>0)
                    throw new InvalidOperationException("Thiết bị đang có phiếu bảo trì mở; hãy xử lý tại Lịch sử bảo trì.");

                ET_THIETBI capNhat = new ET_THIETBI(hienTai.MaTB, hienTai.MaHH,
                    hienTai.SoSerial, hienTai.NgayMua, hienTai.MaNCC, hienTai.MaPhong,
                    tinhTrangMoi, hienTai.HanBaoHanh, hienTai.TrangThai, hienTai.GhiChu);

                // Kiểm tra bản gốc để tránh ghi đè dữ liệu khi người khác vừa thao tác.
                KiemTra(connection, transaction, "THIET_BI", capNhat, hienTai, banGoc);
                connection.Execute(@"
                    UPDATE dbo.[THIET_BI]
                    SET [TinhTrang]=@TinhTrang
                    WHERE [MaTB]=@MaTB", capNhat, transaction);
                return true;
            });
        }

        /// <summary>Khóa thiết bị, kiểm tra bản đã xem và ghi hồ sơ thanh lý cùng việc thu hồi phòng.</summary>
        public bool thanhLyThietBi(string maTB, string maNV, string lyDo, decimal giaTri, ET_THIETBI banGoc)
        {
            ArgumentNullException.ThrowIfNull(banGoc);
            lyDo = (lyDo ?? "").Trim();
            if (lyDo.Length is 0 or > 1000) throw new InvalidOperationException("Nhập lý do thanh lý từ 1 đến 1000 ký tự.");
            if (giaTri < 0 || giaTri > 9999999999999999.99m || decimal.Round(giaTri, 2) != giaTri)
                throw new InvalidOperationException("Giá trị thu hồi không âm và có tối đa hai chữ số thập phân.");
            return ThucHien((db, tx) =>
            {
                db.Execute("""
                    IF COL_LENGTH(N'dbo.THIET_BI',N'NgayThanhLy') IS NULL
                     THROW 52600,N'Chạy toàn bộ Database/QLBenhVienRangHamMat.sql để đồng bộ thanh lý.',1;
                    EXEC dbo.usp_RHM_KiemTraBaoTriSerial;
                    """, transaction: tx);
                var current = db.QuerySingleOrDefault<ET_THIETBI>("SELECT * FROM dbo.THIET_BI WITH(UPDLOCK,HOLDLOCK) WHERE MaTB=@maTB", new { maTB }, tx)
                    ?? throw new InvalidOperationException("Thiết bị không còn tồn tại. Vui lòng tải lại.");
                KiemTra(db, tx, "THIET_BI", current, current, banGoc);
                if (current.NgayThanhLy is not null || current.TinhTrang != "Hư hại" || current.TrangThai is "Ngưng sử dụng" or "Ngừng sử dụng")
                    throw new InvalidOperationException("Chỉ thanh lý thiết bị hư hại chưa ngưng sử dụng.");
                if (db.ExecuteScalar<int>("SELECT COUNT(*) FROM dbo.BAO_TRI_THIET_BI WITH(UPDLOCK,HOLDLOCK) WHERE MaTB=@maTB AND TrangThai IN(N'Chưa xác nhận',N'Đã xác nhận',N'Đang thực hiện')", new { maTB }, tx) > 0)
                    throw new InvalidOperationException("Thiết bị còn phiếu bảo trì mở. Hãy hủy phiếu chưa xác nhận hoặc hoàn tất bảo trì trước khi thanh lý.");
                var tenNV = db.QuerySingleOrDefault<string>("SELECT HoTen FROM dbo.NHAN_VIEN WITH(HOLDLOCK) WHERE MaNV=@maNV AND TrangThai=N'Đang làm việc'", new { maNV }, tx)
                    ?? throw new InvalidOperationException("Chọn nhân viên phụ trách đang làm việc.");
                db.Execute("""
                    UPDATE t SET NgayThanhLy=SYSDATETIME(),MaNVThanhLy=@maNV,LyDoThanhLy=@lyDo,GiaTriThanhLy=@giaTri,
                     TenThietBiThanhLy=h.TenHH,TenPhongThanhLy=COALESCE(p.TenPhong,N'Chưa bố trí phòng'),TenNhanVienThanhLy=@tenNV,
                     TinhTrang=N'Ngưng sử dụng',TrangThai=N'Ngưng sử dụng',MaPhong=NULL
                    FROM dbo.THIET_BI t JOIN dbo.HANG_HOA h WITH(HOLDLOCK) ON h.MaHH=t.MaHH
                     LEFT JOIN dbo.PHONG p WITH(HOLDLOCK) ON p.MaPhong=t.MaPhong WHERE t.MaTB=@maTB
                    """, new { maTB, maNV, lyDo, giaTri, tenNV }, tx);
                return true;
            });
        }

        // Phân phòng chỉ được phép cho thiết bị hoạt động; thu hồi thiết bị hư vẫn được phép.
        public bool phanThietBiVaoPhong(string maTB, string? maPhong, ET_THIETBI banGoc)
        {
            return ThucHien((connection, transaction) =>
            {
                ET_THIETBI? hienTai = connection.QuerySingleOrDefault<ET_THIETBI>(
                    "SELECT * FROM dbo.[THIET_BI] WITH(UPDLOCK,HOLDLOCK) WHERE [MaTB]=@MaTB",
                    new { MaTB = maTB }, transaction);
                if (hienTai == null)
                    throw new InvalidOperationException("Thiết bị không còn tồn tại. Vui lòng tải lại.");

                ET_THIETBI capNhat = new ET_THIETBI(hienTai.MaTB, hienTai.MaHH,
                    hienTai.SoSerial, hienTai.NgayMua, hienTai.MaNCC, maPhong,
                    hienTai.TinhTrang, hienTai.HanBaoHanh, hienTai.TrangThai, hienTai.GhiChu);

                if (maPhong != null)
                {
                    // Hỗ trợ dữ liệu cũ (Tốt/Bình thường) mà không tự động sửa dữ liệu trong SQL.
                    if (hienTai.TinhTrang != "Đang hoạt động" && hienTai.TinhTrang != "Tốt"
                        && hienTai.TinhTrang != "Bình thường")
                        throw new InvalidOperationException("Thiết bị đang hư hại hoặc đã ngưng sử dụng, không thể phân phòng.");
                    if (hienTai.TrangThai != "Chờ bàn giao" && hienTai.TrangThai != "Đang sử dụng"
                        && hienTai.TrangThai != "Đang hoạt động")
                        throw new InvalidOperationException("Trạng thái quản lý hiện tại không cho phép phân phòng.");

                    bool phongHoatDong = connection.ExecuteScalar<int>(
                        "SELECT COUNT(1) FROM dbo.[PHONG] WITH (HOLDLOCK) WHERE [MaPhong]=@MaPhong AND [TrangThai]=N'Đang hoạt động'",
                        new { MaPhong = maPhong }, transaction) > 0;
                    if (!phongHoatDong)
                        throw new InvalidOperationException("Phòng được chọn không tồn tại hoặc đã ngừng hoạt động.");
                    capNhat.TrangThai = "Đang sử dụng";
                }
                else if (hienTai.TrangThai == "Đang sử dụng" || hienTai.TrangThai == "Chờ bàn giao"
                    || hienTai.TrangThai == "Đang hoạt động")
                {
                    capNhat.TrangThai = "Chờ bàn giao";
                }

                KiemTra(connection, transaction, "THIET_BI", capNhat, hienTai, banGoc);
                connection.Execute(@"
                    UPDATE dbo.[THIET_BI]
                    SET [MaPhong]=@MaPhong, [TrangThai]=@TrangThai
                    WHERE [MaTB]=@MaTB", capNhat, transaction);
                return true;
            });
        }
    }
}
