using System;

namespace ET
{
    // Dòng hàng nhập mới. Mã lô do hệ thống cấp, người dùng chỉ nhập số lô của NCC.
    public class ET_DONGNHAPMOI
    {
        /// <summary>
        /// Mã hàng hóa.
        /// </summary>
        public string MaHH { get; set; } = "";
        /// <summary>
        /// Số lô do nghiệp vụ kho ghi nhận.
        /// </summary>
        public string SoLo { get; set; } = "";
        /// <summary>
        /// Hạn sử dụng của lô hàng. Có thể để null khi dữ liệu chưa được cung cấp.
        /// </summary>
        public DateTime? HanSuDung { get; set; }
        /// <summary>
        /// Số lượng của dòng dữ liệu.
        /// </summary>
        public decimal SoLuong { get; set; }
        /// <summary>
        /// Đơn giá áp dụng cho dòng dữ liệu.
        /// </summary>
        public decimal DonGia { get; set; }
        /// <summary>
        /// Nội dung ghi chú bổ sung của bản ghi. Có thể để null khi dữ liệu chưa được cung cấp.
        /// </summary>
        public string? GhiChu { get; set; }
    }
}
