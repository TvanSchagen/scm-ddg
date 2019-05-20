using System;
using MySql.Data;
using MySql.Data.MySqlClient;

namespace DummyDataGenerator
{
	class Program
	{

		public const int DEFAULT_CHAIN_DEPTH = 10;
		public const int DEFAULT_NO_OF_CHAINS = 100;
		public const string MYSQL_DATABASE_NAME = "scm_test_dummy_generated_v2";
		public const string NEO4J_DATABASE_NAME = "scm_test_graph_dummydata";

		static void Main(string[] args)
		{
			Configuration conf = new Configuration(
				ReadInt("number of chains: ", DEFAULT_NO_OF_CHAINS),
				ReadInt("chain depth: ", DEFAULT_CHAIN_DEPTH),
				ReadInt("activities: ", 10),
				ReadInt("suppliers: ", 100),
				ReadInt("products: ", 50)
			);
			Console.WriteLine("Choose your database to import the data into: \n\tN:\tNeo4j\n\tM:\tMySql\n");
			ChooseDatabase(conf, Console.ReadKey().Key);
		}

		private static void ChooseDatabase(Configuration conf, ConsoleKey input)
		{
			Database database;
			if (input == ConsoleKey.M)
			{
				database = new MySqlGenerator();
			}
			else if (input == ConsoleKey.N)
			{
				database = new Neo4jGenerator();
			}
			else
			{
				ChooseDatabase(conf, Console.ReadKey().Key);
				return;
			}
			database.InitializeConnection();
			database.GenerateData(conf);
			database.CloseConnection();
		}

		private static int ReadInt(string message, int defaultInput)
		{
			Console.WriteLine(message);
			int result;
			if (!Int32.TryParse(Console.ReadLine(), out result) || result < 0)
			{
				return defaultInput;
			}
			return result;
		}

	}
}
