namespace ET;

/// <summary>
/// Số dòng của một bảng người dùng theo schema, dùng mô tả phạm vi database đã đối soát.
/// </summary>
public sealed record ThongKeBangDoiSoat(string LuocDo,string Bang,long SoDong);
/// <summary>Chỉ PK/mã liên quan; không sao chép tên, ngày sinh hoặc nội dung bệnh án.</summary>
public sealed record ChiTietDoiSoat(string Bang,string Khoa,string LienQuan);
/// <summary>
/// Kết quả một quy tắc gồm mức độ, tổng vi phạm và chi tiết có giới hạn; tổng số có thể lớn hơn số dòng trả về.
/// </summary>
public sealed record QuyTacDoiSoat(string Ma,string MucDo,string MoTa,long TongSo,IReadOnlyList<ChiTietDoiSoat> ChiTiet)
{
    public bool DaGioiHan=>ChiTiet.Count<TongSo;
}
/// <summary>
/// Báo cáo đối soát của database trong một khoảng UTC, tách tổng lỗi xác định với trường hợp cần kiểm tra thêm.
/// </summary>
public sealed record KetQuaDoiSoatDuLieu(string Database,DateTime BatDauUtc,DateTime KetThucUtc,
    IReadOnlyList<ThongKeBangDoiSoat> CacBang,IReadOnlyList<QuyTacDoiSoat> QuyTac)
{
    public long TongSaiXacDinh=>QuyTac.Where(x=>x.MucDo=="SaiXacDinh").Sum(x=>x.TongSo);
    public long TongCanDoiChieu=>QuyTac.Where(x=>x.MucDo=="CanDoiChieu").Sum(x=>x.TongSo);
}
