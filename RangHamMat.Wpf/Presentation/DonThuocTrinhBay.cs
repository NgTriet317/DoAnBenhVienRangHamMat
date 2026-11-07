using System.Globalization;
using ET;
using RangHamMat.Controls;

namespace RangHamMat.Views;

/// <summary>Dữ liệu trình bày dùng chung cho thẻ thuốc và phiếu in; không suy diễn chỉ định từ văn bản.</summary>
public sealed record DongDonThuocTrinhBay(int SoThuTu, string TenThuoc, string HamLuong, string SoLuong,
    string DonViTinh, string SoNgay, string LieuDung, string DuongDung, string HuongDan)
{
    public string ThuTu => SoThuTu.ToString("00", CultureInfo.InvariantCulture);
    public string ThoiGianDung => SoNgay + " ngày";
    public string LuongThuoc => (SoLuong + " " + DonViTinh).Trim();
    public bool CoHamLuong => !string.IsNullOrWhiteSpace(HamLuong);
    public bool CoDuongDung => !string.IsNullOrWhiteSpace(DuongDung);
}

public sealed record DonThuocTrinhBay(string MaDon, string MaLanKham, string MaBenhNhan, string TenBenhNhan,
    string NgaySinh, string DienThoai, string TenBacSi, string MaBacSi, string NgayKe, string ChanDoan,
    string? GhiChu, string TrangThai, IReadOnlyList<DongDonThuocTrinhBay> CacThuoc)
{
    public string TieuDe => "Đơn thuốc #" + MaDon;
    public string SoLoaiThuoc => CacThuoc.Count.ToString("00", CultureInfo.InvariantCulture);
    public string BacSi => string.IsNullOrWhiteSpace(MaBacSi) ? TenBacSi : $"{TenBacSi} ({MaBacSi})";
    public bool CoGhiChu => !string.IsNullOrWhiteSpace(GhiChu);
    public bool CoDienThoai => !string.IsNullOrWhiteSpace(DienThoai) && DienThoai != "—";
    public string DienThoaiHienThi => CoDienThoai ? DienThoai : "Chưa ghi nhận";
    public string ChanDoanHienThi => string.IsNullOrWhiteSpace(ChanDoan) ? "Chưa ghi nhận" : ChanDoan;

    public static DonThuocTrinhBay TuBanIn(BanInDonThuoc source) => new(
        source.Nguon.Don.MaDT, source.Nguon.Don.MaLS, source.Nguon.MaBN, source.HoTenBN,
        BirthDate(source.NgaySinh), "", source.TenBS, source.Nguon.Don.MaBS,
        source.Nguon.Don.NgayKe.ToString("dd/MM/yyyy HH:mm"), source.ChanDoan, source.Nguon.Don.GhiChu,
        source.Nguon.Don.TrangThai, source.Nguon.CacDong.Select((line, index) => From(line, index + 1)).ToArray());

    public static DonThuocTrinhBay TuLichSu(DongLichSuDon source, DonThuocChiTiet detail) => new(
        detail.DonThuoc.MaDT, detail.DonThuoc.MaLS, detail.MaBN, source.HoTenBN,
        BirthDate(source.NgaySinh), source.SDT ?? "", source.TenBS, detail.DonThuoc.MaBS,
        detail.DonThuoc.NgayKe.ToString("dd/MM/yyyy HH:mm"), source.ChanDoan, detail.DonThuoc.GhiChu,
        detail.DonThuoc.TrangThai, detail.CacDong.Select((line, index) => From(line, index + 1)).ToArray());

    public static DonThuocTrinhBay TuTheCu(RxCard source) => new(
        source.Code, source.Visit, source.PatientId,
        source.Record is DongLichSuDon history ? history.HoTenBN : source.Patient,
        source.BirthDate, source.Phone, source.Doctor, "", source.When, source.Diagnosis, source.Note, source.Status,
        source.Lines.Select((line, index) => new DongDonThuocTrinhBay(index + 1,
            SnapshotName(UiRecord.Text(line.Record, "TenThuocChot"), UiRecord.Text(line.Record, "MaThuoc")),
            UiRecord.Text(line.Record, "HamLuongChot"), line.SoLuong, UiRecord.Text(line.Record, "DonViTinhChot"),
            line.SoNgayDung, line.LieuDung, line.DuongDung, line.HuongDan)).ToArray());

    private static DongDonThuocTrinhBay From(ET_CHITIETDONTHUOC line, int number) => new(number,
        SnapshotName(line.TenThuocChot, line.MaThuoc), line.HamLuongChot ?? "",
        line.SoLuong.ToString("0.###", CultureInfo.GetCultureInfo("vi-VN")), line.DonViTinhChot ?? "",
        line.SoNgayDung.ToString(CultureInfo.InvariantCulture), line.LieuDung, line.DuongDung ?? "", line.HuongDan);

    private static string SnapshotName(string? value, string code) => string.IsNullOrWhiteSpace(value) ? "Thuốc #" + code : value;
    private static string BirthDate(DateTime? value) => value?.ToString("dd/MM/yyyy") ?? "Chưa ghi nhận";
}
