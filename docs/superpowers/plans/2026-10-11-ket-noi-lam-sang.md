# Kết nối lâm sàng Implementation Plan

> **For agentic workers:** Use superpowers:executing-plans; independent ET/UI/finance work follows superpowers:dispatching-parallel-agents.

**Goal:** Thực hiện tất cả yêu cầu KeHoach.txt bằng dữ liệu SQL thật.
**Architecture:** Giữ form/điều hướng, thay cache-only writes bằng BUS/DAL transaction. Finance dùng nguồn lâm sàng và trigger v8.
**Tech Stack:** .NET 8, WPF, Dapper, Microsoft.Data.SqlClient, SQL Server.
**Spec:** ../specs/2026-10-11-ket-noi-lam-sang-design.md

## Global Constraints
Giữ các thay đổi có sẵn; không reset hoặc chạy script khởi tạo DB; không thêm dữ liệu minh họa; không thêm dependency.

## Review Focus
Lỗi giữa các dòng phải rollback; xác nhận ngày/trạng thái đồng thời; khóa nguồn nhiều cửa sổ; cấp thiếu thuốc không ghi sổ; thu trùng/vượt dư phải bị chặn.

## Tasks
- [x] ET: đối chiếu sys.columns và SQL v8; sửa ET liên quan, thêm hóa đơn/thanh toán/lịch khám; kiểm tra kiểu/null/computed.
- [x] Persistence: DAL_LAMSANG.Load/Save/Delete/Transaction và BUS_LAMSANG; ClinicalData nạp/lưu database, transaction cache rollback; sửa các commit nhiều dòng và xác nhận.
- [x] Finance: DAL/BUS_HOADON tạo/xác nhận/hủy hóa đơn, cấp đủ thuốc, thu/hoàn; form nguồn/chi tiết/thanh toán/in; kiểm tra SQL rollback.
- [x] UI: bỏ demo, chỉnh bệnh nhân MaBHYT và validation; Dịch vụ thực hiện liên kết patient/visit/prescription/finance.
- [x] Verification: build Release, checks, smoke, tích hợp DB; sửa lỗi; cập nhật hướng dẫn và kết quả thật.

Verification 11/10/2026: solution Release build 0 errors; XAML checks PASS; ET 18 tables/160 columns/0 failures; persistence 9 PASS; finance 29 PASS; real WPF/SQL smoke PASS. See THUC_HIEN_KE_HOACH.md for scope, commands, screenshots and printing limits.

Ruling: yêu cầu người dùng thực hiện tất cả đã cấp quyền sửa và kiểm tra; thực hiện trực tiếp trong workspace vì toàn bộ bản giao diện hiện tại là thay đổi chưa commit. Không chuyển sang worktree mất các thay đổi này. Chỉ kiểm tra nghiệp vụ bằng transaction rollback, không thay dữ liệu bệnh nhân hiện có.
