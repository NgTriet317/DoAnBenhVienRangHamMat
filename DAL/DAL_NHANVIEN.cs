using Dapper;
using Dapper.Contrib.Extensions;
using ET;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DAL
{
    public class DAL_NHANVIEN : dbconnect
    {
        public IEnumerable<ET_NHANVIEN> layToanBoNV()
        {
            try
            {
                using var connection = CreateConnection();
                return connection.GetAll<ET_NHANVIEN>();
            }
            catch
            {
                using var connection = CreateConnection();
                return connection.Query<ET_NHANVIEN>("SELECT * FROM NHAN_VIEN");
            }
        }

        public ET_NHANVIEN layNhanVienTheoMa(string maNV)
        {
            using var connection = CreateConnection();
            return connection.Query<ET_NHANVIEN>("SELECT * FROM NHAN_VIEN WHERE MaNV = @MaNV", new { MaNV = maNV }).FirstOrDefault();
        }

        public bool themNhanVien(ET_NHANVIEN nhanVien)
        {
            try
            {
                using var connection = CreateConnection();
                connection.Insert(nhanVien);
                return true;
            }
            catch
            {
                try
                {
                    using var connection = CreateConnection();
                    int affected = connection.Execute(
                        @"INSERT INTO NHAN_VIEN (MaNV, HoTen, GioiTinh, NgaySinh, SDT, DiaChi, MaChucVu, MaCK, BangCap, NgayVaoLam, TrangThai)
                          VALUES (@MaNV, @HoTen, @GioiTinh, @NgaySinh, @SDT, @DiaChi, @MaChucVu, @MaCK, @BangCap, @NgayVaoLam, @TrangThai)",
                        nhanVien);
                    return affected > 0;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool capNhatNhanVien(ET_NHANVIEN nhanVien)
        {
            try
            {
                using var connection = CreateConnection();
                return connection.Update(nhanVien);
            }
            catch
            {
                try
                {
                    using var connection = CreateConnection();
                    int affected = connection.Execute(
                        @"UPDATE NHAN_VIEN 
                          SET HoTen = @HoTen, GioiTinh = @GioiTinh, NgaySinh = @NgaySinh, 
                              SDT = @SDT, DiaChi = @DiaChi, MaChucVu = @MaChucVu, 
                              MaCK = @MaCK, BangCap = @BangCap, NgayVaoLam = @NgayVaoLam, 
                              TrangThai = @TrangThai 
                          WHERE MaNV = @MaNV",
                        nhanVien);
                    return affected > 0;
                }
                catch
                {
                    return false;
                }
            }
        }

        // Cập nhật duy nhất trường trạng thái, tránh lưu nhầm các thay đổi chưa được xác nhận ở form.
        public bool khoiPhucNhanVien(string maNV)
        {
            try
            {
                using var connection = CreateConnection();
                int affected = connection.Execute(
                    "UPDATE NHAN_VIEN SET TrangThai = N'Đang làm việc' WHERE MaNV = @MaNV AND TrangThai = N'Đã nghỉ'",
                    new { MaNV = maNV });
                return affected > 0;
            }
            catch
            {
                return false;
            }
        }

        public bool nghiViecNhanVien(string maNV)
        {
            try
            {
                using var connection = CreateConnection();
                int affected = connection.Execute(
                    "UPDATE NHAN_VIEN SET TrangThai = N'Đã nghỉ' WHERE MaNV = @MaNV",
                    new { MaNV = maNV });
                return affected > 0;
            }
            catch
            {
                return false;
            }
        }
    }
}