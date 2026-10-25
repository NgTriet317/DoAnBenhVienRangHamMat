using System.Data;
using DAL;
using ET;

namespace BUS;

public sealed class BUS_TIEPNHAN(IDbTransaction? transaction=null)
{
    private readonly DAL_TIEPNHAN dal=new(transaction);
    public IReadOnlyList<BacSiTiepNhan> LayBacSi(string maDV,NguCanhNghiepVu nguoiDung) => XuLyLoi.ThucHien(()=>dal.LayBacSi(maDV,nguoiDung));
    public KetQuaTiepNhan TiepNhan(YeuCauTiepNhan yeuCau,NguCanhNghiepVu nguoiDung) => XuLyLoi.ThucHien(()=>dal.TiepNhan(yeuCau,nguoiDung));
}
