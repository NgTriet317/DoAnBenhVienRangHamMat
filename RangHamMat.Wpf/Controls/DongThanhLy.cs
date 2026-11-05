namespace RangHamMat.Controls;

/// <summary>Thông tin tại thời điểm thanh lý; không đổi theo tên danh mục hiện tại.</summary>
public sealed class DongThanhLy(object record)
{
    public object Record { get; } = record;
    public string MaTB => UiRecord.Text(Record, "MaTB");
    public string SoSerial => UiRecord.Text(Record, "SoSerial");
    public string TenThietBi => UiRecord.Text(Record, "TenThietBiThanhLy");
    public string TenPhong => UiRecord.Text(Record, "TenPhongThanhLy");
    public string NhanVien => UiRecord.Text(Record, "TenNhanVienThanhLy");
    public string MaNV => UiRecord.Text(Record, "MaNVThanhLy");
    public DateTime NgayThanhLy => UiRecord.Date(Record, "NgayThanhLy") ?? DateTime.MinValue;
    public string LyDo => UiRecord.Text(Record, "LyDoThanhLy");
    public decimal GiaTri => Convert.ToDecimal(UiRecord.Get(Record, "GiaTriThanhLy"));
    public string TuKhoa => string.Join(" ", MaTB, SoSerial, TenThietBi, TenPhong, NhanVien, MaNV, LyDo);
}
