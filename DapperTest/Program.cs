using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Data.Common;
using System.Threading;

DbConnection conn = new SqlConnection("Server=localhost;Database=master;Trusted_Connection=True;");

//var result3 = await conn.QueryAsync("qwe", cancell)

//var result1 = await conn.Query<string>(sql: "qwe", buffered: , );

var result2 = conn.ExecuteScalar<string>(new CommandDefinition("sp_CrunchNumbers",
           new
           {
               WarpFactor = 43
           },
           commandType: CommandType.StoredProcedure,
           cancellationToken: CancellationToken.None));