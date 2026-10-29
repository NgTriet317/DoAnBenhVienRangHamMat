using Dapper.Contrib.Extensions;

namespace ET
{
    /// <summary>Hợp đồng dữ liệu bảng DM_TINH_TRANG_BENH; không tự ghi SQL.</summary>
    [Table("DM_TINH_TRANG_BENH")]
    public class ET_DMTINHTRANGBENH
    {
        private string maDMTT = string.Empty;
        private string tenTinhTrang = string.Empty;
        private string? moTa;
        private string trangThai = string.Empty;
        public ET_DMTINHTRANGBENH() { }
        public ET_DMTINHTRANGBENH(string maDMTT, string tenTinhTrang, string? moTa, string trangThai)
        {
            this.maDMTT = maDMTT;
            this.tenTinhTrang = tenTinhTrang;
            this.moTa = moTa;
            this.trangThai = trangThai;
        }

        [ExplicitKey]
        public string MaDMTT { get => maDMTT; set => maDMTT = value; }
        public string TenTinhTrang { get => tenTinhTrang; set => tenTinhTrang = value; }
        public string? MoTa { get => moTa; set => moTa = value; }
        public string TrangThai { get => trangThai; set => trangThai = value; }
        [Write(false)]
        public string Nhom
        {
            get
            {
                if(MoTa?.StartsWith("Nhóm: ",StringComparison.Ordinal)==true)
                {var end=MoTa.IndexOf('.',6);if(end>6)return MoTa[6..end].Trim();}
                return MaDMTT.StartsWith("REF28_",StringComparison.Ordinal)?"Răng và mô quanh răng":"Khác";
            }
        }
    }
}
