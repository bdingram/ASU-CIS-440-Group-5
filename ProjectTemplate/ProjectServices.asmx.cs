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

        public class SurveyResponse
        {
            public string Category { get; set; }
            public string Response { get; set; }
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

            string sqlSelect = "SELECT id FROM users WHERE userid=@idValue and pass=@passValue";

            MySqlConnection sqlConnection = new MySqlConnection(sqlConnectString);
            MySqlCommand sqlCommand = new MySqlCommand(sqlSelect, sqlConnection);

            sqlCommand.Parameters.AddWithValue("@idValue", HttpUtility.UrlDecode(uid));
            sqlCommand.Parameters.AddWithValue("@passValue", HttpUtility.UrlDecode(pass));

            MySqlDataAdapter sqlDa = new MySqlDataAdapter(sqlCommand);

            DataTable sqlDt = new DataTable();

            sqlDa.Fill(sqlDt);

            if (sqlDt.Rows.Count > 0)
            {
                Session["id"] = sqlDt.Rows[0]["id"];
                success = true;
            }

            return success;

        }

        // New method to handle survey submission
        [WebMethod(EnableSession = true)]
        public string SubmitSurvey(Dictionary<string, string> responses)
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
                        cmd.Parameters.AddWithValue("@category", entry.Key);
                        cmd.Parameters.AddWithValue("@response", entry.Value);
                        cmd.ExecuteNonQuery();
                    }
                    con.Close();
                }
                return "Survey responses recorded successfully.";
            }
            catch (Exception e)
            {
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
            string testQuery = "select * from registeredUsers where username = \'" + username + "\';";
            try
            {
                MySqlCommand cmd = new MySqlCommand(testQuery, con);

                MySqlDataAdapter adapter = new MySqlDataAdapter(cmd);
                DataTable table = new DataTable();
                adapter.Fill(table);

                if (table.Rows.Count != 0)
                {
                    throw new Exception("User Name already exists !");
                }

                return true;
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        /////////////////////////////////////////////////////////////////////////
        //The method is used to register a new user providing a valid user name and
        // password, where username should be unique and atleast 6 chars
        // and password should contain atleast 8 chars.
        [WebMethod(EnableSession = true)]
        /////////////////////////////////////////////////////////////////////////
        public string RegisterNewUser(string username = "", string password = "")
        {
            if (username.Length < 6)
            {
                return "User name must be atleat length of 6 characters!";
            }

            if (password.Length < 8)
            {
                return "Password must be atleat length of 8 characters!";
            }

            try
            {
                string insertCommand = "INSERT INTO registeredUsers VALUES(\'" + username + "\', \'" + password + "\');";

                ////////////////////////////////////////////////////////////////////////
                ///here's an example of using the getConString method!
                ////////////////////////////////////////////////////////////////////////
                MySqlConnection con = new MySqlConnection(getConString());
                ////////////////////////////////////////////////////////////////////////

                //CreateUsersTable(con);

                if (CheckIfUserNameAlreadyExists(con, username))
                {
                    MySqlCommand cmd = new MySqlCommand(insertCommand, con);
                    MySqlDataAdapter adapter = new MySqlDataAdapter(cmd);
                    DataTable table = new DataTable();
                    adapter.Fill(table);
                }

                return "User registered successfully!";
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }

    }


}
