using System;
using System.Collections.Generic;
using DAL;
using ET;

namespace BUS;

// Lớp mở rộng riêng để có thể giữ nguyên BUS_AITIEPNHAN của bản MVP.
public class BUS_AI_TAOPHIEU
{
    private readonly DAL_AI_TAOPHIEU data = new();

    public List<ET_AI_DANHMUC> layPhong() => data.layPhong();
    public List<ET_AI_DANHMUC> layBacSi(string maPhong) => data.layBacSi(maPhong);
    public List<ET_AI_DANHMUC> layDichVu(string maPhong, string? maBS) => data.layDichVu(maPhong, maBS);
    public List<ET_AI_BENHNHANTIM> timBenhNhan(string tim)
    {
        if (string.IsNullOrWhiteSpace(tim) || tim.Trim().Length < 3)
            return new List<ET_AI_BENHNHANTIM>();
        return data.timBenhNhan(tim.Trim());
    }

    public List<ET_AI_BENHNHANTIM> doiChieuBenhNhan(string hoTen, DateTime ngaySinh, string sdt)
    {
        if (string.IsNullOrWhiteSpace(hoTen) || ngaySinh.Date > DateTime.Today ||
            ngaySinh == DateTime.MinValue || string.IsNullOrWhiteSpace(sdt))
            return new List<ET_AI_BENHNHANTIM>();
        return data.doiChieuBenhNhan(hoTen, ngaySinh, sdt);
    }

    public ET_AI_KETQUATAO taoPhieu(ET_AI_TAOPHIEU model)
    {
        if (model.MaPhien == Guid.Empty || string.IsNullOrWhiteSpace(model.MaNV))
            throw new ArgumentException("Thiếu thông tin phiên hoặc nhân viên.");
        if (string.IsNullOrWhiteSpace(model.HoTen) || model.NgaySinh.Date > DateTime.Today ||
            string.IsNullOrWhiteSpace(model.SDT) || string.IsNullOrWhiteSpace(model.LyDoKham) ||
            string.IsNullOrWhiteSpace(model.MaPhong))
            throw new ArgumentException("Cần xác minh họ tên, ngày sinh, điện thoại, lý do và phòng khám.");
        if (model.LoaiKham == "DichVu" &&
            (string.IsNullOrWhiteSpace(model.MaBS) || string.IsNullOrWhiteSpace(model.MaDV)))
            throw new ArgumentException("Khám dịch vụ bắt buộc chọn bác sĩ và dịch vụ.");
        return data.taoPhieu(model);
    }
}
