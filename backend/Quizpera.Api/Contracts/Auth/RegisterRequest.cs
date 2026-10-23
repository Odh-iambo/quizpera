using System.ComponentModel.DataAnnotations;

namespace Quizpera.Api.Contracts.Auth;

public class RegisterRequest
{
    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(10)]
    public string Password { get; set; } = string.Empty;
}
