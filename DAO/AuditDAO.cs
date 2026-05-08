using BankAuditSystem.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;

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

        private string ConstructHashString(int accountId, decimal amount, string type, DateTime timestamp)
        {

            return string.Format(CultureInfo.InvariantCulture,
                "{0}|{1:F2}|{2}|{3:yyyy-MM-dd HH:mm:ss.fff}",
                accountId, amount, type, timestamp);
        }

        public void InsertAuditEntry(AuditEntry entry)
        {


            string row_hash_input_string = ConstructHashString(entry.AccountID, entry.Amount, entry.TransactionType, entry.TimeStp);

            Console.WriteLine(row_hash_input_string);
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
            const string BalanceQuery = "SELECT AccountID, Amount, TransactionType FROM AuditEntry WHERE AccountId = @AccountId;";

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

                            if (type.Equals("withdrawal", StringComparison.OrdinalIgnoreCase))
                            {
                                currentBalance -= amount;
                            }
                            else if(type.Equals("deposit", StringComparison.OrdinalIgnoreCase))
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

            const string BalanceQuery = "SELECT AccountID, Amount, TransactionType, TimeStp, RowHash FROM AuditEntry;";

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(BalanceQuery, conn))
                {

                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {

                            string verificationString = ConstructHashString(
                                                    reader.GetInt32(0), // AccountID
                                                    reader.GetDecimal(1), // Amount
                                                    reader.GetString(2), // TransactionType
                                                    reader.GetDateTime(3) // TimeStp
                                                );

                            Console.WriteLine(reader.GetDateTime(3));
                            Console.WriteLine(verificationString);
                            
                            string test_Hash = RowHash(verificationString);
                            string row_Hash = reader.GetString(4);

                            if (test_Hash != row_Hash)
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
