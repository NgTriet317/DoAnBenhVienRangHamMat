# WPF comment audit

Baseline: obj/DataLinkAudit_20261114/comments-baseline/RangHamMat.Wpf (captured for this task before edits).
After snapshot: obj/DataLinkAudit_20261114/comments-wpf/after/RangHamMat.Wpf.

PASS: all 127 C# token streams and all 88 XAML trees match the task baseline; project configuration unchanged. All original comments retained.
Changed: 49 files; added 253 C# documentation blocks and 65 XAML comments (1032 lines).
Missing declarations: 253 -> 0 across 2672 authored declarations. Files with no comments: 0.
PowerShell: no .ps1 files in the WPF scope; none edited, so PowerShell token comparison is not applicable.
No commits. Source edits are limited to RangHamMat.Wpf; audit artifacts live under the user-approved obj folder.

| Changed file | Added comment blocks | Added lines |
| --- | ---: | ---: |
| RangHamMat.Wpf/App.xaml.cs | 5 | 17 |
| RangHamMat.Wpf/Controls/BaoCaoDuLieu.cs | 1 | 6 |
| RangHamMat.Wpf/Controls/BaoCaoSoSanh.cs | 11 | 36 |
| RangHamMat.Wpf/Controls/ChartView.xaml.cs | 4 | 12 |
| RangHamMat.Wpf/Controls/ClinicalWorkflow/ClinicalFieldsPanel.cs | 4 | 16 |
| RangHamMat.Wpf/Controls/DongThanhLy.cs | 11 | 33 |
| RangHamMat.Wpf/Controls/DuLieuSoSanhBaoCao.cs | 4 | 36 |
| RangHamMat.Wpf/Controls/PrescriptionPageView.cs | 3 | 11 |
| RangHamMat.Wpf/DichVuDangNhap.cs | 7 | 30 |
| RangHamMat.Wpf/KhoPhienDangNhap.cs | 9 | 39 |
| RangHamMat.Wpf/MainWindow.xaml.cs | 2 | 7 |
| RangHamMat.Wpf/PhienDangNhap.cs | 3 | 10 |
| RangHamMat.Wpf/PhienNhatKy.cs | 6 | 18 |
| RangHamMat.Wpf/Presentation/BaoTri.ThanhLy.cs | 8 | 24 |
| RangHamMat.Wpf/Presentation/DonThuocDocument.cs | 22 | 106 |
| RangHamMat.Wpf/Presentation/DonThuocPaginator.cs | 13 | 47 |
| RangHamMat.Wpf/Presentation/DonThuocTrinhBay.cs | 19 | 85 |
| RangHamMat.Wpf/Presentation/InvoicePrintLines.cs | 1 | 3 |
| RangHamMat.Wpf/Presentation/NhatKy.ChiTiet.cs | 9 | 41 |
| RangHamMat.Wpf/Themes/DocumentPreviewStyles.xaml | 4 | 4 |
| RangHamMat.Wpf/Themes/PrescriptionStyles.xaml | 8 | 8 |
| RangHamMat.Wpf/Views/AnhXquangDuocGiao.xaml | 3 | 3 |
| RangHamMat.Wpf/Views/AnhXquangDuocGiao.xaml.cs | 8 | 27 |
| RangHamMat.Wpf/Views/AnhXquangLanKham.xaml | 7 | 7 |
| RangHamMat.Wpf/Views/AnhXquangLanKham.xaml.cs | 24 | 92 |
| RangHamMat.Wpf/Views/BaoTri.xaml | 1 | 1 |
| RangHamMat.Wpf/Views/ChiTietKhamDieuTri.xaml | 1 | 1 |
| RangHamMat.Wpf/Views/ChiTietKhamDieuTri.xaml.cs | 1 | 3 |
| RangHamMat.Wpf/Views/CuaSoThanhLy.xaml | 4 | 4 |
| RangHamMat.Wpf/Views/CuaSoThanhLy.xaml.cs | 3 | 12 |
| RangHamMat.Wpf/Views/DangNhap.xaml.cs | 2 | 7 |
| RangHamMat.Wpf/Views/DichVu.xaml | 6 | 6 |
| RangHamMat.Wpf/Views/DichVu.xaml.cs | 26 | 89 |
| RangHamMat.Wpf/Views/InvoiceDocument.xaml.cs | 4 | 16 |
| RangHamMat.Wpf/Views/KhamLamSang.xaml | 1 | 1 |
| RangHamMat.Wpf/Views/KhamLamSang.xaml.cs | 2 | 6 |
| RangHamMat.Wpf/Views/NhatKy.xaml | 8 | 8 |
| RangHamMat.Wpf/Views/NhatKy.xaml.cs | 16 | 50 |
| RangHamMat.Wpf/Views/PrescriptionHistoryDetail.xaml | 6 | 6 |
| RangHamMat.Wpf/Views/PrescriptionPrintLine.xaml | 3 | 3 |
| RangHamMat.Wpf/Views/PrescriptionPrintLine.xaml.cs | 3 | 10 |
| RangHamMat.Wpf/Views/ThucHienDichVuEditor.xaml | 1 | 1 |
| RangHamMat.Wpf/Views/ThucHienDichVuEditor.xaml.cs | 2 | 6 |
| RangHamMat.Wpf/Views/TiepNhanKham.xaml.cs | 9 | 29 |
| RangHamMat.Wpf/Views/XemAnhXquang.xaml | 3 | 3 |
| RangHamMat.Wpf/Views/XemAnhXquang.xaml.cs | 2 | 7 |
| RangHamMat.Wpf/Views/XemTruocDonThuoc.xaml | 5 | 5 |
| RangHamMat.Wpf/Views/XemTruocDonThuoc.xaml.cs | 9 | 36 |
| RangHamMat.Wpf/Views/XemTruocHoaDon.xaml | 4 | 4 |

## Preexisting user changes

HospitalStyles.xaml, DonThuoc.xaml, DonThuoc.xaml.cs and PrescriptionHistoryDetail.xaml.cs are byte-for-byte unchanged from the task baseline. PrescriptionHistoryDetail.xaml only received comments; its XAML tree is identical.

## Read-only inventory outside WPF

Snapshot time (UTC): 2026-11-14T05:38:47.4532099Z
Files containing undocumented C# declarations: 38. See missing-outside-wpf.json for member names and lines. Other agents may be updating those files concurrently.
- BUS/BUS_ANHXQUANG.cs: 1
- BUS/BUS_DANHMUC.cs: 1
- BUS/BUS_NHATKY.cs: 2
- BUS/CauHinhKhoAnh.cs: 4
- BUS/KhoLuuAnhXquang.cs: 3
- DAL/DAL_ANHXQUANG.cs: 9
- DAL/DAL_BAOCAO.BenhLy.cs: 2
- DAL/DAL_DANHMUC.cs: 3
- DAL/DAL_DOISOATDULIEU.cs: 9
- DAL/DAL_THANHTOANTRONGOI.cs: 2
- DAL/dbconnect.cs: 1
- ET/ET_ANHXQUANGLANKHAM.cs: 14
- ET/ET_CHUCVU.cs: 1
- ET/ET_DOISOATDULIEU.cs: 3
- ET/ET_DULIEUANHXQUANG.cs: 1
- ET/ET_DULIEUDANHMUCVANHATKY.cs: 11
- ET/ET_DULIEUQUYTRINHKHAM.cs: 2
- ET/ET_PHANCONGANHXQUANG.cs: 7
- ET/ET_THIETBI.cs: 6
- ET/ET_THONGKE.cs: 39
- ET/ET_TONGKETKHAM.cs: 1
- ET/HanhDongNhatKy.cs: 1
- tools/AcademicData.cs.txt: 20
- tools/DataConventions.cs.txt: 8
- tools/DocumentChecks.cs.txt: 15
- tools/LoginSessionChecks.cs.txt: 5
- tools/LoginSessionSqlChecks.cs.txt: 14
- tools/MaintenanceSerialChecks.cs.txt: 1
- tools/P5Checks.cs.txt: 15
- tools/P5DemoData.cs.txt: 8
- tools/ReportChecks.cs.txt: 11
- tools/ServiceReceptionChecks.cs.txt: 8
- tools/Sprint3Acceptance.cs.txt: 10
- tools/Sprint3FoundationChecks.cs.txt: 14
- tools/Sprint3Integration.cs.txt: 7
- tools/ThanhLyUiChecks.cs.txt: 8
- tools/XrayChecks.cs.txt: 13
- tools/XrayStorageOperations.cs.txt: 14
