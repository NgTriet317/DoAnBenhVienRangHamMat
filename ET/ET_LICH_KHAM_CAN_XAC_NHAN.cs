using System;

namespace ET
{
    public class ET_LICH_KHAM_CAN_XAC_NHAN
    {
        public string MaLich { get; set; } = "";
        public string TenBenhNhan { get; set; } = "";
        public DateTime NgayGioKham { get; set; }
        public int ThoiLuongDuKien { get; set; }
        public string MaCa { get; set; } = "";
        public string MaBSCu { get; set; } = "";
        public string TenBSCu { get; set; } = "";
        public string TenPhongCu { get; set; } = "";
        public string MaPhongCu { get; set; } = "";
        public string MaBSMoi { get; set; } = "";
        public string TenBSMoi { get; set; } = "";
        public string MaPhongMoi { get; set; } = "";
        public string TenPhongMoi { get; set; } = "";
        public string MaLLVThayThe { get; set; } = "";
        public bool DaXacNhan { get; set; }
        public string MaNVXacNhan { get; set; } = "";
        public string TenNguoiXacNhan { get; set; } = "";
        public DateTime? NgayXacNhan { get; set; }
        public string TrangThaiLuu { get; set; } = "";

        public string TrangThaiXacNhan
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(TrangThaiLuu)) return TrangThaiLuu;
                if (DaXacNhan) return "Đã xác nhận";
                if (string.IsNullOrWhiteSpace(MaLLVThayThe)) return "Chưa có ca thay";
                return "Chưa xác nhận";
            }
        }
    }
}
