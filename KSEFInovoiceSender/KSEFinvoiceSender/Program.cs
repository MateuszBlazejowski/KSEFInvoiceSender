
using Dapper;
using KSEFinvoiceSender;
using KSEFinvoiceSender.Configuration;
using KSEFinvoiceSender.Database;
using MySqlConnector;
Program1 program1 = new Program1();


var dbConfig = ConfigSingleton.Instance.dbConfig;

await program1.TestedConnectionEstablishing();

//await program1.RunIntegrationTestAsync(); 

// Console.WriteLine($"{dbConfig.ConnectionString}");

//using var connection = new DbConnectionFactory().CreateConnection();
//await connection.OpenAsync();
//Console.WriteLine("✅ Connection successful!");


//await program1.TestedConnectionEstablishing();

// await program1.NotTestedInvoiceSending();