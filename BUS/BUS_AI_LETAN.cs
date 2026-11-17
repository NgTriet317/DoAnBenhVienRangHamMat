using System;
using System.Collections.Generic;
using DAL;
using ET;

namespace BUS;

/// <summary>Đường nghiệp vụ tạo phiếu chính thức: BUS -> DAL_AI_TAOPHIEU -> stored procedure.</summary>
public sealed class BUS_AI_LETAN
{
    private readonly DAL_AI_LETAN dal = new();
    private readonly BUS_AI_TAOPHIEU taoPhieu = new();

    public List<ET_AI_PHONG_LETAN> LayPhongDangCho(string maKhoa) => dal.LayPhongDangCho(maKhoa);
    public List<ET_AI_DANHMUC> LayBacSi(string maPhong) => taoPhieu.layBacSi(maPhong, false);
    public string TraCuu(string queryType) => dal.TraCuu(queryType);

    public ET_AI_KETQUATAO TaoPhieuKham(ET_AI_TAOPHIEU model)
    {
        ArgumentNullException.ThrowIfNull(model);
        // Tạo phiên SQL dựa vào nội dung đã được bệnh nhân xác nhận.
        dal.DamBaoPhienChoTaoPhieu(model);
        // Hàm hiện có sẽ tái xác thực quyền, đối chiếu hồ sơ, bác sĩ, chống trùng và cấp STT.
        var result = taoPhieu.taoPhieu(model);
        // Tạo phiếu là thao tác đã commit trong stored procedure. Lỗi bổ sung ghi chú
        // không được làm WPF báo "chưa tạo" rồi tạo lại phiếu đã có.
        try { dal.GhiNguonGocTuDong(result.MaPhieu, model.MaNV); }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceWarning(
                $"AI lễ tân: đã tạo phiếu nhưng không cập nhật ghi chú nguồn ({ex.GetType().Name}).");
        }
        return result;
    }
}
