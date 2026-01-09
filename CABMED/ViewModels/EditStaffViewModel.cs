using System.ComponentModel.DataAnnotations;

namespace CABMED.ViewModels
{
    public class EditStaffViewModel
    {
        [Required]
        public int UserId { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Nom")]
        public string Nom { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Prénom")]
        public string Prenom { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(150)]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [StringLength(20)]
        [Display(Name = "Téléphone")]
        public string Telephone { get; set; }

        [StringLength(100)]
        [Display(Name = "Spécialité")]
        public string Specialite { get; set; }

        [Display(Name = "Rôle")]
        public string Role { get; set; }
    }
}
