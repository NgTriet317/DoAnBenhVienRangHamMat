using DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace BUS
{
    /// <summary>Quản lý danh mục phân hệ và quyền truy cập theo vai trò.</summary>
    public class BUS_PHANQUYENVAITRO
    {
        private readonly DAL_PHANQUYENVAITRO data = new();
        private static int phienBan;
        public static int PhienBan => Volatile.Read(ref phienBan);

        // Phải khớp tên Tag của các ứng dụng trong MainWindow.xaml.cs.
        public static readonly (string Ma, string Ten)[] CacPhanHe =
        {
            ("TongQuan", "Tổng quan"),
            ("BenhNhan", "Hồ sơ bệnh nhân"),
            ("KhamDieuTri", "Khám điều trị"),
            ("DichVuThucHien", "Dịch vụ thực hiện"),
            ("DonThuoc", "Đơn thuốc"),
            ("NhanSu", "Bác sĩ và nhân sự"),
            ("ChungChiNhanVien", "Chứng chỉ nhân viên"),
            ("CaLamViec", "Ca làm việc"),
            ("ThietBi", "Thiết bị"),
            ("BaoTri", "Bảo trì"),
            ("Kho", "Tồn kho"),
            ("DanhMucHangHoa", "Danh mục hàng hóa"),
            ("NhaCungCap", "Nhà cung cấp"),
            ("NhapXuatVatTu", "Nhập xuất vật tư"),
            ("LichKham", "Điều phối lịch khám"),
            ("DichVu", "Danh mục dịch vụ"),
            ("HoaDon", "Hóa đơn và thanh toán"),
            ("BangLuong", "Bảng lương"),
            ("ChamCong", "Chấm công"),
            ("ChinhSachLuong", "Chính sách lương"),
            ("TaiKhoan", "Tài khoản và vai trò"),
            ("BaoCao", "Báo cáo"),
            ("PhieuHuongDan", "Phiếu hướng dẫn"),
            ("NhatKy", "Nhật ký hệ thống"),
            ("XinNghiViec", "Xin nghỉ phép / nghỉ việc")
        };

        public IEnumerable<string> layQuyenTheoVaiTro(string maVaiTro)
            => data.layQuyenTheoVaiTro(maVaiTro);

        public IEnumerable<string> layQuyenTheoTaiKhoan(string maTK)
            => data.layQuyenTheoTaiKhoan(maTK);

        public bool taiKhoanConHoatDong(string maTK)
            => data.taiKhoanConHoatDong(maTK);

        public void luuQuyenVaiTro(string maNguoiSua, string maVaiTro,
                                  IEnumerable<string> cacPhanHeDuocCap)
        {
            if (string.IsNullOrWhiteSpace(maNguoiSua) || string.IsNullOrWhiteSpace(maVaiTro))
                throw new InvalidOperationException("Phiên đăng nhập hoặc vai trò không hợp lệ.");
            var hopLe = CacPhanHe.Select(x => x.Ma).ToHashSet(StringComparer.Ordinal);
            var duocCap = cacPhanHeDuocCap.Distinct(StringComparer.Ordinal).ToArray();
            if (duocCap.Any(x => !hopLe.Contains(x)))
                throw new InvalidOperationException("Danh sách phân hệ có mã không hợp lệ.");
            data.luuQuyenVaiTro(maNguoiSua, maVaiTro, CacPhanHe.Select(x => x.Ma).ToArray(), duocCap);
            Interlocked.Increment(ref phienBan);
        }
    }
}
