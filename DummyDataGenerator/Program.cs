﻿using System;
using System.IO;
using System.Text;
using DotNetEnv;
using DummyDataGenerator.Evaluators;
using DummyDataGenerator.Generators;
using DummyDataGenerator.Utils;

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
				Logger.Info("Loaded existing .env file with connection properties");
				// load environment file with connection properties
				Env.Load(envPath);
			}
			else
			{
				Logger.Warn("Did not find an .env file, creating one..");
				// create an .env file if it doesn't exist
				using (FileStream fs = File.Create(envPath))
				{
					// adds the keys to the file
					Byte[] info = new UTF8Encoding(true).GetBytes("MYSQL_HOST =\nMYSQL_DB = \nMYSQL_USER = \nMYSQL_PW = \nNEO4J_HOST = \nNEO4J_USER = \nNEO4J_PW =");
					fs.Write(info, 0, info.Length);
					Logger.Info("Created .env file, please specify connection properties in the file and restart the program");
				}
				System.Environment.Exit(0);
			}
			Logger.Info("Choose your application mode: [G]enerate data / [E]valuate database");
			ChooseMode(Console.ReadKey().Key);
		}

		/// <summary>
		/// Lets the user choose a mode: evaluation or generation
		/// </summary>
		/// <param name="input">The key-input with which the user chooses their mode</param>
		private static void ChooseMode(ConsoleKey input)
		{
			// E for evaluator mode
			if (input == ConsoleKey.E)
			{
				Console.WriteLine();
				Logger.Info("Choose your database to evaluate: [N]eo4j / [A]djacency List MySQL / [C]losure Table MySQL");
				ChooseEvaluator(Console.ReadKey().Key);
			}
			// G for generating mode
			else if (input == ConsoleKey.G)
			{
				Console.WriteLine();
				// get user input for data generation parameters
				Logger.Info("Enter parameters (leave blank for default):");
				Configuration conf = new Configuration(
					ReadInt("Chain depth: ", DEFAULT_CHAIN_DEPTH),
					ReadInt("Chain breadth: ", DEFAULT_CHAIN_BREADTH),
					ReadInt("Number of activities: ", DEFAULT_NO_OF_ACTIVITIES),
					ReadInt("Number of organizations: ", DEFAULT_NO_OF_SUPPLIERS),
					ReadInt("Number of top level organizations: ", DEFAULT_NO_OF_TOPLEVELSUPPLIERS),
					ReadInt("Products per top level organization: ", DEFAULT_NO_OF_PRODUCTS)
				);
				Logger.Info("Allow multiple threads to be used: ([Y]es/[N]o)");
				ConsoleKey allow = Console.ReadKey().Key;
				Logger.Info("Choose your database to use for generation: ([N]eo4j / [A]djacency List MySQL / [C]losure Table MySQL)");
				ChooseGenerator(conf, allow, Console.ReadKey().Key);
			} else
			{
				ChooseMode(Console.ReadKey().Key);
			}
		}

		/// <summary>
		/// Directs the program to the chosen database
		/// </summary>
		/// <param name="input">The key-input with which the user chooses their database</param>
		private static void ChooseEvaluator(ConsoleKey input)
		{
			if (input == ConsoleKey.A)
			{
				MySqlEvaluator e = new MySqlEvaluator();
				e.Execute();
			}
			else if (input == ConsoleKey.N)
			{
				Neo4jEvaluator e = new Neo4jEvaluator();
				e.Execute();
			}
			else if (input == ConsoleKey.C)
			{
				MySqlClosureTableEvaluator e = new MySqlClosureTableEvaluator();
				e.Execute();
			}
			else
			{
				Logger.Warn("Unsupported Input, please try again");
				ChooseEvaluator(Console.ReadKey().Key);
				return;
			}
		}

		/// <summary>
		/// Directs the program to the chosen database
		/// </summary>
		/// <param name="conf">The associated configuration for the data generation</param>
		/// <param name="input">The key-input with which the user chooses their database</param>
		private static void ChooseGenerator(Configuration conf, ConsoleKey allowMultipleThreads, ConsoleKey input)
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
				Logger.Warn("Unsupported Input, please try again");
				ChooseGenerator(conf, allowMultipleThreads, Console.ReadKey().Key);
				return;
			}
			Logger.Info("Initializing connection..");
			database.InitializeConnection();
			Logger.Info("Preparing dummy data..");
			database.GenerateFakeData(10000);
			bool allow = allowMultipleThreads == ConsoleKey.Y;
			Logger.Info("Starting data generation process..");
			database.GenerateData(conf, allow);
			Logger.Info("Closing connection..");
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
