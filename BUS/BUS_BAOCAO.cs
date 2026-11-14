using DAL;
using ET;

namespace BUS;

/// <summary>
/// Tầng nghiệp vụ báo cáo, nhận yêu cầu từ giao diện và điều phối lớp DAL tương ứng.
/// </summary>
public sealed class BUS_BAOCAO
{
    /// <summary>
    /// Đọc quyền báo cáo được DAL xác thực từ principal, phiên đăng nhập và vai trò SQL hiện tại.
    /// </summary>
    public QuyenBaoCao LayQuyen() => new DAL_BAOCAO().LayQuyen();
    /// <summary>
    /// Kiểm tra cờ dữ liệu trình diễn trong database sau bước xác thực quyền báo cáo.
    /// </summary>
    public bool LaDuLieuTrinhDien()=>new DAL_BAOCAO().LaDuLieuTrinhDien();
    /// <summary>
    /// Lấy danh mục chuyên khoa và dịch vụ làm bộ lọc thống kê bệnh lý qua DAL.
    /// </summary>
    public (LuaChonThongKe[] ChuyenKhoa,LuaChonThongKe[] DichVu) LayDanhMucLoc() => new DAL_BAOCAO().LayDanhMucLoc();
    /// <summary>
    /// Ghi lượt xem hoặc xuất báo cáo cùng khoảng ngày sau khi DAL kiểm tra quyền và phiên.
    /// </summary>
    public void GhiTruyCap(string ma,string hanhDong,DateTime tu,DateTime den) => new DAL_BAOCAO().GhiTruyCap(ma,hanhDong,tu,den);
    /// <summary>
    /// Xác thực lại quyền xuất ngay trước khi xuất dữ liệu đã tải.
    /// </summary>
    /// <param name="ma">Mã chuyên đề báo cáo cần xuất.</param>
    /// <param name="gomChiTiet">true khi tệp xuất kèm dữ liệu chi tiết và cần quyền chuyên đề tương ứng.</param>
    public void KiemTraXuat(string ma,bool gomChiTiet=false) => new DAL_BAOCAO().KiemTraXuat(ma,gomChiTiet);
    /// <summary>
    /// Đọc chi tiết chuyên đề qua DAL, nơi kiểm tra quyền riêng và giới hạn số dòng chứng từ.
    /// </summary>
    public ET_DONGBAOCAO[] DocChiTiet(string ma, DateTime tu, DateTime den, int canhBao=30)
        => new DAL_BAOCAO().DocChiTiet(ma,tu,den,canhBao);
    /// <summary>
    /// Đọc chất lượng gắn mã và số liệu bệnh lý theo kỳ nửa mở, chuyên khoa và dịch vụ được chọn.
    /// </summary>
    public KetQuaBenhLy DocBenhLy(KySoSanh ky, string? maCK = null, string? maDV = null)
        => new DAL_BAOCAO().DocBenhLy(ky,maCK,maDV);

    /// <summary>
    /// Đọc báo cáo với quyền được xác thực lại tại DAL từ phiên đăng nhập hiện tại.
    /// </summary>
    /// <remarks>Đầu mối này không nhận cờ quyền từ giao diện; overload có cờ quyền thực hiện thêm kiểm tra đầu vào trước khi chuyển về đây.</remarks>
    public ET_DONGBAOCAO[] Doc(string ma, DateTime tuNgay, DateTime denNgay, int canhBao = 30)
    {
        // DAL không tin cờ quyền do caller truyền; xác thực principal, phiên và vai trò SQL mỗi lần đọc.
        return new DAL_BAOCAO().Doc(ma,tuNgay,denNgay,canhBao);
    }
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
        return Doc(ma, tuNgay, denNgay, canhBao);
    }
}
