using DAL;
using ET;

namespace BUS;

public sealed class BUS_AI_BANKHAI
{
    private readonly DAL_AI_BANKHAI dal = new();

    public KetQuaLuuBanKhaiAI Luu(YeuCauLuuBanKhaiAI m)
    {
        ArgumentNullException.ThrowIfNull(m);
        if (m.MaPhien == Guid.Empty || !m.DongY)
            throw new ArgumentException("Phiên hoặc xác nhận gửi bản khai không hợp lệ.");
        if (m.HoTen?.Length > 255 || m.SoDienThoai?.Length > 25 || m.LyDoKham?.Length > 1000
            || m.TrieuChung?.Length > 1000 || m.LoaiKham is not (null or "ThongThuong" or "DichVu"))
            throw new ArgumentException("Dữ liệu bản khai vượt giới hạn.");
        return dal.Luu(m);
    }
}
