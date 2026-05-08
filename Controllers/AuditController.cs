using BankAuditSystem.DAO;
using BankAuditSystem.Data;
using BankAuditSystem.Models;
using Microsoft.AspNetCore.Mvc;


namespace BankAuditSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuditController : Controller
    {

        private readonly AuditDAO _auditDao;

        public AuditController(AuditDAO auditDao)
        {
            _auditDao = auditDao;
        }

        public IActionResult Index()
        {
            return View();
        }

        // GET REQUEST for Table Data at specific time
        [HttpGet("pointInTime")]
        public IActionResult GetTableData(DateTime input_time)
        {
            try
            {
                List<AuditEntry> auditList = _auditDao.PointInTimeReport(input_time);

                return Ok(new { AuditList = auditList});

            }
            catch (Exception ex)
            {
                // 4. Error Handling: Return a 500 error if something fails
                return StatusCode(500, "An error occurred while retrieving the balance.");
            }
        }

        // GET REQUEST for Getting Account Balance
        [HttpGet("balance/{accountId}")]
        public IActionResult GetBalance(int accountId)
        {
            // Check if Id is valid
            if (accountId <= 0)
            {
                return BadRequest("Invalid Account Id.");
            }
            try
            {
                // 2. Data Handoff: Request the calculated balance from the DAO
                decimal balance = _auditDao.GetBalance(accountId);

                // 3. Response: Return a 200 OK with the result as JSON
                return Ok(new { AccountID = accountId, CurrentBalance = balance });
            }
            catch (Exception ex)
            {
                // 4. Error Handling: Return a 500 error if something fails
                return StatusCode(500, "An error occurred while retrieving the balance.");
            }
        }

        // GET REQUEST for Integrity Check
        [HttpGet("verify")]
        public IActionResult GetIntegrityCheck()
        {
            try
            {
                List<int> issues = _auditDao.Integrity_Check();
                if (issues.Count == 0){
                    return Ok("System Integrity Verified.");
                }
                return BadRequest(new { Message = "Tampering detected in accounts", Accounts = issues});
            }
            catch (Exception ex)
            {
                // 4. Error Handling: Return a 500 error if something fails
                return StatusCode(500, "An error occurred while retrieving the balance.");
            }
        }

        // POST REQUEST for Inserting Row
        [HttpPost("Insert")]
        public IActionResult InsertEntry([FromBody] AuditEntry entry)
        {
            try
            {
                _auditDao.InsertAuditEntry(entry);
                return Ok("Entry Logged Successfully");
            }
            catch (Exception ex)
            {
                // 4. Error Handling: Return a 500 error if something fails
                return StatusCode(500, "An error occurred while retrieving the balance.");
            }
        }

    }
}
