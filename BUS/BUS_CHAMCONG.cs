using System;
using System.Collections.Generic;
using System.Data;
using DAL;
using ET;

namespace BUS
{
    /// <summary>
    /// Tầng nghiệp vụ chấm công, nhận yêu cầu từ giao diện và điều phối lớp DAL tương ứng.
    /// </summary>
    public class BUS_CHAMCONG
    {
        /// <summary>
        /// Đối tượng truy cập dữ liệu chấm công phục vụ các thao tác của lớp.
        /// </summary>
        private readonly DAL_CHAMCONG dalChamCong = new DAL_CHAMCONG();

        /// <summary>
        /// Đọc dữ liệu chấm công theo kỳ được chọn. Chuyển thao tác dữ liệu đến DAL; giữ kết quả trả về cho nơi gọi.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>dalChamCong.layChamCongTheoThang</c>.</remarks>
        /// <param name="thang">Giá trị tháng dùng khi thực hiện thao tác; kiểu int.</param>
        /// <param name="nam">Giá trị năm dùng khi thực hiện thao tác; kiểu int.</param>
        /// <returns>Kết quả kiểu DataTable dùng cho bước xử lý tiếp theo.</returns>
        public DataTable layChamCongTheoThang(int thang, int nam)
        {
            if (thang < 1 || thang > 12 || nam < 2000 || nam > 2100)
                throw new ArgumentException("Kỳ chấm công không hợp lệ.");
            return dalChamCong.layChamCongTheoThang(new DateTime(nam, thang, 1));
        }

        /// <summary>
        /// Kiểm tra thông tin và điều phối duyệt kết quả chấm công. Chuyển thao tác dữ liệu đến DAL; giữ kết quả trả về cho nơi gọi.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>dalChamCong.duyetChamCong</c>.</remarks>
        /// <param name="danhSach">Giá trị danh sách dùng khi thực hiện thao tác; kiểu IReadOnlyList&lt;ET_CHAMCONG&gt;.</param>
        /// <param name="maNVQuanLy">Mã nhân viên quản lý xác định đối tượng cần xử lý.</param>
        /// <returns>Giá trị số được đọc, đếm hoặc tính toán trong hàm.</returns>
        public int duyetChamCong(IReadOnlyList<ET_CHAMCONG> danhSach, string maNVQuanLy)
        {
            if (string.IsNullOrWhiteSpace(maNVQuanLy))
                throw new InvalidOperationException("Chưa xác định tài khoản BQL đang đăng nhập.");
            if (danhSach == null || danhSach.Count == 0)
                throw new ArgumentException("Vui lòng chọn ca cần duyệt.");
            foreach (ET_CHAMCONG ca in danhSach)
            {
                if (string.IsNullOrWhiteSpace(ca.MaLLV) ||
                    (ca.KetQua != "Hoàn tất" && ca.KetQua != "Vắng" && ca.KetQua != "Không hợp lệ"))
                    throw new ArgumentException("Thông tin ca hoặc kết quả đối soát không hợp lệ.");
                if (ca.KetQua == "Hoàn tất" && (!ca.GioVao.HasValue || !ca.GioRa.HasValue || ca.GioRa <= ca.GioVao))
                    throw new ArgumentException("Ca hoàn tất phải nhập giờ vào và giờ ra hợp lệ.");
                if (ca.KetQua == "Không hợp lệ" && string.IsNullOrWhiteSpace(ca.LyDo))
                    throw new ArgumentException("Ca không hợp lệ phải ghi lý do.");
                if (ca.KetQua == "Vắng" && (ca.GioVao.HasValue || ca.GioRa.HasValue))
                    throw new ArgumentException("Ca vắng không được kèm giờ vào/ra.");
            }
            return dalChamCong.duyetChamCong(danhSach, maNVQuanLy);
        }
    }
}
