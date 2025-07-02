using System;

namespace DispatchManager.Models
{
    public class DispatchRecord
    {
        public Guid ID { get; set; }
        public int WeekNo { get; set; }
        public DateTime DispatchDate { get; set; }
        public DateTime? MaterialsOrderedBy { get; set; }
        public DateTime? BenchtopOrderedBy { get; set; }
        public string Day { get; set; }
        public int JobNo { get; set; }
        public string ProdInput { get; set; }
        public string MaterialsOrdered { get; set; }
        public string ReleasedToFactory { get; set; }
        public string MainContractor { get; set; }
        public string ProjectName { get; set; }
        public string ProjectColour { get; set; }
        public int Qty { get; set; }
        public bool FB { get; set; }
        public bool EB { get; set; }
        public bool ASS { get; set; }
        public string BoardETA { get; set; }
        public string Installed { get; set; }
        public string Freight { get; set; }
        public string BenchTopSupplier { get; set; }
        public string BenchTopColour { get; set; }
        public string Installer { get; set; }
        public string Comment { get; set; }
        public string DeliveryAddress { get; set; }
        public string Phone { get; set; }
        public string M3 { get; set; }
        public decimal Amount { get; set; }
        public int OrderNumber { get; set; }
        public DateTime DateOrdered { get; set; }
        public string LeadTime { get; set; }
        public string ProdInputColor { get; set; }
        public string MaterialsOrderedColor { get; set; }
        public string ReleasedtoFactoryColor { get; set; }
        public string MainContractorColor { get; set; }
        public string ProjectNameColor { get; set; }
        public string FreightColor { get; set; }
        public string AmountColor { get; set; }

    }
    public class DispatchBlankRow : DispatchRecord
    {
        public bool IsSpacer { get; set; } = true;

        public DispatchBlankRow()
        {
            ID = Guid.NewGuid();  // Prevent binding issues
            WeekNo = 0;
            DispatchDate = DateTime.MinValue;
            MaterialsOrderedBy = null;
            BenchtopOrderedBy = null;
            Day = "";
            JobNo = 0;
            ProdInput = "";
            MaterialsOrdered = "";
            ReleasedToFactory = "";
            MainContractor = "";
            ProjectName = "";
            ProjectColour = "";
            Qty = 0;
            FB = false;
            EB = false;
            ASS = false;
            BoardETA = "";
            Installed = "";
            Freight = "";
            BenchTopSupplier = "";
            BenchTopColour = "";
            Installer = "";
            Comment = "";
            DeliveryAddress = "";
            Phone = "";
            M3 = "";
            Amount = 0;
            OrderNumber = 0;
            DateOrdered = DateTime.MinValue;
            LeadTime = "";
        }
    }


}
