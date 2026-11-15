using System;
using System.Data;
using Microsoft.Data.SqlClient;

namespace DAL
{
    // Lấy số liệu từ schema quỹ phép đã gộp; không viết lại phép tính trong giao diện.
    public class DAL_QUYPHEP : dbconnect
    {
        public DataTable layDanhSachQuyPhep(int nam)
        {
            using var ketNoi = (SqlConnection)CreateConnection();
            using var lenh = ketNoi.CreateCommand();
            lenh.CommandText = "dbo.RHM_DanhSachQuyPhep";
            lenh.CommandType = CommandType.StoredProcedure;
            lenh.Parameters.Add(new SqlParameter("@Nam", SqlDbType.Int) { Value = nam });
            ketNoi.Open();
            using var doc = lenh.ExecuteReader();
            var ketQua = new DataTable();
            ketQua.Load(doc);
            return ketQua;
        }

        public DataTable layQuyPhep(string maNV, int nam)
        {
            using var ketNoi = (SqlConnection)CreateConnection();
            using var lenh = ketNoi.CreateCommand();
            lenh.CommandText = "dbo.RHM_LayQuyPhep";
            lenh.CommandType = CommandType.StoredProcedure;
            lenh.Parameters.Add(new SqlParameter("@MaNV", SqlDbType.NVarChar, 50) { Value = maNV });
            lenh.Parameters.Add(new SqlParameter("@Nam", SqlDbType.Int) { Value = nam });
            ketNoi.Open();
            using var doc = lenh.ExecuteReader();
            var ketQua = new DataTable();
            ketQua.Load(doc);
            return ketQua;
        }

        public DataTable layNgayPhepNhanVien(string maNV, int nam)
        {
            using var ketNoi = (SqlConnection)CreateConnection();
            using var lenh = ketNoi.CreateCommand();
            lenh.CommandText = @"
                SELECT p.NgayNghi,p.MaDonNghi,d.LoaiNghi,d.TrangThaiDuyet,
                       p.CongHuongLuong,p.NgayGhiNhan,d.LyDo
                FROM dbo.RHM_NGAY_PHEP_NAM p
                JOIN dbo.DON_XIN_NGHI d ON d.MaDonNghi=p.MaDonNghi
                WHERE p.MaNV=@MaNV AND p.NgayNghi>=@Dau AND p.NgayNghi<@Cuoi
                  AND d.TrangThaiDuyet=N'Đã duyệt' AND d.LoaiNghi=N'Nghỉ phép năm'
                ORDER BY p.NgayNghi DESC,p.MaDonNghi";
            lenh.Parameters.Add(new SqlParameter("@MaNV", SqlDbType.NVarChar, 50) { Value = maNV });
            lenh.Parameters.Add(new SqlParameter("@Dau", SqlDbType.Date) { Value = new DateTime(nam, 1, 1) });
            lenh.Parameters.Add(new SqlParameter("@Cuoi", SqlDbType.Date) { Value = new DateTime(nam + 1, 1, 1) });
            ketNoi.Open();
            using var doc = lenh.ExecuteReader();
            var ketQua = new DataTable();
            ketQua.Load(doc);
            return ketQua;
        }

        public DataTable layCauHinh(int nam)
        {
            using var ketNoi = (SqlConnection)CreateConnection();
            using var lenh = ketNoi.CreateCommand();
            lenh.CommandText = @"SELECT TOP (1) Nam,SoNgayPhepNam,SoThangTieuChuan
                                 FROM dbo.RHM_CAU_HINH_QUY_PHEP WHERE Nam<=@Nam ORDER BY Nam DESC";
            lenh.Parameters.Add(new SqlParameter("@Nam", SqlDbType.Int) { Value = nam });
            ketNoi.Open();
            using var doc = lenh.ExecuteReader();
            var ketQua = new DataTable();
            ketQua.Load(doc);
            return ketQua;
        }

        // Dự kiến dựa trên ca đã xếp; ngày thực tế ghi quỹ do SQL xác nhận tại lúc duyệt.
        public int demNgayDuKien(string maNV, DateTime tuNgay, DateTime denNgay)
        {
            using var ketNoi = (SqlConnection)CreateConnection();
            using var lenh = ketNoi.CreateCommand();
            lenh.CommandText = @"
                SELECT COUNT(DISTINCT ll.NgayLam)
                FROM dbo.LICH_LAM_VIEC ll
                WHERE ll.MaNV=@MaNV AND ll.NgayLam>=@TuNgay AND ll.NgayLam<=@DenNgay
                  AND ll.TrangThai NOT IN (N'Hủy',N'Từ chối',N'Nghỉ việc')
                  AND DATEDIFF(DAY,CONVERT(date,'19000107'),ll.NgayLam)%7<>0
                  AND NOT EXISTS (
                    SELECT 1 FROM dbo.NGAY_NGHI_LE h
                    WHERE h.NgayNghi=ll.NgayLam AND h.CoHuongLuong=1)";
            lenh.Parameters.Add(new SqlParameter("@MaNV", SqlDbType.NVarChar, 50) { Value = maNV });
            lenh.Parameters.Add(new SqlParameter("@TuNgay", SqlDbType.Date) { Value = tuNgay.Date });
            lenh.Parameters.Add(new SqlParameter("@DenNgay", SqlDbType.Date) { Value = denNgay.Date });
            ketNoi.Open();
            return Convert.ToInt32(lenh.ExecuteScalar());
        }
    }
}
