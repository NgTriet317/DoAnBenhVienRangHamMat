using Dapper.Contrib.Extensions;

namespace ET
{
    /// <summary>Hợp đồng dữ liệu bảng BAN_GIAO_THUOC; không tự ghi SQL.</summary>
    [Table("BAN_GIAO_THUOC")]
    public class ET_BANGIAOTHUOC
    {
        private string maDT = string.Empty;
        private string maHD = string.Empty;
        private string maPX = string.Empty;
        private string trangThai = string.Empty;
        private string? maNVGiao;
        private DateTime? thoiGianGiao;
        private string? ghiChu;
        public ET_BANGIAOTHUOC() { }
        public ET_BANGIAOTHUOC(string maDT, string maHD, string maPX, string trangThai, string? maNVGiao, DateTime? thoiGianGiao, string? ghiChu)
        {
            this.maDT = maDT;
            this.maHD = maHD;
            this.maPX = maPX;
            this.trangThai = trangThai;
            this.maNVGiao = maNVGiao;
            this.thoiGianGiao = thoiGianGiao;
            this.ghiChu = ghiChu;
        }

        [ExplicitKey]
        public string MaDT { get => maDT; set => maDT = value; }
        public string MaHD { get => maHD; set => maHD = value; }
        public string MaPX { get => maPX; set => maPX = value; }
        public string TrangThai { get => trangThai; set => trangThai = value; }
        public string? MaNVGiao { get => maNVGiao; set => maNVGiao = value; }
        public DateTime? ThoiGianGiao { get => thoiGianGiao; set => thoiGianGiao = value; }
        public string? GhiChu { get => ghiChu; set => ghiChu = value; }
    }
}
