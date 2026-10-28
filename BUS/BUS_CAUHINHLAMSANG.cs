using System.Data;
using DAL;
using ET;

namespace BUS;

public abstract class BUS_CAUHINHLAMSANG<T>(DAL_CAUHINHLAMSANG<T> dal) where T:class,new()
{
    public IReadOnlyList<T> LayDanhSach(NguCanhNghiepVu nguoiDung) => XuLyLoi.ThucHien(()=>dal.LayDanhSach(nguoiDung));
    public bool LuuNhap(T duLieu,NguCanhNghiepVu nguoiDung,T? banGoc=null) => XuLyLoi.ThucHien(()=>dal.LuuNhap(duLieu,nguoiDung,banGoc));
}

public sealed class BUS_NHOMTHUOCKEDON(IDbTransaction? transaction=null) : BUS_CAUHINHLAMSANG<ET_NHOMTHUOCKEDON>(new DAL_NHOMTHUOCKEDON(transaction));
public sealed class BUS_CAUHINHTHUOCKEDON(IDbTransaction? transaction=null) : BUS_CAUHINHLAMSANG<ET_CAUHINHTHUOCKEDON>(new DAL_CAUHINHTHUOCKEDON(transaction));
