using System.ComponentModel.DataAnnotations;

namespace CrossSolar.Models
{
    public class PanelModel
    {
        public int Id { get; set; }

        [Required]
        [Range(-90, 90)]
        [RegularExpression(@"^\d+(\.\d{6})$",ErrorMessage ="Latitude should have 6 decimal places")]
        public double Latitude { get; set; }

      
		[Required]
        [Range(-180, 180)]
		[RegularExpression(@"^\d+(\.\d{6})$", ErrorMessage = "Latitude should have 6 decimal places")]
		public double Longitude { get; set; }

        [Required]
		[StringLength(16,MinimumLength =16, ErrorMessage ="Serial should have 16 Characters")]
        public string Serial { get; set; }

        public string Brand { get; set; }

    }
}
