using System;
using System.Collections.Generic;
using DAL;
using ET;

namespace BUS;

public class BUS_AITIEPNHAN
{
    private readonly DAL_AITIEPNHAN data = new();

    // Chỉ ghi bản khai chờ nhân viên; không tạo BN/HS/phiếu khám tại đây.
    public string luuBanKhai(ET_AI_BAN_KHAI banKhai, bool choPhepThieu = false)
    {
        ArgumentNullException.ThrowIfNull(banKhai);
        if (banKhai.MaPhien == Guid.Empty)
            throw new ArgumentException("Thiếu mã phiên AI.");
        if (!choPhepThieu && (string.IsNullOrWhiteSpace(banKhai.HoTen)
            || banKhai.NgaySinh is null || string.IsNullOrWhiteSpace(banKhai.SoDienThoai)
            || string.IsNullOrWhiteSpace(banKhai.LyDoKham)))
            throw new InvalidOperationException("Vui lòng khai đủ họ tên, ngày sinh, số điện thoại và lý do trước khi gửi bản khai.");
        if (banKhai.NgaySinh is DateTime dob && (dob.Year < 1900 || dob.Date > DateTime.Today))
            throw new InvalidOperationException("Ngày sinh không hợp lệ.");
        if (banKhai.HoTen.Length > 255 || banKhai.SoDienThoai.Length > 25
            || banKhai.LyDoKham.Length > 1000 || banKhai.TrieuChung?.Length > 1000)
            throw new InvalidOperationException("Nội dung bản khai vượt độ dài cho phép.");
        if (!string.IsNullOrWhiteSpace(banKhai.SoDienThoai)
            && !System.Text.RegularExpressions.Regex.IsMatch(banKhai.SoDienThoai, @"^0[0-9]{9}$"))
            throw new InvalidOperationException("Số điện thoại phải gồm 10 chữ số, bắt đầu bằng 0.");
        if (banKhai.LoaiKham is not (null or "ThongThuong" or "DichVu"))
            throw new InvalidOperationException("Loại khám không hợp lệ.");
        return data.luuBanKhai(banKhai);
    }
    public List<ET_AITIEPNHAN> layDanhSachChoDuyet() => data.layDanhSachChoDuyet();

    public void xacNhanPhieu(Guid maPhien, string maPhieu, string maNV)
    {
        if (maPhien == Guid.Empty || string.IsNullOrWhiteSpace(maPhieu) ||
            string.IsNullOrWhiteSpace(maNV))
            throw new ArgumentException("Thiếu mã phiên, mã phiếu hoặc mã nhân viên.");
        data.xacNhanPhieu(maPhien, maPhieu.Trim(), maNV.Trim());
    }
}
