using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DinnerClubPlanner.Models
{
    [Serializable]
    public class DinnerClubEvent
    {
        public DateTime Date { get; set; }
        public List<ApplicationUser> NotAttending { get; set; }
        public string Id { get; set; }
    }
}