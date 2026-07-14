using System.ComponentModel.DataAnnotations;

namespace API.DTOs;

public class UpdateLanguageDto
{
    [Required] public string Language { get; set; } = "en";
}
