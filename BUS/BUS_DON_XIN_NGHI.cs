using DAL;
using ET;
using System;
using System.Collections.Generic;

namespace BUS
{
    public class BUS_DON_XIN_NGHI
    {
        private DAL_DON_XIN_NGHI dal = new DAL_DON_XIN_NGHI();

        public string guiDon(ET_DON_XIN_NGHI don)
        {
            if (string.IsNullOrWhiteSpace(don.MaNV) || string.IsNullOrWhiteSpace(don.LyDo))
                throw new ArgumentException("Thiếu mã nhân viên hoặc lý do xin nghỉ.");
            if (don.TuNgay.Date < DateTime.Today.AddDays(7))
                throw new ArgumentException("Phải xin nghỉ trước ít nhất 7 ngày. Ngày bắt đầu nghỉ sớm nhất là " + DateTime.Today.AddDays(7).ToString("dd/MM/yyyy") + ".");
            if (don.DenNgay.Date < don.TuNgay.Date)
                throw new ArgumentException("Ngày kết thúc không được trước ngày bắt đầu.");
            don.NgayGui = DateTime.Now;
            don.TrangThaiDuyet = "Chờ duyệt";
            don.MaNVDuyet = null;
            don.NgayDuyet = null;
            return dal.guiDon(don);
        }

        public string taoDonKhanCap(ET_DON_XIN_NGHI don, string maQuanLy)
        {
            if (string.IsNullOrWhiteSpace(maQuanLy) || string.IsNullOrWhiteSpace(don.MaNV))
                throw new ArgumentException("Phải chọn nhân viên và người quản lý.");
            if (don.MaNV==maQuanLy) throw new ArgumentException("Quản lý không được tự lập đơn khẩn cấp đã duyệt cho chính mình.");
            if (string.IsNullOrWhiteSpace(don.LyDo)) throw new ArgumentException("Phải nêu lý do nghỉ khẩn cấp.");
            if (don.TuNgay.Date<DateTime.Today || don.DenNgay.Date<don.TuNgay.Date)
                throw new ArgumentException("Ngày nghỉ khẩn cấp phải từ hôm nay, ngày kết thúc không được trước ngày bắt đầu.");
            // Ngoại lệ 7 ngày CHỈ áp dụng cho đơn khẩn cấp do quản lý tạo.
            don.TuNgay=don.TuNgay.Date;
            don.DenNgay=don.DenNgay.Date;
            don.GhiChu="Loại đơn: Nghỉ phép\nĐơn khẩn cấp do quản lý tạo.\n"+don.GhiChu;
            return dal.taoDonKhanCap(don,maQuanLy);
        }

        public bool daDuocDuyetNghi(string maNV, DateTime ngay)
        {
            return dal.daDuocDuyetNghi(maNV, ngay);
        }

        public int demDonChoDuyet()
        {
            return dal.demDonChoDuyet();
        }

        public List<ET_DON_XIN_NGHI_HIENTHI> layDanhSachDon()
        {
            return dal.layDanhSachDon();
        }

        public List<ET_CA_CAN_THAY_THE> layCaCanThayThe(string maDon)
        {
            return dal.layCaCanThayThe(maDon);
        }

        public List<ET_NGUOI_THAY_THE> layNhanVienThayThe(string maDon)
        {
            return dal.layNhanVienThayThe(maDon);
        }

        public bool phanCongThayThe(string maDon, string maLLV, string maNVThay, string maPhong)
        {
            if (string.IsNullOrWhiteSpace(maDon) || string.IsNullOrWhiteSpace(maLLV) ||
                string.IsNullOrWhiteSpace(maNVThay) || string.IsNullOrWhiteSpace(maPhong))
                throw new ArgumentException("Vui lòng chọn ca, nhân viên thay thế và phòng.");
            return dal.phanCongThayThe(maDon, maLLV, maNVThay, maPhong);
        }

        public bool huyPhanCongThayThe(string maDon, string maLLVGoc)
        {
            if (string.IsNullOrWhiteSpace(maDon) || string.IsNullOrWhiteSpace(maLLVGoc))
                throw new ArgumentException("Vui lòng chọn đơn và ca cần hủy phương án thay.");
            return dal.huyPhanCongThayThe(maDon, maLLVGoc);
        }

        public List<ET_LICH_KHAM_CAN_XAC_NHAN> layLichKhamCanXacNhan(string maDon)
        {
            if (string.IsNullOrWhiteSpace(maDon))
                throw new ArgumentException("Thiếu mã đơn xin nghỉ.");
            return dal.layLichKhamCanXacNhan(maDon);
        }

        public bool xacNhanTungLichKham(string maDon, string maLich, string maNguoiXacNhan)
        {
            if (string.IsNullOrWhiteSpace(maDon) || string.IsNullOrWhiteSpace(maLich) ||
                string.IsNullOrWhiteSpace(maNguoiXacNhan))
                throw new ArgumentException("Thiếu thông tin lịch khám hoặc người xác nhận.");
            return dal.xacNhanTungLichKham(maDon, maLich, maNguoiXacNhan);
        }

        public bool xuLyDon(string maDon, string maNguoiDuyet, bool duyet, string lyDoKhongDuyet)
        {
            if (string.IsNullOrWhiteSpace(maDon) || string.IsNullOrWhiteSpace(maNguoiDuyet))
                throw new ArgumentException("Thiếu mã đơn hoặc người duyệt.");
            if (!duyet && string.IsNullOrWhiteSpace(lyDoKhongDuyet))
                throw new ArgumentException("Phải nhập lý do không duyệt đơn.");
            if (lyDoKhongDuyet.Length > 1000)
                throw new ArgumentException("Lý do không duyệt tối đa 1000 ký tự.");
            return dal.xuLyDon(maDon, maNguoiDuyet, duyet, lyDoKhongDuyet.Trim());
        }
    }
}
