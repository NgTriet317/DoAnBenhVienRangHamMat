using DAL;
using ET;

namespace BUS;

public sealed class BUS_BAOCAO
{
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
