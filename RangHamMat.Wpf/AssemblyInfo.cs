// PHẦN AI ĐÃ THỰC HIỆN: AI đã giải thích ThemeInfo và thứ tự WPF tìm resource: tra ở trang/ứng dụng rồi tìm dictionary theme trong assembly hiện tại.
// AI hỗ trợ: bổ sung chú thích tiếng Việt cho file này ngày 13/10/2026.
// Nhãn này ghi nhận phần hỗ trợ/chú thích;
using System.Windows;

[assembly: ThemeInfo(
    ResourceDictionaryLocation.None,            // Không có dictionary riêng cho từng theme hệ điều hành.
                                                // WPF dùng quy tắc tìm resource của trang/ứng dụng trước.
    ResourceDictionaryLocation.SourceAssembly   // Dictionary theme chung nằm trong assembly hiện tại,
                                                // dùng khi resource không có ở trang hoặc ứng dụng.
)]
