using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using ET;

namespace DAL
{
    public class DAL_CHAMCONG : dbconnect
    {
        public DataTable layChamCongTheoThang(DateTime dauThang)
        {
            using var ketNoi = (SqlConnection)CreateConnection();
            using var lenh = ketNoi.CreateCommand();
            lenh.CommandText = @"
                SELECT l.MaLLV,l.MaNV,n.HoTen,ISNULL(cv.TenChucVu,N'Chưa phân loại') AS TenChucVu,
                    l.MaCa,l.NgayLam,c.GioBatDau,c.GioKetThuc,l.GioVao,l.GioRa,
                    l.TrangThai AS TrangThaiLich,l.TrangThaiHienDien,
                    ISNULL(cc.TrangThai,N'Chờ đối soát') AS TrangThai,
                    ISNULL(cc.SoPhutDiTre,0) AS SoPhutDiTre,
                    ISNULL(cc.CongDuocTinh,0) AS CongDuocTinh,
                    cc.MaNVDoiSoat,cc.NgayDoiSoat,cc.GhiChu,
                    CAST(CASE WHEN EXISTS(SELECT 1 FROM dbo.DON_XIN_NGHI dn
                      WHERE dn.MaNV=l.MaNV AND dn.TrangThaiDuyet=N'Đã duyệt'
                        AND l.NgayLam BETWEEN dn.TuNgay AND dn.DenNgay) THEN 1 ELSE 0 END AS BIT) AS CoDonNghi,
                    CAST(CASE WHEN EXISTS(SELECT 1 FROM dbo.BANG_LUONG bl
                      WHERE bl.MaNV=l.MaNV AND bl.TrangThai=N'Đã chốt'
                        AND l.NgayLam BETWEEN bl.TuNgay AND bl.DenNgay) THEN 1 ELSE 0 END AS BIT) AS DaChot
                FROM dbo.LICH_LAM_VIEC l
                JOIN dbo.CA_LAM c ON c.MaCa=l.MaCa
                JOIN dbo.NHAN_VIEN n ON n.MaNV=l.MaNV
                LEFT JOIN dbo.CHUC_VU cv ON cv.MaChucVu=n.MaChucVu
                LEFT JOIN dbo.CHAM_CONG cc ON cc.MaLLV=l.MaLLV
                WHERE l.NgayLam>=@TuNgay AND l.NgayLam<@DenNgay
                ORDER BY l.NgayLam,l.MaCa,n.HoTen";
            lenh.Parameters.Add(new SqlParameter("@TuNgay",SqlDbType.Date){Value=dauThang.Date});
            lenh.Parameters.Add(new SqlParameter("@DenNgay",SqlDbType.Date){Value=dauThang.AddMonths(1).Date});
            ketNoi.Open();
            using var doc = lenh.ExecuteReader();
            var duLieu = new DataTable();
            duLieu.Load(doc);
            return duLieu;
        }

        // Toàn bộ duyệt hàng loạt được thực hiện trong một giao dịch; nếu một ca sai thì không lưu ca nào.
        public int duyetChamCong(IReadOnlyList<ET_CHAMCONG> danhSach, string maNVQuanLy)
        {
            using var ketNoi = (SqlConnection)CreateConnection();
            ketNoi.Open();
            using var giaoDich = ketNoi.BeginTransaction(IsolationLevel.Serializable);
            try
            {
                using (var quyen = new SqlCommand(@"
                  IF NOT EXISTS(SELECT 1 FROM dbo.NHAN_VIEN WHERE MaNV=@MaNV)
                    THROW 51100,N'Nhân viên duyệt không tồn tại.',1;
                  IF OBJECT_ID('dbo.NHAT_KY_DUYET_CHAM_CONG','U') IS NULL
                    THROW 51101,N'Cần chạy SQL nâng cấp đối soát chấm công trước.',1;",ketNoi,giaoDich))
                {
                    quyen.Parameters.AddWithValue("@MaNV",maNVQuanLy);
                    quyen.ExecuteNonQuery();
                }
                // Khóa chung để hai BQL không đồng thời duyệt, tránh phát sinh mã trùng.
                using (var khoa = new SqlCommand(@"
                  DECLARE @ketQua int;
                  EXEC @ketQua=sys.sp_getapplock @Resource=N'RHM:DuyetChamCong',
                    @LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=10000;
                  IF @ketQua<0 THROW 51102,N'Có người đang duyệt công, vui lòng thử lại.',1;",ketNoi,giaoDich))
                    khoa.ExecuteNonQuery();
                var daChon = new HashSet<string>(StringComparer.Ordinal);
                foreach (ET_CHAMCONG ca in danhSach)
                {
                    if (!daChon.Add(ca.MaLLV)) throw new InvalidOperationException("Danh sách có ca bị trùng.");
                    duyetMotCa(ketNoi,giaoDich,ca,maNVQuanLy);
                }
                giaoDich.Commit();
                return danhSach.Count;
            }
            catch
            {
                try { giaoDich.Rollback(); } catch (InvalidOperationException) { } catch (SqlException) { }
                throw;
            }
        }

        private static void duyetMotCa(SqlConnection ketNoi, SqlTransaction giaoDich,
            ET_CHAMCONG ca, string maNVQuanLy)
        {
            using var lenh = new SqlCommand(@"
              SELECT l.MaNV,l.NgayLam,l.TrangThai AS TrangThaiLich,
                     l.GioVao,l.GioRa,c.GioBatDau,c.GioKetThuc,
                     cc.MaCC,ISNULL(cc.TrangThai,N'Chờ đối soát') AS TrangThai,
                     cc.CongDuocTinh,cc.SoPhutDiTre
              FROM dbo.LICH_LAM_VIEC l WITH(UPDLOCK,HOLDLOCK)
              JOIN dbo.CA_LAM c ON c.MaCa=l.MaCa
              LEFT JOIN dbo.CHAM_CONG cc WITH(UPDLOCK,HOLDLOCK) ON cc.MaLLV=l.MaLLV
              WHERE l.MaLLV=@MaLLV",ketNoi,giaoDich);
            lenh.Parameters.AddWithValue("@MaLLV",ca.MaLLV);
            var hienTai = new DataTable();
            using (var doc = lenh.ExecuteReader()) hienTai.Load(doc);
            if (hienTai.Rows.Count!=1) throw new InvalidOperationException("Không tìm thấy ca làm: "+ca.MaLLV);
            DataRow dong = hienTai.Rows[0];
            string maNV=dong.Field<string>("MaNV") ?? "";
            DateTime ngay=dong.Field<DateTime>("NgayLam").Date;
            string trangThai=dong.Field<string>("TrangThai") ?? "";
            DateTime? gioVaoCu=dong.Field<DateTime?>("GioVao");
            DateTime? gioRaCu=dong.Field<DateTime?>("GioRa");
            if (trangThai!=ca.TrangThaiCu || gioVaoCu!=ca.GioVaoCu || gioRaCu!=ca.GioRaCu)
                throw new InvalidOperationException("Ca đã được người khác cập nhật, vui lòng tải lại: "+ca.MaLLV);
            if (ngay>DateTime.Today) throw new InvalidOperationException("Không được duyệt ca ở tương lai.");
            // Không yêu cầu làm đủ giờ ca mới được BQL xác nhận công.
            string trangThaiLich=dong.Field<string>("TrangThaiLich") ?? "";
            if (trangThaiLich=="Hủy" || trangThaiLich=="Từ chối" || trangThaiLich=="Nghỉ phép" || trangThaiLich=="Nghỉ việc")
                throw new InvalidOperationException("Không thể chấm công lịch đã hủy/nghỉ: "+ca.MaLLV);
            using (var kiemTra = new SqlCommand(@"
                IF EXISTS(SELECT 1 FROM dbo.BANG_LUONG WITH(UPDLOCK,HOLDLOCK)
                  WHERE MaNV=@MaNV AND TrangThai=N'Đã chốt' AND @Ngay BETWEEN TuNgay AND DenNgay)
                  THROW 51103,N'Kỳ lương đã chốt, không thể thay đổi chấm công.',1;
                IF EXISTS(SELECT 1 FROM dbo.DON_XIN_NGHI WITH(UPDLOCK,HOLDLOCK)
                  WHERE MaNV=@MaNV AND TrangThaiDuyet=N'Đã duyệt' AND @Ngay BETWEEN TuNgay AND DenNgay)
                  THROW 51104,N'Ngày này đã có đơn nghỉ được duyệt. Không được tính trùng công.',1;",ketNoi,giaoDich))
            {
                kiemTra.Parameters.AddWithValue("@MaNV",maNV);
                kiemTra.Parameters.Add(new SqlParameter("@Ngay",SqlDbType.Date){Value=ngay});
                kiemTra.ExecuteNonQuery();
            }
            if (ca.LyDo.Length>500)
                throw new InvalidOperationException("Ghi chú/lý do không được quá 500 ký tự.");
            if ((trangThai=="Đã xác nhận" || (ca.KetQua=="Vắng" && (gioVaoCu.HasValue || gioRaCu.HasValue))) && ca.LyDo.Trim().Length<5)
                throw new InvalidOperationException("Sửa ca đã duyệt phải ghi lý do ít nhất 5 ký tự.");
            if (ca.KetQua=="Không hợp lệ" && ca.LyDo.Trim().Length<5)
                throw new InvalidOperationException("Ca không hợp lệ phải có lý do ít nhất 5 ký tự.");
            int soPhut=0;
            decimal cong=0;
            string hienDien="Vắng";
            DateTime? gioVao=null;
            DateTime? gioRa=null;
            if (ca.KetQua=="Hoàn tất")
            {
                gioVao=ca.GioVao;
                gioRa=ca.GioRa;
                DateTime batDau=ngay+dong.Field<TimeSpan>("GioBatDau");
                if (!gioVao.HasValue || !gioRa.HasValue || gioVao.Value.Date!=ngay ||
                    gioRa.Value<gioVao.Value || gioRa.Value>DateTime.Now)
                    throw new InvalidOperationException("Giờ vào/ra không hợp lệ hoặc nằm trong tương lai: "+ca.MaLLV);
                soPhut=Math.Max(0,(int)Math.Floor((gioVao.Value-batDau).TotalMinutes));
                cong=0.5m; // Mỗi ca hoàn tất là 0,5 công, không phụ thuộc thời lượng ca.
                hienDien="Có mặt";
            }
            if (ca.KetQua=="Không hợp lệ")
            {
                // Giữ giờ gốc để BQL vẫn có bằng chứng của ca chưa hợp lệ.
                hienDien="Chưa xác định";
                gioVao=gioVaoCu;
                gioRa=gioRaCu;
            }
            string trangThaiMoi=ca.KetQua=="Không hợp lệ"?"Không hợp lệ":"Đã xác nhận";
            string? maCC=dong.Field<string?>("MaCC");
            if (string.IsNullOrWhiteSpace(maCC)) maCC="CC"+Guid.NewGuid().ToString("N");
            using var luu = new SqlCommand(@"
                UPDATE dbo.LICH_LAM_VIEC SET GioVao=@GioVao,GioRa=@GioRa,
                    TrangThaiHienDien=@HienDien WHERE MaLLV=@MaLLV;
                IF EXISTS(SELECT 1 FROM dbo.CHAM_CONG WHERE MaLLV=@MaLLV)
                    UPDATE dbo.CHAM_CONG SET SoPhutDiTre=@SoPhut,CongDuocTinh=@Cong,
                        TrangThai=@TrangThai,MaNVDoiSoat=@NguoiDuyet,NgayDoiSoat=SYSDATETIME(),
                        GhiChu=@GhiChu WHERE MaLLV=@MaLLV;
                ELSE
                    INSERT dbo.CHAM_CONG(MaCC,MaLLV,SoPhutDiTre,CongDuocTinh,TrangThai,MaNVDoiSoat,NgayDoiSoat,GhiChu)
                    VALUES (@MaCC,@MaLLV,@SoPhut,@Cong,@TrangThai,@NguoiDuyet,SYSDATETIME(),@GhiChu);
                INSERT dbo.NHAT_KY_DUYET_CHAM_CONG(MaLLV,MaNV,NguoiThucHien,NgayThucHien,
                    TrangThaiCu,TrangThaiMoi,GioVaoCu,GioRaCu,GioVaoMoi,GioRaMoi,
                    CongCu,CongMoi,LyDo)
                VALUES (@MaLLV,@MaNV,@NguoiDuyet,SYSDATETIME(),@TrangThaiCu,@TrangThai,
                    @GioVaoCu,@GioRaCu,@GioVao,@GioRa,@CongCu,@Cong,@GhiChu);",ketNoi,giaoDich);
            luu.Parameters.AddWithValue("@MaLLV",ca.MaLLV);
            luu.Parameters.AddWithValue("@MaCC",maCC);
            luu.Parameters.AddWithValue("@MaNV",maNV);
            luu.Parameters.AddWithValue("@NguoiDuyet",maNVQuanLy);
            luu.Parameters.AddWithValue("@TrangThai",trangThaiMoi);
            luu.Parameters.AddWithValue("@TrangThaiCu",trangThai);
            luu.Parameters.AddWithValue("@HienDien",hienDien);
            luu.Parameters.AddWithValue("@SoPhut",soPhut);
            luu.Parameters.Add(new SqlParameter("@Cong",SqlDbType.Decimal){Precision=4,Scale=2,Value=cong});
            luu.Parameters.Add(new SqlParameter("@CongCu",SqlDbType.Decimal){Precision=4,Scale=2,Value=dong.Field<decimal?>("CongDuocTinh") ?? 0m});
            foreach (var pair in new[]{("@GioVao",gioVao),("@GioRa",gioRa),("@GioVaoCu",gioVaoCu),("@GioRaCu",gioRaCu)})
                luu.Parameters.Add(new SqlParameter(pair.Item1,SqlDbType.DateTime2){Value=(object?)pair.Item2 ?? DBNull.Value});
            luu.Parameters.AddWithValue("@GhiChu",ca.LyDo.Trim());
            luu.ExecuteNonQuery();
        }
    }
}
