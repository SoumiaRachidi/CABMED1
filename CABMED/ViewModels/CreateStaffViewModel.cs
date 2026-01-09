using System.ComponentModel.DataAnnotations;

namespace CABMED.ViewModels
{
    public class CreateStaffViewModel
    {
        [Required(ErrorMessage = "Le nom est requis")]
        [StringLength(100)]
        [Display(Name = "Nom")]
        public string Nom { get; set; }

        [Required(ErrorMessage = "Le prénom est requis")]
        [StringLength(100)]
        [Display(Name = "Prénom")]
        public string Prenom { get; set; }

        [Required(ErrorMessage = "L'email est requis")]
        [EmailAddress(ErrorMessage = "Format d'email invalide")]
        [StringLength(150)]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [StringLength(20)]
        [Display(Name = "Téléphone")]
        public string Telephone { get; set; }

        [Required(ErrorMessage = "Le rôle est requis")]
        [Display(Name = "Rôle")]
        public string Role { get; set; }

        [StringLength(100)]
        [Display(Name = "Spécialité")]
        public string Specialite { get; set; }

        [Required(ErrorMessage = "Le mot de passe est requis")]
        [StringLength(255, MinimumLength = 6, ErrorMessage = "Le mot de passe doit contenir au moins 6 caractères")]
        [DataType(DataType.Password)]
        [Display(Name = "Mot de passe")]
        public string Password { get; set; }
    }
}
