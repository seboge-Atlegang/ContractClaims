using System.ComponentModel.DataAnnotations;

namespace ContractClaims.Models
{
    public class EditUserVM
    {
        [Required]
        public string Id { get; set; }

        [Required, MaxLength(50)]
        public string FirstName { get; set; }

        [Required, MaxLength(50)]
        public string LastName { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }

        public decimal? HourlyRate { get; set; }
    }
}
