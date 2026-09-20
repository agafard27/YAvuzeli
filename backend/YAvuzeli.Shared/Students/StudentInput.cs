using System.ComponentModel.DataAnnotations;

namespace YAvuzeli.Shared.Students;

public sealed class StudentInput : IValidatableObject
{
    [Required(ErrorMessage = "Ad zorunludur.")]
    [StringLength(100, ErrorMessage = "Ad en fazla 100 karakter olabilir.")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Soyad zorunludur.")]
    [StringLength(100, ErrorMessage = "Soyad en fazla 100 karakter olabilir.")]
    public string LastName { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi girin.")]
    [StringLength(200, ErrorMessage = "E-posta en fazla 200 karakter olabilir.")]
    public string? Email { get; set; }

    [StringLength(30, ErrorMessage = "Telefon en fazla 30 karakter olabilir.")]
    public string? Phone { get; set; }

    [Required(ErrorMessage = "Doğum tarihi zorunludur.")]
    public DateOnly? DateOfBirth { get; set; }

    public bool IsActive { get; set; } = true;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DateOfBirth is { } date && (date < new DateOnly(1900, 1, 1) || date > DateOnly.FromDateTime(DateTime.Today)))
            yield return new ValidationResult("Doğum tarihi 1900 yılı ile bugün arasında olmalıdır.", [nameof(DateOfBirth)]);
    }
}
