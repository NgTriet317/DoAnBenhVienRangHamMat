using BUS;

void Check(bool value, string name) { if (!value) throw new Exception(name); Console.WriteLine("PASS " + name); }
var pending = new TrangThaiDongBo();
Check(!pending.CoThayDoi, "Không tự tải lại khi không có thay đổi");
pending.GhiNhan(new[] { "BENH_NHAN", "HOA_DON", "BENH_NHAN" });
var first = pending.Chup();
Check(first.Count == 2, "Gom bảng trùng và giữ đủ bảng khác nhau");
Check(pending.Chup().Count == 2, "Hoãn/đọc lỗi không làm mất yêu cầu");
pending.GhiNhan(new[] { "BENH_NHAN", "KHO" });
pending.XacNhan(first);
Check(pending.Chup().Keys.ToHashSet().SetEquals(new[]{"BENH_NHAN","KHO"}), "Xác nhận cũ không xóa cập nhật mới đến trong lúc tải");
pending.XacNhan(pending.Chup());
Check(!pending.CoThayDoi, "Chỉ xóa thay đổi sau áp dụng thành công");
pending.GhiNhan(new[]{"THUOC"}); pending.Xoa();
Check(!pending.CoThayDoi, "Đổi phiên không mang pending sang người dùng mới");
Console.WriteLine("Sync state checks passed.");
