using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using DinnerClubPlanner.Models;
using Microsoft.AspNet.Identity;

namespace DinnerClubPlanner.Controllers
{
    public class DinnerListController : Controller
    {

        private List<ApplicationUser> _totalUsers = new List<ApplicationUser>
        {
            new ApplicationUser
            {
                UserName = "Jonas"
            },
            new ApplicationUser
            {
                UserName = "Julie"
            }
        };

        

        // GET: DinnerList
        public ActionResult List()
        {
            ViewBag.Message = "Yo! Velkommen.";

            // Make SQL connection
            var conn = new SqlConnection();
            conn.ConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            conn.Open();

            var query = new SqlCommand("SELECT * FROM DinnerEvents", conn);

            // Read form SQL server
            var reader = query.ExecuteReader();

            var eventList = new List<DinnerClubEvent>();
            while (reader.Read())
            {
                var id = reader.GetString(0);
                var date = reader.GetDateTime(1);
                eventList.Add(new DinnerClubEvent
                {
                    Date = date,
                    NotAttending = new List<ApplicationUser>(),
                    Id = id
                });
            }
            
            reader.Close();
            
            // Query for cancellations
            foreach (var dinnerClubEvent in eventList)
            {
                // Join on the AspNetUsers table to get all information
                var cancelQuery = new SqlCommand("SELECT Cancellations.UserId, AspNetUsers.Username " +
                                                 "FROM Cancellations " +
                                                 "INNER JOIN AspNetUsers " +
                                                 "ON Cancellations.UserId=AspNetUsers.Id " +
                                                 $"AND Cancellations.EventId='{dinnerClubEvent.Id}'", conn);

                var cancelReader = cancelQuery.ExecuteReader();

                // Read from the query and add the users to the list of not attending users
                while (cancelReader.Read())
                {
                    // Check that Id is set
                    if (cancelReader.IsDBNull(0)) continue;

                    dinnerClubEvent.NotAttending.Add(new ApplicationUser
                    {
                        Id = cancelReader.GetString(0),
                        UserName = !cancelReader.IsDBNull(1) ? cancelReader.GetString(1) : null
                    });
                }

                cancelReader.Close();
            }


            // Count the total users 
            var countQuery = new SqlCommand("SELECT COUNT(*) FROM AspNetUsers", conn);
            var countReader = countQuery.ExecuteReader();

            var users = 0;
            while (countReader.Read())
            {
                users = !countReader.IsDBNull(0) ? countReader.GetInt32(0) : 0;
            }
            countReader.Close();

            conn.Close();

            var modelObj = new Tuple<List<DinnerClubEvent>, int>(eventList, users);
            return View(modelObj);
        }

        [Authorize]
        public ActionResult Confirmation(DinnerClubEvent dinnerEvent)
        {
            ViewBag.Message = "Your cancellation was successful";
            var conn = new SqlConnection();

            conn.ConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            conn.Open();

            var currentUserId = User.Identity.GetUserId();
            var sqlCommand = new SqlCommand($"INSERT INTO Cancellations (UserId,EventId) VALUES ('{currentUserId}', '{dinnerEvent.Id}')", conn);
            sqlCommand.ExecuteNonQuery();
            conn.Close();
            return View(true);
        }   
    }
}