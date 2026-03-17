using Microsoft.AspNetCore.Mvc;
using LicensingServer.Data;
using LicensingServer.Models;
using Microsoft.EntityFrameworkCore;

namespace LicensingServer.Controllers;

[ApiController]
[Route("api")]
public class ActivationController : ControllerBase
{
    private readonly AppDbContext _db;

    public ActivationController(AppDbContext db)
    {
        _db = db;
    }

    // Request para activar
    public class ActivateRequest
    {
        public required string LicenseKey { get; set; }
        public required string HwidHash { get; set; }
    }

    [HttpPost("activate")]
    public async Task<IActionResult> Activate([FromBody] ActivateRequest req)
    {
        if (req == null || string.IsNullOrWhiteSpace(req.LicenseKey) || string.IsNullOrWhiteSpace(req.HwidHash))
            return BadRequest(new { error = "Datos incompletos" });

        var license = await _db.Licenses
                               .AsNoTracking()
                               .FirstOrDefaultAsync(l => l.LicenseKey == req.LicenseKey);

        if (license == null || license.Revoked)
            return Unauthorized(new { error = "Licencia inválida o revocada" });

        var existingActivation = await _db.Activations
                                          .FirstOrDefaultAsync(a => a.LicenseId == license.Id && a.HwidHash == req.HwidHash);

        if (existingActivation != null)
        {
            var payloadExisting = $"OK|{existingActivation.Id}";
            var signatureExisting = SecurityHelper.GenerateSignature(payloadExisting);

            return Ok(new
            {
                message = "Ya activado",
                activationId = existingActivation.Id,
                signature = signatureExisting
            });
        }

        var activationCount = await _db.Activations.CountAsync(a => a.LicenseId == license.Id);

        if (activationCount >= license.MaxActivations)
            return Conflict(new { error = "Límite de activaciones alcanzado" });

        var activation = new Activation
        {
            LicenseId = license.Id,
            HwidHash = req.HwidHash
        };

        _db.Activations.Add(activation);
        await _db.SaveChangesAsync();

        var payload = $"OK|{activation.Id}";
        var signature = SecurityHelper.GenerateSignature(payload);

        return Ok(new
        {
            message = "Activado correctamente",
            activationId = activation.Id,
            signature = signature
        });
    }

    // Request para verificación
    public class VerifyRequest
    {
        public required string LicenseKey { get; set; }
        public required string HwidHash { get; set; }
    }

    [HttpPost("verify")]
    public async Task<IActionResult> Verify([FromBody] VerifyRequest req)
    {
        if (req == null || string.IsNullOrWhiteSpace(req.LicenseKey) || string.IsNullOrWhiteSpace(req.HwidHash))
            return BadRequest(new { valid = false, reason = "datos incompletos" });

        var license = await _db.Licenses
                               .AsNoTracking()
                               .FirstOrDefaultAsync(l => l.LicenseKey == req.LicenseKey);

        if (license == null || license.Revoked)
            return Ok(new { valid = false, reason = "licencia no existe o revocada" });

        var activation = await _db.Activations
                                  .AsNoTracking()
                                  .FirstOrDefaultAsync(a => a.LicenseId == license.Id && a.HwidHash == req.HwidHash);

        if (activation == null)
            return Ok(new { valid = false, reason = "hwid no activado" });

        var payload = $"OK|{activation.Id}";
        var signature = SecurityHelper.GenerateSignature(payload);

        return Ok(new
        {
            valid = true,
            activationId = activation.Id,
            signature = signature
        });
    }
}