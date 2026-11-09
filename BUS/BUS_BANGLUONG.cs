using System;
using System.Data;
using DAL;

namespace BUS
{
    /// <summary>
    /// Tầng nghiệp vụ bảng lương, nhận yêu cầu từ giao diện và điều phối lớp DAL tương ứng.
    /// </summary>
    public class BUS_BANGLUONG
    {
        /// <summary>
        /// Đối tượng truy cập dữ liệu bảng lương phục vụ các thao tác của lớp.
        /// </summary>
        private readonly DAL_BANGLUONG dalBangLuong = new DAL_BANGLUONG();

        /// <summary>
        /// Kiểm tra điều kiện sử dụng cấu trúc tính lương V9. Chuyển thao tác dữ liệu đến DAL; giữ kết quả trả về cho nơi gọi.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>dalBangLuong.suDungSchemaV9</c>.</remarks>
        /// <returns>Kết quả đúng/sai của điều kiện hoặc thao tác được biểu diễn trong thân hàm; lỗi có thể được phát ra bằng ngoại lệ.</returns>
        public bool suDungSchemaV9() => dalBangLuong.suDungSchemaV9();
        /// <summary>
        /// Kiểm tra thành phần SQL tính lương cần thiết đã được cài đặt. Chuyển thao tác dữ liệu đến DAL; giữ kết quả trả về cho nơi gọi.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>dalBangLuong.daCaiTinhLuong</c>.</remarks>
        /// <returns>Kết quả đúng/sai của điều kiện hoặc thao tác được biểu diễn trong thân hàm; lỗi có thể được phát ra bằng ngoại lệ.</returns>
        public bool daCaiTinhLuong() => dalBangLuong.daCaiTinhLuong();
        /// <summary>
        /// Đọc ngày của SQL Server để thống nhất mốc xử lý kỳ trên giao diện. Chuyển thao tác dữ liệu đến DAL; giữ kết quả trả về cho nơi gọi.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>dalBangLuong.layNgayMayChu</c>.</remarks>
        /// <returns>Kết quả kiểu DateTime dùng cho bước xử lý tiếp theo.</returns>
        public DateTime layNgayMayChu() => dalBangLuong.layNgayMayChu();

        /// <summary>
        /// Đọc phiếu lương theo tháng/năm để hiển thị và đối soát. Chuyển thao tác dữ liệu đến DAL; giữ kết quả trả về cho nơi gọi.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>kiemTraKy</c>, <c>dalBangLuong.layBangLuongTheoKy</c>.</remarks>
        /// <param name="tuNgay">Giá trị từ ngày dùng khi thực hiện thao tác; kiểu DateTime.</param>
        /// <param name="denNgay">Giá trị đến ngày dùng khi thực hiện thao tác; kiểu DateTime.</param>
        /// <returns>Kết quả kiểu DataTable dùng cho bước xử lý tiếp theo.</returns>
        public DataTable layBangLuongTheoKy(DateTime tuNgay, DateTime denNgay)
        {
            kiemTraKy(tuNgay, denNgay);
            return dalBangLuong.layBangLuongTheoKy(tuNgay, denNgay);
        }

        /// <summary>
        /// Đọc dữ liệu đối soát kỳ lương để người dùng kiểm tra trước khi tính/chốt. Chuyển thao tác dữ liệu đến DAL; giữ kết quả trả về cho nơi gọi.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>kiemTraKy</c>, <c>dalBangLuong.layDoiSoatTheoKy</c>.</remarks>
        /// <param name="tuNgay">Giá trị từ ngày dùng khi thực hiện thao tác; kiểu DateTime.</param>
        /// <param name="denNgay">Giá trị đến ngày dùng khi thực hiện thao tác; kiểu DateTime.</param>
        /// <returns>Kết quả kiểu DataTable dùng cho bước xử lý tiếp theo.</returns>
        public DataTable layDoiSoatTheoKy(DateTime tuNgay, DateTime denNgay)
        {
            kiemTraKy(tuNgay, denNgay);
            return dalBangLuong.layDoiSoatTheoKy(tuNgay, denNgay);
        }

        /// <summary>
        /// Đếm các ca công còn chờ xử lý trong kỳ phục vụ đối soát lương. Chuyển thao tác dữ liệu đến DAL; giữ kết quả trả về cho nơi gọi.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>kiemTraKy</c>, <c>dalBangLuong.demCongChoDoiSoat</c>.</remarks>
        /// <param name="tuNgay">Giá trị từ ngày dùng khi thực hiện thao tác; kiểu DateTime.</param>
        /// <param name="denNgay">Giá trị đến ngày dùng khi thực hiện thao tác; kiểu DateTime.</param>
        /// <returns>Giá trị số được đọc, đếm hoặc tính toán trong hàm.</returns>
        public int demCongChoDoiSoat(DateTime tuNgay, DateTime denNgay)
        {
            kiemTraKy(tuNgay, denNgay);
            return dalBangLuong.demCongChoDoiSoat(tuNgay, denNgay);
        }

        // Tính lại toàn bộ các phiếu NHÁP từ chứng từ, không động đến kỳ đã chốt.
        public void tinhLuongTheoKy(DateTime tuNgay, DateTime denNgay, string maNVQuanLy)
        {
            kiemTraKy(tuNgay, denNgay);
            if (string.IsNullOrWhiteSpace(maNVQuanLy))
                throw new ArgumentException("Chưa xác định người tính lương.");
            dalBangLuong.tinhLuongTheoKy(tuNgay, denNgay, maNVQuanLy);
        }

        // Chốt từng phiếu, đối chiếu lại số công/tiền đã xem để tránh chốt số liệu cũ.
        public void chotLuongNhanVien(string maBL, string maNVQuanLy, decimal congDaXem,
            decimal tongDaXem, decimal hoaHongDaXem)
        {
            if (string.IsNullOrWhiteSpace(maBL) || string.IsNullOrWhiteSpace(maNVQuanLy))
                throw new ArgumentException("Chưa chọn phiếu lương hoặc chưa xác định người chốt.");
            if (congDaXem < 0 || hoaHongDaXem < 0)
                throw new ArgumentException("Số công và hoa hồng phải hợp lệ.");
            dalBangLuong.chotLuongNhanVien(maBL, maNVQuanLy, congDaXem, tongDaXem, hoaHongDaXem);
        }

        /// <summary>
        /// Kiểm tra khoảng từ ngày đến ngày bao phủ đúng một tháng đầy đủ trước khi xử lý kỳ lương.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>tuNgay.AddMonths</c>.</remarks>
        /// <param name="tuNgay">Giá trị từ ngày dùng khi thực hiện thao tác; kiểu DateTime.</param>
        /// <param name="denNgay">Giá trị đến ngày dùng khi thực hiện thao tác; kiểu DateTime.</param>
        private static void kiemTraKy(DateTime tuNgay, DateTime denNgay)
        {
            if (tuNgay.Year != denNgay.Year || tuNgay.Month != denNgay.Month ||
                tuNgay.Day != 1 || denNgay.Date != tuNgay.AddMonths(1).AddDays(-1).Date)
                throw new ArgumentException("Kỳ lương phải là một tháng đầy đủ.");
        }
    }
}
