
using System.ComponentModel.DataAnnotations;
namespace JobQueue.DTOs;

public record CreateJobDto(

    [Required(AllowEmptyStrings = false, ErrorMessage = "Type is required.")]
    [StringLength(100,MinimumLength = 2)]
     string Type,

    [Required(AllowEmptyStrings = false, ErrorMessage = "Payload is required.")]
    string Payload

    );