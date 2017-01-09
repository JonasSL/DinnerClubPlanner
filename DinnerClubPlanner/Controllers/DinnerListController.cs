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

            // Order eventList by date
            eventList = eventList.OrderBy(x => x.Date).ToList();

            var modelObj = new Tuple<List<DinnerClubEvent>, int>(eventList, users);
            return View(modelObj);
        }

        [Authorize]
        public ActionResult CancelEvent(string dinnerEventId)
        {

            var conn = new SqlConnection();
            var currentUserId = User.Identity.GetUserId();

            conn.ConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            conn.Open();
            try
            {
                var sqlCommand =
                    new SqlCommand(
                        $"INSERT INTO Cancellations (UserId,EventId) VALUES ('{currentUserId}', '{dinnerEventId}')",
                        conn);
                sqlCommand.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                ViewBag.Message = "Your cancellation was NOT registered. Please try again later.";
                return View();
            }
            finally
            {
                conn.Close();
            }

            ViewBag.Message = $"Your cancellation for the dinner event on was successful.";
            return View();
        }

        /// <summary>
        /// "Cancels" the cancellation of the attendance. You now attend the event.
        /// </summary>
        /// <param name="dinnerEventId"></param>
        /// <returns></returns>
        [Authorize]
        public ActionResult AttendEvent(string dinnerEventId)
        {
            var conn = new SqlConnection();
            var currentUserId = User.Identity.GetUserId();

            conn.ConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            conn.Open();
            try
            {
                var sqlCommand =
                    new SqlCommand(
                        $"DELETE FROM Cancellations WHERE EventId='{dinnerEventId}' AND UserId='{currentUserId}'",
                        conn);
                sqlCommand.ExecuteNonQuery();
            }
            catch
            {
                ViewBag.Message = "Something went wrong. Please try again later.";
                return View();
            }
            finally
            {
                conn.Close();
            }

            ViewBag.Message = $"Success! You now attend the dinner again.";
            return View();
        }

        public ActionResult DinnerDetails(string dinnerEventId)
        {
            var conn = new SqlConnection();

            conn.ConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            conn.Open();



            #region Get cancelled users
            // Join on AspNetUsers to get user information
            var query = new SqlCommand("SELECT  AspNetUsers.Username, Cancellations.TimeStamp " +
                                       "FROM Cancellations " +
                                       "INNER JOIN AspNetUsers " +
                                       "ON Cancellations.UserId=AspNetUsers.Id " +
                                       $"AND Cancellations.EventId='{dinnerEventId}'", conn);
            // Read form SQL server
            var reader = query.ExecuteReader();

            // Read the query result
            var notAttending = new List<UserEvent>();
            while (reader.Read())
            {
                notAttending.Add(new UserEvent
                {
                    EventId = dinnerEventId,
                    TimeStamp = reader.GetDateTime(1),
                    UserName = reader.GetString(0)
                });
            }

            // Order by timestamp
            notAttending = notAttending.OrderByDescending(x => x.TimeStamp).ToList();

            // Close connections
            reader.Close();


            #endregion

            #region Get attending users

            var attendingQuery = new SqlCommand("SELECT AspNetUsers.UserName " +
                                                "FROM AspNetUsers " +
                                                "WHERE AspNetUsers.Id NOT IN" +
                                                    "(SELECT UserId " +
                                                    "FROM Cancellations " +
                                                    $"WHERE Cancellations.EventId='{dinnerEventId}')", conn);

            var attendingReader = attendingQuery.ExecuteReader();

            var attending = new List<UserEvent>();
            while (attendingReader.Read())
            {
                attending.Add(new UserEvent
                {
                    EventId = dinnerEventId,
                    UserName = attendingReader.GetString(0)
                });
            }

            attendingReader.Close();
            #endregion

            conn.Close();

            return View(new Tuple<List<UserEvent>, List<UserEvent>>(attending, notAttending));
        }
    }
}