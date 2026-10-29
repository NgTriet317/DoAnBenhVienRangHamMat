using System;
using System.Data;
using DAL;

namespace BUS
{
    public class BUS_BANGLUONG
    {
        private readonly DAL_BANGLUONG dalBangLuong = new DAL_BANGLUONG();

        public bool suDungSchemaV9() => dalBangLuong.suDungSchemaV9();
        public bool daCaiTinhLuong() => dalBangLuong.daCaiTinhLuong();
        public DateTime layNgayMayChu() => dalBangLuong.layNgayMayChu();

        public DataTable layBangLuongTheoKy(DateTime tuNgay, DateTime denNgay)
        {
            kiemTraKy(tuNgay, denNgay);
            return dalBangLuong.layBangLuongTheoKy(tuNgay, denNgay);
        }

        public DataTable layDoiSoatTheoKy(DateTime tuNgay, DateTime denNgay)
        {
            kiemTraKy(tuNgay, denNgay);
            return dalBangLuong.layDoiSoatTheoKy(tuNgay, denNgay);
        }

        public int demCongChoDoiSoat(DateTime tuNgay, DateTime denNgay)
        {
            kiemTraKy(tuNgay, denNgay);
            return dalBangLuong.demCongChoDoiSoat(tuNgay, denNgay);
        }

        // Tính lại toàn bộ các phiếu NHÁP từ chứng từ, không động đến kỳ đã chốt.
        public void tinhLuongTheoKy(DateTime tuNgay, DateTime denNgay, string maNVQuanLy)
        {
            kiemTraKy(tuNgay, denNgay);
            if (string.IsNullOrWhiteSpace(maNVQuanLy))
                throw new ArgumentException("Chưa xác định người tính lương.");
            dalBangLuong.tinhLuongTheoKy(tuNgay, denNgay, maNVQuanLy);
        }

        // Chốt từng phiếu, đối chiếu lại số công/tiền đã xem để tránh chốt số liệu cũ.
        public void chotLuongNhanVien(string maBL, string maNVQuanLy, decimal congDaXem,
            decimal tongDaXem, decimal hoaHongDaXem)
        {
            if (string.IsNullOrWhiteSpace(maBL) || string.IsNullOrWhiteSpace(maNVQuanLy))
                throw new ArgumentException("Chưa chọn phiếu lương hoặc chưa xác định người chốt.");
            if (congDaXem < 0 || hoaHongDaXem < 0)
                throw new ArgumentException("Số công và hoa hồng phải hợp lệ.");
            dalBangLuong.chotLuongNhanVien(maBL, maNVQuanLy, congDaXem, tongDaXem, hoaHongDaXem);
        }

        private static void kiemTraKy(DateTime tuNgay, DateTime denNgay)
        {
            if (tuNgay.Year != denNgay.Year || tuNgay.Month != denNgay.Month ||
                tuNgay.Day != 1 || denNgay.Date != tuNgay.AddMonths(1).AddDays(-1).Date)
                throw new ArgumentException("Kỳ lương phải là một tháng đầy đủ.");
        }
    }
}
