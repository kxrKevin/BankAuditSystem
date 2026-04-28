using BankAuditSystem.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;


namespace BankAuditSystem.DAO
{
    public class AuditDAO
    {
        private readonly string _connectionString;

        public AuditDAO(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("AuditDbConnection");
        }

        public void InsertAuditEntry(AuditEntry entry)
        {
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
                    cmd.Parameters.AddWithValue("@RowHash", entry.RowHash); 
                    
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








    }
}
