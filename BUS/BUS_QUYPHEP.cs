using System;
using System.Data;
using DAL;

namespace BUS
{
    public class BUS_QUYPHEP
    {
        private readonly DAL_QUYPHEP dal = new DAL_QUYPHEP();

        private static void kiemTraNam(int nam)
        {
            if (nam < 2000 || nam > 2099)
                throw new ArgumentException("Năm quỹ phép phải từ 2000 đến 2099.");
        }

        public DataTable layDanhSachQuyPhep(int nam)
        {
            kiemTraNam(nam);
            return dal.layDanhSachQuyPhep(nam);
        }

        public DataTable layQuyPhep(string maNV, int nam)
        {
            kiemTraNam(nam);
            if (string.IsNullOrWhiteSpace(maNV))
                throw new ArgumentException("Nhân viên chưa được xác định.");
            return dal.layQuyPhep(maNV, nam);
        }

        public DataTable layNgayPhepNhanVien(string maNV, int nam)
        {
            kiemTraNam(nam);
            if (string.IsNullOrWhiteSpace(maNV))
                throw new ArgumentException("Nhân viên chưa được xác định.");
            return dal.layNgayPhepNhanVien(maNV, nam);
        }

        public DataTable layCauHinh(int nam)
        {
            kiemTraNam(nam);
            return dal.layCauHinh(nam);
        }

        public int demNgayDuKien(string maNV, DateTime tuNgay, DateTime denNgay)
        {
            if (string.IsNullOrWhiteSpace(maNV) || denNgay.Date < tuNgay.Date ||
                (denNgay.Date - tuNgay.Date).TotalDays > 366)
                throw new ArgumentException("Khoảng xin nghỉ không hợp lệ.");
            return dal.demNgayDuKien(maNV, tuNgay, denNgay);
        }
    }
}
