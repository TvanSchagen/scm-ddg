using System;
using MySql.Data;
using MySql.Data.MySqlClient;

namespace DummyDataGenerator
{
	class Program
	{

		public const int DEFAULT_CHAIN_DEPTH = 10;
		public const int DEFAULT_NO_OF_CHAINS = 100;
		public const int DEFAULT_CHAIN_BREADTH = 2;
		public const int DEFAULT_NO_OF_ACTIVITIES = 10;
		public const int DEFAULT_NO_OF_SUPPLIERS = 100;
		public const int DEFAULT_NO_OF_TOPLEVELSUPPLIERS = 10;
		public const int DEFAULT_NO_OF_PRODUCTS = 10;

		public const string MYSQL_DATABASE_NAME		= "test";

		// use for local
		public const string MYSQL_DATABASE_HOST		= "localhost";
		public const string MYSQL_USER				= "root";
		public const string MYSQL_PASSWORD			= "teun1996";

		// use for virtual machine
		// public const string MYSQL_DATABASE_HOST	= "192.168.178.94";
		// public const string MYSQL_USER			= "client";
		// public const string MYSQL_PASSWORD		= "client";

		public const string NEO4J_USERNAME			= "neo4j";
		public const string NEO4J_PASSWORD			= "teun1996";

		static void Main(string[] args)
		{
			Console.WriteLine("Enter parameters (leave blank for default):");
			Configuration conf = new Configuration(
				ReadInt("chain depth: ", DEFAULT_CHAIN_DEPTH),
				ReadInt("chain breadth: ", DEFAULT_CHAIN_BREADTH),
				ReadInt("activities: ", DEFAULT_NO_OF_ACTIVITIES),
				ReadInt("suppliers: ", DEFAULT_NO_OF_SUPPLIERS),
				ReadInt("top level suppliers: ", DEFAULT_NO_OF_TOPLEVELSUPPLIERS),
				ReadInt("products per top level supplier: ", DEFAULT_NO_OF_PRODUCTS)
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
				Console.WriteLine("Unsupported input, choose another: ");
				ChooseDatabase(conf, Console.ReadKey().Key);
				return;
			}
			Console.WriteLine("\nInitializing connection");
			database.InitializeConnection();
			Console.WriteLine("Attempting to generate data");
			database.GenerateData(conf);
			Console.WriteLine("Closing connection");
			database.CloseConnection();
			Console.ReadLine();
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
