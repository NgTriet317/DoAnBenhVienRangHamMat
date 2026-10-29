using System;

namespace ET
{
    // Thông tin BQL gửi khi đối soát một ca làm việc (không dùng làm dòng hiển thị DataGrid).
    public class ET_CHAMCONG
    {
        public string MaLLV { get; set; } = "";
        public string TrangThaiCu { get; set; } = "";
        public DateTime? GioVaoCu { get; set; }
        public DateTime? GioRaCu { get; set; }
        public DateTime? GioVao { get; set; }
        public DateTime? GioRa { get; set; }
        // Hoàn tất: có đủ giờ; Vắng: không có giờ; Không hợp lệ: yêu cầu xử lý riêng.
        public string KetQua { get; set; } = "";
        public string LyDo { get; set; } = "";
    }
}
