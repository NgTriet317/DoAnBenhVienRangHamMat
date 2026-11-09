using System;
using System.Collections.Generic;
using DAL;
using ET;

namespace BUS
{
    /// <summary>
    /// Tầng nghiệp vụ nhập kho, nhận yêu cầu từ giao diện và điều phối lớp DAL tương ứng.
    /// </summary>
    public class BUS_NHAPKHO
    {
        /// <summary>
        /// Đối tượng truy cập dữ liệu nhập kho phục vụ các thao tác của lớp.
        /// </summary>
        private readonly DAL_NHAPKHO data = new DAL_NHAPKHO();

        /// <summary>
        /// Tạo phiếu nhập và các dòng hàng sau khi kiểm tra đầu vào, nguồn hóa đơn và các liên kết. Chuyển thao tác dữ liệu đến DAL; giữ kết quả trả về cho nơi gọi.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>KiemTra</c>, <c>XuLyLoi.ThucHien</c>, <c>data.TaoPhieu</c>, <c>lyDo.Trim</c>.</remarks>
        /// <param name="maHDN">Mã hóa đơn nhập xác định đối tượng cần xử lý.</param>
        /// <param name="maKho">Mã kho xác định đối tượng cần xử lý.</param>
        /// <param name="maNV">Mã nhân viên xác định đối tượng cần xử lý.</param>
        /// <param name="ngayNhap">Giá trị ngày nhập dùng khi thực hiện thao tác; kiểu DateTime.</param>
        /// <param name="lyDo">Giá trị lý do dùng khi thực hiện thao tác; kiểu string.</param>
        /// <param name="ghiChu">Giá trị ghi chú dùng khi thực hiện thao tác; kiểu string?.</param>
        /// <param name="cacDong">Giá trị các dòng dùng khi thực hiện thao tác; kiểu List&lt;ET_DONGNHAPMOI&gt;.</param>
        /// <param name="ghiSo">Giá trị ghi sổ dùng khi thực hiện thao tác; kiểu bool.</param>
        /// <returns>Chuỗi kết quả của thao tác tạo phiếu.</returns>
        public string TaoPhieu(string maHDN, string maKho, string maNV, DateTime ngayNhap,
            string lyDo, string? ghiChu, List<ET_DONGNHAPMOI> cacDong, bool ghiSo)
        {
            KiemTra(maHDN, maKho, maNV, ngayNhap, lyDo, cacDong);
            return XuLyLoi.ThucHien(() => data.TaoPhieu(maHDN, maKho, maNV, ngayNhap, lyDo.Trim(), ghiChu, cacDong, ghiSo));
        }

        /// <summary>
        /// Cập nhật phiếu nhập nháp và các dòng hàng trong giới hạn nghiệp vụ cho phép. Chuyển thao tác dữ liệu đến DAL; giữ kết quả trả về cho nơi gọi.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>KiemTra</c>, <c>XuLyLoi.ThucHien</c>, <c>data.CapNhatPhieu</c>, <c>lyDo.Trim</c>.</remarks>
        /// <param name="maPN">Mã phiếu nhập xác định đối tượng cần xử lý.</param>
        /// <param name="maHDN">Mã hóa đơn nhập xác định đối tượng cần xử lý.</param>
        /// <param name="maKho">Mã kho xác định đối tượng cần xử lý.</param>
        /// <param name="maNV">Mã nhân viên xác định đối tượng cần xử lý.</param>
        /// <param name="ngayNhap">Giá trị ngày nhập dùng khi thực hiện thao tác; kiểu DateTime.</param>
        /// <param name="lyDo">Giá trị lý do dùng khi thực hiện thao tác; kiểu string.</param>
        /// <param name="ghiChu">Giá trị ghi chú dùng khi thực hiện thao tác; kiểu string?.</param>
        /// <param name="cacDong">Giá trị các dòng dùng khi thực hiện thao tác; kiểu List&lt;ET_DONGNHAPMOI&gt;.</param>
        /// <param name="ghiSo">Giá trị ghi sổ dùng khi thực hiện thao tác; kiểu bool.</param>
        public void CapNhatPhieu(string maPN, string maHDN, string maKho, string maNV,
            DateTime ngayNhap, string lyDo, string? ghiChu, List<ET_DONGNHAPMOI> cacDong, bool ghiSo)
        {
            if (string.IsNullOrWhiteSpace(maPN))
                throw new InvalidOperationException("Chưa chọn phiếu cần sửa.");
            KiemTra(maHDN, maKho, maNV, ngayNhap, lyDo, cacDong);
            XuLyLoi.ThucHien(() =>
            {
                data.CapNhatPhieu(maPN, maHDN, maKho, maNV, ngayNhap, lyDo.Trim(), ghiChu, cacDong, ghiSo);
                return true;
            });
        }

        /// <summary>
        /// Kiểm tra các dữ liệu đầu vào và điều kiện nghiệp vụ trước khi chuyển sang bước lưu.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>dong.SoLo.Trim</c>, <c>decimal.Round</c>, <c>soLoMoi.Add</c>.</remarks>
        /// <param name="maHDN">Mã hóa đơn nhập xác định đối tượng cần xử lý.</param>
        /// <param name="maKho">Mã kho xác định đối tượng cần xử lý.</param>
        /// <param name="maNV">Mã nhân viên xác định đối tượng cần xử lý.</param>
        /// <param name="ngayNhap">Giá trị ngày nhập dùng khi thực hiện thao tác; kiểu DateTime.</param>
        /// <param name="lyDo">Giá trị lý do dùng khi thực hiện thao tác; kiểu string.</param>
        /// <param name="cacDong">Giá trị các dòng dùng khi thực hiện thao tác; kiểu List&lt;ET_DONGNHAPMOI&gt;.</param>
        private static void KiemTra(string maHDN, string maKho, string maNV, DateTime ngayNhap,
            string lyDo, List<ET_DONGNHAPMOI> cacDong)
        {
            if (string.IsNullOrWhiteSpace(maHDN) || string.IsNullOrWhiteSpace(maKho) || string.IsNullOrWhiteSpace(maNV))
                throw new InvalidOperationException("Phải chọn hóa đơn, kho và người lập phiếu.");
            if (string.IsNullOrWhiteSpace(lyDo))
                throw new InvalidOperationException("Vui lòng nhập lý do nhập kho.");
            if (ngayNhap == DateTime.MinValue || ngayNhap.Date > DateTime.Today)
                throw new InvalidOperationException("Ngày nhập không được lớn hơn ngày hiện tại.");
            if (cacDong == null || cacDong.Count == 0)
                throw new InvalidOperationException("Phiếu nhập phải có ít nhất một dòng vật tư.");

            HashSet<string> soLoMoi = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (ET_DONGNHAPMOI dong in cacDong)
            {
                if (string.IsNullOrWhiteSpace(dong.MaHH) || string.IsNullOrWhiteSpace(dong.SoLo))
                    throw new InvalidOperationException("Mỗi dòng cần có vật tư và số lô mới.");
                dong.SoLo = dong.SoLo.Trim();
                if (dong.SoLo.Length > 255)
                    throw new InvalidOperationException("Số lô không được dài quá 255 ký tự.");
                if (dong.SoLuong <= 0 || decimal.Round(dong.SoLuong, 3) != dong.SoLuong)
                    throw new InvalidOperationException("Số lượng phải lớn hơn 0 và tối đa 3 chữ số thập phân.");
                if (dong.DonGia < 0 || decimal.Round(dong.DonGia, 2) != dong.DonGia)
                    throw new InvalidOperationException("Đơn giá không được âm và tối đa 2 chữ số thập phân.");
                if (dong.HanSuDung.HasValue && dong.HanSuDung.Value.Date < ngayNhap.Date)
                    throw new InvalidOperationException("Hạn sử dụng phải từ ngày nhập trở đi.");
                if (!soLoMoi.Add(dong.MaHH + "\u001f" + dong.SoLo))
                    throw new InvalidOperationException("Cùng mặt hàng không thể có hai dòng cùng số lô trong một phiếu.");
            }
        }

        /// <summary>
        /// Kiểm tra phiếu và ghi sổ nhập/xuất để các thay đổi tồn kho có hiệu lực theo nghiệp vụ. Chuyển thao tác dữ liệu đến DAL; giữ kết quả trả về cho nơi gọi.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>XuLyLoi.ThucHien</c>, <c>data.GhiSo</c>.</remarks>
        /// <param name="maPN">Mã phiếu nhập xác định đối tượng cần xử lý.</param>
        public void GhiSo(string maPN)
        {
            if (string.IsNullOrWhiteSpace(maPN))
                throw new InvalidOperationException("Chưa chọn phiếu nhập cần ghi sổ.");
            XuLyLoi.ThucHien(() => { data.GhiSo(maPN); return true; });
        }
    }
}
