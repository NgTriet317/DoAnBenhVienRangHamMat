using DAL;
using ET;

namespace BUS;

/// <summary>
/// Tầng nghiệp vụ báo cáo, nhận yêu cầu từ giao diện và điều phối lớp DAL tương ứng.
/// </summary>
public sealed class BUS_BAOCAO
{
    /// <summary>
    /// Đọc báo cáo theo chuyên đề, thời gian và các bộ lọc được truyền vào; ánh xạ dữ liệu cho bước tổng hợp/hiển thị.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>DateTime.MaxValue.Date.AddDays</c>.</remarks>
    /// <param name="ma">Mã định danh của bản ghi cần xử lý.</param>
    /// <param name="tuNgay">Giá trị từ ngày dùng khi thực hiện thao tác; kiểu DateTime.</param>
    /// <param name="denNgay">Giá trị đến ngày dùng khi thực hiện thao tác; kiểu DateTime.</param>
    /// <param name="canhBao">Giá trị cảnh báo dùng khi thực hiện thao tác; kiểu int.</param>
    /// <param name="coQuyenBaoCao">Giá trị có quyền báo cáo dùng khi thực hiện thao tác; kiểu bool.</param>
    /// <returns>Tập dữ liệu kiểu ET_DONGBAOCAO[] theo điều kiện và phạm vi xử lý của hàm.</returns>
    public ET_DONGBAOCAO[] Doc(string ma, DateTime tuNgay, DateTime denNgay, int canhBao, bool coQuyenBaoCao)
    {
        if (!coQuyenBaoCao) throw new UnauthorizedAccessException("Tài khoản hiện tại không có quyền đọc báo cáo.");
        if (tuNgay.Date > denNgay.Date) throw new ArgumentException("Từ ngày phải trước hoặc bằng Đến ngày.");
        if ((denNgay.Date - tuNgay.Date).TotalDays > 3660) throw new ArgumentException("Khoảng báo cáo tối đa 10 năm.");
        if (denNgay.Date >= DateTime.MaxValue.Date || tuNgay.Date > DateTime.MaxValue.Date.AddDays(-90))
            throw new ArgumentException("Ngày báo cáo nằm ngoài phạm vi hỗ trợ.");
        if (canhBao is not (30 or 60 or 90)) throw new ArgumentException("Ngưỡng cảnh báo phải là 30, 60 hoặc 90 ngày.");
        if (ma == "14" && tuNgay.Date != denNgay.Date) throw new ArgumentException("Hạn dùng phải kiểm tra tại một ngày chốt.");
        return new DAL_BAOCAO().Doc(ma, tuNgay, denNgay, canhBao);
    }
}
