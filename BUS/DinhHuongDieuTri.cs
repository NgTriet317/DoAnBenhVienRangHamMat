using System.Globalization;
using System.Text;
using ET;
namespace BUS;

/// <summary>
/// Mô hình và thao tác hỗ trợ định hướng điều trị dùng bởi các thành phần liên quan.
/// </summary>
public static class DinhHuongDieuTri
{
    // Match a recorded condition, never infer diagnoses from arbitrary notes.
    public static ET_DMTINHTRANGBENH? Tim(string name,IEnumerable<ET_DMTINHTRANGBENH> reference)
    {
        var key=Key(name);if(key.Length==0)return null;
        return reference.FirstOrDefault(r=>r.TrangThai=="Đang áp dụng"&&key==Key(r.TenTinhTrang));
    }
    /// <summary>
    /// Chuẩn hóa chuỗi thành khóa đối chiếu dùng gom nhóm hoặc tra dữ liệu định hướng.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>text.Trim</c>, <c>CharUnicodeInfo.GetUnicodeCategory</c>.</remarks>
    /// <param name="text">Giá trị nội dung dùng khi thực hiện thao tác; kiểu string.</param>
    /// <returns>Chuỗi kết quả của thao tác khóa.</returns>
    private static string Key(string text)=>string.Join(" ",new string(text.Trim().Normalize(NormalizationForm.FormD).Where(c=>CharUnicodeInfo.GetUnicodeCategory(c)!=UnicodeCategory.NonSpacingMark).ToArray()).Replace('đ','d').Replace('Đ','D').ToUpperInvariant().Split((char[]?)null,StringSplitOptions.RemoveEmptyEntries));
}
