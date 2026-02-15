using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace OrderManagementSystem.Models
{
    public enum OrderStatus
    {
        Pending,
        Approved,
        Rejected
    }

    public class Order
    {

        public int Id { get; set; }

        [Required]
        [Display(Name = "Product Name")]
        public string ProductName { get; set; }

        [Required]
        public int Quantity { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        public DateTime OrderDate { get; set; } = DateTime.Now;

        //FOreign key to Identity User
        public string? CreatedByUserId { get; set; }
        // Navigation property

        [ForeignKey("CreatedByUserId")]
        public IdentityUser? CreatedByUser { get; set; }

        public OrderStatus Status { get; set; } = OrderStatus.Pending;

    }
}
