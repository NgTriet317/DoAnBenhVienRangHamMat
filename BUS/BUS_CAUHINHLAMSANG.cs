using System.Data;
using DAL;
using ET;

namespace BUS;

public abstract class BUS_CAUHINHLAMSANG<T>(DAL_CAUHINHLAMSANG<T> dal) where T:class,new()
{
    public IReadOnlyList<T> LayDanhSach(NguCanhNghiepVu nguoiDung) => XuLyLoi.ThucHien(()=>dal.LayDanhSach(nguoiDung));
    public bool LuuNhap(T duLieu,NguCanhNghiepVu nguoiDung,T? banGoc=null) => XuLyLoi.ThucHien(()=>dal.LuuNhap(duLieu,nguoiDung,banGoc));
}

public sealed class BUS_DMTINHTRANGBENH(IDbTransaction? transaction=null) : BUS_CAUHINHLAMSANG<ET_DMTINHTRANGBENH>(new DAL_DMTINHTRANGBENH(transaction));
public sealed class BUS_MAUKEHOACHDIEUTRI(IDbTransaction? transaction=null) : BUS_CAUHINHLAMSANG<ET_MAUKEHOACHDIEUTRI>(new DAL_MAUKEHOACHDIEUTRI(transaction));
public sealed class BUS_CHITIETMAUKEHOACH(IDbTransaction? transaction=null) : BUS_CAUHINHLAMSANG<ET_CHITIETMAUKEHOACH>(new DAL_CHITIETMAUKEHOACH(transaction));
public sealed class BUS_NHOMTHUOCKEDON(IDbTransaction? transaction=null) : BUS_CAUHINHLAMSANG<ET_NHOMTHUOCKEDON>(new DAL_NHOMTHUOCKEDON(transaction));
public sealed class BUS_CAUHINHTHUOCKEDON(IDbTransaction? transaction=null) : BUS_CAUHINHLAMSANG<ET_CAUHINHTHUOCKEDON>(new DAL_CAUHINHTHUOCKEDON(transaction));
public sealed class BUS_BANGIAOTHUOC(IDbTransaction? transaction=null)
{
    private readonly DAL_BANGIAOTHUOC dal=new(transaction);
    public ET_BANGIAOTHUOC? LayTheoDon(string maDT,NguCanhNghiepVu nguoiDung) => XuLyLoi.ThucHien(()=>dal.LayTheoDon(maDT,nguoiDung));
}
