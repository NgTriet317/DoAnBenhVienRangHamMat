using Dapper.Contrib.Extensions;

namespace ET
{
    /// <summary>Hợp đồng dữ liệu bảng NHOM_THUOC_KE_DON; không tự ghi SQL.</summary>
    [Table("NHOM_THUOC_KE_DON")]
    public class ET_NHOMTHUOCKEDON
    {
        private string maNhomThuoc = string.Empty;
        private string tenNhom = string.Empty;
        private string? moTa;
        private string trangThai = string.Empty;
        public ET_NHOMTHUOCKEDON() { }
        public ET_NHOMTHUOCKEDON(string maNhomThuoc, string tenNhom, string? moTa, string trangThai)
        {
            this.maNhomThuoc = maNhomThuoc;
            this.tenNhom = tenNhom;
            this.moTa = moTa;
            this.trangThai = trangThai;
        }

        [ExplicitKey]
        public string MaNhomThuoc { get => maNhomThuoc; set => maNhomThuoc = value; }
        public string TenNhom { get => tenNhom; set => tenNhom = value; }
        public string? MoTa { get => moTa; set => moTa = value; }
        public string TrangThai { get => trangThai; set => trangThai = value; }
    }
}
