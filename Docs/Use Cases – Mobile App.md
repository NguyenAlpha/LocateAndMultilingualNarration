# Use Cases – Mobile App

> **Actor chính:** Visitor (Anonymous) – Khách tham quan, không cần đăng nhập, định danh qua `DeviceId`.
> **Actor phụ:** SyncBackgroundService – tiến trình nền tự động poll/sync.
> **Điều kiện bắt buộc toàn module:** App hoạt động hoàn toàn anonymous, không có luồng login.

---

## Bảng tổng hợp Use Cases Mobile

| Mã UC | Tên Use Case | Actor | Mục tiêu nghiệp vụ |
|-------|-------------|-------|-------------------|
| UC-M01 | Khởi động & Routing (LoadingPage) | Visitor | Quyết định màn hình đích dựa trên device_id + QR + preference |
| UC-M02 | Quét QR kích hoạt quyền truy cập | Visitor | Dùng mã QR một lần để vào app với thời hạn cấu hình |
| UC-M03 | Chọn ngôn ngữ & giọng đọc | Visitor | Lưu DevicePreference theo DeviceId |
| UC-M04 | Xem MainPage + bottom nav | Visitor | Điều hướng chính 3 nút Bản đồ / Tour / Ngôn ngữ |
| UC-M05 | Xem bản đồ tương tác & pin gian hàng | Visitor | Quan sát stall theo không gian thực với geofence circles |
| UC-M06 | Tap pin → Popup → Phát audio thủ công | Visitor | Chủ động nghe narration cho stall cụ thể |
| UC-M07 | Geofence auto-play | Visitor | Tự phát audio khi khách đi vào vùng stall |
| UC-M08 | Cache-first & chế độ Offline | Hệ thống | Hiển thị dữ liệu + phát audio khi mất mạng |
| UC-M09 | Background Sync stall | Hệ thống | Cập nhật stall mỗi 3 phút + reconnect trigger |
| UC-M10 | Flush batch GPS | Hệ thống | Gửi điểm GPS định kỳ 20 giây + piggyback heartbeat |
| UC-M11 | Xem danh sách gian hàng (Stall List) | Visitor | Tìm kiếm + phân trang stall |
| UC-M12 | Xem & bắt đầu Tour | Visitor | Chọn tour có sẵn, chạy theo lộ trình |
| UC-M13 | Thực hiện Tour (tracking progress) | Visitor | Hoàn thành từng stop, lưu progress, resume sau kill app |
| UC-M14 | Pull cờ Reset Thiết bị | Hệ thống | Nhận lệnh reset từ Admin, clear Preferences về LoadingPage |
| UC-M15 | Notify Offline khi thoát app | Hệ thống | Đánh dấu thiết bị rời khỏi active-devices list ngay |
| UC-M16 | Đăng xuất (xóa QR access) | Visitor | Vô hiệu mã QR hiện tại, giữ ngôn ngữ/giọng |

---

## Đặc tả chi tiết

### UC-M01 – Khởi động & Routing (LoadingPage)

| Trường | Nội dung |
|--------|---------|
| **Actor** | Visitor |
| **Mô tả** | LoadingPage quyết định điều hướng dựa trên 3 điều kiện xếp chồng: có `device_id`, QR còn hạn, và đã có preference. |
| **Tiền điều kiện** | App đã cài, có quyền ghi `Preferences`. |
| **Hậu điều kiện** | Điều hướng tới `//ScanPage` hoặc `//MainPage` hoặc `LanguagePage` tuỳ trạng thái. |

**Luồng chính:**
1. Đọc trực tiếp `Preferences.Get("device_id", null)` — không qua `DeviceService`.
2. Nếu `device_id = null` → `//ScanPage`.
3. Gọi `QrService.IsAccessValid()` — so `qr_verified` + `qr_expiry` với `UtcNow`.
4. Nếu QR hết hạn hoặc không hợp lệ → `//ScanPage`.
5. Gọi `LocalPreferenceService.Load()`.
6. Nếu có local preference → `//MainPage` (**không gọi API**, tối ưu cold start).
7. Nếu chưa có local → `GET /api/device-preference/{deviceId}`.
8. API trả preference → save local → `//MainPage`.

**Luồng thay thế:**
- **7a.** API 404 → `GoToAsync(nameof(LanguagePage))` (relative route, không `//`).

**Ngoại lệ:**
- Lỗi mạng ở bước 7 → hiển thị lỗi nhẹ + ở lại LoadingPage, retry sau khi người dùng tương tác.

---

### UC-M02 – Quét QR kích hoạt quyền truy cập

| Trường | Nội dung |
|--------|---------|
| **Actor** | Visitor |
| **Mô tả** | Khách quét mã QR một lần để nhận quyền sử dụng app với hạn hiệu lực (`ValidDays` do Admin cấu hình). |
| **Tiền điều kiện** | Camera được cấp quyền; Admin đã in/phát QR code (chưa dùng). |
| **Hậu điều kiện** | `device_id` được sinh (nếu chưa có); `qr_verified` + `qr_expiry` lưu Preferences; vào LanguagePage. |

**Luồng chính:**
1. Visitor nhấn nút Quét camera → ZXing detect QR code.
2. `ScanViewModel.ScanResultCommand(text)` nhận payload.
3. Gọi `DeviceService.GetOrCreateDeviceId()` — đây là lần đầu sinh device_id nếu chưa có.
4. Gọi `QrService.VerifyAsync(code, deviceId)` với timeout 5 giây.
5. API set `IsUsed=true`, `UsedAt=now`, trả `{isValid: true, expiryAt}`.
6. `QrService.SaveAccess(expiryAt)` lưu `qr_verified` + `qr_expiry`.
7. Navigate `LanguagePage` (relative).

**Luồng thay thế:**
- **1a.** Chọn ảnh từ thư viện → `PickImageFromGalleryCommand` → MediaPicker → ZXing decode → tiếp bước 2.
- **4a.** Timeout 5s hoặc lỗi mạng → "Không thể kết nối. Vui lòng thử lại."

**Ngoại lệ:**
- API trả `isValid=false` với message:
  - "Mã QR đã được sử dụng" → yêu cầu mã khác.
  - "Mã QR không tồn tại" → nhập lại.
- Không có quyền camera → hướng dẫn cấp quyền trong Settings.

---

### UC-M03 – Chọn ngôn ngữ & giọng đọc

| Trường | Nội dung |
|--------|---------|
| **Actor** | Visitor |
| **Mô tả** | Chọn cả ngôn ngữ và giọng đọc trong cùng màn hình, lưu `DevicePreference` lên server và cache local. |
| **Tiền điều kiện** | Có `device_id`; QR access còn hạn. |
| **Hậu điều kiện** | Preference được lưu trên server + local; navigate `//MapPage`. |

**Luồng chính:**
1. `LanguagePage` load, gọi `LanguageService.GetActiveLanguagesAsync()` — cache 15 phút.
2. `GET /api/languages/active` (cache miss) → render danh sách với cờ + tên.
3. Visitor chọn ngôn ngữ.
4. `VoiceService.GetVoicesByLanguageAsync(languageId)` → `GET /api/tts-voice-profiles/active?languageId=X`.
5. Render danh sách giọng đọc (DisplayName + Gender + Description).
6. Visitor chọn giọng → nhấn Xác nhận.
7. Build `DevicePreferenceUpsertDto` đầy đủ: `deviceId, languageId, voiceId?, speechRate, autoPlay, platform, deviceModel, manufacturer, osVersion`.
8. `POST /api/device-preference` qua `DevicePreferenceApiService.UpsertAsync`.
9. Overload service tự gọi `LocalPreferenceService.Save(dto)` sau khi API thành công.
10. Navigate `//MapPage`.

**Luồng thay thế:**
- **4a.** Ngôn ngữ không có voice → hiển thị danh sách rỗng, cho phép bỏ qua (voiceId = null, dùng audio TTS mặc định).

**Ngoại lệ:**
- API lỗi ở bước 8 → hiển thị "Không thể lưu. Thử lại" — giữ lựa chọn, không navigate.

---

### UC-M04 – Xem MainPage + bottom nav

| Trường | Nội dung |
|--------|---------|
| **Actor** | Visitor |
| **Mô tả** | Home screen với 3 nút nav chính, featured stalls phân trang, và nút Đăng xuất. |
| **Tiền điều kiện** | Đã có preference (từ UC-M03). |
| **Hậu điều kiện** | Visitor có thể điều hướng vào Map / Tour / Language / Logout. |

**Luồng chính:**
1. `MainPage` load.
2. `MainViewModel` gọi `StallService.GetAllStallsAsync()` (cache-first 3 lớp).
3. Paging client-side: PageSize = 3 featured stalls.
4. Render 3 nút: Bản đồ, Tour, Ngôn ngữ + nút Đăng xuất.
5. Visitor nhấn nút tương ứng → Shell navigate.

**Luồng thay thế:**
- **5a.** Nút Bản đồ → `//MapPage`.
- **5b.** Nút Tour → `//TourListPage` (UC-M12).
- **5c.** Nút Ngôn ngữ → `LanguagePage` (UC-M03).
- **5d.** Đăng xuất → UC-M16.

---

### UC-M05 – Xem bản đồ tương tác & pin gian hàng

| Trường | Nội dung |
|--------|---------|
| **Actor** | Visitor |
| **Mô tả** | Hiển thị Mapsui + OSM, pin gian hàng, geofence circles, user location marker. |
| **Tiền điều kiện** | Đã có preference; cấp quyền vị trí (tùy chọn). |
| **Hậu điều kiện** | Map hiển thị, GPS polling chạy, geofence engine sẵn sàng. |

**Luồng chính:**
1. `MapPage.OnAppearing` → `MapViewModel.EnsureReadyAsync(forceReload: false)`.
2. State machine: `Uninitialized → Syncing → Loading → Ready` bảo vệ bằng `SemaphoreSlim(1, 1)`.
3. `SyncService.EnsureSyncedAsync()` — chỉ sync nếu cache hết hạn.
4. `StallService.GetAllStallsAsync()` trả stall (cache-first 3 lớp).
5. Render pins + geofence circles (SkiaSharp) + polyline nếu tour mode.
6. Subscribe 3 event: `PlaybackCompleted`, `LocationUpdated`, `AudioDownloaded`.
7. Visitor có thể tap/kéo map, zoom.

**Luồng thay thế:**
- **1a.** Pull-to-refresh → `EnsureReadyAsync(forceReload: true)` → `SyncService.SyncAsync()` force.
- **3a.** Offline → dùng SQLite cache đã có, không block UI.

**Ngoại lệ:**
- Cấp quyền GPS bị từ chối → map hiển thị nhưng không có user marker; geofence auto-play vẫn hoạt động nếu GPS ngầm bật được.
- `MapPage.Unloaded` → `vm.Dispose()` unsubscribe 3 event tránh leak.

---

### UC-M06 – Tap pin → Popup → Phát audio thủ công

| Trường | Nội dung |
|--------|---------|
| **Actor** | Visitor |
| **Mô tả** | Tap vào pin gian hàng mở popup chi tiết, Visitor chủ động nhấn Phát. |
| **Tiền điều kiện** | Map đã load, stall có narration + audio tương ứng ngôn ngữ. |
| **Hậu điều kiện** | Audio phát thành công qua `AudioGuideService`. |

**Luồng chính:**
1. Visitor tap pin → `MapPage.OnPinClickedAsync(stall)`.
2. `MapViewModel.SelectedStall = stall` (CHỈ set ở đây; geofence không chạm).
3. `StallPopup.Init(stall)` + `ShowPopupAsync()`.
4. Popup hiển thị: tên, mô tả, gallery, text thuyết minh, nút Phát.
5. Visitor nhấn Phát → `OnPlayClicked` (async void event handler) → `await _vm.PlayStallAsync(stall)`.
6. `AudioCacheService.GetOrDownloadAsync(url, stallId, langCode)`:
   - Cache hit → trả local path.
   - Cache miss → HttpClient `"download"` (timeout 30s) tải MP3 về `{AppDataDirectory}/audio/{lang}/{stallId}.mp3`.
7. `AudioGuideService.PlayAsync(localPath)`.
8. `StallPopup.CloseAsync()`.

**Luồng thay thế:**
- **6a.** Offline + không có cache → lỗi hiển thị "Không có audio offline cho gian hàng này".
- **7a.** Audio đang phát cho stall khác → stop cái cũ, phát cái mới.

**Ngoại lệ:**
- URL audio 404 hoặc Blob lỗi → log + thông báo lỗi, không crash.

---

### UC-M07 – Geofence auto-play

| Trường | Nội dung |
|--------|---------|
| **Actor** | Visitor (thụ động) + Hệ thống |
| **Mô tả** | Khi khách đi vào vùng geofence của stall, app tự phát audio. Hỗ trợ queue nếu đang phát. |
| **Tiền điều kiện** | MapPage đang active, GPS polling bật, có stall với location + radiusMeters. |
| **Hậu điều kiện** | Audio tự phát, `_triggeredIds` ghi nhận stall đã trigger. |

**Luồng chính:**
1. `GpsPollingService` raise `LocationUpdated(lat, lng, accuracy)` khoảng 1 giây/lần.
2. `MapViewModel` subscriber gọi `GeofenceEngine.CheckAsync(lat, lng, stalls)`.
3. `GeofenceEngine` tính Haversine tới từng stall, so với `radiusMeters`.
4. Nếu vào vùng stall chưa trigger → `_triggeredIds.Add(stallId)` + `_queue.Enqueue(stall)`.
5. Raise event `AutoPlayRequested(stall)` awaitable (`Func<GeoStallDto, Task>`).
6. MapVM nhận event:
   - Nếu `AudioGuideService.IsPlaying = true` → giữ trong queue.
   - Ngược lại → `PlayStallAsync(stall)`.
7. Khi audio phát xong → `PlaybackCompleted` → MapVM dequeue next → play tiếp.

**Luồng thay thế:**
- **Tour mode:** Nếu `GeofenceEngine.SetActiveTour(stallIds)` đã được gọi → chỉ trigger auto-play cho stall trong set; stall ngoài vẫn tap thủ công được (UC-M06).
- **6a.** `AutoPlay = false` trong preference → chỉ thông báo visual, không phát.

**Ngoại lệ:**
- Trigger lặp: `_triggeredIds` là session state, stall đã trigger không re-trigger dù đứng lâu trong vùng.

---

### UC-M08 – Cache-first & chế độ Offline

| Trường | Nội dung |
|--------|---------|
| **Actor** | Hệ thống (StallService) |
| **Mô tả** | Đảm bảo Mobile hoạt động offline bằng cache 3 lớp memory → SQLite → API. |
| **Tiền điều kiện** | SQLite đã khởi tạo. |
| **Hậu điều kiện** | Dữ liệu hiển thị ngay dù offline; SQLite đồng bộ khi online. |

**Luồng chính:**
1. Caller (MapVM, MainVM, StallListVM) gọi `StallService.GetAllStallsAsync()`.
2. Service check memory cache (TTL 10 phút). Hit → trả ngay.
3. Miss → `LocalStallRepository.GetAllAsync()` đọc SQLite.
4. Map `LocalStall → GeoStallDto`, update memory cache.
5. Background: `SyncService.SyncAsync()` gọi `GET /api/geo/stalls?deviceId=X` → `UpsertBatchAsync` với diff check `HasChanged` → raise `DataUpdated` event.
6. Caller có thể subscribe event để refresh UI.

**Luồng thay thế:**
- **5a.** Offline (không mạng) → Skip sync, tiếp tục dùng SQLite data hiện có.
- **5b.** API lỗi → log + giữ cache cũ.

---

### UC-M09 – Background Sync stall

| Trường | Nội dung |
|--------|---------|
| **Actor** | SyncBackgroundService |
| **Mô tả** | Chu kỳ 3 phút sync stall + piggyback check reset flag. Trigger lại khi ConnectivityChanged. |
| **Tiền điều kiện** | `SyncBackgroundService.Start()` đã gọi (từ `App.OnStart` / `OnResume`). |
| **Hậu điều kiện** | SQLite cập nhật, audio mới cache, memory cache invalidated. |

**Luồng chính:**
1. `PeriodicTimer(StallSyncInterval = 3 phút)` tick.
2. `SyncService.IsSyncing` check atomic qua `Interlocked.CompareExchange` — skip nếu đang chạy tick trước.
3. `SyncAsync()` gọi `GET /api/geo/stalls?deviceId=X`.
4. `LocalStallRepository.UpsertBatchAsync(stalls)` với diff check.
5. `AudioCacheService.EnsureDownloadedAsync(audioUrls)` — semaphore 3 concurrent.
6. **Piggyback:** `CheckResetFlagAsync()` — xem UC-M14.

**Luồng thay thế:**
- **ConnectivityChanged (offline → online):** Capture local `_cts` trước khi dùng → trigger sync ngay (không chờ hết chu kỳ).
- **App.OnResume:** Gọi `Start()` → `CleanupInternal()` (không `Stop()` để không gửi offline notify nhầm) → timer restart.

---

### UC-M10 – Flush batch GPS

| Trường | Nội dung |
|--------|---------|
| **Actor** | SyncBackgroundService + LocationLogService |
| **Mô tả** | Gửi batch điểm GPS đã buffer mỗi 20 giây. Piggyback cập nhật LastSeenAt (heartbeat chính). |
| **Tiền điều kiện** | GpsPollingService đang bật trong MapPage active. |
| **Hậu điều kiện** | Điểm GPS lưu DB; heatmap Admin phản ánh dữ liệu mới. |

**Luồng chính:**
1. `GpsPollingService.LocationUpdated` → `LocationLogService.Add(lat, lng, accuracy, timestamp)` buffer in-memory.
2. `PeriodicTimer(FlushInterval = 20 giây)` tick.
3. `LocationLogService.FlushAsync()` — nếu buffer có điểm, build `DeviceLocationLogBatchDto`.
4. `POST /api/device-location-log/batch` với max 500 điểm.
5. API piggyback: `ExecuteUpdateAsync SET LastSeenAt = now`.
6. Clear buffer local.

**Luồng thay thế:**
- **3a.** Buffer rỗng → skip, không gọi API.
- **4a.** Mạng lỗi → giữ buffer, thử lại ở tick sau (buffer có thể vượt 500 → cắt lô, gửi nhiều lần).

---

### UC-M11 – Xem danh sách gian hàng (Stall List)

| Trường | Nội dung |
|--------|---------|
| **Actor** | Visitor |
| **Mô tả** | Trang danh sách stall với search + phân trang + (tương lai) filter category. |
| **Tiền điều kiện** | Có dữ liệu stall (cache hoặc API). |
| **Hậu điều kiện** | Visitor xem được danh sách lọc theo keyword. |

**Luồng chính:**
1. Visitor vào `StallListPage`.
2. `LoadStallsAsync()` gọi `GetAllStallsAsync(forceRefresh: true)` — **known issue: luôn bỏ qua cache**.
3. `POST /api/geo/stalls?deviceId=X`.
4. Filter client-side theo `SearchText` + `CurrentFilter` (hiện = "All", chưa dùng).
5. Paging (PageSize = 10), render trang hiện tại.

**Luồng thay thế:**
- **Nhập search:** `SearchText` setter trigger `LoadStallsAsync` mỗi lần gõ (**known issue: chưa debounce**) → API gọi liên tục.
- **Prev/Next:** PreviousPageCommand / NextPageCommand trên `_allStalls` đã lấy, không gọi lại API.

---

### UC-M12 – Xem & bắt đầu Tour

| Trường | Nội dung |
|--------|---------|
| **Actor** | Visitor |
| **Mô tả** | Chọn tour có sẵn do Admin tạo, xem chi tiết, bắt đầu chạy. |
| **Tiền điều kiện** | Có tour `IsActive = true` trong hệ thống. |
| **Hậu điều kiện** | `ActiveTourId` lưu Preferences; MapPage vào tour mode. |

**Luồng chính:**
1. Visitor vào MainPage → nhấn "Tour" → `//TourListPage`.
2. `TourListPage` gọi `TourService.GetToursAsync()` — cache memory only 10 phút.
3. `GET /api/tours` (AllowAnonymous, filter `IsActive=true`).
4. Render list với tên, mô tả, EstimatedMinutes, StopCount.
5. Visitor tap tour → `Shell.Current.GoToAsync($"TourDetailPage?tourId={id}")`.
6. `TourDetailPage` (via `[QueryProperty("TourId", "tourId")]`) gọi `GetTourDetailAsync(id)`.
7. `GET /api/tours/{id}` → trả detail với stops có tọa độ + thumbnail.
8. Render stops theo order, hiển thị nút "Bắt đầu".
9. Visitor nhấn Bắt đầu → `LocalPreferenceService.SetActiveTourId(id)` + `Shell.Current.GoToAsync($"//MapPage?tourId={id}")`.

---

### UC-M13 – Thực hiện Tour (tracking progress)

| Trường | Nội dung |
|--------|---------|
| **Actor** | Visitor |
| **Mô tả** | MapPage vào tour mode: polyline route, filter geofence, track completed stops, hoàn tất alert. |
| **Tiền điều kiện** | ActiveTourId có trong Preferences. |
| **Hậu điều kiện** | Completed stops lưu Preferences; khi hoàn tất → clear progress. |

**Luồng chính:**
1. `MapPage.OnAppearing` → `ResolveTourAsync()`:
   - Đọc query `tourId` (từ navigation).
   - Fallback: `LocalPreferenceService.GetActiveTourId()` (để resume sau kill app).
2. Nếu có `tourId` → `MapViewModel.SetActiveTourAsync(tourId)`.
3. Fetch tour detail qua `TourService`.
4. `GeofenceEngine.SetActiveTour(tourStallIds)` → chỉ auto-play stall trong tour.
5. Raise `TourRouteChanged` event → MapPage vẽ Mapsui polyline theo sequence stops.
6. Khi geofence trigger stall trong tour:
   - `LocalPreferenceService.AddCompletedStop(stallId)` lưu vào `pref_tour_completed_stops` (JSON array).
   - `RecomputeNextStop()` cập nhật stop tiếp theo.
7. Khi đủ mọi stop trong tour → `CompleteTourAsync()`:
   - Alert "Đã hoàn thành tour".
   - `ClearTourProgress()` xóa `pref_active_tour_id` + `pref_tour_completed_stops`.
   - `GeofenceEngine.SetActiveTour(null)` tắt tour mode.

**Luồng thay thế:**
- **Resume:** App bị kill lúc tour đang chạy → mở lại → LoadingPage → MainPage → Map → `GetActiveTourId()` trả giá trị → tour mode tự kích hoạt.
- Visitor muốn huỷ tour giữa chừng → hiện tại **chưa có nút hủy** (nợ UX).

---

### UC-M14 – Pull cờ Reset Thiết bị

| Trường | Nội dung |
|--------|---------|
| **Actor** | SyncBackgroundService + Admin (qua Web) |
| **Mô tả** | Admin set cờ reset trên DB; Mobile pull định kỳ → nhận cờ → clear Preferences + về LoadingPage. |
| **Tiền điều kiện** | Admin đã bấm Reset trong ActiveDevices view. |
| **Hậu điều kiện** | Preferences clear hoàn toàn; Mobile về ScanPage (cần QR mới). |

**Luồng chính:**
1. `SyncBackgroundService` chu kỳ 3 phút (piggyback sau stall sync) gọi `GET /api/device-preference/reset-flag?deviceId=X`.
2. API đọc `NeedsReset`. Nếu `true` → atomic `ExecuteUpdateAsync SET NeedsReset = false`. Trả bool.
3. Mobile nhận `true`:
   - `Preferences.Clear()` — xóa tất cả khóa (device_id + 8 pref_* + qr_verified + qr_expiry + pref_active_tour_id + pref_tour_completed_stops).
   - Xóa folder `{AppDataDirectory}/audio/`.
   - `Shell.Current.GoToAsync("//LoadingPage")`.
4. `LoadingPage` thấy `device_id = null` → `//ScanPage`.
5. Khách phải quét QR mới để dùng lại.

**Luồng thay thế:**
- **1a.** Không có `DevicePreference` trong DB → API trả `200 false` (không lỗi).
- **Mobile crash giữa chừng sau khi API clear cờ nhưng chưa clear Preferences** → cờ đã false ở DB, không trigger reset lần 2. Admin phải bấm Reset lại nếu cần.

---

### UC-M15 – Notify Offline khi thoát app

| Trường | Nội dung |
|--------|---------|
| **Actor** | App lifecycle + SyncBackgroundService |
| **Mô tả** | Thiết bị báo offline cho API khi thoát — rớt khỏi active-devices list ngay thay vì đợi hết cửa sổ. |
| **Tiền điều kiện** | App đang chạy, có `device_id`. |
| **Hậu điều kiện** | API set `LastSeenAt = MinValue` → Admin dashboard phản ánh ngay. |

**Luồng chính:**
1. Trigger từ 3 nơi:
   - `App.OnSleep` (background button, task switcher).
   - `MapPage.OnDisappearing` (rời MapPage sang page khác).
   - Reset flag detected trong UC-M14 trước khi navigate về LoadingPage.
2. `SyncBackgroundService.Stop()`:
   - Fire-and-forget `POST /api/device-preference/{deviceId}/offline` (không await).
   - Dispose timers + `_cts.Cancel()`.
3. API `ExecuteUpdateAsync SET LastSeenAt = DateTimeOffset.MinValue` (sentinel `0001-01-01`).
4. Thiết bị rớt khỏi `/api/geo/active-devices` ngay trong response tiếp theo.

**Luồng thay thế:**
- **App.OnResume:** Gọi `Start()` → `CleanupInternal()` + tạo timers mới. Tick đầu khôi phục `LastSeenAt = now`.
- **App bị kill đột ngột:** Notify không kịp gửi → API có fallback time-based (hết cửa sổ `withinSeconds`).

---

### UC-M16 – Đăng xuất (xóa QR access)

| Trường | Nội dung |
|--------|---------|
| **Actor** | Visitor |
| **Mô tả** | Vô hiệu mã QR hiện tại để người khác không dùng được máy. Giữ language/voice preference. |
| **Tiền điều kiện** | Visitor đang ở MainPage. |
| **Hậu điều kiện** | `qr_verified` + `qr_expiry` xóa; vào ScanPage. |

**Luồng chính:**
1. Visitor nhấn "Đăng xuất" trên MainPage.
2. `MainViewModel.LogoutCommand` → `QrService.ClearAccess()`.
3. Xóa `qr_verified` + `qr_expiry` khỏi Preferences.
4. **Giữ** các khóa khác: `device_id`, `pref_*` (language/voice), `pref_active_tour_id`, ... — cho phép lần sau quét QR mới vẫn giữ được cài đặt cũ.
5. `Shell.Current.GoToAsync("//ScanPage")`.

**Luồng thay thế:**
- Nếu muốn reset toàn bộ → dùng UC-M14 (Admin reset) hoặc uninstall app.
