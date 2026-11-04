namespace ET
{
    // Chỉ phục vụ xem lịch khám, không dùng đối tượng này để INSERT/UPDATE.
    public class ET_LICHKHAM_HIENTHI : ET_LICHKHAM
    {
        /// <summary>
        /// Tên bệnh nhân.
        /// </summary>
        public string TenBenhNhan { get; set; } = "";
        /// <summary>
        /// Tên bác sĩ.
        /// </summary>
        public string TenBacSi { get; set; } = "";
        /// <summary>
        /// Tên phòng.
        /// </summary>
        public string TenPhong { get; set; } = "";
        /// <summary>
        /// Tên dịch vụ.
        /// </summary>
        public string TenDichVu { get; set; } = "";
        /// <summary>
        /// Tên ca.
        /// </summary>
        public string TenCa { get; set; } = "";
        /// <summary>
        /// Tên bác sĩ dự bị.
        /// </summary>
        public string TenBacSiDuBi { get; set; } = "";
        /// <summary>
        /// Có thể phân công lại.
        /// </summary>
        public bool CanPhanCongLai { get; set; }
        /// <summary>
        /// Giá trị Tình trạng điều phối; được xác định bởi phần getter/setter của thuộc tính.
        /// </summary>
        public string TinhTrangDieuPhoi
        {
            get
            {
                if (CanPhanCongLai) return "Cần phân công lại";
                if (MucDoNghiemTrong=="Cao" && string.IsNullOrWhiteSpace(MaBSDuBi) &&
                    TrangThai!="Đã khám" && TrangThai!="Hoàn thành" && TrangThai!="Đã hủy" && TrangThai!="Hủy")
                    return "Cần bổ sung dự bị";
                return TrangThai;
            }
        }

    }
}
