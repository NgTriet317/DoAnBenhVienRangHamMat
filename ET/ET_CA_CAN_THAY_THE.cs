using System;

namespace ET
{
    public class ET_CA_CAN_THAY_THE
    {
        public string MaLLV { get; set; } = "";
        public string MaCa { get; set; } = "";
        public string MaPhong { get; set; } = "";
        public string TenPhong { get; set; } = "";
        public string TenCa { get; set; } = "";
        public DateTime NgayLam { get; set; }
        public string TrangThai { get; set; } = "";
        public string MaNVThayThe { get; set; } = "";
        public string HoTenThayThe { get; set; } = "";
        public string MaLLVThayThe { get; set; } = "";
        public string TinhTrang
        {
            get
            {
                if (string.IsNullOrWhiteSpace(MaLLVThayThe)) return "Chưa phân người thay";
                return "Đã chọn: " + HoTenThayThe;
            }
        }
    }

    public class ET_NGUOI_THAY_THE
    {
        public string MaNV { get; set; } = "";
        public string HoTen { get; set; } = "";
        public string ThongTin
        {
            get { return HoTen; }
        }
    }
}
