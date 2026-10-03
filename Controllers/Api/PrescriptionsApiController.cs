using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartClinicManagementSystem.DTOs;
using SmartClinicManagementSystem.Models;
using SmartClinicManagementSystem.Services.Interfaces;

namespace SmartClinicManagementSystem.Controllers.Api;

[ApiController]
[Route("api/prescriptions")]
[Authorize(Policy = "ClinicalStaff")]
public class PrescriptionsApiController : ControllerBase
{
    private readonly IPrescriptionService _prescriptionService;

    public PrescriptionsApiController(
        IPrescriptionService prescriptionService)
    {
        _prescriptionService = prescriptionService;
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(PrescriptionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PrescriptionDto>> GetById(int id)
    {
        if (id <= 0)
        {
            return BadRequest(new
            {
                status = 400,
                message = "A valid prescription ID is required."
            });
        }

        var prescription =
            await _prescriptionService.GetByIdAsync(id);

        if (prescription is null)
        {
            return NotFound(new
            {
                status = 404,
                message = "The prescription could not be found."
            });
        }

        return Ok(MapToDto(prescription));
    }

    [HttpGet("consultation/{consultationId:int}")]
    [ProducesResponseType(typeof(IEnumerable<PrescriptionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<PrescriptionDto>>> GetByConsultation(
        int consultationId)
    {
        if (consultationId <= 0)
        {
            return BadRequest(new
            {
                status = 400,
                message = "A valid consultation ID is required."
            });
        }

        var prescriptions =
            await _prescriptionService.GetByConsultationAsync(
                consultationId);

        return Ok(prescriptions.Select(MapToDto));
    }

    [HttpGet("patient/{patientId:int}")]
    [ProducesResponseType(typeof(IEnumerable<PrescriptionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<PrescriptionDto>>> GetByPatient(
        int patientId)
    {
        if (patientId <= 0)
        {
            return BadRequest(new
            {
                status = 400,
                message = "A valid patient ID is required."
            });
        }

        var prescriptions =
            await _prescriptionService.GetByPatientAsync(patientId);

        return Ok(prescriptions.Select(MapToDto));
    }

    [HttpGet("doctor/{doctorId:int}")]
    [ProducesResponseType(typeof(IEnumerable<PrescriptionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<PrescriptionDto>>> GetByDoctor(
        int doctorId)
    {
        if (doctorId <= 0)
        {
            return BadRequest(new
            {
                status = 400,
                message = "A valid doctor ID is required."
            });
        }

        var prescriptions =
            await _prescriptionService.GetByDoctorAsync(doctorId);

        return Ok(prescriptions.Select(MapToDto));
    }

    [HttpPost]
    [ProducesResponseType(typeof(PrescriptionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PrescriptionDto>> Create(
        [FromBody] PrescriptionDto dto)
    {
        try
        {
            var prescription = new Prescription
            {
                ConsultationId = dto.ConsultationId,
                PatientId = dto.PatientId,
                DoctorId = dto.DoctorId,
                MedicationName = dto.MedicationName,
                Dosage = dto.Dosage,
                Frequency = dto.Frequency,
                Duration = dto.Duration,
                Instructions = dto.Instructions,
                PrescribedAtUtc = dto.PrescribedAtUtc
            };

            var created =
                await _prescriptionService.CreateAsync(
                    prescription);

            var response = MapToDto(created);

            return CreatedAtAction(
                nameof(GetById),
                new { id = created.Id },
                response);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                status = 400,
                message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new
            {
                status = 409,
                message = ex.Message
            });
        }
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] PrescriptionDto dto)
    {
        if (id <= 0)
        {
            return BadRequest(new
            {
                status = 400,
                message = "A valid prescription ID is required."
            });
        }

        if (dto.Id != 0 && dto.Id != id)
        {
            return BadRequest(new
            {
                status = 400,
                message = "The route prescription ID does not match the request body."
            });
        }

        try
        {
            var prescription = new Prescription
            {
                Id = id,
                ConsultationId = dto.ConsultationId,
                PatientId = dto.PatientId,
                DoctorId = dto.DoctorId,
                MedicationName = dto.MedicationName,
                Dosage = dto.Dosage,
                Frequency = dto.Frequency,
                Duration = dto.Duration,
                Instructions = dto.Instructions,
                PrescribedAtUtc = dto.PrescribedAtUtc
            };

            await _prescriptionService.UpdateAsync(
                prescription);

            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                status = 400,
                message = ex.Message
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new
            {
                status = 404,
                message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new
            {
                status = 409,
                message = ex.Message
            });
        }
    }

    private static PrescriptionDto MapToDto(
        Prescription prescription)
    {
        return new PrescriptionDto
        {
            Id = prescription.Id,
            ConsultationId = prescription.ConsultationId,
            PatientId = prescription.PatientId,
            DoctorId = prescription.DoctorId,
            MedicationName = prescription.MedicationName,
            Dosage = prescription.Dosage,
            Frequency = prescription.Frequency,
            Duration = prescription.Duration,
            Instructions = prescription.Instructions,
            PrescribedAtUtc = prescription.PrescribedAtUtc,
            PatientName = prescription.Patient?.FullName,
            DoctorName = prescription.Doctor?.FullName
        };
    }
}