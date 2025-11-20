using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ContractClaims.Models;

namespace ContractClaims.Models
{
    public enum ClaimStatus
    {
        Pending,
        Approved,
        Rejected
    }

    public class LecturerClaim
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string LecturerId { get; set; }

        [ForeignKey(nameof(LecturerId))]
        public ApplicationUser Lecturer { get; set; }

        [Required]
        public DateTime DateSubmitted { get; set; } = DateTime.UtcNow;

        [Range(0, 1000)]
        public decimal HoursWorked { get; set; }

        [Range(0, 10000)]
        public decimal HourlyRate { get; set; }

        [DataType(DataType.Currency)]
        public decimal TotalPayment => HoursWorked * HourlyRate;

        public string Notes { get; set; }

        public ClaimStatus Status { get; set; } 

        public string DocumentPath { get; set; } // relative wwwroot path to uploaded file

        public string ReviewedById { get; set; }
        public DateTime? ReviewedAt { get; set; }
    }
}





