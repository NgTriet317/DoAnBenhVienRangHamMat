using System;
using System.Collections.Generic;
using System.Data;
using DAL;
using ET;

namespace BUS
{
    /// <summary>
    /// Tầng nghiệp vụ xuất kho, nhận yêu cầu từ giao diện và điều phối lớp DAL tương ứng.
    /// </summary>
    public class BUS_XUATKHO
    {
        /// <summary>
        /// Đối tượng truy cập dữ liệu xuất kho phục vụ các thao tác của lớp.
        /// </summary>
        private readonly DAL_XUATKHO data = new DAL_XUATKHO();

        /// <summary>
        /// Đọc tồn từng lô trong kho để người dùng chọn nguồn xuất phù hợp. Chuyển thao tác dữ liệu đến DAL; giữ kết quả trả về cho nơi gọi.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>XuLyLoi.ThucHien</c>, <c>data.LayTonTheoLo</c>.</remarks>
        /// <param name="maKho">Mã kho xác định đối tượng cần xử lý.</param>
        /// <returns>Tập dữ liệu kiểu List&lt;ET_TONLOXUAT&gt; theo điều kiện và phạm vi xử lý của hàm.</returns>
        public List<ET_TONLOXUAT> LayTonTheoLo(string maKho)
        {
            if (string.IsNullOrWhiteSpace(maKho)) return new List<ET_TONLOXUAT>();
            return XuLyLoi.ThucHien(() => data.LayTonTheoLo(maKho));
        }

        /// <summary>
        /// Kiểm tra và lưu đầu phiếu cùng các dòng hàng; nội dung được ghi theo trạng thái và quyền hiện tại. Chuyển thao tác dữ liệu đến DAL; giữ kết quả trả về cho nơi gọi.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>DateTime.Now.AddMinutes</c>, <c>decimal.Round</c>, <c>XuLyLoi.ThucHien</c>, <c>data.LuuPhieu</c>.</remarks>
        /// <param name="maPX">Mã phiếu xuất xác định đối tượng cần xử lý.</param>
        /// <param name="maKho">Mã kho xác định đối tượng cần xử lý.</param>
        /// <param name="maNV">Mã nhân viên xác định đối tượng cần xử lý.</param>
        /// <param name="maKhoa">Mã khóa xác định đối tượng cần xử lý.</param>
        /// <param name="maPhong">Mã phòng xác định đối tượng cần xử lý.</param>
        /// <param name="ngayXuat">Giá trị ngày xuất dùng khi thực hiện thao tác; kiểu DateTime.</param>
        /// <param name="lyDo">Giá trị lý do dùng khi thực hiện thao tác; kiểu string.</param>
        /// <param name="ghiChu">Giá trị ghi chú dùng khi thực hiện thao tác; kiểu string?.</param>
        /// <param name="dong">Giá trị dòng dùng khi thực hiện thao tác; kiểu List&lt;ET_DONGXUATMOI&gt;.</param>
        /// <param name="ghiSo">Giá trị ghi sổ dùng khi thực hiện thao tác; kiểu bool.</param>
        /// <returns>Chuỗi kết quả của thao tác lưu phiếu.</returns>
        public string LuuPhieu(string? maPX, string maKho, string maNV, string? maKhoa,
            string? maPhong, DateTime ngayXuat, string lyDo, string? ghiChu,
            List<ET_DONGXUATMOI> dong, bool ghiSo)
        {
            if (string.IsNullOrWhiteSpace(maKho) || string.IsNullOrWhiteSpace(maNV))
                throw new InvalidOperationException("Phải chọn kho và nhân viên lập phiếu.");
            if (maKhoa != null && maPhong != null)
                throw new InvalidOperationException("Chỉ chọn khoa hoặc phòng nhận, không được chọn cả hai.");
            if (string.IsNullOrWhiteSpace(lyDo))
                throw new InvalidOperationException("Vui lòng nhập lý do xuất kho.");
            if (ngayXuat == DateTime.MinValue || ngayXuat > DateTime.Now.AddMinutes(1))
                throw new InvalidOperationException("Ngày xuất không được vượt thời gian hiện tại.");
            if (dong == null || dong.Count == 0)
                throw new InvalidOperationException("Phải thêm ít nhất một dòng vật tư.");
            foreach (ET_DONGXUATMOI d in dong)
            {
                if (string.IsNullOrWhiteSpace(d.MaHH) || string.IsNullOrWhiteSpace(d.MaLo))
                    throw new InvalidOperationException("Mỗi dòng phải chọn mặt hàng và lô còn tồn.");
                if (d.SoLuong <= 0 || decimal.Round(d.SoLuong, 3) != d.SoLuong)
                    throw new InvalidOperationException("Số lượng xuất phải lớn hơn 0 và tối đa 3 chữ số thập phân.");
            }
            return XuLyLoi.ThucHien(() => data.LuuPhieu(maPX, maKho, maNV, maKhoa,
                maPhong, ngayXuat, lyDo.Trim(), ghiChu, dong, ghiSo));
        }

        /// <summary>
        /// Kiểm tra phiếu và ghi sổ nhập/xuất để các thay đổi tồn kho có hiệu lực theo nghiệp vụ. Chuyển thao tác dữ liệu đến DAL; giữ kết quả trả về cho nơi gọi.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>XuLyLoi.ThucHien</c>, <c>data.GhiSo</c>.</remarks>
        /// <param name="maPX">Mã phiếu xuất xác định đối tượng cần xử lý.</param>
        public void GhiSo(string maPX)
        {
            if (string.IsNullOrWhiteSpace(maPX))
                throw new InvalidOperationException("Chưa chọn phiếu xuất cần ghi sổ.");
            XuLyLoi.ThucHien(() => { data.GhiSo(maPX); return true; });
        }
    }
}
