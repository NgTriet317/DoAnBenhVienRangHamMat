using System.Data;
using ET;
using DAL;

namespace BUS;

public sealed class BUS_QUYENNGHIEPVU
{
    private readonly DAL_QUYENNGHIEPVU dal;
    public void KiemTraGhiHoSo(string bang, IDictionary<string, object?> giaTri, string? ma, bool xoa, NguCanhNghiepVu nguoiDung)
        => XuLyLoi.ThucHien(() => { dal.KiemTraGhiHoSo(bang, giaTri, ma, xoa, nguoiDung); return true; });
    public BUS_QUYENNGHIEPVU(IDbTransaction? transaction = null) => dal = new(transaction);
    public NguCanhNghiepVu LayNguCanh(string maNV) => XuLyLoi.ThucHien(() => dal.LayNguCanh(maNV));
    public void KiemTraQuyenGhiLanKham(string maLS, NguCanhNghiepVu nguoiDung)
        => XuLyLoi.ThucHien(() => { dal.KiemTraQuyenGhiLanKham(maLS, nguoiDung); return true; });
}
