using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;
using MySql.Data;
using MySql.Data.MySqlClient;
using System.Data;
using Org.BouncyCastle.Crypto.Generators;
using BCrypt.Net;
using System.Web.Script.Services;
using System.ComponentModel;
using System.Web.Http;
using System.Configuration;
using System.Data.SqlClient;


namespace ProjectTemplate
{
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    [System.Web.Script.Services.ScriptService]

    public class ProjectServices : System.Web.Services.WebService
    {
        ////////////////////////////////////////////////////////////////////////
        ///replace the values of these variables with your database credentials
        ////////////////////////////////////////////////////////////////////////
        private string dbID = "spring2024team5";
        private string dbPass = "spring2024team5";
        private string dbName = "spring2024team5";
        ////////////////////////////////////////////////////////////////////////

        ////////////////////////////////////////////////////////////////////////
        ///call this method anywhere that you need the connection string!
        ////////////////////////////////////////////////////////////////////////
        private string getConString()
        {
            return "SERVER=107.180.1.16; PORT=3306; DATABASE=" + dbName + "; UID=" + dbID + "; PASSWORD=" + dbPass;
        }
        ////////////////////////////////////////////////////////////////////////



        /////////////////////////////////////////////////////////////////////////
        //don't forget to include this decoration above each method that you want
        //to be exposed as a web service!
        [WebMethod(EnableSession = true)]
        /////////////////////////////////////////////////////////////////////////
        public string TestConnection()
        {
            try
            {
                string testQuery = "select * from test";

                ////////////////////////////////////////////////////////////////////////
                ///here's an example of using the getConString method!
                ////////////////////////////////////////////////////////////////////////
                MySqlConnection con = new MySqlConnection(getConString());
                ////////////////////////////////////////////////////////////////////////

                MySqlCommand cmd = new MySqlCommand(testQuery, con);
                MySqlDataAdapter adapter = new MySqlDataAdapter(cmd);
                DataTable table = new DataTable();
                adapter.Fill(table);
                return "Success!";
            }
            catch (Exception e)
            {
                return "Something went wrong, please check your credentials and db name and try again.  Error: " + e.Message;
            }
        }



        /// <summary>
        /// Logs in a user
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="pass"></param>
        /// <returns>bool</returns>
        [WebMethod(EnableSession = true)]
        public bool LogOn(string uid, string pass)
        {
            bool success = false;

            string sqlConnectString = getConString();

            // Adjusted to also select the username
            string sqlSelect = "SELECT id, userid FROM users WHERE userid=@idValue and pass=@passValue";

            MySqlConnection sqlConnection = new MySqlConnection(sqlConnectString);
            MySqlCommand sqlCommand = new MySqlCommand(sqlSelect, sqlConnection);

            sqlCommand.Parameters.AddWithValue("@idValue", HttpUtility.UrlDecode(uid));
            sqlCommand.Parameters.AddWithValue("@passValue", HttpUtility.UrlDecode(pass));

            MySqlDataAdapter sqlDa = new MySqlDataAdapter(sqlCommand);

            DataTable sqlDt = new DataTable();

            sqlDa.Fill(sqlDt);

            if (sqlDt.Rows.Count > 0)
            {
                // Store both the ID and username in the session
                Session["id"] = sqlDt.Rows[0]["id"];
                Session["username"] = sqlDt.Rows[0]["userid"]; // Assumes 'username' column exists
                success = true;
            }

            return success;
        }

        [WebMethod(EnableSession = true)]
        public bool IsUserLoggedIn()
        {
            return Session["username"] != null;
        }

        [WebMethod(EnableSession = true)]
        public void Logout()
        {
            Session["username"] = null; // Clear the specific session variable
                                        // Or, to remove all session data:
                                        // Session.Clear();
            Session.Abandon(); // This will destroy the session altogether
        }


        /////////////////////// Social Media ////////////////////////////////////
        /// <summary>
        [WebMethod(EnableSession = true)]
        public string GetUsername()
        {
            if (Session["username"] != null)
            {
                return Session["username"].ToString();
            }
            else
            {
                return "Not logged in";
            }
        }

        /// </summary>
        public class Post
        {
            public int PostId { get; set; }
            public string Username { get; set; }
            public string Tag { get; set; }
            public string Content { get; set; }
            public int Votes {  get; set; }
        }


        // This could be replaced with actual database logic
        private static List<Post> posts = new List<Post>();

        public Post FindPostById(int postId)
        {
            Post foundPost = null;

            using (var connection = new MySqlConnection(getConString()))
            {
                string query = "SELECT idexec_posts, username, post_tag, post_content FROM exec_posts WHERE idexec_posts = @PostId";

                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@PostId", postId);

                    try
                    {
                        connection.Open();
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                foundPost = new Post()
                                {
                                    PostId = Convert.ToInt32(reader["PostId"]),
                                    Username = reader["Username"].ToString(),
                                    Tag = reader["Tag"].ToString(),
                                    Content = reader["Content"].ToString()
                                };
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log or handle the exception as needed
                        Console.WriteLine("An error occurred: " + ex.Message);
                    }
                }
            }

            return foundPost;
        }

        [WebMethod(EnableSession = true)]
        public string EditPost(int postId, string newContent)
        {
            if (Session["username"] == null)
            {
                return "User not logged in.";
            }

            string sessionUsername = Session["username"].ToString();
            try
            {
                using (MySqlConnection connection = new MySqlConnection(getConString()))
                {
                    string query = "UPDATE exec_posts SET post_content = @Content WHERE idexec_posts = @PostId AND username = @Username";
                    MySqlCommand command = new MySqlCommand(query, connection);
                    command.Parameters.AddWithValue("@Content", newContent);
                    command.Parameters.AddWithValue("@PostId", postId);
                    command.Parameters.AddWithValue("@Username", sessionUsername);

                    connection.Open();
                    int result = command.ExecuteNonQuery();

                    if (result > 0)
                    {
                        return "Success";
                    }
                    else
                    {
                        return "Post not found or user mismatch.";
                    }
                }
            }
            catch (Exception ex)
            {
                return "Error: " + ex.Message;
            }
        }


        [WebMethod(EnableSession = true)]
        public string DeletePost(int postId)
        {
            if (Session["username"] == null)
            {
                return "User not logged in.";
            }

            string sessionUsername = Session["username"].ToString();
            try
            {
                using (MySqlConnection connection = new MySqlConnection(getConString()))
                {
                    string query = "DELETE FROM exec_posts WHERE idexec_posts = @PostId AND username = @Username";
                    MySqlCommand command = new MySqlCommand(query, connection);
                    command.Parameters.AddWithValue("@PostId", postId);
                    command.Parameters.AddWithValue("@Username", sessionUsername);

                    connection.Open();
                    int result = command.ExecuteNonQuery();

                    if (result > 0)
                    {
                        return "Success";
                    }
                    else
                    {
                        return "Post not found or user mismatch.";
                    }
                }
            }
            catch (Exception ex)
            {
                return "Error: " + ex.Message;
            }
        }


        [WebMethod(EnableSession = true)]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
       public List<Post> GetPosts(string category = null)
{
    List<Post> posts = new List<Post>();

    try
    {
        using (MySqlConnection conn = new MySqlConnection(getConString()))
        {
            conn.Open();
            string query = "SELECT idexec_posts, username, post_tag, post_content, votes FROM exec_posts";
            
            // Add a WHERE clause if a specific category is requested
            if (!string.IsNullOrEmpty(category) && category != "All")
            {
                query += " WHERE post_tag = @Category";
            }

            query += " ORDER BY votes DESC";

            using (MySqlCommand cmd = new MySqlCommand(query, conn))
            {
                // Add the category parameter if filtering by category
                if (!string.IsNullOrEmpty(category) && category != "All")
                {
                    cmd.Parameters.AddWithValue("@Category", category);
                }

                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        posts.Add(new Post
                        {
                            PostId = Convert.ToInt32(reader["idexec_posts"]),
                            Username = reader["username"].ToString(),
                            Tag = reader["post_tag"].ToString(),
                            Content = reader["post_content"].ToString(),
                            Votes = Convert.ToInt32(reader["votes"])
                        });
                    }
                }
            }
        }
    }
    catch (Exception ex)
    {
        // Log or handle the exception appropriately
        Console.WriteLine("Error fetching posts: " + ex.Message);
    }

    return posts;
}



        [WebMethod(EnableSession = true)]
        public string CreatePost(string tag, string content)
        {
            string username = Convert.ToString(Session["Username"]);
            if (string.IsNullOrEmpty(username))
            {
                return "User not logged in.";
            }
            try
            {
                using (MySqlConnection conn = new MySqlConnection(getConString()))
                {
                    conn.Open();
                    
                    string query = "INSERT INTO exec_posts (username, post_tag, post_content) VALUES (@Username, @Tag, @Content)";
                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Username", username);
                        cmd.Parameters.AddWithValue("@Tag", tag);
                        cmd.Parameters.AddWithValue("@Content", content);

                        int result = cmd.ExecuteNonQuery();
                        return result > 0 ? "Success" : "Failure";
                    }
                }
            }
            catch (Exception ex)
            {
                // Log or handle exception
                return "Error: " + ex.Message;
            }
        }

        [WebMethod(EnableSession = true)]
        public string VotePost(int postId, int vote)
        {
            // Check if the user is authenticated
            if (Session["username"] == null)
            {
                return "User not authenticated";
            }

            // Validate vote value
            if (vote != -1 && vote != 0 && vote != 1)
            {
                return "Invalid vote value";
            }

            string result = "Success";

            try
            {

                string username = Session["username"].ToString();

                // Fetch the user's ID using their username
                int userId = GetUserIdByUsername(username);

                if (userId == -1)
                {
                    return "User not found";
                }

                using (MySqlConnection connection = new MySqlConnection(getConString()))
                {
                    connection.Open();

                    // Check if the post exists
                    string checkPostQuery = "SELECT COUNT(*) FROM exec_posts WHERE idexec_posts = @PostId";
                    using (MySqlCommand checkPostCommand = new MySqlCommand(checkPostQuery, connection))
                    {
                        checkPostCommand.Parameters.Add("@PostId", MySqlDbType.Int32).Value = postId;
                        int postCount = Convert.ToInt32(checkPostCommand.ExecuteScalar());
                        if (postCount == 0)
                        {
                            return "Post not found";
                        }
                    }

                    // Remove any existing vote by the user for this post
                    string removeVoteQuery = "DELETE FROM post_votes WHERE user_id = @UserId AND post_id = @PostId";
                    using (MySqlCommand removeVoteCommand = new MySqlCommand(removeVoteQuery, connection))
                    {
                        removeVoteCommand.Parameters.Add("@UserId", MySqlDbType.Int32).Value = userId;
                        removeVoteCommand.Parameters.Add("@PostId", MySqlDbType.Int32).Value = postId;
                        removeVoteCommand.ExecuteNonQuery();
                    }

                    // Insert new vote if it's not zero
                    if (vote != 0)
                    {
                        // Record the user's vote
                        string recordVoteQuery = "INSERT INTO post_votes (user_id, post_id, vote) VALUES (@UserId, @PostId, @Vote)";
                        using (MySqlCommand recordVoteCommand = new MySqlCommand(recordVoteQuery, connection))
                        {
                            recordVoteCommand.Parameters.Add("@UserId", MySqlDbType.Int32).Value = userId;
                            recordVoteCommand.Parameters.Add("@PostId", MySqlDbType.Int32).Value = postId;
                            recordVoteCommand.Parameters.Add("@Vote", MySqlDbType.Int32).Value = vote;
                            recordVoteCommand.ExecuteNonQuery();
                        }
                    }

                    // Update the votes for the post
                    string updateVotesQuery = "UPDATE exec_posts SET votes = (SELECT SUM(vote) FROM post_votes WHERE post_id = @PostId) WHERE idexec_posts = @PostId";
                    using (MySqlCommand updateVotesCommand = new MySqlCommand(updateVotesQuery, connection))
                    {
                        updateVotesCommand.Parameters.Add("@PostId", MySqlDbType.Int32).Value = postId;
                        updateVotesCommand.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                result = "Error: " + ex.Message;
            }

            return result;
        }



        // Helper method to fetch user ID by username
        private int GetUserIdByUsername(string username)
        {
            int userId = -1;
            using (MySqlConnection connection = new MySqlConnection(getConString()))
            {
                connection.Open();
                string query = "SELECT id FROM users WHERE userId = @Username";
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Username", username);
                    object result = command.ExecuteScalar();
                    if (result != null)
                    {
                        userId = Convert.ToInt32(result);
                    }
                }
            }
            return userId;
        }




        //////////////////////////// End Social Media //////////////////////////////////

        public class SurveyResponse
        {
            public string Category { get; set; }
            public string Response { get; set; }

        }

        // New method to handle survey submission
        [WebMethod(EnableSession = true)]
        public string SubmitSurvey(List<SurveyResponse> responses)
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(getConString()))
                {
                    con.Open();
                    
                    foreach (var entry in responses)
                    {
                        string query = "INSERT INTO survey_responses (category, response) VALUES (@category, @response)";
                        MySqlCommand cmd = new MySqlCommand(query, con);

                        // Use dictionary key-value pair to set parameters
                        cmd.Parameters.AddWithValue("@category", entry.Category);
                        cmd.Parameters.AddWithValue("@response", entry.Response);

                        cmd.ExecuteNonQuery();
                    }

                    con.Close();
                    return "Survey responses recorded successfully.";
                }
            }
            catch (Exception e)
            {
                // Log the exception for troubleshooting
                Console.WriteLine("Error in SubmitSurvey: " + e.ToString());
                return "Error in SubmitSurvey: " + e.Message;
            }
        }


        /// <summary>
        /// Displays a list of survey responses
        /// </summary>
        /// <returns>a list of survey responses</returns>
        [WebMethod(EnableSession = true)]
        [ScriptMethod(ResponseFormat = ResponseFormat.Xml)]
        public List<SurveyResponse> GetSurveyResponses()
        {
            List<SurveyResponse> surveyResponses = new List<SurveyResponse>();

            try
            {
                using (MySqlConnection con = new MySqlConnection(getConString()))
                {
                    con.Open();
                    string query = "SELECT category, response FROM survey_responses";
                    MySqlCommand cmd = new MySqlCommand(query, con);

                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            SurveyResponse response = new SurveyResponse
                            {
                                Category = reader["category"].ToString(),
                                Response = reader["response"].ToString()
                            };
                            surveyResponses.Add(response);
                        }
                    }
                    con.Close();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error in GetSurveyResponses: " + e.Message);
            }

            return surveyResponses;
        }

        private void DropTable(MySqlConnection con)
        {
            //string testQuery = "CREATE TABLE IF NOT EXISTS users (username VARCHAR(10), password VARCHAR(10))";
            string testQuery = "drop table if exists registeredUsers;";
            try
            {

                MySqlCommand cmd = new MySqlCommand(testQuery, con);

                MySqlDataAdapter adapter = new MySqlDataAdapter(cmd);
                DataTable table = new DataTable();
                adapter.Fill(table);

            }
            catch (Exception e)
            {
                throw new Exception("Unable to create User table !");
            }
        }

        ////////////////////////////////////////////////////////////////////////
        ///Helper method to create users table if not exists
        ////////////////////////////////////////////////////////////////////////
        private void CreateUsersTable(MySqlConnection con)
        {
            //string testQuery = "CREATE TABLE IF NOT EXISTS users (username VARCHAR(10), password VARCHAR(10))";
            string testQuery = "Create table if not exists registeredUsers(username varchar(10) NOT NULL UNIQUE, password varchar(10));";
            try
            {

                MySqlCommand cmd = new MySqlCommand(testQuery, con);

                MySqlDataAdapter adapter = new MySqlDataAdapter(cmd);
                DataTable table = new DataTable();
                adapter.Fill(table);

            }
            catch (Exception e)
            {
                throw new Exception("Unable to create User table !");
            }
        }

        ////////////////////////////////////////////////////////////////////////
        ///Helper method to check if the user name already exists
        ////////////////////////////////////////////////////////////////////////
        private bool CheckIfUserNameAlreadyExists(MySqlConnection con, string username)
        {
            string testQuery = "SELECT * FROM registeredUsers WHERE username = @Username;";
            try
            {
                MySqlCommand cmd = new MySqlCommand(testQuery, con);
                // Use parameterized queries to prevent SQL injection
                cmd.Parameters.AddWithValue("@Username", username);

                MySqlDataAdapter adapter = new MySqlDataAdapter(cmd);
                DataTable table = new DataTable();
                adapter.Fill(table);

                // Return true if user exists (found rows), false otherwise
                return table.Rows.Count > 0;
            }
            catch (Exception e)
            {
                // Consider logging the exception details here
                // Rethrowing exceptions in this manner can be problematic; 
                // it's generally better to handle exceptions or let them bubble up naturally
                throw;
            }
        }


        /////////////////////////////////////////////////////////////////////////
        //The method is used to register a new user providing a valid user name and
        // password, where username should be unique and atleast 6 chars
        // and password should contain atleast 8 chars.
        [WebMethod(EnableSession = true)]
        public string RegisterNewUser(string username = "", string password = "")
        {
            if (username.Length < 6)
            {
                return "User name must be at least length of 6 characters!";
            }

            if (password.Length < 8)
            {
                return "Password must be at least length of 8 characters!";
            }

            try
            {
                using (MySqlConnection con = new MySqlConnection(getConString()))
                {
                    con.Open();

                    if (CheckIfUserNameAlreadyExists(con, username))
                    {
                        return "Username already exists. Please choose a different username.";
                    }

                    string insertCommand = "INSERT INTO users (userId, pass) VALUES(@Username, @Password);";

                    using (MySqlCommand cmd = new MySqlCommand(insertCommand, con))
                    {
                        cmd.Parameters.AddWithValue("@Username", username);
                        cmd.Parameters.AddWithValue("@Password", password);
                        int rowsAffected = cmd.ExecuteNonQuery();
                        if (rowsAffected > 0)
                        {
                            // Set session variable after successful registration
                            Session["username"] = username;
                            return "User registered successfully!";
                        }
                        else
                        {
                            return "An error occurred during registration.";
                        }
                    }
                }
            }
            catch (Exception e)
            {
                return "Error: " + e.Message;
            }
        }


    }
}