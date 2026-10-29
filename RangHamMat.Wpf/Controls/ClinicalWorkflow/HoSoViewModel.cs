using System.Globalization;

namespace RangHamMat.Controls.ClinicalWorkflow;

public static class HoSoViewModel
{
    public static IReadOnlyDictionary<string,DateTime> LanKhamGanNhatTheoBenhNhan()
    {
        var historyPatients=DuLieuGiaoDien.Rows("HO_SO_BENH_AN").ToDictionary(h=>UiRecord.Text(h,"MaHS"),h=>UiRecord.Text(h,"MaBN"),StringComparer.Ordinal);
        var receiptPatients=new Dictionary<string,string>(StringComparer.Ordinal);
        foreach(var receipt in DuLieuGiaoDien.Rows("PHIEU_KHAM_BENH"))
            if(historyPatients.TryGetValue(UiRecord.Text(receipt,"MaHS"),out var patient))receiptPatients[UiRecord.Text(receipt,"MaPhieu")]=patient;
        var latest=new Dictionary<string,DateTime>(StringComparer.Ordinal);
        foreach(var visit in DuLieuGiaoDien.Rows("LICH_SU_KHAM_BENH"))
            if(UiRecord.Date(visit,"ThoiGianKham") is DateTime when && receiptPatients.TryGetValue(UiRecord.Text(visit,"MaPhieu"),out var patient)
                && (!latest.TryGetValue(patient,out var current) || when>current))latest[patient]=when;
        return latest;
    }
    public static bool KhopNgaySinh(object patient,string keyword)
    {
        var date=UiRecord.Date(patient,"NgaySinh");
        return date is not null && !string.IsNullOrWhiteSpace(keyword) &&
            (date.Value.ToString("dd/MM/yyyy",CultureInfo.InvariantCulture).Contains(keyword.Trim(),StringComparison.OrdinalIgnoreCase)
             || date.Value.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture).Contains(keyword.Trim(),StringComparison.OrdinalIgnoreCase));
    }
}

/// <summary>Dòng trình bày chỉ đọc, giữ mã BN làm nguồn chọn; không ghi giá trị tính toán vào cache.</summary>
public sealed class BenhNhanDanhSachItem(object record,DateTime? lastVisit=null)
{
    public string MaBN=>UiRecord.Text(record,"MaBN");
    public string HoTenBN=>UiRecord.Text(record,"HoTenBN");
    public string SDT=>UiRecord.Text(record,"SDT");
    public string GioiTinh=>UiRecord.Text(record,"GioiTinh");
    public DateTime? NgaySinh=>UiRecord.Date(record,"NgaySinh");
    public string LanKhamGanNhat=>lastVisit is null?"Chưa có lần khám":"Khám gần nhất: "+lastVisit.Value.ToString("dd/MM/yyyy");
    public string NgaySinhHienThi=>NgaySinh is null?"Chưa ghi ngày sinh":"Sinh: "+NgaySinh.Value.ToString("dd/MM/yyyy");
}
