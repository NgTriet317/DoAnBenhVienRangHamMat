namespace ET;

/// <summary>Dòng chỉ đọc từ truy vấn báo cáo, gồm chỉ số và thông tin đối chiếu nguồn.</summary>
public sealed class ET_DONGBAOCAO
{
    /// <summary>
    /// Ngày.
    /// </summary>
    public DateTime Ngay { get; set; }
    /// <summary>
    /// Một.
    /// </summary>
    public decimal Mot { get; set; }
    /// <summary>
    /// Hai.
    /// </summary>
    public decimal Hai { get; set; }
    /// <summary>
    /// Ba.
    /// </summary>
    public decimal Ba { get; set; }
    /// <summary>
    /// Bốn.
    /// </summary>
    public decimal Bon { get; set; }
    /// <summary>
    /// Nhóm.
    /// </summary>
    public string Nhom { get; set; } = "";
    /// <summary>
    /// Giá trị cơ cấu.
    /// </summary>
    public decimal GiaTriCoCau { get; set; } = 1;
    /// <summary>
    /// Mã.
    /// </summary>
    public string Ma { get; set; } = "";
    /// <summary>
    /// Tên.
    /// </summary>
    public string Ten { get; set; } = "";
    /// <summary>
    /// Phân loại.
    /// </summary>
    public string PhanLoai { get; set; } = "";
    /// <summary>
    /// Đơn vị.
    /// </summary>
    public string DonVi { get; set; } = "";
    /// <summary>
    /// Ngày chi tiết. Có thể để null khi dữ liệu chưa được cung cấp.
    /// </summary>
    public DateTime? NgayChiTiet { get; set; }
    /// <summary>
    /// Đến ngày chi tiết. Có thể để null khi dữ liệu chưa được cung cấp.
    /// </summary>
    public DateTime? DenNgayChiTiet { get; set; }
    /// <summary>
    /// Chi tiết một. Có thể để null khi dữ liệu chưa được cung cấp.
    /// </summary>
    public decimal? ChiTietMot { get; set; }
    /// <summary>
    /// Chi tiết hai. Có thể để null khi dữ liệu chưa được cung cấp.
    /// </summary>
    public decimal? ChiTietHai { get; set; }
    /// <summary>
    /// Chi tiết ba. Có thể để null khi dữ liệu chưa được cung cấp.
    /// </summary>
    public decimal? ChiTietBa { get; set; }
    /// <summary>
    /// Chi tiết bốn. Có thể để null khi dữ liệu chưa được cung cấp.
    /// </summary>
    public decimal? ChiTietBon { get; set; }
    /// <summary>
    /// Trạng thái.
    /// </summary>
    public string TrangThai { get; set; } = "";
    /// <summary>
    /// Nội dung ghi chú bổ sung của bản ghi.
    /// </summary>
    public string GhiChu { get; set; } = "";
}
