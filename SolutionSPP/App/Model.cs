using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App
{
    public class Model
    {
        // статич
        public string ApplicantName { get; set; } = "Unknown";
        public int Age { get; set; }
        public decimal Income { get; set; } 
        public decimal DebtAmount { get; set; }

        // модифиц 
        public int CreditAmount { get; set; } = 650; 
        public bool IsApproved { get; set; } 
        public decimal ApprovedAmount { get; set; } 

    }
}
