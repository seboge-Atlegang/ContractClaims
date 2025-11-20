using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace ContractClaims.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required, MaxLength(50)]
        public string FirstName { get; set; }

        [Required, MaxLength(50)]
        public string LastName { get; set; }

        // Hourly rate for lecturers (nullable for non-lecturers)
        [DataType(DataType.Currency)]
        public decimal? HourlyRate { get; set; }
    }
}
