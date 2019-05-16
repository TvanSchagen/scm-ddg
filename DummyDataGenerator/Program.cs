using System;
using MySql.Data;
using MySql.Data.MySqlClient;

namespace DummyDataGenerator
{
	class Program
	{

		const int DEFAULT_CHAIN_DEPTH = 10;
		const int DEFAULT_NO_OF_CHAINS = 100;

		static void Main(string[] args)
		{
			Configuration conf = new Configuration(
				ReadInt("number of chains: ", 100),
				ReadInt("chain depth: ", 100),
				ReadInt("activities: ", 100),
				ReadInt("suppliers: ", 100),
				ReadInt("products: ", 100)
			);
			MySqlDummy m = new MySqlDummy();
			m.InitializeConnection();
			m.GenerateData(conf);
			m.CloseConnection();
			Console.ReadLine();
		}

		private static int ReadInt(string message, int defaultInput)
		{
			Console.WriteLine(message);
			int result;
			if (!Int32.TryParse(Console.ReadLine(), out result))
			{
				return defaultInput;
			}
			if (result < 0)
			{
				return defaultInput;
			}
			return result;
		}

	}
}
