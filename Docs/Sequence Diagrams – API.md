> Các sequence diagram mô tả những luồng API phức tạp hoặc có business rule đặc thù — không bao gồm CRUD thông thường. Ký hiệu: **CLIENT** = Web MVC hoặc Mobile MAUI, **CTR** = API Controller, **SVC** = Application Service, **DB** = SQL Server qua EF Core, **EXT** = External Service (Azure).

| Mã | Tên |
|----|-----|
| [SD-A01](#sd-a01-đăng-nhập--phát-hành-token) | Đăng nhập & Phát hành Token |
| [SD-A02](#sd-a02-refresh-token) | Refresh Token |
| [SD-A03](#sd-a03-tts-background-service) | TTS Background Service |
| [SD-A04](#sd-a04-xác-thực-mã-qr-mobile) | Xác thực Mã QR (Mobile) |
| [SD-A05](#sd-a05-thanh-toán--kích-hoạt-plan) | Thanh toán & Kích hoạt Plan |
| [SD-A06](#sd-a06-lấy-danh-sách-gian-hàng-geo) | Lấy danh sách Gian hàng (Geo) |

---

### SD-A01: Đăng nhập & Phát hành Token

```mermaid
sequenceDiagram
    participant CLIENT as Client (Web/Mobile)
    participant CTR as AuthController
    participant DB as Database

    CLIENT->>CTR: POST /api/auth/login {email, password}
    CTR->>DB: Tìm user theo NormalizedEmail hoặc NormalizedUserName

    alt Không tìm thấy user
        CTR-->>CLIENT: 401 "Email hoặc mật khẩu không đúng"
    end

    CTR->>CTR: BCrypt.Verify(password, user.PasswordHash)
    alt Sai mật khẩu
        CTR-->>CLIENT: 401 "Email hoặc mật khẩu không đúng"
    end

    alt user.IsActive = false
        CTR-->>CLIENT: 401 "Tài khoản đã bị vô hiệu hóa"
    end

    CTR->>CTR: JwtService.GenerateToken(user, roles) — JWT 30 phút
    CTR->>CTR: JwtService.GenerateRefreshToken() — raw token + SHA256 hash
    CTR->>DB: Lưu RefreshToken {TokenHash, ExpiresAtUtc = now+30d, DeviceId, IP}
    CTR->>DB: Cập nhật user.LastLoginAt = now
    CTR-->>CLIENT: 200 {Token, ExpiresAt, RefreshToken (raw), RefreshTokenExpiresAt, UserInfo, Roles}
```

---

### SD-A02: Refresh Token

```mermaid
sequenceDiagram
    participant CLIENT as Client (Web/Mobile)
    participant CTR as AuthController
    participant DB as Database

    CLIENT->>CTR: POST /api/auth/refresh {refreshToken (raw)}
    CTR->>CTR: JwtService.HashRefreshToken(raw) → SHA256 hash
    CTR->>DB: Tìm RefreshToken theo hash

    alt Không tìm thấy
        CTR-->>CLIENT: 401 "Refresh token không hợp lệ"
    end

    alt RevokedAtUtc != null hoặc ExpiresAtUtc <= now
        CTR-->>CLIENT: 401 "Refresh token đã hết hạn hoặc bị thu hồi"
    end

    CTR->>DB: Lấy user kèm roles
    alt user không tồn tại hoặc IsActive = false
        CTR-->>CLIENT: 401 "Tài khoản đã bị vô hiệu hóa"
    end

    CTR->>CTR: GenerateToken(user, roles) — JWT mới 30 phút
    CTR->>CTR: GenerateRefreshToken() — cặp token mới
    CTR->>DB: Set oldRefreshToken.RevokedAtUtc = now
    CTR->>DB: Lưu RefreshToken mới {TokenHash, ExpiresAtUtc = now+30d}
    CTR-->>CLIENT: 200 {Token mới, RefreshToken mới (raw), ExpiresAt}
```

---

### SD-A03: TTS Background Service

```mermaid
sequenceDiagram
    participant CTRL as StallNarrationContentController
    participant DB as Database
    participant BG as TtsBackgroundService
    participant SVC as NarrationAudioService
    participant AZ_TR as Azure Translator
    participant AZ_TTS as Azure Speech SDK
    participant AZ_BLOB as Azure Blob Storage

    Note over BG: Khởi động cùng API (BackgroundService)

    Note over BG: Khi khởi động: reset stale jobs
    BG->>DB: Tìm StallNarrationContent có TtsStatus="Processing" và UpdatedAt < now-10min
    DB-->>BG: Danh sách stale jobs
    BG->>DB: Set TtsStatus="Pending" cho tất cả stale jobs

    loop Mỗi 5 giây (PeriodicTimer)
        BG->>DB: Tìm tối đa 5 StallNarrationContent có TtsStatus="Pending" (cũ nhất trước)
        alt Không có job nào
            BG->>BG: Bỏ qua tick này
        else Có jobs
            BG->>DB: Set TtsStatus="Processing" cho tất cả jobs trong batch (commit trước khi xử lý)

            loop Từng job (tuần tự, không song song)
                BG->>SVC: CreateOrUpdateFromTtsAsync(contentId, scriptText, languageId)
                SVC->>DB: Lấy languageCode và danh sách TtsVoiceProfile
                SVC->>AZ_TR: Dịch scriptText sang ngôn ngữ đích (nếu cần)
                AZ_TR-->>SVC: Văn bản đã dịch
                loop Từng voice profile
                    SVC->>AZ_TTS: Tổng hợp giọng nói (SSML)
                    AZ_TTS-->>SVC: Audio bytes (.wav)
                    SVC->>AZ_BLOB: Upload audio lên container "narration-audio"
                    AZ_BLOB-->>SVC: AudioUrl (public URL)
                    SVC->>DB: Upsert NarrationAudio {AudioUrl, IsTts=true, VoiceProfileId}
                end

                alt Thành công
                    BG->>DB: TtsStatus="Completed", TtsError=null, UpdatedAt=now
                else Thất bại (Azure lỗi, network...)
                    BG->>DB: TtsStatus="Failed", TtsError=message (tối đa 500 ký tự), UpdatedAt=now
                end
            end
        end
    end

    Note over CTRL,DB: Khi người dùng tạo/cập nhật content
    CTRL->>DB: Lưu StallNarrationContent với TtsStatus="Pending"
    Note over BG: Background service sẽ nhận job này ở tick tiếp theo
```

---

### SD-A04: Xác thực Mã QR (Mobile)

```mermaid
sequenceDiagram
    participant MOBILE as Mobile App
    participant CTR as QrCodeController
    participant DB as Database

    Note over CTR: [AllowAnonymous] — không cần JWT

    MOBILE->>CTR: POST /api/qrcodes/verify {code, deviceId}

    CTR->>DB: Tìm QrCode theo code

    alt Không tìm thấy
        CTR-->>MOBILE: 200 {isValid: false, "Mã QR không tồn tại"}
    end

    alt qrCode.IsUsed = true
        CTR-->>MOBILE: 200 {isValid: false, "Mã QR đã được sử dụng"}
    end

    CTR->>CTR: usedAt = now
    CTR->>CTR: expiryAt = usedAt + qrCode.ValidDays
    CTR->>DB: Set IsUsed=true, UsedAt=usedAt, UsedByDeviceId=deviceId

    CTR-->>MOBILE: 200 {isValid: true, expiryAt}

    Note over MOBILE: Lưu expiryAt vào Preferences
    Note over MOBILE: Mỗi lần mở app: kiểm tra expiryAt > now
```

---

### SD-A05: Thanh toán & Kích hoạt Plan

```mermaid
sequenceDiagram
    participant CLIENT as Web Client
    participant CTR as SubscriptionOrderController
    participant DB as Database

    CLIENT->>CTR: POST /api/subscription-orders {businessId, plan, cardNumber...}

    alt Plan không phải Basic hoặc Pro
        CTR-->>CLIENT: 400 "Chỉ có thể đăng ký gói Basic hoặc Pro"
    end

    CTR->>DB: Lấy business theo businessId
    alt Không tìm thấy hoặc không phải owner
        CTR-->>CLIENT: 404 / 403
    end

    CTR->>CTR: Kiểm tra downgrade
    Note over CTR: hasActivePlan = Plan != Free AND PlanExpiresAt > now
    alt hasActivePlan AND rank(request.Plan) < rank(business.Plan)
        CTR-->>CLIENT: 400 "Không thể đăng ký gói thấp hơn khi plan hiện tại còn hạn"
    end

    CTR->>CTR: Mock payment validation
    Note over CTR: Strip spaces/dashes khỏi cardNumber
    Note over CTR: Đúng 16 chữ số → Completed, ngược lại → Failed

    CTR->>CTR: Tính thời gian plan
    alt Business đang có plan active (PlanExpiresAt > now)
        CTR->>CTR: planStartAt = PlanExpiresAt hiện tại (extend, không bắt đầu từ now)
    else
        CTR->>CTR: planStartAt = now
    end
    CTR->>CTR: planEndAt = planStartAt + 1 tháng

    CTR->>DB: Lưu SubscriptionOrder {status, plan, amount, planStartAt, planEndAt}

    alt status = Completed
        CTR->>DB: Cập nhật business.Plan = request.Plan, business.PlanExpiresAt = planEndAt
    end

    CTR-->>CLIENT: 200 SubscriptionOrderDetailDto
```

---

### SD-A06: Lấy danh sách Gian hàng (Geo)

```mermaid
sequenceDiagram
    participant MOBILE as Mobile App
    participant CTR as GeoController
    participant SVC as GeoService
    participant DB as Database

    Note over CTR: [AllowAnonymous] — không cần JWT

    MOBILE->>CTR: GET /api/geo/stalls?deviceId=ABC123
    CTR->>SVC: GetAllStallsAsync(deviceId)

    SVC->>DB: Tìm DevicePreference theo deviceId
    alt Có preference
        DB-->>SVC: {languageId, voiceId}
    else Không có preference hoặc deviceId rỗng
        SVC->>DB: Tìm ngôn ngữ fallback (ưu tiên code="vi")
        DB-->>SVC: languageId fallback
    end

    SVC->>DB: Query StallLocations kèm Stalls, NarrationContents (lọc theo languageId + IsActive), NarrationAudios, StallMedia
    Note over DB: Filtered Include: chỉ lấy content active đúng ngôn ngữ

    DB-->>SVC: Danh sách StallLocation với dữ liệu đầy đủ

    loop Mỗi StallLocation
        SVC->>SVC: PickAudioUrl(audios, preferredVoice)
        Note over SVC: Ưu tiên 1: khớp voiceId của thiết bị<br/>Ưu tiên 2: audio TTS (IsTts=true)<br/>Ưu tiên 3: bất kỳ audio có URL
    end

    SVC-->>CTR: List<GeoStallDto> {stallId, lat, lng, radius, narrationContent, audioUrl, mediaImages}
    CTR-->>MOBILE: 200 ApiResult<List<GeoStallDto>>

    Note over MOBILE: Upsert vào SQLite local
    Note over MOBILE: Hiển thị marker trên bản đồ
```
