using System;

namespace ET
{
    /// <summary>
    /// Dữ liệu đơn xin nghỉ trao đổi giữa các tầng và ánh xạ với kết quả truy vấn SQL.
    /// </summary>
    public class ET_DON_XIN_NGHI
    {
        /// <summary>
        /// Mã đơn xin nghỉ.
        /// </summary>
        public string MaDonNghi { get; set; } = "";
        /// <summary>
        /// Mã nhân viên thực hiện hoặc được gán cho bản ghi.
        /// </summary>
        public string MaNV { get; set; } = "";
        /// <summary>
        /// Ngày bắt đầu khoảng áp dụng/lọc.
        /// </summary>
        public DateTime TuNgay { get; set; }
        /// <summary>
        /// Ngày kết thúc khoảng áp dụng/lọc.
        /// </summary>
        public DateTime DenNgay { get; set; }
        /// <summary>
        /// Lý do.
        /// </summary>
        public string LyDo { get; set; } = "";
        /// <summary>
        /// Ngày gửi.
        /// </summary>
        public DateTime NgayGui { get; set; }
        /// <summary>
        /// Trạng thái duyệt của bản ghi nghiệp vụ.
        /// </summary>
        public string TrangThaiDuyet { get; set; } = "Chờ duyệt";
        /// <summary>
        /// Mã nhân viên duyệt. Có thể để null khi dữ liệu chưa được cung cấp.
        /// </summary>
        public string? MaNVDuyet { get; set; }
        /// <summary>
        /// Ngày duyệt. Có thể để null khi dữ liệu chưa được cung cấp.
        /// </summary>
        public DateTime? NgayDuyet { get; set; }
        /// <summary>
        /// Nội dung ghi chú bổ sung của bản ghi. Có thể để null khi dữ liệu chưa được cung cấp.
        /// </summary>
        public string? GhiChu { get; set; }
    }
}
