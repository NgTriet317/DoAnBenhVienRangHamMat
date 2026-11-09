using System.Data;
using DAL;

namespace BUS;

/// <summary>
/// Phiếu hướng dẫn là bản in của phiếu tiếp nhận, không phải một lượt khám mới.
/// </summary>
public class BUS_PHIEUHUONGDAN
{
    private readonly DAL_PHIEUHUONGDAN data = new();

    public DataTable layDanhSachPhieu(string maNV, bool laAdmin)
        => XuLyLoi.ThucHien(() => data.layDanhSachPhieu(maNV, laAdmin));
}
