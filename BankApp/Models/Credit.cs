using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Kurs_Bank.Models
{
    public class Credit
    {
        [Key]
        public int CreditID { get; set; }

        [Required]
        [StringLength(10)]
        [ForeignKey("Client")]
        public string PassportNumber { get; set; }

        [Required]
        [ForeignKey("Account")]
        public int AccountID { get; set; }

        [Required]
        [Column(TypeName = "decimal(15,2)")]
        public decimal Amount { get; set; }

        [Required]
        [Column(TypeName = "decimal(5,2)")]
        public decimal InterestRate { get; set; }

        [Required]
        public string Status { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        public virtual Client Client { get; set; }
        public virtual Account Account { get; set; }
        public virtual ICollection<CreditPayment> CreditPayments { get; set; }
    }
}