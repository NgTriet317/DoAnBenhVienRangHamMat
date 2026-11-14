using System.Data;
using DAL;
using ET;

namespace BUS;

/// <summary>Đầu mối cho công cụ vận hành theo quyền SELECT của tài khoản SQL, không cấp quyền UI.</summary>
public sealed class BUS_DOISOATDULIEU(IDbTransaction? transaction=null)
{
    /// <summary>
    /// Chạy đối soát chỉ đọc trên database đang kết nối, giới hạn số chi tiết trả về cho mỗi quy tắc.
    /// </summary>
    public KetQuaDoiSoatDuLieu KiemTra(int gioiHanMoiQuyTac=200)
        =>new DAL_DOISOATDULIEU(transaction).KiemTra(gioiHanMoiQuyTac);
}
