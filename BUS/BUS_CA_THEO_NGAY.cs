using System;
using System.Collections.Generic;
using DAL;
using ET;

namespace BUS
{
    /// <summary>
    /// Tầng nghiệp vụ ngày, nhận yêu cầu từ giao diện và điều phối lớp DAL tương ứng.
    /// </summary>
    public class BUS_CA_THEO_NGAY
    {
        /// <summary>
        /// Đối tượng truy cập dữ liệu ngày phục vụ các thao tác của lớp.
        /// </summary>
        private readonly DAL_CA_THEO_NGAY dal = new DAL_CA_THEO_NGAY();

        /// <summary>
        /// Đọc các ca theo khoảng tuần được chọn để dựng lịch làm việc. Chuyển thao tác dữ liệu đến DAL; giữ kết quả trả về cho nơi gọi.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>dal.layCaTheoTuan</c>.</remarks>
        /// <param name="dauTuan">Giá trị đầu tuần dùng khi thực hiện thao tác; kiểu DateTime.</param>
        /// <returns>Tập dữ liệu kiểu List&lt;ET_CA_THEO_NGAY&gt; theo điều kiện và phạm vi xử lý của hàm.</returns>
        public List<ET_CA_THEO_NGAY> layCaTheoTuan(DateTime dauTuan)
            => dal.layCaTheoTuan(dauTuan);

        /// <summary>
        /// Thêm cấu hình ca áp dụng cho ngày được chỉ định sau các kiểm tra dữ liệu. Chuyển thao tác dữ liệu đến DAL; giữ kết quả trả về cho nơi gọi.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>dal.themCaTheoNgay</c>.</remarks>
        /// <param name="ca">Giá trị ca dùng khi thực hiện thao tác; kiểu ET_CA_THEO_NGAY.</param>
        /// <returns>Kết quả đúng/sai của điều kiện hoặc thao tác được biểu diễn trong thân hàm; lỗi có thể được phát ra bằng ngoại lệ.</returns>
        public bool themCaTheoNgay(ET_CA_THEO_NGAY ca)
            => !string.IsNullOrWhiteSpace(ca.MaCa) && ca.NgayLam.Date >= DateTime.Today
                && dal.themCaTheoNgay(ca);

        /// <summary>
        /// Xóa cấu hình ca/ngày theo mã và điều kiện được nghiệp vụ cho phép. Chuyển thao tác dữ liệu đến DAL; giữ kết quả trả về cho nơi gọi.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>dal.xoaCaTheoNgay</c>.</remarks>
        /// <param name="ngay">Giá trị ngày dùng khi thực hiện thao tác; kiểu DateTime.</param>
        /// <param name="maCa">Mã ca xác định đối tượng cần xử lý.</param>
        /// <returns>Kết quả đúng/sai của điều kiện hoặc thao tác được biểu diễn trong thân hàm; lỗi có thể được phát ra bằng ngoại lệ.</returns>
        public bool xoaCaTheoNgay(DateTime ngay, string maCa)
            => dal.xoaCaTheoNgay(ngay, maCa);
    }
}
