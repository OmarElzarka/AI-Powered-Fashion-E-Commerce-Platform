using System.ComponentModel.DataAnnotations;

namespace Core.DTOs;

public class UpdateLanguageDto
{
    [Required] public string Language { get; set; } = "en";
}
