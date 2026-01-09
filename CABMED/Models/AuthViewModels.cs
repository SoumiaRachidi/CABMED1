using System;
using System.ComponentModel.DataAnnotations;

namespace CABMED.Models
{
    public class LoginViewModel
    {
        [Required]
        [Display(Name = "Email")]
        public string Login { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Mot de passe")]
        public string Password { get; set; }
    }

    public class RegisterViewModel
    {
        [Required]
        [StringLength(100)]
        public string Nom { get; set; }

        [Required]
        [StringLength(100)]
        public string Prenom { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(150)]
        public string Email { get; set; }

        [StringLength(20)]
        public string Telephone { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 6)]
        [Display(Name = "Mot de passe")]
        public string Password { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Confirmer le mot de passe")]
        [Compare("Password")]
        public string ConfirmPassword { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Date de naissance")]
        public DateTime? DateNaissance { get; set; }

        [Display(Name = "Antécédents médicaux")]
        public string AntecedentsMedicaux { get; set; }
    }
}
