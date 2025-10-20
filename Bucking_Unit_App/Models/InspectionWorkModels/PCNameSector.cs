using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bucking_Unit_App.Models.InspectionWorkModels
{
    public class PCNameSector
    {
        public int Id { get; set; }
        public string NamePC { get; set; }
        public int Sector { get; set; }
        public Sector SectorNavigation { get; set; }
    }
}
