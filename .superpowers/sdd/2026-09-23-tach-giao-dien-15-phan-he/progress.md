# SDD ledger — plan: docs/superpowers/plans/2026-09-23-tach-giao-dien-15-phan-he.md

Setup: inline Native execution; workspace is not a Git checkout, so build/check results are the checkpoints instead of commits.

Pre-flight shared interfaces:
- Task 1 produces the XAML-only checker consumed by Tasks 2-7; the checker will be extended with each page group before that group is created.
- Tasks 2-6 produce page pairs consumed by Task 7 for the static shell list and legacy-reference scan.
- Task 7 produces the final active-source layout consumed by Task 8 documentation and full verification.

Ruling: use the plan's required filenames and static-page scope exactly — the approved spec is binding, and preserving generic data-driven behavior would contradict the user's UI-only requirement; cost if wrong: later navigation/data work would need a separate change.

Task 1: complete — checker now asserts required page pairs and rejects interaction/data-binding markers on new pages; the expected red checkpoint identified the missing support-page files before implementation, then the checker passed after Task 2.
Task 2: complete — created TongQuan, BaoCao, PhieuHuongDan, NhatKy, and ChonRang as static XAML/control surfaces with minimal constructors; checker passed (16 files) and Release WPF build passed (0 warnings, 0 errors).
Task 3: complete — created BenhNhan, KhamDieuTri, and DonThuoc as separate static patient-care surfaces; checker passed (19 files) and Release WPF build passed (0 warnings, 0 errors).
Task 4: complete — created NhanSu, CaLamViec, and LichKham as separate workforce/schedule surfaces; checker passed (22 files) and Release WPF build passed (0 warnings, 0 errors).
Task 5: complete — created ThietBi, BaoTri, VatTu, NhaCungCap, and NhapXuatVatTu as separate static asset/inventory surfaces; checker passed (27 files) and Release WPF build passed (0 warnings, 0 errors).
Task 6: complete — created DichVu, HoaDon, BangLuong, and TaiKhoan as separate static finance/account surfaces; checker passed (31 files) and Release WPF build passed (0 warnings, 0 errors).
Task 7: complete — replaced the data-template shell with 20 static sidebar labels, simplified MainWindow.xaml.cs, removed all eight obsolete generic view pairs after the reference scan, and rechecked successfully (23 XAML files; Release WPF build 0 warnings, 0 errors; no legacy references).
Task 8: complete — updated README.md, KET_QUA_KIEM_TRA.md, and HUONG_DAN_VISUAL_STUDIO_2022.html for the static multi-page architecture. Final Debug/Release solution builds both passed with 0 warnings and 0 errors; Debug/Release checkers both passed with 23 files; final source/document scans returned no matches.

Final review: self-review (no subagent tool) — inspected the checker, shell, page pairs, approved spec/plan, and documentation. No Critical, Important, or Minor issues found within the approved scope. The application has shell-only page navigation; it still has no persistence, API/database, or business behavior.

Follow-up change: approved bounded navigation behavior — added one `Navigation_SelectionChanged` handler in MainWindow, direct mappings for all 20 sidebar items, `TongQuan` default selection, and a separate `ChonRang` window case. Debug/Release checker and solution builds passed after the change; a static mapping check passed for all 20 labels; documentation/spec/plan wording was updated to record the shell-only navigation exception.

Follow-up change: grouped the single scrollable sidebar into seven non-selectable headers with the original 20 selectable page items preserved. Restored the required `ChonRang` page pair found missing in the current workspace; checker and Release build passed.

Follow-up change: added five sample roles to `RolePicker` and role-based sidebar visibility using the approved matrix. This is UI-only simulation; Debug/Release builds and checkers passed, and page views remain free of interaction/data-binding markers.

Follow-up change: added 15 separate action-window pairs (`CuaSo*.xaml/.xaml.cs`), each with static controls and only `Thêm`/`Đóng`; added `MoCuaSo:<stem>` links on all 15 business pages and one routed shell opener. Debug/Release builds passed with 0 warnings and 0 errors; checker passed with 38 XAML files.

Follow-up change: added the static `DangNhap.xaml/.xaml.cs` window with username, password, role, `Đăng nhập`, and `Thoát` controls. No authentication or navigation was added; checker scope is now 39 XAML files.

Follow-up change: temporarily hid the tooth-related UI by removing the `Chọn răng` sidebar item and replacing the tooth selector in `KhamDieuTri` with the general treatment form. `ChonRang.xaml/.xaml.cs` remains in the source tree for later restoration; checker and source layout now enforce the hidden state.

Follow-up change: reorganized the visible sidebar into the approved six groups — Tổng quan, Khám chữa bệnh, Nhân sự, Kho và thiết bị, Tài chính, Hệ thống — with 19 renamed items and updated shell navigation mappings. The tooth-related item remains hidden.
