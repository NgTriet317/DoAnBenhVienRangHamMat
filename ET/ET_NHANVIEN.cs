namespace ET
{
    public class ET_NHANVIEN
    {
        private string maBN;
        private string hoTenBN;
        private string gioiTinh;
        private DateTime ngaySinh;
        private int sdt;
        private string diaChi;
        private string nhomMau;
        private string tienSuBenh;
        private string diUng;
        private string trangThai;

        public ET_NHANVIEN()
        {

        }

        public ET_NHANVIEN(string maBN, string hoTenBN, string gioiTinh, DateTime ngaySinh, int sdt, string diaChi, string nhomMau, string tienSuBenh, string diUng, string trangThai)
        {
            this.maBN = maBN;
            this.hoTenBN = hoTenBN;
            this.gioiTinh = gioiTinh;
            this.ngaySinh = ngaySinh;
            this.sdt = sdt;
            this.diaChi = diaChi;
            this.nhomMau = nhomMau;
            this.tienSuBenh = tienSuBenh;
            this.diUng = diUng;
            this.trangThai = trangThai;
        }

        public string MaBN { get => maBN; set => maBN = value; }
        public string HoTenBN { get => hoTenBN; set => hoTenBN = value; }
        public string GioiTinh { get => gioiTinh; set => gioiTinh = value; }
        public DateTime NgaySinh { get => ngaySinh; set => ngaySinh = value; }
        public int Sdt { get => sdt; set => sdt = value; }
        public string DiaChi { get => diaChi; set => diaChi = value; }
        public string NhomMau { get => nhomMau; set => nhomMau = value; }
        public string TienSuBenh { get => tienSuBenh; set => tienSuBenh = value; }
        public string DiUng { get => diUng; set => diUng = value; }
        public string TrangThai { get => trangThai; set => trangThai = value; }
    }
}
