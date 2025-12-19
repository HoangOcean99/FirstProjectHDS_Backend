
using System.Data;
using Microsoft.Data.SqlClient;

public class DapperContext
{
    private readonly IConfiguration _configuration;
    public DapperContext(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    public IDbConnection CreateConnection()
    {
        return new SqlConnection(
            _configuration.GetConnectionString("DefaultConnection")
        );
    }
}