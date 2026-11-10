namespace ET;

public sealed record ThongKeBangDoiSoat(string LuocDo,string Bang,long SoDong);
/// <summary>Chỉ PK/mã liên quan; không sao chép tên, ngày sinh hoặc nội dung bệnh án.</summary>
public sealed record ChiTietDoiSoat(string Bang,string Khoa,string LienQuan);
public sealed record QuyTacDoiSoat(string Ma,string MucDo,string MoTa,long TongSo,IReadOnlyList<ChiTietDoiSoat> ChiTiet)
{
    public bool DaGioiHan=>ChiTiet.Count<TongSo;
}
public sealed record KetQuaDoiSoatDuLieu(string Database,DateTime BatDauUtc,DateTime KetThucUtc,
    IReadOnlyList<ThongKeBangDoiSoat> CacBang,IReadOnlyList<QuyTacDoiSoat> QuyTac)
{
    public long TongSaiXacDinh=>QuyTac.Where(x=>x.MucDo=="SaiXacDinh").Sum(x=>x.TongSo);
    public long TongCanDoiChieu=>QuyTac.Where(x=>x.MucDo=="CanDoiChieu").Sum(x=>x.TongSo);
}
