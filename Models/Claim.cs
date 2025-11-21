using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace ContractClaims.Models
{
    public enum ClaimStatus
    {
        Pending,
        Approved,
        Rejected
    }

    public class Claim
    {
        [Key]
        public int Id { get; set; }

        // Disable validation on LecturerId completely
        [ValidateNever]
        public string LecturerId { get; set; }

        [ForeignKey(nameof(LecturerId))]
        [ValidateNever]
        public ApplicationUser Lecturer { get; set; }

        public DateTime DateSubmitted { get; set; } = DateTime.UtcNow;

        [Range(0.1, 1000)]
        public decimal HoursWorked { get; set; }

        [Range(0.1, 100000)]
        [Column(TypeName = "decimal(18,2)")]
        public decimal HourlyRate { get; set; }

        [NotMapped]
        public decimal TotalPayment => HoursWorked * HourlyRate;

        public string Notes { get; set; }

        public ClaimStatus Status { get; set; } = ClaimStatus.Pending;

        [ValidateNever]
        public string? DocumentPath { get; set; }

        [ValidateNever]
        public string? ReviewedById { get; set; }

        [ValidateNever]
        public DateTime? ReviewedAt { get; set; }
    }
}
