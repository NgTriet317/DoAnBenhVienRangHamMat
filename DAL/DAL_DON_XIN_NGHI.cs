using Dapper;
using ET;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace DAL
{
    public class DAL_DON_XIN_NGHI : dbconnect
    {
        public string guiDon(ET_DON_XIN_NGHI don)
        {
            using var connection = MoKetNoi();
            using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
            try
            {
                int hopLe = connection.ExecuteScalar<int>(@"SELECT CASE WHEN @TuNgay >=
                    DATEADD(day,7,CONVERT(date,SYSDATETIME())) THEN 1 ELSE 0 END",
                    new { don.TuNgay }, transaction);
                if (hopLe != 1)
                    throw new InvalidOperationException("Phải gửi đơn trước ngày bắt đầu nghỉ ít nhất 7 ngày.");
                int trung = connection.ExecuteScalar<int>(@"
                    SELECT COUNT(*) FROM dbo.DON_XIN_NGHI WITH(UPDLOCK,HOLDLOCK)
                    WHERE MaNV=@MaNV AND TrangThaiDuyet IN (N'Chờ duyệt',N'Đã duyệt')
                      AND (TuNgay<=@DenNgay OR @NghiViec=1)
                      AND (DenNgay>=@TuNgay OR GhiChu LIKE N'%Loại đơn: Nghỉ việc%')",
                    new { don.MaNV,don.TuNgay,don.DenNgay,NghiViec=LaDonNghiViec(don.GhiChu) }, transaction);
                if (trung > 0) throw new InvalidOperationException("Đã có đơn đang chờ hoặc đã duyệt trùng thời gian nghỉ.");
                int hoatDong = connection.ExecuteScalar<int>(
                    "SELECT COUNT(*) FROM dbo.NHAN_VIEN WHERE MaNV=@MaNV AND TrangThai=N'Đang làm việc'",
                    don, transaction);
                if (hoatDong == 0) throw new InvalidOperationException("Nhân viên không ở trạng thái đang làm việc.");

                don.MaDonNghi = TaoMa(connection, transaction, "DON_XIN_NGHI", "MaDonNghi");
                int soDong = connection.Execute(@"INSERT INTO dbo.DON_XIN_NGHI
                    (MaDonNghi,MaNV,TuNgay,DenNgay,LyDo,NgayGui,TrangThaiDuyet,MaNVDuyet,NgayDuyet,GhiChu)
                    VALUES (@MaDonNghi,@MaNV,@TuNgay,@DenNgay,@LyDo,@NgayGui,@TrangThaiDuyet,@MaNVDuyet,@NgayDuyet,@GhiChu)", don, transaction);
                if (soDong != 1) throw new InvalidOperationException("Không thể lưu đơn xin nghỉ.");
                transaction.Commit();
                return don.MaDonNghi;
            }
            catch { transaction.Rollback(); throw; }
        }

        // Nếu lịch khám cũ chưa được đồng bộ thành ca làm, tạo ca gốc ở trạng thái nghỉ
        // để quản lý vẫn có thể tìm và bố trí người thay sau khi duyệt.
        private void TaoCaNghiThieu(IDbConnection connection, IDbTransaction tx, ET_DON_XIN_NGHI don, string trangThaiCa)
        {
            var lichKham = connection.Query<ET_LICHKHAM>(@"SELECT l.* FROM dbo.LICH_KHAM l
                WHERE l.MaBS=@MaNV AND l.NgayGioKham>=SYSDATETIME()
                  AND CONVERT(date,l.NgayGioKham)>=@TuNgay
                  AND (CONVERT(date,l.NgayGioKham)<=@DenNgay OR @NghiViec=1)
                  AND l.TrangThai NOT IN (N'Hủy',N'Đã hủy',N'Đã đổi',N'Hoàn thành',N'Đã khám')
                ORDER BY l.NgayGioKham",
                new { don.MaNV,don.TuNgay,don.DenNgay,NghiViec=LaDonNghiViec(don.GhiChu) },tx).ToList();
            foreach (ET_LICHKHAM lich in lichKham)
            {
                int dem = connection.ExecuteScalar<int>(@"SELECT COUNT(*) FROM dbo.LICH_LAM_VIEC
                    WHERE MaNV=@MaNV AND NgayLam=@Ngay AND MaCa=@MaCa
                      AND TrangThai IN (N'Nghỉ phép',N'Nghỉ việc',N'Đã xác nhận',N'Chờ xác nhận')",
                    new { MaNV=don.MaNV,Ngay=lich.NgayGioKham.Date,lich.MaCa },tx);
                if (dem>0) continue;
                connection.Execute(@"IF NOT EXISTS (SELECT 1 FROM dbo.CA_THEO_NGAY
                    WHERE NgayLam=@Ngay AND MaCa=@MaCa)
                    INSERT INTO dbo.CA_THEO_NGAY (NgayLam,MaCa) VALUES (@Ngay,@MaCa)",
                    new { Ngay=lich.NgayGioKham.Date,lich.MaCa },tx);
                string maLLV = TaoMa(connection,tx,"LICH_LAM_VIEC","MaLLV");
                connection.Execute(@"INSERT INTO dbo.LICH_LAM_VIEC
                    (MaLLV,MaNV,MaCa,NgayLam,MaPhong,TrangThai,TrangThaiHienDien,GioVao,GioRa,GhiChu)
                    VALUES (@MaLLV,@MaNV,@MaCa,@Ngay,@MaPhong,@TrangThai,N'Chưa vào ca',NULL,NULL,@GhiChu)",
                    new { MaLLV=maLLV,MaNV=don.MaNV,lich.MaCa,Ngay=lich.NgayGioKham.Date,
                        lich.MaPhong,TrangThai=trangThaiCa,GhiChu="Ca nghỉ từ lịch khám theo đơn "+don.MaDonNghi },tx);
            }
        }

        // Chỉ gọi từ màn hình quản lý. Tạo và duyệt trong cùng giao dịch.
        public string taoDonKhanCap(ET_DON_XIN_NGHI don, string maQuanLy)
        {
            using var connection = MoKetNoi();
            using var tx = connection.BeginTransaction(IsolationLevel.Serializable);
            try
            {
                if (don.TuNgay.Date<DateTime.Today || don.DenNgay.Date<don.TuNgay.Date)
                    throw new InvalidOperationException("Thời gian nghỉ khẩn cấp không hợp lệ.");
                int khoa = connection.ExecuteScalar<int>(@"DECLARE @k int;
                    EXEC @k=sys.sp_getapplock @Resource=N'RHM:PhanCong',@LockMode='Exclusive',
                        @LockOwner='Transaction',@LockTimeout=10000; SELECT @k",transaction:tx);
                if (khoa<0) throw new InvalidOperationException("Đang có người phân công lịch. Hãy thử lại.");
                if (don.MaNV==maQuanLy) throw new InvalidOperationException("Không thể tự duyệt đơn khẩn cấp của mình.");
                int nguoi = connection.ExecuteScalar<int>(@"SELECT COUNT(*) FROM dbo.NHAN_VIEN
                    WHERE MaNV=@MaNV AND TrangThai=N'Đang làm việc'",new { don.MaNV },tx);
                if (nguoi!=1) throw new InvalidOperationException("Nhân viên không còn đang làm việc.");
                int quanLy = connection.ExecuteScalar<int>(@"SELECT COUNT(*) FROM dbo.NHAN_VIEN
                    WHERE MaNV=@MaNV AND TrangThai=N'Đang làm việc'",new { MaNV=maQuanLy },tx);
                if (quanLy!=1) throw new InvalidOperationException("Không tìm thấy nhân viên quản lý hợp lệ.");
                int trung = connection.ExecuteScalar<int>(@"SELECT COUNT(*) FROM dbo.DON_XIN_NGHI WITH(UPDLOCK,HOLDLOCK)
                    WHERE MaNV=@MaNV AND TrangThaiDuyet IN (N'Chờ duyệt',N'Đã duyệt')
                      AND (TuNgay<=@DenNgay OR @NghiViec=1)
                      AND (DenNgay>=@TuNgay OR GhiChu LIKE N'%Loại đơn: Nghỉ việc%')",
                    new { don.MaNV,don.TuNgay,don.DenNgay,NghiViec=LaDonNghiViec(don.GhiChu) },tx);
                if (trung>0) throw new InvalidOperationException("Nhân viên đã có đơn trùng kỳ nghỉ.");
                don.MaDonNghi = TaoMa(connection,tx,"DON_XIN_NGHI","MaDonNghi");
                don.NgayGui=DateTime.Now;
                don.TrangThaiDuyet="Đã duyệt";
                don.MaNVDuyet=maQuanLy;
                don.NgayDuyet=DateTime.Now;
                connection.Execute(@"INSERT INTO dbo.DON_XIN_NGHI
                    (MaDonNghi,MaNV,TuNgay,DenNgay,LyDo,NgayGui,TrangThaiDuyet,MaNVDuyet,NgayDuyet,GhiChu)
                    VALUES (@MaDonNghi,@MaNV,@TuNgay,@DenNgay,@LyDo,@NgayGui,@TrangThaiDuyet,@MaNVDuyet,@NgayDuyet,@GhiChu)",don,tx);
                connection.Execute(@"UPDATE dbo.LICH_LAM_VIEC SET TrangThai=N'Nghỉ phép',
                    GhiChu=CONCAT(ISNULL(GhiChu,N''),N' | Nghỉ khẩn cấp theo đơn ',@MaDon)
                    WHERE MaNV=@MaNV AND NgayLam>=@TuNgay AND NgayLam<=@DenNgay
                      AND TrangThai IN (N'Chờ xác nhận',N'Đã xác nhận')",
                    new { don.MaNV,don.TuNgay,don.DenNgay,MaDon=don.MaDonNghi },tx);
                TaoCaNghiThieu(connection,tx,don,"Nghỉ phép");
                // Đơn khẩn cấp có thể bắt đầu ngay hôm nay; đồng bộ trong cùng giao dịch.
                new DAL_NHANVIEN(tx).dongBoTrangThaiNghi();
                // Không đụng vào LICH_KHAM: mỗi lịch phải được chuyển thủ công.
                tx.Commit();
                return don.MaDonNghi;
            }
            catch { tx.Rollback(); throw; }
        }

        public bool daDuocDuyetNghi(string maNV, DateTime ngay)
        {
            using var connection = MoKetNoi();
            int soDon = connection.ExecuteScalar<int>(@"SELECT COUNT(*) FROM dbo.DON_XIN_NGHI
                WHERE MaNV=@MaNV AND TrangThaiDuyet=N'Đã duyệt' AND TuNgay<=@Ngay
                  AND (DenNgay>=@Ngay OR GhiChu LIKE N'%Loại đơn: Nghỉ việc%')",
                new { MaNV=maNV,Ngay=ngay.Date });
            return soDon>0;
        }

        public int demDonChoDuyet()
        {
            using var connection = MoKetNoi();
            return connection.ExecuteScalar<int>("SELECT COUNT(*) FROM dbo.DON_XIN_NGHI WHERE TrangThaiDuyet=N'Chờ duyệt'");
        }

        public List<ET_DON_XIN_NGHI_HIENTHI> layDanhSachDon()
        {
            using var connection = MoKetNoi();
            string sql = @"SELECT d.MaDonNghi,d.MaNV,d.TuNgay,d.DenNgay,d.LyDo,d.NgayGui,
                d.TrangThaiDuyet,d.MaNVDuyet,d.NgayDuyet,d.GhiChu,n.HoTen,
                ISNULL(ck.TenCK,N'Chưa phân chuyên khoa') TenCK,
                ISNULL(duyet.HoTen,N'') TenNguoiDuyet
                FROM dbo.DON_XIN_NGHI d JOIN dbo.NHAN_VIEN n ON n.MaNV=d.MaNV
                LEFT JOIN dbo.CHUYEN_KHOA ck ON ck.MaCK=n.MaCK
                LEFT JOIN dbo.NHAN_VIEN duyet ON duyet.MaNV=d.MaNVDuyet
                ORDER BY CASE WHEN d.TrangThaiDuyet=N'Chờ duyệt' THEN 0 ELSE 1 END,d.NgayGui DESC";
            return connection.Query<ET_DON_XIN_NGHI_HIENTHI>(sql).ToList();
        }

        public List<ET_CA_CAN_THAY_THE> layCaCanThayThe(string maDon)
        {
            using var connection = MoKetNoi();
            string sql = @"SELECT l.MaLLV,l.MaCa,ISNULL(l.MaPhong,N'') MaPhong,l.NgayLam,l.TrangThai,
                ISNULL(phong.TenPhong,N'Chưa phân phòng') TenPhong,
                COALESCE(NULLIF(LTRIM(RTRIM(ca.GhiChu)),N''),
                    CONVERT(nvarchar(5),ca.GioBatDau,108)+N' – '+CONVERT(nvarchar(5),ca.GioKetThuc,108)) TenCa,
                ISNULL(thay.MaNV,N'') MaNVThayThe,ISNULL(n.HoTen,N'') HoTenThayThe,
                ISNULL(thay.MaLLV,N'') MaLLVThayThe
                FROM dbo.DON_XIN_NGHI d
                JOIN dbo.LICH_LAM_VIEC l ON l.MaNV=d.MaNV AND l.NgayLam>=d.TuNgay
                     AND (l.NgayLam<=d.DenNgay OR d.GhiChu LIKE N'%Loại đơn: Nghỉ việc%')
                     AND l.NgayLam>=CONVERT(date,GETDATE())
                     AND (l.TrangThai IN (N'Đã xác nhận',N'Chờ xác nhận')
                        OR (d.TrangThaiDuyet=N'Đã duyệt' AND l.TrangThai IN (N'Nghỉ phép',N'Nghỉ việc')))
                OUTER APPLY (SELECT TOP 1 p.MaLLVThayThe FROM dbo.PHAN_CONG_THAY_THE p
                    WHERE p.MaDonNghi=d.MaDonNghi AND p.GhiChu=N'Thay ca '+l.MaLLV
                        AND p.TrangThai IN (N'Chờ duyệt',N'Đã xác nhận') ORDER BY p.NgayXuLy DESC) p
                LEFT JOIN dbo.LICH_LAM_VIEC thay ON thay.MaLLV=p.MaLLVThayThe
                LEFT JOIN dbo.NHAN_VIEN n ON n.MaNV=thay.MaNV
                LEFT JOIN dbo.CA_LAM ca ON ca.MaCa=l.MaCa
                LEFT JOIN dbo.PHONG phong ON phong.MaPhong=l.MaPhong
                WHERE d.MaDonNghi=@MaDon
                ORDER BY l.NgayLam,l.MaCa,l.MaLLV";
            return connection.Query<ET_CA_CAN_THAY_THE>(sql, new { MaDon = maDon }).ToList();
        }

        public List<ET_NGUOI_THAY_THE> layNhanVienThayThe(string maDon)
        {
            using var connection = MoKetNoi();
            return connection.Query<ET_NGUOI_THAY_THE>(@"SELECT nv.MaNV,nv.HoTen
                FROM dbo.NHAN_VIEN nv JOIN dbo.DON_XIN_NGHI d ON d.MaDonNghi=@MaDon
                JOIN dbo.NHAN_VIEN cu ON cu.MaNV=d.MaNV
                WHERE nv.MaNV<>d.MaNV AND nv.TrangThai=N'Đang làm việc'
                    AND nv.MaCK=cu.MaCK AND nv.MaChucVu=cu.MaChucVu ORDER BY nv.HoTen", new { MaDon = maDon }).ToList();
        }

        // Dùng cùng truy vấn ở màn hình và khi ghi nhận xác nhận từng lịch khám.
        private const string sqlLichChoXacNhan = @"SELECT l.MaLich,
            ISNULL(bn.HoTenBN,N'') TenBenhNhan,l.NgayGioKham,l.ThoiLuongDuKien,l.MaCa,
            l.MaBS MaBSCu,ISNULL(bscu.HoTen,N'Chưa có thông tin') TenBSCu,
            l.MaPhong MaPhongCu,ISNULL(pcu.TenPhong,N'Chưa phân phòng') TenPhongCu,
            ISNULL(thay.MaNV,N'') MaBSMoi,ISNULL(n.HoTen,N'') TenBSMoi,
            ISNULL(thay.MaPhong,N'') MaPhongMoi,ISNULL(pmoi.TenPhong,N'Chưa phân phòng') TenPhongMoi,
            ISNULL(thay.MaLLV,N'') MaLLVThayThe,
            l.MaBSDuBi,ISNULL(duBi.HoTen,N'Chưa có dự bị') TenBSDuBi,
            CAST(CASE WHEN xn.TrangThai=N'Đã xác nhận' AND xn.MaBSCu=l.MaBS
                AND xn.MaPhongCu=l.MaPhong AND xn.MaBSMoi=thay.MaNV
                AND xn.MaPhongMoi=thay.MaPhong AND xn.MaLLVThayThe=thay.MaLLV
                AND xn.MaCa=l.MaCa AND xn.NgayGioKham=l.NgayGioKham
                AND xn.ThoiLuongDuKien=l.ThoiLuongDuKien THEN 1 ELSE 0 END AS bit) DaXacNhan,
            ISNULL(xn.MaNVXacNhan,N'') MaNVXacNhan,ISNULL(xacnhan.HoTen,N'') TenNguoiXacNhan,xn.NgayXacNhan
            FROM dbo.DON_XIN_NGHI d
            JOIN dbo.LICH_KHAM l ON l.MaBS=d.MaNV
                AND CONVERT(date,l.NgayGioKham)>=d.TuNgay
                AND (CONVERT(date,l.NgayGioKham)<=d.DenNgay OR d.GhiChu LIKE N'%Loại đơn: Nghỉ việc%')
                AND l.NgayGioKham>=SYSDATETIME()
                AND l.TrangThai NOT IN (N'Hủy',N'Đã hủy',N'Đã đổi',N'Hoàn thành',N'Đã khám')
            LEFT JOIN dbo.BENH_NHAN bn ON bn.MaBN=l.MaBN
            LEFT JOIN dbo.NHAN_VIEN bscu ON bscu.MaNV=l.MaBS
            LEFT JOIN dbo.NHAN_VIEN duBi ON duBi.MaNV=l.MaBSDuBi
            LEFT JOIN dbo.PHONG pcu ON pcu.MaPhong=l.MaPhong
            OUTER APPLY (SELECT TOP 1 t.MaLLV,t.MaNV,t.MaPhong
                FROM dbo.LICH_LAM_VIEC goc
                JOIN dbo.PHAN_CONG_THAY_THE p ON p.MaDonNghi=d.MaDonNghi
                    AND p.GhiChu=N'Thay ca '+goc.MaLLV AND p.TrangThai=N'Đã xác nhận'
                JOIN dbo.LICH_LAM_VIEC t ON t.MaLLV=p.MaLLVThayThe
                    AND t.MaCa=goc.MaCa AND t.NgayLam=goc.NgayLam
                    AND t.TrangThai IN (N'Dự kiến thay thế',N'Đã xác nhận')
                WHERE goc.MaNV=d.MaNV AND goc.MaCa=l.MaCa
                    AND goc.MaPhong=l.MaPhong
                    AND goc.NgayLam=CONVERT(date,l.NgayGioKham)
                    AND goc.TrangThai IN (N'Nghỉ phép',N'Nghỉ việc')
                ORDER BY p.NgayXuLy DESC) thay
            LEFT JOIN dbo.NHAN_VIEN n ON n.MaNV=thay.MaNV
            LEFT JOIN dbo.PHONG pmoi ON pmoi.MaPhong=thay.MaPhong
            LEFT JOIN dbo.XAC_NHAN_DOI_LICH_KHAM xn ON xn.MaDonNghi=d.MaDonNghi AND xn.MaLich=l.MaLich
            LEFT JOIN dbo.NHAN_VIEN xacnhan ON xacnhan.MaNV=xn.MaNVXacNhan
            WHERE d.MaDonNghi=@MaDon AND d.TrangThaiDuyet=N'Đã duyệt'";

        public List<ET_LICH_KHAM_CAN_XAC_NHAN> layLichKhamCanXacNhan(string maDon)
        {
            using var connection = MoKetNoi();
            string? trangThai = connection.QuerySingleOrDefault<string>(
                "SELECT TrangThaiDuyet FROM dbo.DON_XIN_NGHI WHERE MaDonNghi=@MaDon",
                new { MaDon=maDon });
            List<ET_LICH_KHAM_CAN_XAC_NHAN> dangCho = new List<ET_LICH_KHAM_CAN_XAC_NHAN>();
            if (trangThai=="Đã duyệt")
                dangCho = connection.Query<ET_LICH_KHAM_CAN_XAC_NHAN>(
                    sqlLichChoXacNhan + " ORDER BY l.NgayGioKham,l.MaLich",
                    new { MaDon=maDon }).ToList();
            // Các lịch đã chuyển không còn trong danh sách đang chờ, nên gộp lịch sử vào.
            List<ET_LICH_KHAM_CAN_XAC_NHAN> daChuyen = connection.Query<ET_LICH_KHAM_CAN_XAC_NHAN>(@"SELECT xn.MaLich,
                ISNULL(bn.HoTenBN,N'') TenBenhNhan,xn.NgayGioKham,xn.ThoiLuongDuKien,xn.MaCa,
                xn.MaBSCu,ISNULL(bscu.HoTen,N'Chưa có thông tin') TenBSCu,
                xn.MaPhongCu,ISNULL(pcu.TenPhong,N'Chưa phân phòng') TenPhongCu,
                xn.MaBSMoi,ISNULL(n.HoTen,N'') TenBSMoi,
                xn.MaPhongMoi,ISNULL(pmoi.TenPhong,N'Chưa phân phòng') TenPhongMoi,
                xn.MaLLVThayThe,l.MaBSDuBi,ISNULL(duBi.HoTen,N'Chưa có dự bị') TenBSDuBi,
                xn.MaNVXacNhan,ISNULL(xacnhan.HoTen,N'') TenNguoiXacNhan,xn.NgayXacNhan,
                CAST(CASE WHEN xn.TrangThai=N'Đã xác nhận' THEN 1 ELSE 0 END AS bit) DaXacNhan,
                CASE WHEN d.TrangThaiDuyet=N'Đã duyệt' THEN N'Đã chuyển'
                    ELSE N'Đã hủy' END TrangThaiLuu
                FROM dbo.XAC_NHAN_DOI_LICH_KHAM xn
                JOIN dbo.DON_XIN_NGHI d ON d.MaDonNghi=xn.MaDonNghi
                LEFT JOIN dbo.LICH_KHAM l ON l.MaLich=xn.MaLich
                LEFT JOIN dbo.BENH_NHAN bn ON bn.MaBN=l.MaBN
                LEFT JOIN dbo.NHAN_VIEN n ON n.MaNV=xn.MaBSMoi
                LEFT JOIN dbo.NHAN_VIEN bscu ON bscu.MaNV=xn.MaBSCu
                LEFT JOIN dbo.NHAN_VIEN duBi ON duBi.MaNV=l.MaBSDuBi
                LEFT JOIN dbo.NHAN_VIEN xacnhan ON xacnhan.MaNV=xn.MaNVXacNhan
                LEFT JOIN dbo.PHONG pcu ON pcu.MaPhong=xn.MaPhongCu
                LEFT JOIN dbo.PHONG pmoi ON pmoi.MaPhong=xn.MaPhongMoi
                WHERE xn.MaDonNghi=@MaDon AND xn.TrangThai=N'Đã xác nhận'
                ORDER BY xn.NgayGioKham,xn.MaLich",
                new { MaDon=maDon }).ToList();
            foreach (ET_LICH_KHAM_CAN_XAC_NHAN da in daChuyen)
            {
                bool daCo = false;
                foreach (ET_LICH_KHAM_CAN_XAC_NHAN cho in dangCho)
                {
                    if (cho.MaLich==da.MaLich) { daCo=true; break; }
                }
                if (!daCo) dangCho.Add(da);
            }
            return dangCho;
        }

        public bool xacNhanTungLichKham(string maDon, string maLich, string maNguoiXacNhan)
        {
            using var connection = MoKetNoi();
            using var tx = connection.BeginTransaction(IsolationLevel.Serializable);
            try
            {
                int khoa = connection.ExecuteScalar<int>(@"DECLARE @k int;
                    EXEC @k=sys.sp_getapplock @Resource=N'RHM:PhanCong',@LockMode='Exclusive',
                    @LockOwner='Transaction',@LockTimeout=10000; SELECT @k", transaction:tx);
                if (khoa<0) throw new InvalidOperationException("Có người đang sửa lịch. Hãy thử lại.");
                string? maNVDangNghi = connection.QuerySingleOrDefault<string>(@"SELECT MaNV
                    FROM dbo.DON_XIN_NGHI WITH(UPDLOCK,HOLDLOCK)
                    WHERE MaDonNghi=@MaDon AND TrangThaiDuyet=N'Đã duyệt'",
                    new { MaDon=maDon },tx);
                if (string.IsNullOrWhiteSpace(maNVDangNghi))
                    throw new InvalidOperationException("Đơn chưa được duyệt hoặc không còn hợp lệ.");
                if (maNVDangNghi==maNguoiXacNhan)
                    throw new InvalidOperationException("Không thể tự xác nhận lịch khám của mình.");
                ET_LICH_KHAM_CAN_XAC_NHAN? lich = null;
                foreach (ET_LICH_KHAM_CAN_XAC_NHAN item in connection.Query<ET_LICH_KHAM_CAN_XAC_NHAN>(
                    sqlLichChoXacNhan, new { MaDon=maDon },tx))
                {
                    if (item.MaLich==maLich) lich = item;
                }
                if (lich==null)
                    throw new InvalidOperationException("Lịch khám đã thay đổi hoặc không còn cần chuyển.");
                if (string.IsNullOrWhiteSpace(lich.MaLLVThayThe) || string.IsNullOrWhiteSpace(lich.MaBSMoi) ||
                    string.IsNullOrWhiteSpace(lich.MaPhongMoi))
                    throw new InvalidOperationException("Phải bố trí người thay cho ca tương ứng trước khi xác nhận lịch khám.");
                if (lich.MaBSMoi==lich.MaBSCu)
                    throw new InvalidOperationException("Bác sĩ thay thế không được trùng bác sĩ nghỉ.");
                int trung = connection.ExecuteScalar<int>(@"SELECT COUNT(*) FROM dbo.LICH_KHAM WITH(UPDLOCK,HOLDLOCK)
                    WHERE MaLich<>@MaLich
                      AND TrangThai NOT IN (N'Hủy',N'Đã hủy',N'Đã đổi',N'Hoàn thành',N'Đã khám')
                      AND (MaBS=@MaBS OR MaBSDuBi=@MaBS OR MaPhong=@MaPhong)
                      AND NgayGioKham<DATEADD(minute,@ThoiLuong,@Ngay)
                      AND DATEADD(minute,ThoiLuongDuKien,NgayGioKham)>@Ngay",
                    new { lich.MaLich,MaBS=lich.MaBSMoi,MaPhong=lich.MaPhongMoi,
                        Ngay=lich.NgayGioKham,ThoiLuong=lich.ThoiLuongDuKien },tx);
                if (trung>0)
                    throw new InvalidOperationException("Bác sĩ/phòng thay thế đang có lịch khám trùng giờ. Cần điều chỉnh trước khi xác nhận.");
                int nghi = connection.ExecuteScalar<int>(@"SELECT COUNT(*) FROM dbo.DON_XIN_NGHI WITH(UPDLOCK,HOLDLOCK)
                    WHERE MaNV=@MaNV AND TrangThaiDuyet=N'Đã duyệt' AND TuNgay<=@Ngay
                      AND (DenNgay>=@Ngay OR GhiChu LIKE N'%Loại đơn: Nghỉ việc%')",
                    new { MaNV=lich.MaBSMoi,Ngay=lich.NgayGioKham.Date },tx);
                if (nghi>0)
                    throw new InvalidOperationException("Bác sĩ thay thế đã được duyệt nghỉ trong ngày khám.");

                connection.Execute(@"UPDATE dbo.XAC_NHAN_DOI_LICH_KHAM SET
                    MaBSCu=@MaBSCu,MaPhongCu=@MaPhongCu,MaBSMoi=@MaBSMoi,MaPhongMoi=@MaPhongMoi,
                    MaLLVThayThe=@MaLLVThayThe,MaCa=@MaCa,NgayGioKham=@NgayGioKham,
                    ThoiLuongDuKien=@ThoiLuongDuKien,MaNVXacNhan=@MaNVXacNhan,
                    NgayXacNhan=SYSDATETIME(),TrangThai=N'Đã xác nhận'
                    WHERE MaDonNghi=@MaDon AND MaLich=@MaLich;
                    IF @@ROWCOUNT=0 INSERT INTO dbo.XAC_NHAN_DOI_LICH_KHAM
                    (MaDonNghi,MaLich,MaBSCu,MaPhongCu,MaBSMoi,MaPhongMoi,MaLLVThayThe,MaCa,
                     NgayGioKham,ThoiLuongDuKien,MaNVXacNhan,NgayXacNhan,TrangThai)
                    VALUES (@MaDon,@MaLich,@MaBSCu,@MaPhongCu,@MaBSMoi,@MaPhongMoi,@MaLLVThayThe,
                     @MaCa,@NgayGioKham,@ThoiLuongDuKien,@MaNVXacNhan,SYSDATETIME(),N'Đã xác nhận')",
                    new { MaDon=maDon,MaLich=maLich,lich.MaBSCu,lich.MaPhongCu,lich.MaBSMoi,
                        lich.MaPhongMoi,lich.MaLLVThayThe,lich.MaCa,lich.NgayGioKham,
                        lich.ThoiLuongDuKien,MaNVXacNhan=maNguoiXacNhan },tx);
                // Chỉ lúc quản lý xác nhận riêng lịch mới chuyển bác sĩ/phòng.
                int doi = connection.Execute(@"UPDATE dbo.LICH_KHAM
                    SET MaBS=@MaBSMoi, MaPhong=@MaPhongMoi,
                        MaBSDuBi=CASE WHEN MaBSDuBi=@MaBSMoi THEN NULL ELSE MaBSDuBi END,
                        LyDoThayDoi=CONCAT(ISNULL(LyDoThayDoi,N''),N' | Chuyển theo đơn ',@MaDon),
                        NgayCapNhat=SYSDATETIME()
                    WHERE MaLich=@MaLich AND MaBS=@MaBSCu AND MaPhong=@MaPhongCu
                      AND NgayGioKham=@NgayGioKham AND ThoiLuongDuKien=@ThoiLuongDuKien",
                    new { lich.MaLich,lich.MaBSCu,lich.MaPhongCu,lich.MaBSMoi,lich.MaPhongMoi,
                        lich.NgayGioKham,lich.ThoiLuongDuKien,MaDon=maDon },tx);
                if (doi!=1) throw new InvalidOperationException("Lịch vừa thay đổi. Hãy tải lại và xác nhận lại.");
                tx.Commit();
                return true;
            }
            catch { tx.Rollback(); throw; }
        }

        public bool phanCongThayThe(string maDon, string maLLVGoc, string maNVThay, string maPhong)
        {
            using var connection = MoKetNoi();
            using var tx = connection.BeginTransaction(IsolationLevel.Serializable);
            try
            {
                int khoa = connection.ExecuteScalar<int>(@"DECLARE @k int;
                    EXEC @k=sys.sp_getapplock @Resource=N'RHM:PhanCong',@LockMode='Exclusive',
                    @LockOwner='Transaction',@LockTimeout=10000; SELECT @k", transaction: tx);
                if (khoa < 0) throw new InvalidOperationException("Có người đang sửa phân ca. Hãy thử lại.");
                var ca = connection.QuerySingleOrDefault<ET_CA_CAN_THAY_THE>(@"
                    SELECT l.MaLLV,l.MaCa,ISNULL(l.MaPhong,N'') MaPhong,l.NgayLam,l.TrangThai
                    FROM dbo.LICH_LAM_VIEC l WITH(UPDLOCK,HOLDLOCK)
                    JOIN dbo.DON_XIN_NGHI d ON d.MaNV=l.MaNV
                    WHERE d.MaDonNghi=@MaDon AND d.TrangThaiDuyet=N'Đã duyệt'
                        AND l.MaLLV=@MaLLV AND l.NgayLam>=d.TuNgay
                        AND (l.NgayLam<=d.DenNgay OR d.GhiChu LIKE N'%Loại đơn: Nghỉ việc%')
                        AND l.NgayLam>=CONVERT(date,GETDATE())
                        AND l.TrangThai IN (N'Nghỉ phép',N'Nghỉ việc')",
                    new { MaDon = maDon, MaLLV = maLLVGoc }, tx);
                if (ca == null) throw new InvalidOperationException("Ca không hợp lệ hoặc đơn đã xử lý.");
                int duDieuKien = connection.ExecuteScalar<int>(@"SELECT COUNT(*)
                    FROM dbo.NHAN_VIEN nv JOIN dbo.DON_XIN_NGHI d ON d.MaDonNghi=@MaDon
                    JOIN dbo.NHAN_VIEN cu ON cu.MaNV=d.MaNV
                    WHERE nv.MaNV=@MaNV AND nv.MaNV<>d.MaNV AND nv.TrangThai=N'Đang làm việc'
                      AND nv.MaCK=cu.MaCK AND nv.MaChucVu=cu.MaChucVu", new { MaDon=maDon, MaNV=maNVThay }, tx);
                if (duDieuKien==0) throw new InvalidOperationException("Người thay thế phải đang làm việc, cùng chức vụ và chuyên khoa.");
                int phongHopLe = connection.ExecuteScalar<int>(
                    "SELECT COUNT(*) FROM dbo.PHONG WHERE MaPhong=@MaPhong AND TrangThai=N'Đang hoạt động'",
                    new { MaPhong=maPhong }, tx);
                if (phongHopLe==0) throw new InvalidOperationException("Phòng thay thế không hoạt động.");
                int nghi = connection.ExecuteScalar<int>(@"SELECT COUNT(*) FROM dbo.DON_XIN_NGHI
                    WHERE MaNV=@MaNV AND TrangThaiDuyet=N'Đã duyệt' AND TuNgay<=@Ngay
                    AND (DenNgay>=@Ngay OR GhiChu LIKE N'%Loại đơn: Nghỉ việc%')",
                    new { MaNV=maNVThay, Ngay=ca.NgayLam }, tx);
                if (nghi>0) throw new InvalidOperationException("Người thay thế đang có lịch nghỉ đã duyệt.");
                int daThay = connection.ExecuteScalar<int>(@"SELECT COUNT(*) FROM dbo.PHAN_CONG_THAY_THE
                    WHERE MaDonNghi=@MaDon AND GhiChu=@GhiChu AND TrangThai=N'Đã xác nhận'",
                    new { MaDon=maDon, GhiChu="Thay ca "+maLLVGoc }, tx);
                if (daThay>0) throw new InvalidOperationException("Ca đã được chọn người thay. Hãy tải lại danh sách.");
                // Nếu người thay đã được phân đúng ca/phòng, có thể dùng lịch đó,
                // không tạo thêm một lịch làm việc trùng giờ.
                var lichDaCo = connection.Query<ET_LICHLAMVIEC>(@"SELECT x.*
                    FROM dbo.LICH_LAM_VIEC x WITH(UPDLOCK,HOLDLOCK)
                    JOIN dbo.CA_LAM xca ON xca.MaCa=x.MaCa JOIN dbo.CA_LAM ca ON ca.MaCa=@MaCa
                    WHERE x.MaNV=@MaNV AND x.NgayLam=@Ngay
                      AND x.TrangThai NOT IN (N'Từ chối',N'Hủy',N'Nghỉ phép',N'Nghỉ việc')
                      AND xca.GioBatDau<ca.GioKetThuc AND ca.GioBatDau<xca.GioKetThuc",
                    new { MaNV=maNVThay,Ngay=ca.NgayLam,MaCa=ca.MaCa },tx).ToList();
                string maLLV = "";
                foreach (ET_LICHLAMVIEC lich in lichDaCo)
                {
                    if (lich.MaCa==ca.MaCa && lich.MaPhong==maPhong && lich.TrangThai=="Đã xác nhận")
                    {
                        maLLV = lich.MaLLV;
                    }
                    else
                    {
                        throw new InvalidOperationException("Người thay thế có ca trùng giờ hoặc đang chờ xác nhận.");
                    }
                }
                int trungKham = connection.ExecuteScalar<int>(@"SELECT COUNT(*)
                    FROM dbo.LICH_KHAM goc JOIN dbo.LICH_KHAM khac ON khac.MaLich<>goc.MaLich
                    WHERE goc.MaBS=(SELECT MaNV FROM dbo.LICH_LAM_VIEC WHERE MaLLV=@MaLLV)
                      AND goc.MaCa=@MaCa AND CONVERT(date,goc.NgayGioKham)=@Ngay
                      AND goc.TrangThai NOT IN (N'Hủy',N'Đã hủy',N'Đã đổi',N'Hoàn thành',N'Đã khám')
                      AND khac.TrangThai NOT IN (N'Hủy',N'Đã hủy',N'Đã đổi',N'Hoàn thành',N'Đã khám')
                      AND (khac.MaBS=@MaNV OR khac.MaBSDuBi=@MaNV OR khac.MaPhong=@MaPhong)
                      AND goc.NgayGioKham<DATEADD(minute,khac.ThoiLuongDuKien,khac.NgayGioKham)
                      AND khac.NgayGioKham<DATEADD(minute,goc.ThoiLuongDuKien,goc.NgayGioKham)",
                    new { MaLLV=maLLVGoc, MaCa=ca.MaCa,Ngay=ca.NgayLam,MaNV=maNVThay,MaPhong=maPhong },tx);
                if (trungKham>0) throw new InvalidOperationException("Người thay thế hoặc phòng đã có lịch khám trùng giờ. Hãy điều chỉnh lịch khám trước.");

                if (string.IsNullOrWhiteSpace(maLLV))
                {
                    maLLV = TaoMa(connection,tx,"LICH_LAM_VIEC","MaLLV");
                    connection.Execute(@"INSERT INTO dbo.LICH_LAM_VIEC
                        (MaLLV,MaNV,MaCa,NgayLam,MaPhong,TrangThai,TrangThaiHienDien,GioVao,GioRa,GhiChu)
                        VALUES (@MaLLV,@MaNV,@MaCa,@Ngay,@MaPhong,N'Đã xác nhận',N'Chưa vào ca',NULL,NULL,@GhiChu)",
                        new { MaLLV=maLLV,MaNV=maNVThay,MaCa=ca.MaCa,Ngay=ca.NgayLam,
                            MaPhong=maPhong,GhiChu="Thay ca "+maLLVGoc+" theo đơn "+maDon },tx);
                }
                string maPCTT = TaoMa(connection,tx,"PHAN_CONG_THAY_THE","MaPCTT");
                connection.Execute(@"INSERT INTO dbo.PHAN_CONG_THAY_THE
                    (MaPCTT,MaDonNghi,MaLLVThayThe,NgayXuLy,TrangThai,GhiChu)
                    VALUES (@MaPCTT,@MaDon,@MaLLV,SYSDATETIME(),N'Đã xác nhận',@GhiChu)",
                    new { MaPCTT=maPCTT, MaDon=maDon, MaLLV=maLLV, GhiChu="Thay ca "+maLLVGoc },tx);
                tx.Commit();
                return true;
            }
            catch { tx.Rollback(); throw; }
        }

        public bool huyPhanCongThayThe(string maDon, string maLLVGoc)
        {
            using var connection = MoKetNoi();
            using var tx = connection.BeginTransaction(IsolationLevel.Serializable);
            try
            {
                int khoa = connection.ExecuteScalar<int>(@"DECLARE @k int;
                    EXEC @k=sys.sp_getapplock @Resource=N'RHM:PhanCong',@LockMode='Exclusive',
                    @LockOwner='Transaction',@LockTimeout=10000; SELECT @k",transaction:tx);
                if (khoa<0) throw new InvalidOperationException("Đang có người sửa phân ca.");
                var ma = connection.QuerySingleOrDefault<string>(@"SELECT TOP 1 tt.MaLLVThayThe
                    FROM dbo.PHAN_CONG_THAY_THE tt WITH(UPDLOCK,HOLDLOCK)
                    JOIN dbo.DON_XIN_NGHI d ON d.MaDonNghi=tt.MaDonNghi
                    WHERE tt.MaDonNghi=@MaDon AND tt.GhiChu=@GhiChu
                      AND d.TrangThaiDuyet=N'Đã duyệt' AND tt.TrangThai=N'Đã xác nhận'",
                    new { MaDon=maDon,GhiChu="Thay ca "+maLLVGoc },tx);
                if (string.IsNullOrWhiteSpace(ma)) return false;
                int daChuyen = connection.ExecuteScalar<int>(@"SELECT COUNT(*) FROM dbo.XAC_NHAN_DOI_LICH_KHAM
                    WHERE MaDonNghi=@MaDon AND MaLLVThayThe=@MaLLV AND TrangThai=N'Đã xác nhận'",
                    new { MaDon=maDon,MaLLV=ma },tx);
                if (daChuyen>0) throw new InvalidOperationException("Ca có lịch khám đã chuyển; không thể hủy người thay.");
                // Đổi người thay ca buộc xác nhận lại từng lịch khám của ca đó.
                connection.Execute(@"UPDATE xn SET TrangThai=N'Đã hủy'
                    FROM dbo.XAC_NHAN_DOI_LICH_KHAM xn
                    JOIN dbo.DON_XIN_NGHI d ON d.MaDonNghi=xn.MaDonNghi
                    JOIN dbo.LICH_KHAM l ON l.MaLich=xn.MaLich AND l.MaBS=d.MaNV
                    JOIN dbo.LICH_LAM_VIEC goc ON goc.MaLLV=@MaLLVGoc
                    WHERE xn.MaDonNghi=@MaDon AND xn.TrangThai=N'Đã xác nhận'
                      AND l.MaCa=goc.MaCa AND CONVERT(date,l.NgayGioKham)=goc.NgayLam",
                    new { MaDon=maDon,MaLLVGoc=maLLVGoc },tx);
                // Chỉ hủy ca do chính phương án thay thế này tạo, không hủy ca sẵn có.
                connection.Execute(@"UPDATE dbo.LICH_LAM_VIEC SET TrangThai=N'Hủy'
                    WHERE MaLLV=@MaLLV AND TrangThai=N'Đã xác nhận'
                      AND GhiChu LIKE N'%theo đơn '+@MaDon+'%'",
                    new { MaLLV=ma,MaDon=maDon },tx);
                connection.Execute(@"UPDATE dbo.PHAN_CONG_THAY_THE
                    SET TrangThai=N'Hủy',NgayXuLy=SYSDATETIME()
                    WHERE MaDonNghi=@MaDon AND MaLLVThayThe=@MaLLV AND TrangThai=N'Đã xác nhận'",
                    new { MaDon=maDon,MaLLV=ma },tx);
                tx.Commit();
                return true;
            }
            catch { tx.Rollback(); throw; }
        }

        public bool xuLyDon(string maDon, string maNguoiDuyet, bool duyet, string lyDoKhongDuyet)
        {
            using var connection = MoKetNoi();
            using var tx = connection.BeginTransaction(IsolationLevel.Serializable);
            try
            {
                int khoa = connection.ExecuteScalar<int>(@"DECLARE @k int;
                    EXEC @k=sys.sp_getapplock @Resource=N'RHM:PhanCong',@LockMode='Exclusive',
                    @LockOwner='Transaction',@LockTimeout=10000; SELECT @k", transaction: tx);
                if (khoa < 0) throw new InvalidOperationException("Có người đang xử lý lịch. Hãy thử lại.");
                var don = connection.QuerySingleOrDefault<ET_DON_XIN_NGHI>(
                    "SELECT * FROM dbo.DON_XIN_NGHI WITH(UPDLOCK,HOLDLOCK) WHERE MaDonNghi=@MaDon",
                    new { MaDon=maDon },tx);
                if (don==null || don.TrangThaiDuyet!="Chờ duyệt") return false;
                if (don.MaNV==maNguoiDuyet) throw new InvalidOperationException("Không thể tự duyệt đơn của mình.");

                if (duyet)
                {
                    // Duyệt trước, phân người thay và xác nhận từng lịch SAU.
                    int trung = connection.ExecuteScalar<int>(@"SELECT COUNT(*) FROM dbo.DON_XIN_NGHI WITH(UPDLOCK,HOLDLOCK)
                        WHERE MaDonNghi<>@MaDon AND MaNV=@MaNV AND TrangThaiDuyet=N'Đã duyệt'
                            AND (TuNgay<=@DenNgay OR @NghiViec=1)
                            AND (DenNgay>=@TuNgay OR GhiChu LIKE N'%Loại đơn: Nghỉ việc%')",
                        new { MaDon=maDon, don.MaNV, don.TuNgay, don.DenNgay,
                            NghiViec=LaDonNghiViec(don.GhiChu) },tx);
                    if (trung>0) throw new InvalidOperationException("Có đơn đã duyệt trùng kỳ nghỉ.");
                    // Không cập nhật bác sĩ/phòng của LICH_KHAM ở bước duyệt.
                    string trangThaiCa = "Nghỉ phép";
                    if (LaDonNghiViec(don.GhiChu)) trangThaiCa = "Nghỉ việc";
                    connection.Execute(@"UPDATE dbo.LICH_LAM_VIEC SET TrangThai=@TrangThai,
                            GhiChu=CONCAT(ISNULL(GhiChu,N''),N' | Nghỉ theo đơn ',@MaDon)
                        WHERE MaNV=@MaNV AND NgayLam>=@TuNgay
                          AND (NgayLam<=@DenNgay OR @NghiViec=1)
                          AND TrangThai IN (N'Chờ xác nhận',N'Đã xác nhận')",
                        new { MaNV=don.MaNV,MaDon=maDon,don.TuNgay,don.DenNgay,
                            NghiViec=LaDonNghiViec(don.GhiChu),TrangThai=trangThaiCa },tx);
                    TaoCaNghiThieu(connection,tx,don,trangThaiCa);
                    // Hủy phương án cũ chưa được thực hiện, tránh tự động đổi lịch.
                    connection.Execute(@"UPDATE llv SET TrangThai=N'Hủy'
                        FROM dbo.LICH_LAM_VIEC llv
                        JOIN dbo.PHAN_CONG_THAY_THE p ON p.MaLLVThayThe=llv.MaLLV
                        WHERE p.MaDonNghi=@MaDon AND p.TrangThai=N'Chờ duyệt'
                          AND llv.TrangThai=N'Dự kiến thay thế'",
                        new { MaDon=maDon },tx);
                    connection.Execute(@"UPDATE dbo.PHAN_CONG_THAY_THE SET TrangThai=N'Hủy'
                        WHERE MaDonNghi=@MaDon AND TrangThai=N'Chờ duyệt'",
                        new { MaDon=maDon },tx);
                    connection.Execute(@"UPDATE dbo.XAC_NHAN_DOI_LICH_KHAM SET TrangThai=N'Đã hủy'
                        WHERE MaDonNghi=@MaDon AND TrangThai=N'Đã xác nhận'",
                        new { MaDon=maDon },tx);
                }
                else
                {
                    connection.Execute(@"UPDATE dbo.XAC_NHAN_DOI_LICH_KHAM SET TrangThai=N'Đã hủy'
                        WHERE MaDonNghi=@MaDon AND TrangThai=N'Đã xác nhận'",
                        new { MaDon=maDon },tx);
                    connection.Execute(@"UPDATE l SET l.TrangThai=N'Hủy'
                        FROM dbo.LICH_LAM_VIEC l JOIN dbo.PHAN_CONG_THAY_THE p ON p.MaLLVThayThe=l.MaLLV
                        WHERE p.MaDonNghi=@MaDon AND p.TrangThai=N'Chờ duyệt' AND l.TrangThai=N'Dự kiến thay thế'",
                        new { MaDon=maDon },tx);
                    connection.Execute(@"UPDATE dbo.PHAN_CONG_THAY_THE SET TrangThai=N'Hủy',NgayXuLy=SYSDATETIME()
                        WHERE MaDonNghi=@MaDon AND TrangThai=N'Chờ duyệt'",new { MaDon=maDon },tx);
                }
                string trangThai = "Đã duyệt";
                string ghiChuTuChoi = "";
                if (!duyet) { trangThai="Không duyệt"; ghiChuTuChoi="\nLý do không duyệt: "+lyDoKhongDuyet; }
                int soDong = connection.Execute(@"UPDATE dbo.DON_XIN_NGHI SET TrangThaiDuyet=@TrangThai,
                    MaNVDuyet=@MaNguoiDuyet,NgayDuyet=SYSDATETIME(),
                    GhiChu=CONCAT(ISNULL(GhiChu,N''),@GhiChuTuChoi)
                    WHERE MaDonNghi=@MaDon AND TrangThaiDuyet=N'Chờ duyệt' AND MaNV<>@MaNguoiDuyet",
                    new { TrangThai=trangThai,MaNguoiDuyet=maNguoiDuyet,GhiChuTuChoi=ghiChuTuChoi,MaDon=maDon },tx);
                if (soDong!=1) throw new InvalidOperationException("Không thể cập nhật đơn xin nghỉ.");
                if (duyet) new DAL_NHANVIEN(tx).dongBoTrangThaiNghi();
                tx.Commit();
                return true;
            }
            catch { tx.Rollback(); throw; }
        }

        private static bool LaDonNghiViec(string? ghiChu)
        {
            if (string.IsNullOrWhiteSpace(ghiChu)) return false;
            return ghiChu.Contains("Loại đơn: Nghỉ việc",StringComparison.OrdinalIgnoreCase);
        }
    }
}
