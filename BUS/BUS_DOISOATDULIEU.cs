using System.Data;
using DAL;
using ET;

namespace BUS;

/// <summary>Đầu mối cho công cụ vận hành theo quyền SELECT của tài khoản SQL, không cấp quyền UI.</summary>
public sealed class BUS_DOISOATDULIEU(IDbTransaction? transaction=null)
{
    public KetQuaDoiSoatDuLieu KiemTra(int gioiHanMoiQuyTac=200)
        =>new DAL_DOISOATDULIEU(transaction).KiemTra(gioiHanMoiQuyTac);
}
