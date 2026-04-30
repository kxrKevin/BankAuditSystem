using BankAuditSystem.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Security.Cryptography;
using System.Text;

namespace BankAuditSystem.DAO
{
    public class AuditDAO
    {
        private readonly string _connectionString;

        public AuditDAO(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("AuditDbConnection");
        }

        public string RowHash(string input)
        {
            // Convert string to bytes
            byte[] inputBytes = Encoding.UTF8.GetBytes(input);
            byte[] hashBytes = SHA256.HashData(inputBytes);
            return Convert.ToHexString(hashBytes);
        }

        public void InsertAuditEntry(AuditEntry entry)
        {

            string row_hash_input_string = entry.AccountID.ToString() + entry.Amount.ToString() + entry.TransactionType + entry.TimeStp.ToString();
            string row_hash = RowHash(row_hash_input_string);
            
            // SQL Query that inserts
            const string InsertQuery = @"INSERT INTO AuditEntry 
    (AccountID, Amount, TransactionType, TimeStp, RowHash) 
    VALUES (@AccountID, @Amount, @TransactionType, @TimeStp, @RowHash)";


            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(InsertQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@AccountID", entry.AccountID);
                    cmd.Parameters.AddWithValue("@Amount", entry.Amount);
                    cmd.Parameters.AddWithValue("@TransactionType", entry.TransactionType);
                    cmd.Parameters.AddWithValue("@TimeStp", entry.TimeStp);
                    cmd.Parameters.AddWithValue("@RowHash", row_hash); 
                    
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public decimal GetBalance(int acc_Id)
        {
            decimal currentBalance = 0;
            const string BalanceQuery = "SELECT AccountID, Amount, TransactionType FROM AuditEntry;";

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(BalanceQuery, conn))
                {
                    conn.Open();
                    
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int account_id = reader.GetInt32(0);
                            decimal amount = reader.GetDecimal(1); // Amount
                            string type = reader.GetString(2);

                            if (account_id != acc_Id)
                            {
                                continue;
                            }
                            if (type.Equals("Withdrawal", StringComparison.OrdinalIgnoreCase))
                            {
                                currentBalance -= amount;
                            }
                            else if(type.Equals("Deposit", StringComparison.OrdinalIgnoreCase))
                            {
                                currentBalance += amount;
                            }
                        }
                    }
                }
            }
            
            return currentBalance;
        }


        public List<AuditEntry> PointInTimeReport(DateTime input_time)
        {
            List<AuditEntry> results = new List<AuditEntry>();
            const string ReportQuery = "SELECT TransactionID, AccountID, Amount, TransactionType, TimeStp FROM AuditEntry " +
                                        "WHERE TimeStp <= @TargetDate ORDER BY TimeStp ASC;";

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(ReportQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@TargetDate", SqlDbType.DateTime2).Value = input_time;

                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            results.Add(new AuditEntry { 
                                TransactionID = reader.GetInt32(0), 
                                AccountID = reader.GetInt32(1),
                                Amount = reader.GetDecimal(2),
                                TransactionType = reader.GetString(3),
                                TimeStp = reader.GetDateTime(4)
                            });
                        }
                    }
                }
            }

            return results;

        }


        public List<int> Integrity_Check()
        {

            List<int> discrepancies_List = new List<int>();

            const string BalanceQuery = "SELECT TransactionID, AccountID, Amount, TransactionType, TimeStp, RowHash FROM AuditEntry;";

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(BalanceQuery, conn))
                {

                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {

                            string row_hash_input_string = 
                                reader.GetInt32(0).ToString() + 
                                reader.GetInt32(1).ToString() + 
                                reader.GetDecimal(2).ToString("0.00") +
                                reader.GetString(3) +
                                reader.GetDateTime(4).ToString("yyyy-MM-dd HH:mm:ss.fff");

                            string test_Hash = RowHash(row_hash_input_string);

                            if (test_Hash != reader.GetString(5))
                            {
                                discrepancies_List.Add(reader.GetInt32(0));
                            }

                        }
                    }
                }
            }

            return discrepancies_List;

        }
    }
}
