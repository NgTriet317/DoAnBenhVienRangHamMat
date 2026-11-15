using DAL;
using ET;

namespace BUS;

public sealed class BUS_AI_TIEPNHAN
{
    private readonly DAL_AI_TIEPNHAN data = new();

    public bool XacThucThietBi(string thumbprint)
    {
        if (string.IsNullOrWhiteSpace(thumbprint) || thumbprint.Length != 40)
            return false;
        return data.XacThucThietBi(thumbprint);
    }

    public KetQuaTaoPhieuAI? LayTheoIdempotency(string thumbprint, Guid key)
    {
        if (key == Guid.Empty || !XacThucThietBi(thumbprint))
            throw new UnauthorizedAccessException("Thiết bị chưa được cấp quyền tra cứu.");
        return data.LayTheoIdempotency(thumbprint, key);
    }

    public IReadOnlyList<LuaChonTiepNhanAI> LayBacSi(DateOnly ngay)
    {
        if (ngay != DateOnly.FromDateTime(DateTime.Today)) return [];
        return data.LayBacSi(ngay);
    }

    public IReadOnlyList<LuaChonTiepNhanAI> LayDichVu(string maBS)
    {
        if (string.IsNullOrWhiteSpace(maBS) || maBS.Length > 50)
            throw new ArgumentException("Chọn bác sĩ hợp lệ trước khi tải dịch vụ.", nameof(maBS));
        return data.LayDichVu(maBS);
    }
}
