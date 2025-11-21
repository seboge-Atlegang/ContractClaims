using System.ComponentModel.DataAnnotations;

namespace ContractClaims.Models
{
    public class CreateUserVM
    {
        [Required, MaxLength(50)]
        public string FirstName { get; set; }

        [Required, MaxLength(50)]
        public string LastName { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Role { get; set; }

       
        public string Password { get; set; }

        public decimal? HourlyRate { get; set; }
    }
}
