using System;

namespace ET
{
    // Thông tin BQL gửi khi đối soát một ca làm việc (không dùng làm dòng hiển thị DataGrid).
    public class ET_CHAMCONG
    {
        /// <summary>
        /// Mã lịch làm việc.
        /// </summary>
        public string MaLLV { get; set; } = "";
        /// <summary>
        /// Trạng thái cũ.
        /// </summary>
        public string TrangThaiCu { get; set; } = "";
        /// <summary>
        /// Giờ vào cũ. Có thể để null khi dữ liệu chưa được cung cấp.
        /// </summary>
        public DateTime? GioVaoCu { get; set; }
        /// <summary>
        /// Giờ ra cũ. Có thể để null khi dữ liệu chưa được cung cấp.
        /// </summary>
        public DateTime? GioRaCu { get; set; }
        /// <summary>
        /// Giờ vào. Có thể để null khi dữ liệu chưa được cung cấp.
        /// </summary>
        public DateTime? GioVao { get; set; }
        /// <summary>
        /// Giờ ra. Có thể để null khi dữ liệu chưa được cung cấp.
        /// </summary>
        public DateTime? GioRa { get; set; }
        // Hoàn tất: có đủ giờ; Vắng: không có giờ; Không hợp lệ: yêu cầu xử lý riêng.
        public string KetQua { get; set; } = "";
        /// <summary>
        /// Lý do.
        /// </summary>
        public string LyDo { get; set; } = "";
    }
}
