using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartClinicManagementSystem.DTOs;
using SmartClinicManagementSystem.Models;
using SmartClinicManagementSystem.Services.Interfaces;

namespace SmartClinicManagementSystem.Controllers.Api;

[ApiController]
[Route("api/medical-records")]
[Authorize(Policy = "ClinicalStaff")]
public class MedicalRecordsApiController : ControllerBase
{
    private readonly IMedicalRecordService _medicalRecordService;

    public MedicalRecordsApiController(
        IMedicalRecordService medicalRecordService)
    {
        _medicalRecordService = medicalRecordService;
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(MedicalRecordDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MedicalRecordDto>> GetById(int id)
    {
        if (id <= 0)
        {
            return BadRequest(new
            {
                status = 400,
                message = "A valid medical record ID is required."
            });
        }

        var record =
            await _medicalRecordService.GetByIdAsync(id);

        if (record is null)
        {
            return NotFound(new
            {
                status = 404,
                message = "The medical record could not be found."
            });
        }

        return Ok(MapToDto(record));
    }

    [HttpGet("patient/{patientId:int}")]
    [ProducesResponseType(typeof(IEnumerable<MedicalRecordDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<MedicalRecordDto>>> GetByPatient(
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

        var records =
            await _medicalRecordService.GetByPatientAsync(patientId);

        return Ok(records.Select(MapToDto));
    }

    [HttpGet("doctor/{doctorId:int}")]
    [ProducesResponseType(typeof(IEnumerable<MedicalRecordDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<MedicalRecordDto>>> GetByDoctor(
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

        var records =
            await _medicalRecordService.GetByDoctorAsync(doctorId);

        return Ok(records.Select(MapToDto));
    }

    [HttpPost]
    [ProducesResponseType(typeof(MedicalRecordDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MedicalRecordDto>> Create(
        [FromBody] MedicalRecordDto dto)
    {
        try
        {
            var record = new MedicalRecord
            {
                PatientId = dto.PatientId,
                DoctorId = dto.DoctorId,
                RecordType = dto.RecordType,
                Description = dto.Description,
                ClinicalNotes = dto.ClinicalNotes,
                RecordedAtUtc = dto.RecordedAtUtc
            };

            var created =
                await _medicalRecordService.CreateAsync(record);

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
        [FromBody] MedicalRecordDto dto)
    {
        if (id <= 0)
        {
            return BadRequest(new
            {
                status = 400,
                message = "A valid medical record ID is required."
            });
        }

        if (dto.Id != 0 && dto.Id != id)
        {
            return BadRequest(new
            {
                status = 400,
                message = "The route medical record ID does not match the request body."
            });
        }

        try
        {
            var record = new MedicalRecord
            {
                Id = id,
                PatientId = dto.PatientId,
                DoctorId = dto.DoctorId,
                RecordType = dto.RecordType,
                Description = dto.Description,
                ClinicalNotes = dto.ClinicalNotes,
                RecordedAtUtc = dto.RecordedAtUtc
            };

            await _medicalRecordService.UpdateAsync(record);

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

    private static MedicalRecordDto MapToDto(
        MedicalRecord record)
    {
        return new MedicalRecordDto
        {
            Id = record.Id,
            PatientId = record.PatientId,
            DoctorId = record.DoctorId,
            RecordType = record.RecordType,
            Description = record.Description,
            ClinicalNotes = record.ClinicalNotes,
            RecordedAtUtc = record.RecordedAtUtc,
            PatientName = record.Patient?.FullName,
            DoctorName = record.Doctor?.FullName
        };
    }
}