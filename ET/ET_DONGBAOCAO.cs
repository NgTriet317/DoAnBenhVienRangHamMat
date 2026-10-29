namespace ET;

/// <summary>Dòng chỉ đọc từ truy vấn báo cáo, gồm chỉ số và thông tin đối chiếu nguồn.</summary>
public sealed class ET_DONGBAOCAO
{
    public DateTime Ngay { get; set; }
    public decimal Mot { get; set; }
    public decimal Hai { get; set; }
    public decimal Ba { get; set; }
    public decimal Bon { get; set; }
    public string Nhom { get; set; } = "";
    public decimal GiaTriCoCau { get; set; } = 1;
    public string Ma { get; set; } = "";
    public string Ten { get; set; } = "";
    public string PhanLoai { get; set; } = "";
    public string DonVi { get; set; } = "";
    public DateTime? NgayChiTiet { get; set; }
    public DateTime? DenNgayChiTiet { get; set; }
    public decimal? ChiTietMot { get; set; }
    public decimal? ChiTietHai { get; set; }
    public decimal? ChiTietBa { get; set; }
    public decimal? ChiTietBon { get; set; }
    public string TrangThai { get; set; } = "";
    public string GhiChu { get; set; } = "";
}
