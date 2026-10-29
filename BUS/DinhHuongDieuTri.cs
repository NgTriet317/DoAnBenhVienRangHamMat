using System.Globalization;
using System.Text;
using ET;
namespace BUS;

public static class DinhHuongDieuTri
{
    // Match a recorded condition, never infer diagnoses from arbitrary notes.
    public static ET_DMTINHTRANGBENH? Tim(string name,IEnumerable<ET_DMTINHTRANGBENH> reference)
    {
        var key=Key(name);if(key.Length==0)return null;
        return reference.FirstOrDefault(r=>r.TrangThai=="Đang áp dụng"&&key==Key(r.TenTinhTrang));
    }
    private static string Key(string text)=>string.Join(" ",new string(text.Trim().Normalize(NormalizationForm.FormD).Where(c=>CharUnicodeInfo.GetUnicodeCategory(c)!=UnicodeCategory.NonSpacingMark).ToArray()).Replace('đ','d').Replace('Đ','D').ToUpperInvariant().Split((char[]?)null,StringSplitOptions.RemoveEmptyEntries));
}
