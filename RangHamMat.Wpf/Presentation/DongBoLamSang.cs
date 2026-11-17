using BUS;
using ET;
using RangHamMat.Controls;
using RangHamMat.Controls.ClinicalWorkflow;
using RangHamMat.Services;

namespace RangHamMat.Views;

public partial class HuongDanThuocEditor : IClinicalPendingWork
{
    private sealed record AnhChupHuongDan(LuaChonDanhMuc[] Nhom, LuaChonDanhMuc[] Thuoc,
        string? MaThuoc, BanNhapHuongDanThuoc? HuongDan);

    private void DangKyDongBoManHinh()
    {
        DongBoManHinh.SetLaBoLoc(DrugQuery, true);
        DongBoManHinh.RegisterPrepared(this,
            ["CAU_HINH_THUOC_KE_DON", "NHOM_THUOC_KE_DON", "THUOC", "HANG_HOA"],
            ChuanBiDongBoHuongDan, () => loading || CoThayDoiChuaLuu);
    }

    private Task<Action> ChuanBiDongBoHuongDan(CancellationToken token)
    {
        string query = DrugQuery.Text;
        string? selected = selectedDrug;
        return DongBoChuanBi.Doc(() =>
        {
            var groups = new[] { new LuaChonDanhMuc("", "Chưa xếp nhóm") }.Concat(
                new BUS_NHOMTHUOCKEDON(transaction).LayDanhSach(actor).Where(g => g.TrangThai == "Đang áp dụng")
                    .Select(g => new LuaChonDanhMuc(g.MaNhomThuoc, g.TenNhom))).ToArray();
            var drugs = service.TimThuoc(new(TuKhoa: query), actor)
                .Select(d => new LuaChonDanhMuc(d.MaThuoc, $"{d.TenThuoc} · {d.HamLuong} ({d.MaThuoc})")).ToArray();
            string? keep = drugs.Any(d => d.Value == selected) ? selected : null;
            return new AnhChupHuongDan(groups, drugs, keep, keep is null ? null : service.LayHuongDan(keep, actor));
        }, data =>
        {
            bool previous = loading;
            loading = true;
            try
            {
                GroupChoice.ItemsSource = data.Nhom;
                DrugChoice.ItemsSource = data.Thuoc;
                selectedDrug = data.MaThuoc;
                DrugChoice.SelectedValue = data.MaThuoc;
                Fill(data.HuongDan);
            }
            finally { loading = previous; }
        }, token);
    }

    string IClinicalPendingWork.WorkTitle => "Nhóm và hướng dẫn kê đơn";
    bool IClinicalPendingWork.HasChanges => CoThayDoiChuaLuu;
    bool IClinicalPendingWork.SaveBeforeLeave(out string error)
    {
        if (GroupName.Text.Length > 0)
        {
            error = ErrorText.Text = "Lưu nhóm kê đơn tại màn hình này trước khi rời, hoặc chọn Bỏ thay đổi.";
            return false;
        }
        return TrySave(out error);
    }
    bool IClinicalPendingWork.DiscardBeforeLeave(out string error) => Run(() =>
    {
        var current = selectedDrug is string id ? service.LayHuongDan(id, actor) : null;
        GroupName.Clear();
        Fill(current);
    }, out error);
}
