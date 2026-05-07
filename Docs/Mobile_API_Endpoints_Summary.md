# Mobile API Endpoints Summary

Tài liệu này liệt kê các endpoint mà Mobile đang gọi trực tiếp tới API production:

https://locateandmultilingualnarration-amgrfua6fbd7gnce.eastasia-01.azurewebsites.net

## Tổng quan

Mobile là app anonymous, nên các endpoint dưới đây được gọi không cần đăng nhập. Một số endpoint chỉ dùng trong luồng nền như sync, reset cờ hoặc báo offline.

## Danh sách endpoint

| Method | REST URL | Mục đích |
| --- | --- | --- |
| POST | https://locateandmultilingualnarration-amgrfua6fbd7gnce.eastasia-01.azurewebsites.net/api/qrcodes/verify | Kiểm tra mã QR, xác thực quyền vào app và trả về thời điểm hết hạn truy cập. |
| GET | https://locateandmultilingualnarration-amgrfua6fbd7gnce.eastasia-01.azurewebsites.net/api/device-preference/{deviceId} | Lấy cấu hình thiết bị đã lưu, gồm ngôn ngữ, giọng đọc, tốc độ đọc và trạng thái auto-play. |
| POST | https://locateandmultilingualnarration-amgrfua6fbd7gnce.eastasia-01.azurewebsites.net/api/device-preference | Tạo mới hoặc cập nhật cấu hình thiết bị sau khi khách chọn ngôn ngữ và giọng đọc. |
| GET | https://locateandmultilingualnarration-amgrfua6fbd7gnce.eastasia-01.azurewebsites.net/api/device-preference/reset-flag?deviceId={deviceId} | Kiểm tra cờ reset từ admin; nếu có thì API tự xóa cờ sau khi trả kết quả. |
| POST | https://locateandmultilingualnarration-amgrfua6fbd7gnce.eastasia-01.azurewebsites.net/api/device-preference/{deviceId}/offline | Báo thiết bị offline để admin dashboard loại thiết bị khỏi danh sách đang hoạt động ngay. |
| GET | https://locateandmultilingualnarration-amgrfua6fbd7gnce.eastasia-01.azurewebsites.net/api/languages/active | Lấy danh sách ngôn ngữ đang active để khách chọn ở màn hình Language. |
| GET | https://locateandmultilingualnarration-amgrfua6fbd7gnce.eastasia-01.azurewebsites.net/api/tts-voice-profiles/active?languageId={languageId} | Lấy danh sách giọng đọc đang active theo ngôn ngữ đã chọn. |
| GET | https://locateandmultilingualnarration-amgrfua6fbd7gnce.eastasia-01.azurewebsites.net/api/geo/stalls?deviceId={deviceId} | Lấy danh sách gian hàng để hiển thị bản đồ và đồng bộ xuống SQLite cục bộ. |
| POST | https://locateandmultilingualnarration-amgrfua6fbd7gnce.eastasia-01.azurewebsites.net/api/device-location-log/batch | Gửi batch tọa độ GPS từ Mobile lên server để lưu lịch sử di chuyển và cập nhật LastSeenAt. |
| GET | https://locateandmultilingualnarration-amgrfua6fbd7gnce.eastasia-01.azurewebsites.net/api/tours?page=1&pageSize=100 | Lấy danh sách tour đang active để hiển thị ở trang Tour List. |
| GET | https://locateandmultilingualnarration-amgrfua6fbd7gnce.eastasia-01.azurewebsites.net/api/tours/{tourId} | Lấy chi tiết một tour, gồm danh sách stop và thông tin stall trong tour. |

## Ghi chú sử dụng

- `GET /api/device-preference/{deviceId}` được dùng khi local preference chưa có dữ liệu.
- `POST /api/device-preference` được dùng sau khi khách chọn ngôn ngữ và giọng đọc.
- `GET /api/geo/stalls?deviceId={deviceId}` là endpoint quan trọng nhất cho Map và sync cache-first.
- `POST /api/device-location-log/batch` chỉ gửi khi có đủ điểm GPS trong buffer.
- `GET /api/tours` và `GET /api/tours/{tourId}` chỉ dùng cho flow Tour trên Mobile.
