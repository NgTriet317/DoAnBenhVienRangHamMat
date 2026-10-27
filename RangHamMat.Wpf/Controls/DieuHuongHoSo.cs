// PHẦN AI ĐÃ THỰC HIỆN: AI hỗ trợ giữ đúng bệnh nhân/lần khám/đơn thuốc khi chuyển màn hình, đối chiếu liên kết trước khi mở hồ sơ và giải thích luồng điều hướng.
// AI hỗ trợ: bổ sung chú thích tiếng Việt cho file này ngày 13/10/2026.
// Nhãn này ghi nhận phần hỗ trợ/chú thích;
using System.Windows;
namespace RangHamMat.Controls;
/// <summary>
/// Cầu nối từ control con tới workspace, truyền ngữ cảnh bệnh nhân/lần khám/đơn thuốc khi mở phân hệ liên quan.
/// </summary>
public static class DieuHuongHoSo
{
    /// <summary>
    /// Tìm workspace từ control nguồn và yêu cầu điều hướng cùng bệnh nhân/lần khám/đơn thuốc; workspace kiểm tra lại các liên kết trước khi chuyển.
    /// </summary>
    public static void Open(DependencyObject origin, string module, string patientId, string? visitId = null, string? prescriptionId = null,string? medicalId=null,string? receiptId=null,string? invoiceId=null)
    {
        try { ClinicalWorkflow.QuyenNghiepVu.KiemTraLienKet(patientId, visitId, prescriptionId); }
        catch (InvalidOperationException ex) { MessageBox.Show(ex.Message, "Quyền truy cập"); return; }
        HoTroGiaoDien.Workspace(origin)?.DieuHuongHoSo(module, patientId, visitId, prescriptionId,medicalId,receiptId,invoiceId);
    }
}
