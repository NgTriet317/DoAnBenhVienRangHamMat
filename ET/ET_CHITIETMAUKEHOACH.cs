using Dapper.Contrib.Extensions;

namespace ET
{
    /// <summary>Hợp đồng dữ liệu bảng CHI_TIET_MAU_KE_HOACH; không tự ghi SQL.</summary>
    [Table("CHI_TIET_MAU_KE_HOACH")]
    public class ET_CHITIETMAUKEHOACH
    {
        private string maCTMau = string.Empty;
        private string maMauKH = string.Empty;
        private string maDV = string.Empty;
        private int thuTu;
        private int lanThu;
        private decimal soLuongDuKien;
        private string noiDung = string.Empty;
        public ET_CHITIETMAUKEHOACH() { }
        public ET_CHITIETMAUKEHOACH(string maCTMau, string maMauKH, string maDV, int thuTu, int lanThu, decimal soLuongDuKien, string noiDung)
        {
            this.maCTMau = maCTMau;
            this.maMauKH = maMauKH;
            this.maDV = maDV;
            this.thuTu = thuTu;
            this.lanThu = lanThu;
            this.soLuongDuKien = soLuongDuKien;
            this.noiDung = noiDung;
        }

        [ExplicitKey]
        public string MaCTMau { get => maCTMau; set => maCTMau = value; }
        public string MaMauKH { get => maMauKH; set => maMauKH = value; }
        public string MaDV { get => maDV; set => maDV = value; }
        public int ThuTu { get => thuTu; set => thuTu = value; }
        public int LanThu { get => lanThu; set => lanThu = value; }
        public decimal SoLuongDuKien { get => soLuongDuKien; set => soLuongDuKien = value; }
        public string NoiDung { get => noiDung; set => noiDung = value; }
    }
}
