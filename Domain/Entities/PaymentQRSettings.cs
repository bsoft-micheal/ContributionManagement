using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TeamContributionManagementSystem.Domain.Entities
{
    /// <summary>
    /// Represents the UPI Payment & QR Code configuration per Event Type.
    /// Stores independent UPI settings for each Event Type (e.g. Birthday, Farewell, Team Dinner, Teamouting).
    /// </summary>
    [Table("PaymentQRSettings")]
    public class PaymentQRSettings
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>
        /// The Event Type name (e.g. "Birthday", "Farewell", "Team Dinner", "Teamouting").
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string EventType { get; set; } = string.Empty;

        /// <summary>
        /// Optional identifier referencing the EventType table.
        /// </summary>
        public int? EventTypeId { get; set; }

        /// <summary>
        /// Receiver / Payee Name configured for this event type (e.g. "Team Outing Lead").
        /// </summary>
        [Required]
        [MaxLength(255)]
        public string ReceiverName { get; set; } = string.Empty;

        /// <summary>
        /// UPI ID configured for this event type (e.g. "outing.unit1a@okaxis").
        /// </summary>
        [Required]
        [MaxLength(255)]
        public string UPIId { get; set; } = string.Empty;

        /// <summary>
        /// QR Code Mode: "generated" (dynamic auto-generated QR) or "uploaded" (custom uploaded image).
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string QRCodeMode { get; set; } = "generated";

        /// <summary>
        /// Uploaded custom QR code image (Base64 data URI or relative server path).
        /// </summary>
        public string? QRCodeImage { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedDate { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
