using Dapper.Contrib.Extensions;

namespace ET
{
    /// <summary>Hợp đồng dữ liệu bảng CAU_HINH_THUOC_KE_DON; không tự ghi SQL.</summary>
    [Table("CAU_HINH_THUOC_KE_DON")]
    public class ET_CAUHINHTHUOCKEDON
    {
        private string maThuoc = string.Empty;
        private string? maNhomThuoc;
        private string? doiTuongApDung;
        private string? duongDung;
        private string? lieuDungGoiY;
        private string? huongDanGoiY;
        private int phienBan;
        private string trangThai = string.Empty;
        private string? maNguoiDuyet;
        private DateTime? ngayDuyet;
        public ET_CAUHINHTHUOCKEDON() { }
        public ET_CAUHINHTHUOCKEDON(string maThuoc, string? maNhomThuoc, string? doiTuongApDung, string? duongDung, string? lieuDungGoiY, string? huongDanGoiY, int phienBan, string trangThai, string? maNguoiDuyet, DateTime? ngayDuyet)
        {
            this.maThuoc = maThuoc;
            this.maNhomThuoc = maNhomThuoc;
            this.doiTuongApDung = doiTuongApDung;
            this.duongDung = duongDung;
            this.lieuDungGoiY = lieuDungGoiY;
            this.huongDanGoiY = huongDanGoiY;
            this.phienBan = phienBan;
            this.trangThai = trangThai;
            this.maNguoiDuyet = maNguoiDuyet;
            this.ngayDuyet = ngayDuyet;
        }

        [ExplicitKey]
        public string MaThuoc { get => maThuoc; set => maThuoc = value; }
        public string? MaNhomThuoc { get => maNhomThuoc; set => maNhomThuoc = value; }
        public string? DoiTuongApDung { get => doiTuongApDung; set => doiTuongApDung = value; }
        public string? DuongDung { get => duongDung; set => duongDung = value; }
        public string? LieuDungGoiY { get => lieuDungGoiY; set => lieuDungGoiY = value; }
        public string? HuongDanGoiY { get => huongDanGoiY; set => huongDanGoiY = value; }
        public int PhienBan { get => phienBan; set => phienBan = value; }
        public string TrangThai { get => trangThai; set => trangThai = value; }
        public string? MaNguoiDuyet { get => maNguoiDuyet; set => maNguoiDuyet = value; }
        public DateTime? NgayDuyet { get => ngayDuyet; set => ngayDuyet = value; }
    }
}
