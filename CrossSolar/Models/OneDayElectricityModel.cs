using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CrossSolar.Models;

namespace CrossSolar.Domain
{
    public class OneDayElectricityModel
    {

        public decimal Sum { get; set; }

        public decimal Average { get; set; }

        public decimal Maximum { get; set; }

        public decimal Minimum { get; set; }

        public DateTime DateTime { get; set; }
    }
}
