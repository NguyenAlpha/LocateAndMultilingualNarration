using Api.Authorization;
using Api.Extensions;
using Api.Infrastructure.Persistence;
using LocateAndMultilingualNarration.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using Shared.DTOs.Common;
using Shared.DTOs.QrCodes;

namespace Api.Controllers;

[ApiController]
[Route("api/qrcodes")]
[Authorize(Policy = AppPolicies.AdminOnly)]
public class QrCodeController(AppDbContext db) : AppControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetQrCodes(
        [FromQuery] bool? isUsed = null,
        [FromQuery] bool? expired = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = db.QrCodes.AsNoTracking();

        if (isUsed.HasValue)
            query = query.Where(q => q.IsUsed == isUsed.Value);

        if (expired.HasValue)
        {
            var now = DateTime.UtcNow;
            query = expired.Value
                ? query.Where(q => q.ExpiryAt <= now)
                : query.Where(q => q.ExpiryAt > now);
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(q => q.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(q => new QrCodeDetailDto
            {
                Id = q.Id,
                Code = q.Code,
                CreatedAt = q.CreatedAt,
                ExpiryAt = q.ExpiryAt,
                IsUsed = q.IsUsed,
                UsedAt = q.UsedAt,
                UsedByDeviceId = q.UsedByDeviceId,
                Note = q.Note
            })
            .ToListAsync(ct);

        var result = new PagedResult<QrCodeDetailDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };

        return this.OkResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateQrCode([FromBody] QrCodeCreateDto request, CancellationToken ct = default)
    {
        if (request.ExpiryAt <= DateTime.UtcNow)
            return this.BadRequestResult("Ngày hết hạn phải lớn hơn thời điểm hiện tại.", "ExpiryAt");

        string code;
        do
        {
            code = Guid.NewGuid().ToString("N")[..16].ToUpper();
        } while (await db.QrCodes.AnyAsync(q => q.Code == code, ct));

        var qrCode = new QrCode
        {
            Code = code,
            ExpiryAt = request.ExpiryAt,
            Note = request.Note
        };

        db.QrCodes.Add(qrCode);
        await db.SaveChangesAsync(ct);

        var dto = new QrCodeDetailDto
        {
            Id = qrCode.Id,
            Code = qrCode.Code,
            CreatedAt = qrCode.CreatedAt,
            ExpiryAt = qrCode.ExpiryAt,
            IsUsed = qrCode.IsUsed,
            Note = qrCode.Note
        };

        return this.CreatedResult(dto);
    }

    [HttpGet("{id:guid}/image")]
    public async Task<IActionResult> GetQrCodeImage(Guid id, CancellationToken ct = default)
    {
        var qrCode = await db.QrCodes.AsNoTracking()
            .FirstOrDefaultAsync(q => q.Id == id, ct);

        if (qrCode is null)
            return this.NotFoundResult("Mã QR không tồn tại.");

        var qrGen = new QRCodeGenerator();
        var qrData = qrGen.CreateQrCode(qrCode.Code, QRCodeGenerator.ECCLevel.Q);
        var png = new PngByteQRCode(qrData);
        var bytes = png.GetGraphic(20);

        return File(bytes, "image/png", $"qr-{id}.png");
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteQrCode(Guid id, CancellationToken ct = default)
    {
        var qrCode = await db.QrCodes.FirstOrDefaultAsync(q => q.Id == id, ct);

        if (qrCode is null)
            return this.NotFoundResult("Mã QR không tồn tại.");

        db.QrCodes.Remove(qrCode);
        await db.SaveChangesAsync(ct);

        return this.OkResult<object?>(null);
    }

    [HttpPost("verify")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyQrCode([FromBody] QrCodeVerifyRequestDto request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
            return this.OkResult(new { isValid = false, message = "Mã QR không hợp lệ." });

        var qrCode = await db.QrCodes.FirstOrDefaultAsync(q => q.Code == request.Code, ct);

        if (qrCode is null)
            return this.OkResult(new { isValid = false, message = "Mã QR không tồn tại." });

        if (qrCode.IsUsed)
            return this.OkResult(new { isValid = false, message = "Mã QR đã được sử dụng." });

        if (qrCode.ExpiryAt <= DateTime.UtcNow)
            return this.OkResult(new { isValid = false, message = "Mã QR đã hết hạn." });

        qrCode.IsUsed = true;
        qrCode.UsedAt = DateTime.UtcNow;
        qrCode.UsedByDeviceId = request.DeviceId;
        await db.SaveChangesAsync(ct);

        return this.OkResult(new { isValid = true, message = "OK", expiryAt = qrCode.ExpiryAt });
    }
}
