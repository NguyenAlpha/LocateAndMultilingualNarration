# Mobile API Endpoints Summary

Tài liệu này liệt kê các endpoint mà Mobile đang gọi trực tiếp tới API production:

https://locateandmultilingualnarration-amgrfua6fbd7gnce.eastasia-01.azurewebsites.net

## Tổng quan

Mobile là app anonymous, nên các endpoint dưới đây được gọi không cần đăng nhập. Một số endpoint chỉ dùng trong luồng nền như sync, reset cờ hoặc báo offline.

## Danh sách endpoint

### 1) POST `/api/qrcodes/verify`

Mục đích: Kiểm tra mã QR, xác thực quyền vào app và trả về thời điểm hết hạn truy cập.

REST:

https://locateandmultilingualnarration-amgrfua6fbd7gnce.eastasia-01.azurewebsites.net/api/qrcodes/verify

Mẫu JSON trả về:

```json
{
	"success": true,
	"data": {
		"isValid": true,
		"message": "OK",
		"expiryAt": "2026-05-07T23:59:59+07:00"
	},
	"error": null
}
```

### 2) GET `/api/device-preference/{deviceId}`

Mục đích: Lấy cấu hình thiết bị đã lưu, gồm ngôn ngữ, giọng đọc, tốc độ đọc và trạng thái auto-play.

REST:

https://locateandmultilingualnarration-amgrfua6fbd7gnce.eastasia-01.azurewebsites.net/api/device-preference/{deviceId}

Mẫu JSON trả về:

```json
{
	"success": true,
	"data": {
		"id": "0f4c1f6d-0d3e-4b1c-ae1b-7b8b51f0c5d1",
		"deviceId": "7c8d0e4f-8d7c-4f5b-9a4d-8f9e72a1d1c2",
		"languageId": "1f6f4d50-8b8f-4c0a-a8f0-8f1de7b5f2a1",
		"voiceId": "2a63f2c9-0d6f-4c4d-8fef-5dc5d4fb1d11",
		"voiceDisplayName": "Vietnamese Female",
		"speechRate": 1.0,
		"autoPlay": true,
		"platform": "Android",
		"deviceModel": "Pixel 7",
		"manufacturer": "Google",
		"osVersion": "14",
		"firstSeenAt": "2026-05-07T08:00:00+07:00",
		"lastSeenAt": "2026-05-07T08:10:00+07:00",
		"languageName": "Tiếng Việt",
		"languageDisplayName": "Vietnamese",
		"languageCode": "vi",
		"languageFlagCode": "vn"
	},
	"error": null
}
```

### 3) POST `/api/device-preference`

Mục đích: Tạo mới hoặc cập nhật cấu hình thiết bị sau khi khách chọn ngôn ngữ và giọng đọc.

REST:

https://locateandmultilingualnarration-amgrfua6fbd7gnce.eastasia-01.azurewebsites.net/api/device-preference

Mẫu JSON trả về:

```json
{
	"success": true,
	"data": {
		"id": "0f4c1f6d-0d3e-4b1c-ae1b-7b8b51f0c5d1",
		"deviceId": "7c8d0e4f-8d7c-4f5b-9a4d-8f9e72a1d1c2",
		"languageId": "1f6f4d50-8b8f-4c0a-a8f0-8f1de7b5f2a1",
		"voiceId": "2a63f2c9-0d6f-4c4d-8fef-5dc5d4fb1d11",
		"voiceDisplayName": "Vietnamese Female",
		"speechRate": 1.0,
		"autoPlay": true,
		"platform": "Android",
		"deviceModel": "Pixel 7",
		"manufacturer": "Google",
		"osVersion": "14",
		"firstSeenAt": "2026-05-07T08:00:00+07:00",
		"lastSeenAt": "2026-05-07T08:12:00+07:00",
		"languageName": "Tiếng Việt",
		"languageDisplayName": "Vietnamese",
		"languageCode": "vi",
		"languageFlagCode": "vn"
	},
	"error": null
}
```

### 4) POST `/api/device-preference/{deviceId}/offline`

Mục đích: Báo thiết bị offline để admin dashboard loại thiết bị khỏi danh sách đang hoạt động ngay.

REST:

https://locateandmultilingualnarration-amgrfua6fbd7gnce.eastasia-01.azurewebsites.net/api/device-preference/{deviceId}/offline

Mẫu JSON trả về:

```json
{
	"success": true,
	"data": true,
	"error": null
}
```

### 5) GET `/api/languages/active`

Mục đích: Lấy danh sách ngôn ngữ đang active để khách chọn ở màn hình Language.

REST:

https://locateandmultilingualnarration-amgrfua6fbd7gnce.eastasia-01.azurewebsites.net/api/languages/active

Mẫu JSON trả về:

```json
{
	"success": true,
	"data": [
		{
			"id": "1f6f4d50-8b8f-4c0a-a8f0-8f1de7b5f2a1",
			"code": "vi",
			"name": "Vietnamese",
			"displayName": "Tiếng Việt",
			"flagCode": "vn",
			"isActive": true
		},
		{
			"id": "2b3f5d80-6c7d-4f10-9d0a-5a2a7d5c1b44",
			"code": "en",
			"name": "English",
			"displayName": "English",
			"flagCode": "us",
			"isActive": true
		}
	],
	"error": null
}
```

### 6) GET `/api/tts-voice-profiles/active?languageId={languageId}`

Mục đích: Lấy danh sách giọng đọc đang active theo ngôn ngữ đã chọn.

REST:

https://locateandmultilingualnarration-amgrfua6fbd7gnce.eastasia-01.azurewebsites.net/api/tts-voice-profiles/active?languageId={languageId}

Mẫu JSON trả về:

```json
{
	"success": true,
	"data": [
		{
			"id": "2a63f2c9-0d6f-4c4d-8fef-5dc5d4fb1d11",
			"languageId": "1f6f4d50-8b8f-4c0a-a8f0-8f1de7b5f2a1",
			"displayName": "Vietnamese Female",
			"description": "Giọng nữ tiếng Việt",
			"style": "general",
			"role": "Female",
			"isDefault": true,
			"priority": 1
		}
	],
	"error": null
}
```

### 7) GET `/api/geo/stalls?deviceId={deviceId}`

Mục đích: Lấy danh sách gian hàng để hiển thị bản đồ và đồng bộ xuống SQLite cục bộ.

REST:

https://locateandmultilingualnarration-amgrfua6fbd7gnce.eastasia-01.azurewebsites.net/api/geo/stalls?deviceId={deviceId}

Mẫu JSON trả về:

```json
{
	"success": true,
	"data": [
		{
			"stallId": "7d9f1b10-9e54-4c2f-9f51-5c4fd1d3f7a8",
			"stallName": "Phở Bò Gia Truyền",
			"latitude": 10.7769,
			"longitude": 106.7009,
			"radiusMeters": 20,
			"narrationContent": {
				"id": "3f5ab7b4-1f2c-4e92-8b3f-54a7a5dce6c1",
				"languageId": "1f6f4d50-8b8f-4c0a-a8f0-8f1de7b5f2a1",
				"title": "Giới thiệu Phở Bò",
				"description": "Món phở bò truyền thống",
				"scriptText": "...",
				"updatedAt": "2026-05-07T08:05:00+07:00",
				"audioUrl": "https://storage.example.com/audio/vi/7d9f1b10-9e54-4c2f-9f51-5c4fd1d3f7a8.mp3"
			},
			"mediaImages": [
				{
					"url": "https://storage.example.com/images/stall-1.jpg",
					"caption": "Món đặc trưng",
					"hasCaption": true
				}
			]
		}
	],
	"error": null
}
```

### 8) POST `/api/device-location-log/batch`

Mục đích: Gửi batch tọa độ GPS từ Mobile lên server để lưu lịch sử di chuyển và cập nhật LastSeenAt.

REST:

https://locateandmultilingualnarration-amgrfua6fbd7gnce.eastasia-01.azurewebsites.net/api/device-location-log/batch

Mẫu JSON trả về:

```json
{
	"success": true,
	"data": 5,
	"error": null
}
```

## Ghi chú sử dụng

- `GET /api/device-preference/{deviceId}` được dùng khi local preference chưa có dữ liệu.
- `POST /api/device-preference` được dùng sau khi khách chọn ngôn ngữ và giọng đọc.
- `GET /api/geo/stalls?deviceId={deviceId}` là endpoint quan trọng nhất cho Map và sync cache-first.
- `POST /api/device-location-log/batch` chỉ gửi khi có đủ điểm GPS trong buffer.
