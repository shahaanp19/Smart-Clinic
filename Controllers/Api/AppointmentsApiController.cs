using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartClinicManagementSystem.DTOs;
using SmartClinicManagementSystem.Models;
using SmartClinicManagementSystem.Services.Interfaces;

namespace SmartClinicManagementSystem.Controllers.Api;

[ApiController]
[Route("api/appointments")]
[Authorize(Policy = "StaffOnly")]
public class AppointmentsApiController : ControllerBase
{
    private readonly IAppointmentService _appointmentService;

    public AppointmentsApiController(
        IAppointmentService appointmentService)
    {
        _appointmentService = appointmentService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<AppointmentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<AppointmentDto>>> GetAll()
    {
        var appointments = await _appointmentService.GetAllAsync();

        return Ok(appointments.Select(MapToDto));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(AppointmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AppointmentDto>> GetById(int id)
    {
        if (id <= 0)
        {
            return BadRequest(new
            {
                status = 400,
                message = "A valid appointment ID is required."
            });
        }

        var appointment = await _appointmentService.GetByIdAsync(id);

        if (appointment is null)
        {
            return NotFound(new
            {
                status = 404,
                message = "The appointment could not be found."
            });
        }

        return Ok(MapToDto(appointment));
    }

    [HttpGet("doctor/{doctorId:int}")]
    [ProducesResponseType(typeof(IEnumerable<AppointmentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<AppointmentDto>>> GetByDoctor(
        int doctorId,
        [FromQuery] DateTime fromUtc,
        [FromQuery] DateTime toUtc)
    {
        try
        {
            var appointments =
                await _appointmentService.GetByDoctorAsync(
                    doctorId,
                    fromUtc,
                    toUtc);

            return Ok(appointments.Select(MapToDto));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                status = 400,
                message = ex.Message
            });
        }
    }

    [HttpGet("patient/{patientId:int}")]
    [ProducesResponseType(typeof(IEnumerable<AppointmentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<AppointmentDto>>> GetByPatient(
        int patientId,
        [FromQuery] DateTime fromUtc,
        [FromQuery] DateTime toUtc)
    {
        try
        {
            var appointments =
                await _appointmentService.GetByPatientAsync(
                    patientId,
                    fromUtc,
                    toUtc);

            return Ok(appointments.Select(MapToDto));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                status = 400,
                message = ex.Message
            });
        }
    }

    [HttpGet("availability")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> CheckAvailability(
        [FromQuery] int doctorId,
        [FromQuery] DateTime appointmentDateTimeUtc,
        [FromQuery] int? excludeAppointmentId = null)
    {
        if (doctorId <= 0)
        {
            return BadRequest(new
            {
                status = 400,
                message = "A valid doctor ID is required."
            });
        }

        if (appointmentDateTimeUtc == default)
        {
            return BadRequest(new
            {
                status = 400,
                message = "A valid appointment date and time is required."
            });
        }

        var available =
            await _appointmentService.IsSlotAvailableAsync(
                doctorId,
                appointmentDateTimeUtc,
                excludeAppointmentId);

        return Ok(new
        {
            doctorId,
            appointmentDateTimeUtc,
            available
        });
    }

    [HttpPost]
    [ProducesResponseType(typeof(AppointmentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AppointmentDto>> Create(
        [FromBody] AppointmentDto dto)
    {
        try
        {
            var appointment = new Appointment
            {
                PatientId = dto.PatientId,
                DoctorId = dto.DoctorId,
                AppointmentDateTime = dto.AppointmentDateTime,
                Status = dto.Status,
                Reason = dto.Reason,
                Notes = dto.Notes
            };

            var created =
                await _appointmentService.CreateAsync(appointment);

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
        [FromBody] AppointmentDto dto)
    {
        if (id <= 0)
        {
            return BadRequest(new
            {
                status = 400,
                message = "A valid appointment ID is required."
            });
        }

        if (dto.Id != 0 && dto.Id != id)
        {
            return BadRequest(new
            {
                status = 400,
                message = "The route appointment ID does not match the request body."
            });
        }

        try
        {
            var appointment = new Appointment
            {
                Id = id,
                PatientId = dto.PatientId,
                DoctorId = dto.DoctorId,
                AppointmentDateTime = dto.AppointmentDateTime,
                Status = dto.Status,
                Reason = dto.Reason,
                Notes = dto.Notes
            };

            await _appointmentService.UpdateAsync(appointment);

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

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel(int id)
    {
        if (id <= 0)
        {
            return BadRequest(new
            {
                status = 400,
                message = "A valid appointment ID is required."
            });
        }

        try
        {
            var cancelled =
                await _appointmentService.CancelAsync(id);

            if (!cancelled)
            {
                return NotFound(new
                {
                    status = 404,
                    message = "The appointment could not be found."
                });
            }

            return NoContent();
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

    private static AppointmentDto MapToDto(
        Appointment appointment)
    {
        return new AppointmentDto
        {
            Id = appointment.Id,
            PatientId = appointment.PatientId,
            DoctorId = appointment.DoctorId,
            AppointmentDateTime = appointment.AppointmentDateTime,
            Status = appointment.Status,
            Reason = appointment.Reason,
            Notes = appointment.Notes,
            PatientName = appointment.Patient?.FullName,
            DoctorName = appointment.Doctor?.FullName
        };
    }
}