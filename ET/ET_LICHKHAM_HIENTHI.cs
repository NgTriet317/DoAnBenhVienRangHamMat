namespace ET
{
    // Chỉ phục vụ xem lịch khám, không dùng đối tượng này để INSERT/UPDATE.
    public class ET_LICHKHAM_HIENTHI : ET_LICHKHAM
    {
        public string TenBenhNhan { get; set; } = "";
        public string TenBacSi { get; set; } = "";
        public string TenPhong { get; set; } = "";
        public string TenDichVu { get; set; } = "";
        public string TenCa { get; set; } = "";
        public string TenBacSiDuBi { get; set; } = "";
        public bool CanPhanCongLai { get; set; }
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
