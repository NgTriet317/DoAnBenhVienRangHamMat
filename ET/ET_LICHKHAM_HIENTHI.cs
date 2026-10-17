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
    }
}
