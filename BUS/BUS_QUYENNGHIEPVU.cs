using System.Data;
using ET;
using DAL;

namespace BUS;

public sealed class BUS_QUYENNGHIEPVU
{
    private readonly DAL_QUYENNGHIEPVU dal;
    public BUS_QUYENNGHIEPVU(IDbTransaction? transaction = null) => dal = new(transaction);
    public NguCanhNghiepVu LayNguCanh(string maNV) => XuLyLoi.ThucHien(() => dal.LayNguCanh(maNV));
    public void KiemTraQuyenGhiLanKham(string maLS, NguCanhNghiepVu nguoiDung)
        => XuLyLoi.ThucHien(() => { dal.KiemTraQuyenGhiLanKham(maLS, nguoiDung); return true; });
}
