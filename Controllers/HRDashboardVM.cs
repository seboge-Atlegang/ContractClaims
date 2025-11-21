using ContractClaims.Models;

namespace ContractClaims.Controllers
{
    internal class HRDashboardVM
    {
        public int TotalUsers { get; set; }
        public int TotalClaims { get; set; }
        public int PendingClaims { get; set; }
        public List<Claim> RecentClaims { get; set; }
    }
}