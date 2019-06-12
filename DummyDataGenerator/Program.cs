using System;
using System.IO;
using System.Text;
using DotNetEnv;
using DummyDataGenerator.Generators;

namespace DummyDataGenerator
{
	class Program
	{
		// default parameters for data generation
		public const int DEFAULT_CHAIN_DEPTH = 2;
		public const int DEFAULT_NO_OF_CHAINS = 5;
		public const int DEFAULT_CHAIN_BREADTH = 2;
		public const int DEFAULT_NO_OF_ACTIVITIES = 10;
		public const int DEFAULT_NO_OF_SUPPLIERS = 100;
		public const int DEFAULT_NO_OF_TOPLEVELSUPPLIERS = 10;
		public const int DEFAULT_NO_OF_PRODUCTS = 5;

		static void Main(string[] args)
		{
			string envPath = @"../../../../.env";
			if (File.Exists(envPath))
			{
				Console.WriteLine("Found .env file");
				// load environment file with connection properties
				Env.Load(envPath);
			}
			else
			{
				Console.WriteLine("Did not find .env file, creating one..");
				// create an .env file if it doesn't exist
				using (FileStream fs = File.Create(envPath))
				{
					// adds the keys to the file
					Byte[] info = new UTF8Encoding(true).GetBytes("MYSQL_HOST =\nMYSQL_DB = \nMYSQL_USER = \nMYSQL_PW = \nNEO4J_HOST = \nNEO4J_USER = \nNEO4J_PW =");
					fs.Write(info, 0, info.Length);
					Console.WriteLine("Created .env file, please specify connection properties in the file and restart the program");
				}
				System.Environment.Exit(0);
			}

			// get user input for data generation parameters
			Console.WriteLine("Enter parameters (leave blank for default):");
			Configuration conf = new Configuration(
				ReadInt("chain depth: ", DEFAULT_CHAIN_DEPTH),
				ReadInt("chain breadth: ", DEFAULT_CHAIN_BREADTH),
				ReadInt("activities: ", DEFAULT_NO_OF_ACTIVITIES),
				ReadInt("suppliers: ", DEFAULT_NO_OF_SUPPLIERS),
				ReadInt("top level suppliers: ", DEFAULT_NO_OF_TOPLEVELSUPPLIERS),
				ReadInt("products per top level supplier: ", DEFAULT_NO_OF_PRODUCTS)
			);
			Console.WriteLine("Choose your database to import the data into: \n\tN:\tNeo4j\n\tA:\tMySQL (adjecency list)\n\tC:\tMySQL (closure table)");
			ChooseDatabase(conf, Console.ReadKey().Key);
		}

		/// <summary>
		/// Directs the program to the chosen database
		/// </summary>
		/// <param name="conf">The associated configuration for the data generation</param>
		/// <param name="input">The key-input with which the user chooses their database</param>
		private static void ChooseDatabase(Configuration conf, ConsoleKey input)
		{
			Database database;
			if (input == ConsoleKey.A)
			{
				database = new MySqlGenerator();
			}
			else if (input == ConsoleKey.N)
			{
				database = new Neo4jGenerator();
			}
			else if (input == ConsoleKey.C)
			{
				database = new MySqlClosureTableGenerator();
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

		/// <summary>
		/// Reads an integer and displays a message, returns the default-input if parsing fails
		/// </summary>
		/// <param name="message">The message to be displayed</param>
		/// <param name="defaultInput">The default input that it sent back when parsing fails</param>
		/// <returns></returns>
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
