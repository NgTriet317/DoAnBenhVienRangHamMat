namespace ET
{
    // Dòng người dùng chọn trong biểu mẫu xuất vật tư nội bộ.
    public class ET_DONGXUATMOI
    {
        /// <summary>
        /// Mã hàng hóa.
        /// </summary>
        public string MaHH { get; set; } = "";
        /// <summary>
        /// Mã lô hàng dùng quản lý hạn dùng và tồn kho.
        /// </summary>
        public string MaLo { get; set; } = "";
        /// <summary>
        /// Số lượng của dòng dữ liệu.
        /// </summary>
        public decimal SoLuong { get; set; }
        /// <summary>
        /// Nội dung ghi chú bổ sung của bản ghi.
        /// </summary>
        public string GhiChu { get; set; } = "";
    }

    /// <summary>
    /// Dữ liệu tồn lô xuất trao đổi giữa các tầng và ánh xạ với kết quả truy vấn SQL.
    /// </summary>
    public class ET_TONLOXUAT
    {
        /// <summary>
        /// Mã lô hàng dùng quản lý hạn dùng và tồn kho.
        /// </summary>
        public string MaLo { get; set; } = "";
        /// <summary>
        /// Mã hàng hóa.
        /// </summary>
        public string MaHH { get; set; } = "";
        /// <summary>
        /// Số lô do nghiệp vụ kho ghi nhận.
        /// </summary>
        public string SoLo { get; set; } = "";
        /// <summary>
        /// Tên hàng hóa.
        /// </summary>
        public string TenHH { get; set; } = "";
        /// <summary>
        /// Đơn vị tính.
        /// </summary>
        public string DonViTinh { get; set; } = "";
        /// <summary>
        /// Số lượng tồn.
        /// </summary>
        public decimal SoLuongTon { get; set; }
        /// <summary>
        /// Hạn sử dụng của lô hàng. Có thể để null khi dữ liệu chưa được cung cấp.
        /// </summary>
        public DateTime? HanSuDung { get; set; }
    }
}
