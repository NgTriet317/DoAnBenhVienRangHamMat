# Đối chiếu testcase Thanh Tân — Sprint 1–3

Ngày rà soát: 18/11/2026. Lần đầu điền 142 ô F/G cho 71 testcase trong TestCase_Nhom4.xlsx. Theo yêu cầu tiếp theo, đã sửa B–G tại 5 dòng 6, 27, 29, 30, 32 của Sprint 2 - Thanh Tan để kiểm tra đúng luồng hiện tại; các testcase khác, sheet Triet và định dạng giữ nguyên.

PASS: có kiểm tra tự động SQL/WPF liên quan đạt, kết hợp đối chiếu mã nguồn. Không đồng nghĩa đã thao tác thủ công toàn bộ các bước trên giao diện. PASS (ĐC): phù hợp khi đối chiếu mã nguồn, chưa chạy đầy đủ tình huống giao diện. FAIL (ĐC): luồng hiện tại khác điều kiện/bước/kết quả của testcase; cần rà soát đặc tả hoặc chức năng, không mặc định là lỗi hồi quy.

Database dùng cho kiểm chứng: RHM_Hospital_Test_TestCaseThanhTan_20261118 trên SQL Server cục bộ; được tạo riêng từ SQL nguồn đã bỏ snapshot. Không chạy ghi thử vào QLBenhVienRangHamMat. Mã nguồn sản phẩm không được sửa.

## Kết quả công cụ

- Quy trình khám: 47 PASS, 0 FAIL; obj/TestCaseThanhTan/workflow.log.
- Kiểm tra bổ sung bám dòng testcase: 26 PASS, 0 FAIL; obj/TestCaseThanhTan/cases.log. Mỗi ca rollback giao dịch. Mã harness tạm: CaseChecks.cs và Shared.cs trong cùng thư mục.
- Bảo trì: 78 PASS, 0 FAIL; obj/TestCaseThanhTan/maintenance-final.log. Bản harness tạm chỉ đổi điều kiện đếm schema cũ sang 74 bảng hiện tại; nghiệp vụ kiểm tra giữ nguyên. Đã bổ sung phòng giả lập trong database thử để đủ điều kiện ca thanh lý.
- Báo cáo SQL/WPF: 57 PASS, 1 FAIL; obj/TestCaseThanhTan/reports-final.log. Mục lỗi là kiểm tra chọn tình trạng trong màn khám, báo Sequence contains no elements tại ClinicalSelectionEvent khi thiếu dữ liệu nguồn phù hợp. Các kiểm tra so sánh kỳ, thu/hoàn, trạng thái ghi sổ, CSV, lọc rỗng và khóa xuất đều PASS. Không kết luận toàn bộ suite báo cáo đạt.
- X-quang: 33 PASS, 1 FAIL; obj/TestCaseThanhTan/xray.log. Mục lỗi là phân công thay kỹ thuật viên đã khóa, báo định danh phiên không hợp lệ; ca này không thuộc 71 dòng cần điền. Các kiểm tra lưu theo MaLS, chặn gắn nhầm bệnh nhân và lịch sử chỉ xem đều PASS. Không kết luận toàn bộ suite X-quang đạt.
- Các lần chạy đầu chưa đủ fixture được giữ trong reports.log, maintenance.log; đã phân biệt lỗi điều kiện bộ kiểm với kết quả nghiệp vụ. Không sử dụng các lỗi fixture đó để đánh FAIL testcase trong workbook.

## Năm testcase đã hiệu chỉnh theo yêu cầu

Kết quả hiện tại: **61 PASS, 10 PASS (ĐC), 0 FAIL**. Năm testcase được sửa nội dung và kiểm thử lại, đạt **5/5**; log: obj/TestCaseThanhTan/Revision/results.log. Mỗi ca dùng giao dịch rollback trong database riêng RHM_Hospital_Test_TestCaseRevision_20261118; không sửa mã nguồn sản phẩm.

- Sprint 2, dòng 6: Chốt chi phí trước khi thực hiện và hoàn tất bảo trì — PASS.
- Sprint 2, dòng 27: Chốt tiền dịch vụ và thuốc đã ký trước thanh toán — PASS.
- Sprint 2, dòng 29: Bảo vệ hóa đơn tạo khi hoàn tất khám — PASS.
- Sprint 2, dòng 30: Chặn thu thiếu, thu đủ một lần và trả tiền thừa — PASS.
- Sprint 2, dòng 32: Xem/in lại hóa đơn đã thu và chặn hoàn qua API cũ — PASS.

Các kết luận FAIL (ĐC) trước đây phản ánh sự khác biệt với nội dung testcase cũ. Lần này sửa cả tình huống, điều kiện, bước thực hiện và kết quả mong đợi trước khi xác nhận PASS; không chỉ thay nhãn kết quả.

## Căn cứ từng dòng

| Sheet | Dòng Excel | Kết quả | Căn cứ trong dự án |
|---|---:|---|---|
| Sprint 1 - Thanh Tan | 5 | PASS | cases.log — S1-R5-R6; DAL/DAL_BENHNHAN.cs |
| Sprint 1 - Thanh Tan | 6 | PASS | cases.log — S1-R5-R6; DAL/DAL_HOSOBENHAN.cs |
| Sprint 1 - Thanh Tan | 7 | PASS | cases.log — S1-R7 |
| Sprint 1 - Thanh Tan | 8 | PASS (ĐC) | RangHamMat.Wpf/Views/HoSoBenhNhan.xaml.cs — Reception_Click; DAL/DAL_TIEPNHAN.cs |
| Sprint 1 - Thanh Tan | 9 | PASS (ĐC) | RangHamMat.Wpf/Views/HoSoBenhNhan.xaml.cs — RefreshProfile, OpenVisit; DAL/DAL_KHAMDIEUTRI.DieuHuong.cs |
| Sprint 1 - Thanh Tan | 10 | PASS (ĐC) | RangHamMat.Wpf/Views/HoSoBenhNhan.xaml.cs — PhoneAppointment_Click, PatientAppointments.SetPatient |
| Sprint 1 - Thanh Tan | 11 | PASS | cases.log — S1-R11; DAL/DAL_KHAMDIEUTRI.DieuHuong.cs |
| Sprint 1 - Thanh Tan | 12 | PASS | cases.log — S1-R12; DAL/DAL_QUYENNGHIEPVU.HoSo.cs |
| Sprint 1 - Thanh Tan | 17 | PASS | cases.log — S1-R17-R19 |
| Sprint 1 - Thanh Tan | 18 | PASS | cases.log — S1-R18; workflow.log — Bác sĩ khác và cờ quyền giả không ghi được tình trạng |
| Sprint 1 - Thanh Tan | 19 | PASS | cases.log — S1-R17-R19 |
| Sprint 1 - Thanh Tan | 20 | PASS | workflow.log — Form thêm/sửa/xóa rõ ràng và lưu đọc lại đúng; Gõ trực tiếp rồi Lưu nháp không cần bấm Thêm trước |
| Sprint 1 - Thanh Tan | 21 | PASS (ĐC) | RangHamMat.Wpf/Views/KhamLamSang.xaml.cs; RangHamMat.Wpf/Controls/BieuMauHoSo.Pending.cs; RangHamMat.Wpf/MainWindow.xaml.cs |
| Sprint 1 - Thanh Tan | 22 | PASS | cases.log — S1-R22; DAL/DAL_KHAMDIEUTRI.Kham.cs — HoanTatKham |
| Sprint 1 - Thanh Tan | 23 | PASS | workflow.log — Hoàn tất khám với tình trạng đơn giản, không kê thuốc |
| Sprint 1 - Thanh Tan | 24 | PASS | cases.log — S1-R24 |
| Sprint 1 - Thanh Tan | 25 | PASS | xray.log — XR01 PNG và JPEG lưu nguyên byte, metadata đúng MaLS; XR03 bác sĩ không được gắn ảnh vào MaLS bệnh nhân khác; XR01 lịch sử chỉ xem |
| Sprint 1 - Thanh Tan | 26 | PASS | workflow.log — Lịch sử hiển thị khóa nhập và từ chối lưu; Lịch sử chỉ xem, không có thêm/sửa hoặc lượt chưa hoàn tất |
| Sprint 1 - Thanh Tan | 31 | PASS | cases.log — S1-R31-R32 |
| Sprint 1 - Thanh Tan | 32 | PASS | cases.log — S1-R31-R32 |
| Sprint 1 - Thanh Tan | 33 | PASS | cases.log — S1-R33 |
| Sprint 1 - Thanh Tan | 34 | PASS | cases.log — S1-R34 |
| Sprint 1 - Thanh Tan | 35 | PASS | cases.log — S1-R35 |
| Sprint 1 - Thanh Tan | 36 | PASS | cases.log — S1-R36; DAL/DAL_DONTHUOC.LamSang.cs — CanPrescribe: chặn thay sau xử lý tài chính/xuất thuốc |
| Sprint 1 - Thanh Tan | 37 | PASS | cases.log — S1-R37; DAL/DAL_KHAMDIEUTRI.Kham.cs — LuuKham, HoanTatKham |
| Sprint 1 - Thanh Tan | 38 | PASS (ĐC) | RangHamMat.Wpf/Views/DonThuoc.xaml.cs; RangHamMat.Wpf/Views/XemTruocDonThuoc.xaml.cs; tools/DocumentChecks.cs.txt |
| Sprint 2 - Thanh Tan | 5 | PASS | maintenance-final.log — BUS draft is editable; BUS lifecycle synchronizes only selected serial; DAL/DAL_BAOTRITHIETBI.cs |
| Sprint 2 - Thanh Tan | 6 | PASS | obj/TestCaseThanhTan/Revision/results.log — Sprint2-R6; DAL/DAL_BAOTRITHIETBI.cs |
| Sprint 2 - Thanh Tan | 7 | PASS | maintenance-final.log — BUS/SQL freezes content in Đã hoàn thành; BUS rejects transition |
| Sprint 2 - Thanh Tan | 8 | PASS | maintenance-final.log — BUS cannot delete history; BUS cannot cancel Đang thực hiện; SQL cannot delete even draft history |
| Sprint 2 - Thanh Tan | 9 | PASS (ĐC) | RangHamMat.Wpf/Views/BaoTri.xaml.cs — NapDanhSach; maintenance-final.log — BUS cancellation only preserves draft history and damaged serial |
| Sprint 2 - Thanh Tan | 10 | PASS | maintenance-final.log — BUS rejects stale draft update snapshot; BUS rejects stale lifecycle snapshot |
| Sprint 2 - Thanh Tan | 15 | PASS | cases.log — S2-R15-R16-R19; DAL/DAL_NHACUNGCAP.cs; RangHamMat.Wpf/Views/NhaCungCap.xaml.cs |
| Sprint 2 - Thanh Tan | 16 | PASS | cases.log — S2-R15-R16-R19, S2-R17; DAL/DAL_NHACUNGCAP.cs — suaNhaCungCap; DAL/DAL_HOADONNHAP.cs |
| Sprint 2 - Thanh Tan | 17 | PASS | cases.log — S2-R17; DAL/DAL_NHACUNGCAP.cs — xoaNhaCungCap |
| Sprint 2 - Thanh Tan | 18 | PASS (ĐC) | RangHamMat.Wpf/Views/NhaCungCap.xaml.cs — Xoa_Click |
| Sprint 2 - Thanh Tan | 19 | PASS | cases.log — S2-R15-R16-R19 |
| Sprint 2 - Thanh Tan | 20 | PASS | cases.log — S2-R20 |
| Sprint 2 - Thanh Tan | 25 | PASS | workflow.log — Hoàn tất khám lập hóa đơn ngay; Màn thu tiền hiển thị đúng hóa đơn đã lập và giá đã chốt; DAL/DAL_HOADON.cs — TaoTuKhamHoanTat |
| Sprint 2 - Thanh Tan | 26 | PASS | cases.log — S2-R26 |
| Sprint 2 - Thanh Tan | 27 | PASS | obj/TestCaseThanhTan/Revision/results.log — Sprint2-R27; DAL/DAL_HOADON.cs — TaoTuKhamHoanTat |
| Sprint 2 - Thanh Tan | 28 | PASS | workflow.log — Chốt kiểm đúng bản đã xem, ký nháp và lập một hóa đơn; Hóa đơn gồm thuốc đã ký trước thu, dùng lại khi thanh toán và retry |
| Sprint 2 - Thanh Tan | 29 | PASS | obj/TestCaseThanhTan/Revision/results.log — Sprint2-R29; DAL/DAL_HOADON.cs — KiemTraHoaDonLegacy, KiemTraLuotLegacy; RangHamMat.Wpf/Views/HoaDon.xaml.cs — ShowInvoice |
| Sprint 2 - Thanh Tan | 30 | PASS | obj/TestCaseThanhTan/Revision/results.log — Sprint2-R30; DAL/DAL_THANHTOANTRONGOI.ThanhToan.cs |
| Sprint 2 - Thanh Tan | 31 | PASS (ĐC) | DAL/DAL_THANHTOAN.cs — RecordPaymentCore; DAL/DAL_THANHTOANTRONGOI.ThanhToan.cs — RecordCoordinatedPayment |
| Sprint 2 - Thanh Tan | 32 | PASS | obj/TestCaseThanhTan/Revision/results.log — Sprint2-R32; DAL/DAL_THANHTOANTRONGOI.InHoaDon.cs; DAL/DAL_THANHTOAN.cs |
| Sprint 2 - Thanh Tan | 37 | PASS | reports-final.log — Bệnh lý: hai tháng bất kỳ cùng năm và khác năm; Bệnh lý: bộ lọc, bảng, biểu đồ và ảnh WPF |
| Sprint 2 - Thanh Tan | 38 | PASS | reports-final.log — TC-04 YoY và hai năm; TC-05 kỳ gốc 0 vẫn diễn giải tăng tuyệt đối |
| Sprint 2 - Thanh Tan | 39 | PASS | reports-final.log — TC-12/14 đối soát thu/hoàn và trạng thái ghi sổ Admin/User; DAL/DAL_BAOCAO.TongHop.cs — chuyên đề 09 |
| Sprint 2 - Thanh Tan | 40 | PASS | reports-final.log — TC-12/14 đối soát thu/hoàn và trạng thái ghi sổ; DAL/DAL_BAOCAO.cs — chuyên đề 13 |
| Sprint 2 - Thanh Tan | 41 | PASS | reports-final.log — TC-15/16 phản hồi cũ, mất kết nối và khóa xuất; Tổng quan điều hành: bố cục, biểu đồ và đổi bộ lọc |
| Sprint 2 - Thanh Tan | 42 | PASS | reports-final.log — BC-04 bộ lọc bệnh lý không phát sinh; Tổng quan điều hành: tổng toàn kỳ, thu âm, CSV và trạng thái rỗng |
| Sprint 2 - Thanh Tan | 43 | PASS | reports-final.log — BC-09/10 chuỗi 12 tháng, CSV số âm và metadata; Bệnh lý: đồng hạng, CSV đủ hai kỳ và hơn 10 nhóm; RangHamMat.Wpf/Views/BaoCao.xaml.cs — Xuat_Click |
| Sprint 2 - Thanh Tan | 44 | PASS (ĐC) | RangHamMat.Wpf/MainWindow.PhanQuyen.cs — CoQuyenMoPhanHe; DAL/DAL_BAOCAO.TongHop.cs — XacThucQuyen, KiemTraXuat; ET/ET_THONGKE.cs — ChinhSachBaoCao |
| Sprint 3 - Thanh Tan | 5 | PASS | cases.log — S3-R5-R6-R7-R8-R11-R12; workflow.log — Dịch vụ chọn sinh kế hoạch đầy đủ mã/giá |
| Sprint 3 - Thanh Tan | 6 | PASS | cases.log — S3-R5-R6-R7-R8-R11-R12; workflow.log — Đổi giá sau khi hiển thị được phát hiện; Cập nhật giá giữ nguyên bản nháp |
| Sprint 3 - Thanh Tan | 7 | PASS | cases.log — S3-R5-R6-R7-R8-R11-R12; workflow.log — Ngừng dịch vụ không còn được chọn; DAL/DAL_DANHMUC.cs |
| Sprint 3 - Thanh Tan | 8 | PASS | cases.log — S3-R5-R6-R7-R8-R11-R12 |
| Sprint 3 - Thanh Tan | 9 | PASS | cases.log — S3-R9-R10 |
| Sprint 3 - Thanh Tan | 10 | PASS | cases.log — S3-R9-R10 |
| Sprint 3 - Thanh Tan | 11 | PASS | cases.log — S3-R5-R6-R7-R8-R11-R12; DAL/DAL_DANHMUC.cs — kiểm trùng tên theo phạm vi cha |
| Sprint 3 - Thanh Tan | 12 | PASS | cases.log — S3-R5-R6-R7-R8-R11-R12 |
| Sprint 3 - Thanh Tan | 13 | PASS | cases.log — S3-R13-R25 |
| Sprint 3 - Thanh Tan | 18 | PASS | cases.log — S3-R18-R20 |
| Sprint 3 - Thanh Tan | 19 | PASS | cases.log — S3-R19; DAL/DAL_NHATKY.cs — GhiDangNhapThatBai |
| Sprint 3 - Thanh Tan | 20 | PASS | cases.log — S3-R18-R20; DAL/DAL_NHATKY.cs — LayTrang |
| Sprint 3 - Thanh Tan | 21 | PASS | cases.log — S3-R21; RangHamMat.Wpf/Views/NhatKy.xaml.cs — ToUtc |
| Sprint 3 - Thanh Tan | 22 | PASS (ĐC) | RangHamMat.Wpf/Views/NhatKy.xaml; RangHamMat.Wpf/Presentation/NhatKy.ChiTiet.cs |
| Sprint 3 - Thanh Tan | 23 | PASS | cases.log — S3-R23 |
| Sprint 3 - Thanh Tan | 24 | PASS | cases.log — S3-R24; DAL/DAL_NHATKY.cs — SafeDetail |
| Sprint 3 - Thanh Tan | 25 | PASS | cases.log — S3-R13-R25; DAL/DAL_NHATKY.cs — Admin |

Bản workbook gốc trước lần điền đầu: obj/TestCaseThanhTan/TestCase_Nhom4.original.xlsx. Bản trước khi sửa 5 testcase: obj/TestCaseThanhTan/Revision/TestCase_Nhom4.before_revision.xlsx. Đã kiểm phần ZIP của lần sửa này: chỉ sheet5.xml thay đổi; hoàn nguyên đúng 30 ô B–G thu được nguyên văn XML trước sửa, xác nhận các ô còn lại và định dạng không đổi.
Workbook SHA-256 sau cập nhật: EAB2A1934E545F742452F855205AC69B7E063E94C60360A595B8D1A9F472A15C.
