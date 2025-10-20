using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bucking_Unit_App.Models.InspectionWorkModels
{
    public class WorkFrequency
    {
        public int Id { get; set; }
        public string Type { get; set; }  // ТО1 и т.д.
        public string Frequency { get; set; }
        public int? IntervalDay { get; set; }
        public int? IntervalHour { get; set; }
    }
}
