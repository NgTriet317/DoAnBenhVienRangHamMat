using ET;

namespace BUS;

/// <summary>Ghép bệnh theo mã, giữ số người độc lập với số lượt và không suy diễn số liệu bị ẩn.</summary>
public static class ThongKeBenhLy
{
    public static DongSoSanhBenh[] GhepKy(KetQuaBenhLy hienTai, KetQuaBenhLy? kyGoc)
    {
        var current = hienTai.CacBenh.ToDictionary(x => x.MaDMTT);
        var previous = kyGoc?.CacBenh.ToDictionary(x => x.MaDMTT) ?? new();
        static int? Count(KetQuaBenhLy? period, DongThongKeBenh? row, bool patients)
        {
            if (period is null || row?.DaLamMo == true) return null;
            if (row is not null) return patients ? row.SoBenhNhan : row.SoLuotKham;
            // Thiếu mã có thể chứa bệnh đang xét; chỉ kết luận không có khi phân loại đầy đủ.
            var q = period.ChatLuong;
            return q.LuotCoMa == q.TongLuot && q.DongChuaGanMa == 0 ? 0 : null;
        }
        static decimal? Rate(int? count, KetQuaBenhLy? period) => period?.ChatLuong.SoBenhNhan > 0
            ? 100m * count / period.ChatLuong.SoBenhNhan : null;
        return current.Keys.Union(previous.Keys).Select(code =>
        {
            var a = current.GetValueOrDefault(code); var b = previous.GetValueOrDefault(code);
            var patients = Count(hienTai, a, true); var oldPatients = Count(kyGoc, b, true);
            var visits = Count(hienTai, a, false); var oldVisits = Count(kyGoc, b, false);
            return new DongSoSanhBenh(code, (a ?? b)!.TenTinhTrang,
                patients, oldPatients, visits, oldVisits, Rate(patients, hienTai), Rate(oldPatients, kyGoc),
                SoSanhThongKe.Tinh(patients, oldPatients), SoSanhThongKe.Tinh(visits, oldVisits));
        }).OrderByDescending(x => x.BenhNhanHienTai).ThenByDescending(x => x.BenhNhanGoc).ThenBy(x => x.MaDMTT).ToArray();
    }

    /// <summary>Nêu tất cả nhóm đồng hạng cao nhất trong các nhóm được phép hiển thị.</summary>
    public static string PhoBienNhat(KetQuaBenhLy period, bool theoBenhNhan = true)
    {
        if (period.ChatLuong.TongLuot == 0) return "Không phát sinh lượt khám.";
        var visible = period.CacBenh.Where(x => !x.DaLamMo && (theoBenhNhan ? x.SoBenhNhan : x.SoLuotKham) is > 0).ToArray();
        if (visible.Length == 0) return period.CacBenh.Length == 0
            ? "Chưa đủ mã bệnh để xác định." : "Các nhóm đều dưới 5 bệnh nhân; số liệu được ẩn.";
        int max = visible.Max(x => (theoBenhNhan ? x.SoBenhNhan : x.SoLuotKham)!.Value);
        var names = visible.Where(x => (theoBenhNhan ? x.SoBenhNhan : x.SoLuotKham) == max).Select(x => x.TenTinhTrang).ToArray();
        string result=string.Join("; ", names) + $" · {max:N0} {(theoBenhNhan ? "bệnh nhân" : "lượt khám")}" +
            (names.Length > 1 ? " (đồng hạng)" : "") +
            (period.ChatLuong.LuotCoMa < period.ChatLuong.TongLuot || period.ChatLuong.DongChuaGanMa > 0 ? " · Theo dữ liệu đã phân loại." : "");
        return !theoBenhNhan&&period.CacBenh.Any(x=>x.DaLamMo)
            ? "Chưa thể kết luận tình trạng có nhiều lượt khám nhất vì còn nhóm dưới 5 bệnh nhân bị ẩn. Nhóm hiển thị cao nhất: "+result
            : result;
    }
}
