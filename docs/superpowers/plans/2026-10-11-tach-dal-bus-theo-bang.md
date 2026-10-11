# Kế hoạch ET/DAL/BUS từng bảng

Spec: ../specs/2026-10-11-tach-dal-bus-theo-bang-design.md

- [x] ET: RED schema/constructor; viết 24 ET kiểu fields/constructor/property; PASS với SQL hiện tại (201 cột).
- [x] DAL/BUS lâm sàng và danh mục: tạo DAL_GIAODICH, CRUD typed SQL từng bảng, BUS từng bảng; BUS_LAMSANG điều phối; thay mã int/identity thành string cấp mã trong transaction.
- [x] Tài chính: tách SQL invoice/detail/payment/export/detail vào đúng DAL và BUS; string PK/FK, mã tự cấp, bảo toàn locks/validation; 54 kiểm tra integration rollback đạt.
- [x] View: đổi tên/lớp/tham chiếu sang tiếng Việt không dấu; cập nhật form/mã/string ID và schema UI khác biệt.
- [x] Kiểm tra/Review: schema, 12 persistence, 54 finance, WPF, source checks và build; review toàn phạm vi và sửa lỗi; cập nhật README/hướng dẫn/báo cáo.

Bằng chứng và giới hạn kiểm chứng: ../../../CAP_NHAT_DAL_BUS_ET.md và ../../../THUC_HIEN_KE_HOACH.md.

Ruling: thực hiện trực tiếp workspace có toàn bộ source chưa commit để giữ thay đổi. Yêu cầu hiện tại đã cấp quyền sửa/kiểm tra. Agent ET và tài chính độc lập; root xử lý hạ tầng/CRUD/điều hướng, không sửa file agent đang sở hữu. Không thay schema SQL hoặc dữ liệu thực tế.
