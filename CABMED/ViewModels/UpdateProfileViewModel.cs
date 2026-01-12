using System;
using System.ComponentModel.DataAnnotations;

namespace CABMED.ViewModels
{
    public class UpdateProfileViewModel
    {
        public int UserId { get; set; }

        [Required(ErrorMessage = "Le nom est obligatoire")]
        [Display(Name = "Nom")]
        [StringLength(100)]
        public string Nom { get; set; }

        [Required(ErrorMessage = "Le prénom est obligatoire")]
        [Display(Name = "Prénom")]
        [StringLength(100)]
        public string Prenom { get; set; }

        [Required(ErrorMessage = "L'email est obligatoire")]
        [EmailAddress(ErrorMessage = "Email invalide")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Le téléphone est obligatoire")]
        [Display(Name = "Téléphone")]
        [Phone(ErrorMessage = "Numéro de téléphone invalide")]
        public string Telephone { get; set; }

        [Display(Name = "Date de naissance")]
        [DataType(DataType.Date)]
        public DateTime? DateNaissance { get; set; }

        [Display(Name = "Adresse complète")]
        [StringLength(500)]
        public string Adresse { get; set; }

        [Display(Name = "Ville")]
        [StringLength(100)]
        public string Ville { get; set; }

        [Display(Name = "Code postal")]
        [StringLength(10)]
        public string CodePostal { get; set; }

        [Display(Name = "Nom du contact d'urgence / Tuteur")]
        [StringLength(200)]
        public string GuardianName { get; set; }

        [Display(Name = "Téléphone du contact d'urgence")]
        [Phone(ErrorMessage = "Numéro de téléphone invalide")]
        [StringLength(20)]
        public string GuardianPhone { get; set; }

        [Display(Name = "Relation (parent, conjoint, ami, etc.)")]
        [StringLength(50)]
        public string GuardianRelation { get; set; }

        [Display(Name = "Antécédents médicaux")]
        [StringLength(2000)]
        [DataType(DataType.MultilineText)]
        public string AntecedentsMedicaux { get; set; }
    }
}
