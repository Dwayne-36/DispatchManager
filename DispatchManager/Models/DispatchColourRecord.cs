using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DispatchManager.Models
{
    public class DispatchColourRecord
    {
        public Guid LinkID { get; set; }
        public string ProdInputColor { get; set; }
        public string MaterialsOrderedColor { get; set; }
        public string ReleasedToFactoryColor { get; set; }
        public string MainContractorColor { get; set; }
        public string FreightColor { get; set; }
        public string AmountColor { get; set; }
        public string ProjectNameColor { get; set; }


    }
}


