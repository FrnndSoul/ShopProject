using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Security.Cryptography;
using System.Data.SqlClient;
using MySql.Data.MySqlClient;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

namespace ShopProject
{
    public partial class LoginForm : Form        
    {
        public static string mysqlcon = "server=localhost;user=root;database=cafe;password=";
        public MySqlConnection connection = new MySqlConnection(mysqlcon);
        public static string dataID;
        public LoginForm()
        {
            InitializeComponent();
            FormBorderStyle = FormBorderStyle.FixedSingle;
        }
        
        private void button1_Click(object sender, EventArgs e)
        {
            string user = Username.Text;
            string pass = Password.Text;

            if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
            {
                MessageBox.Show("Please input complete credentials","Warning");
                return;
            }
            try
            {
                using (MySqlConnection connection = new MySqlConnection(mysqlcon))
                {
                    connection.Open();

                    string checkUsernameQuery = "SELECT `Surname`, `FirstName`, `ID`, `Email`, `Status`, `Position`, `PassHash`, `SaltHash`, `UserSaltHash` FROM users WHERE Username = @Username";

                    using (MySqlCommand checkUsernameCommand = new MySqlCommand(checkUsernameQuery, connection))
                    {
                        checkUsernameCommand.Parameters.AddWithValue("@Username", user);

                        using (MySqlDataReader reader = checkUsernameCommand.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string sname = reader["Surname"].ToString();
                                string fname = reader["FirstName"].ToString();
                                string id = reader["ID"].ToString();
                                string email = reader["Email"].ToString();
                                string status = reader["Status"].ToString();
                                string position = reader["Position"].ToString();
                                string ph = reader["PassHash"].ToString();
                                string sh = reader["SaltHash"].ToString();
                                string ush = reader["UserSaltHash"].ToString();

                                string passHash = HashString(pass);
                                string saltHash = SaltHashString(pass);
                                string userSaltHash = UserHashString(pass);
                                dataID = id;

                                if (string.Equals(status, "active", StringComparison.OrdinalIgnoreCase))
                                {

                                    if (passHash == ph && saltHash == sh && userSaltHash == ush)
                                    {
                                        MessageBox.Show("Login success");
                                        ResetPUK(user);
                                        Clear();
                                        return;
                                    }
                                    else
                                    {
                                        HandleIncorrectPassword(user);
                                        Clear();
                                    }
                                }
                                else
                                {
                                    MessageBox.Show("Account is not activated", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    Clear();
                                }
                            }
                            else
                            {
                                MessageBox.Show("Username not found", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                Clear();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Clear();
            }
            finally
            {
                connection.Close();
            }
        }
        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {

        }
        private void HandleIncorrectPassword(string username)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(mysqlcon))
                {
                    connection.Open();

                    string getAttemptQuery = "SELECT Attempts FROM users WHERE Username = @Username";

                    using (MySqlCommand getAttemptCommand = new MySqlCommand(getAttemptQuery, connection))
                    {
                        getAttemptCommand.Parameters.AddWithValue("@Username", username);

                        object attemptObj = getAttemptCommand.ExecuteScalar();
                        int currentAttempt = (attemptObj != null) ? Convert.ToInt32(attemptObj) : 0;

                        currentAttempt++;

                        string updateAttemptQuery = "UPDATE users SET Attempt = @Attempts WHERE Username = @Username";

                        using (MySqlCommand updateAttemptCommand = new MySqlCommand(updateAttemptQuery, connection))
                        {
                            updateAttemptCommand.Parameters.AddWithValue("@Attempt", currentAttempt);
                            updateAttemptCommand.Parameters.AddWithValue("@Username", username);

                            updateAttemptCommand.ExecuteNonQuery();
                        }
                        if (currentAttempt >= 3)
                        {
                            LockAccount(username);
                            MessageBox.Show("Account locked due to too many incorrect login attempts.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                        else
                        {
                            MessageBox.Show($"Incorrect password attempt {currentAttempt} out of 3.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void LockAccount(string username)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(mysqlcon))
                {
                    connection.Open();
                    string lockAccountQuery = "UPDATE users SET Status = 'Locked' WHERE Username = @Username";
                    using (MySqlCommand lockAccountCommand = new MySqlCommand(lockAccountQuery, connection))
                    {
                        lockAccountCommand.Parameters.AddWithValue("@Username", username);
                        lockAccountCommand.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void ResetPUK(string username)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(mysqlcon))
                {
                    connection.Open();

                    string getAttemptQuery = "SELECT Attempts FROM users WHERE Username = @Username";

                    using (MySqlCommand getAttemptCommand = new MySqlCommand(getAttemptQuery, connection))
                    {
                        getAttemptCommand.Parameters.AddWithValue("@Username", username);

                        int currentAttempt = 0;

                        string updateAttemptQuery = "UPDATE users SET Attempts = @Attempt WHERE Username = @Username";

                        using (MySqlCommand updateAttemptCommand = new MySqlCommand(updateAttemptQuery, connection))
                        {
                            updateAttemptCommand.Parameters.AddWithValue("@Attempt", currentAttempt);
                            updateAttemptCommand.Parameters.AddWithValue("@Username", username);

                            updateAttemptCommand.ExecuteNonQuery();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void Clear()
        {
            Username.Text = "";
            Password.Text = "";
            showPass.CheckState.Equals(false);
        }
        public static string HashString(string input)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(input);
                byte[] hashBytes = sha256.ComputeHash(inputBytes);
                string hashedString = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
                return hashedString;
            }
        }
        public static string SaltHashString(string input)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes("maid" + input + "cafe");
                byte[] hashBytes = sha256.ComputeHash(inputBytes);
                string hashedString = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
                return hashedString;
            }
        }
        public static string UserHashString(string input)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(input + dataID);
                byte[] hashBytes = sha256.ComputeHash(inputBytes);
                string hashedString = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
                return hashedString;
            }
        }

    }
}
