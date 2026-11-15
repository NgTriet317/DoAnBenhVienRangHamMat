# SQL hợp nhất Sprint3 và AI

Installer duy nhất là [`Database/QLBenhVienRangHamMat.sql`](../Database/QLBenhVienRangHamMat.sql). Chạy toàn bộ bằng SSMS hoặc `sqlcmd -b -f65001`. File hỗ trợ cài mới và chạy lại trên database đã có dữ liệu; không nạp đè snapshot khi đã có bảng.

Các bản cài/vá AI cũ `07_08`, `07`, `08`, `10`, `12`, `14`, `15`, `17` dừng với thông báo chuyển sang installer chuẩn. Các định nghĩa cũ có thể ghi đè trigger phụ thu và quyền tiếp nhận của bản hợp nhất. Lịch sử của chúng vẫn có trong Git.

Các file `09`, `11`, `13` là truy vấn chẩn đoán. Không cần chạy các file SQL trong thư mục này sau installer chuẩn để sử dụng AI.

Kiểm tra cài đặt, chạy lại và hai luồng nhân viên duyệt trên database mới cách ly:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/KiemTraHopNhatAI.ps1 -Database RHM_Hospital_Test_MergeSql_MyRun
```

Chi tiết schema, phụ thu và tương thích lương: [`docs/database/HOP_NHAT_SPRINT3_AI.md`](../docs/database/HOP_NHAT_SPRINT3_AI.md).
