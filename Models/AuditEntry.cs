using Humanizer;

namespace BankAuditSystem.Models
{
    public class AuditEntry
    {
        public int TransactionID { get; set; }
        public int AccountID { get; set; }
        public decimal Amount { get; set; }
        public string TransactionType { get; set; }
        public DateTime TimeStp { get; set; }
        public string RowHash { get; set; }
    }
}
