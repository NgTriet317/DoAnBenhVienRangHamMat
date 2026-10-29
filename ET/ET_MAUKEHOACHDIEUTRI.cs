using Dapper.Contrib.Extensions;

namespace ET
{
    /// <summary>Hợp đồng dữ liệu bảng MAU_KE_HOACH_DIEU_TRI; không tự ghi SQL.</summary>
    [Table("MAU_KE_HOACH_DIEU_TRI")]
    public class ET_MAUKEHOACHDIEUTRI
    {
        private string maMauKH = string.Empty;
        private string maDMTT = string.Empty;
        private string? mucDo;
        private string tenMau = string.Empty;
        private int phienBan;
        private string trangThai = string.Empty;
        private string? maBSDuyet;
        private DateTime? ngayDuyet;
        public ET_MAUKEHOACHDIEUTRI() { }
        public ET_MAUKEHOACHDIEUTRI(string maMauKH, string maDMTT, string? mucDo, string tenMau, int phienBan, string trangThai, string? maBSDuyet, DateTime? ngayDuyet)
        {
            this.maMauKH = maMauKH;
            this.maDMTT = maDMTT;
            this.mucDo = mucDo;
            this.tenMau = tenMau;
            this.phienBan = phienBan;
            this.trangThai = trangThai;
            this.maBSDuyet = maBSDuyet;
            this.ngayDuyet = ngayDuyet;
        }

        [ExplicitKey]
        public string MaMauKH { get => maMauKH; set => maMauKH = value; }
        public string MaDMTT { get => maDMTT; set => maDMTT = value; }
        public string? MucDo { get => mucDo; set => mucDo = value; }
        public string TenMau { get => tenMau; set => tenMau = value; }
        public int PhienBan { get => phienBan; set => phienBan = value; }
        public string TrangThai { get => trangThai; set => trangThai = value; }
        public string? MaBSDuyet { get => maBSDuyet; set => maBSDuyet = value; }
        public DateTime? NgayDuyet { get => ngayDuyet; set => ngayDuyet = value; }
    }
}
