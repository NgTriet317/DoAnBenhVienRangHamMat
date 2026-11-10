using System;
using System.Data;
using DAL;
using ET;

namespace BUS
{
    public class BUS_CHINHSACHLUONG
    {
        private readonly DAL_CHINHSACHLUONG dal = new();
        public DataTable layDanhSachChinhSach() => dal.layDanhSachChinhSach();
        public DataTable layDanhSachChucVu() => dal.layDanhSachChucVu();

        public string luuChinhSach(string maNguoiSua,ET_CHINHSACHLUONG cs)
        {
            if(string.IsNullOrWhiteSpace(maNguoiSua)||string.IsNullOrWhiteSpace(cs.MaChucVu)
                ||string.IsNullOrWhiteSpace(cs.TenChinhSach))
                throw new ArgumentException("Chọn chức vụ và nhập tên chính sách lương.");
            if(cs.NgayHieuLuc.Day!=1)
                throw new ArgumentException("Ngày hiệu lực phải là ngày đầu tháng.");
            if(cs.LuongCoBan<0 || cs.DonGiaCa<0 || cs.PhuCapCoDinh<0 || cs.SoCaDinhMuc<=0)
                throw new ArgumentException("Đơn giá ca, lương cơ bản và định mức phải hợp lệ.");
            if(cs.TyLeHoaHong<0 || cs.TyLeHoaHong>1)
                throw new ArgumentException("Tỷ lệ hoa hồng phải từ 0% đến 100%.");
            return dal.luuChinhSach(maNguoiSua,cs);
        }
        public void duyetChinhSach(string maCSL,string maNguoiDuyet)
        {
            if(string.IsNullOrWhiteSpace(maCSL)||string.IsNullOrWhiteSpace(maNguoiDuyet))
                throw new ArgumentException("Chưa xác định chính sách hoặc người duyệt.");
            dal.duyetChinhSach(maCSL,maNguoiDuyet);
        }
    }
}
