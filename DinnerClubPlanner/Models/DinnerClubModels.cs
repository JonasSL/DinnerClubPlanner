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

    public class UserEvent
    {
        public DateTime TimeStamp { get; set; }
        public string UserName { get; set; }
        public string EventId { get; set; }
    }
}