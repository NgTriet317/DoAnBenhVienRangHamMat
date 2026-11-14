# Academic Data Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Chuẩn hóa dữ liệu đồ án cũ và bổ sung bộ dữ liệu sử dụng được xuyên suốt các chức năng.

**Architecture:** Công cụ C# gọi SQL chuẩn hóa có phạm vi và BUS/DAL hiện có trong giao dịch Serializable. Marker chống chạy trùng. File SQL chính đóng gói snapshot cuối; công cụ kiểm chứng và bằng chứng giữ trong tools/docs.

**Tech Stack:** PowerShell, .NET 8, Dapper, Microsoft.Data.SqlClient, SQL Server.

**Spec:** docs/superpowers/specs/2026-11-12-academic-data-design.md

## Global Constraints

- Giữ schema 64 bảng, bốn project, mã/tài khoản cũ và các chỉnh sửa ứng dụng đang có.
- Database hoàn toàn cho đồ án; người dùng cho phép tự quyết định chuẩn hóa và dữ liệu mô phỏng.
- Không commit/push/phát hành; không thay dữ liệu database khác ngoài fixture riêng.
- Không làm yếu guard sản phẩm; mọi trigger phải hoạt động và mọi FK/CHECK trusted khi commit.

## Review Focus

- Lặp công cụ sau khi người dùng thu/giao: không tái tạo hoặc hồi trạng thái.
- Chuẩn hóa legacy: không xuất thêm số lượng thuốc đã có nguồn; lưu bằng chứng thu/hoàn cũ.
- Hoá đơn chờ thu dùng tiếp được API trọn gói, giữ phụ thu và giá chốt.
- Tồn theo lô và lịch sử tồn không âm; các lô còn hạn phải tồn đúng kho cấp.
- Cài mới/chạy lại file SQL giữ dữ liệu, identity, trạng thái bàn giao và SHA256.

### Task 1: Công cụ và kiểm chứng ban đầu

- [x] Tạo tools/ChuanHoaDuLieuDoAn.ps1 và tools/AcademicData.cs.txt với các chế độ --check/--apply, không thêm project vào solution.
- [x] Check phát hiện trạng thái cũ/thiếu bản ký/đường dùng, chạy thất bại trên fixture chưa sửa.
- [x] Cài fixture tên riêng từ SQL hiện tại; công cụ tự sao lưu/VERIFYONLY trước áp dụng chính.

### Task 2: Chuẩn hóa legacy và nguồn dữ liệu mới

- [x] Tạo tools/AcademicData.Legacy.sql: mã tình trạng, đơn, nguồn phí, thu đủ v2, bàn giao và trạng thái phiếu; khôi phục trigger và kiểm ràng buộc.
- [x] Tạo tools/AcademicData.Catalog.sql: hàng hóa/thuốc/vật tư/thiết bị, phòng/chuyên khoa, nguồn nhập/lô/tồn và tài khoản/lịch; tên có nguồn, giá/lô/serial mô phỏng.
- [x] Tạo 36 người bệnh qua BUS/DAL với các trạng thái tiếp diễn được, có ca chờ ảnh phân công.
- [x] Check qua API thực, nguồn/tiền/tồn/bản ký; thử lỗi rollback và chạy lại giữ nguyên SHA256 cả 64 bảng.

### Task 3: Áp dụng và đóng gói

- [x] Rà soát độc lập các script/công cụ và sửa lỗi quan trọng trước áp dụng.
- [x] Áp dụng cùng công cụ vào QLBenhVienRangHamMat, kiểm lại và xuất bằng chứng trước/sau; backup mới đã VERIFYONLY.
- [x] Cập nhật snapshot SQL 64 bảng/3.221 dòng; cài mới/chạy lặp khớp SHA256; thao tác thu/giao trên bản thử rồi chạy lại vẫn giữ tiến độ.
- [x] Cập nhật README và báo cáo dữ liệu, tài khoản/các mã dùng thử; dọn đúng năm fixture riêng, kiểm lại API và SHA256 database chính khớp snapshot 64 bảng/3.221 dòng.
