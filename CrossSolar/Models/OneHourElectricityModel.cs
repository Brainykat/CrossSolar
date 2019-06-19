using System;
using System.ComponentModel.DataAnnotations;

namespace CrossSolar.Models
{
    public class OneHourElectricityModel
    {
        public int Id { get; set; }
		[Required]
        public decimal KiloWatt { get; set; }

        public DateTime DateTime { get; set; }
    }
}
