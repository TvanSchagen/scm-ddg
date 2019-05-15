using System;

namespace DummyDataGenerator
{
	class Program
	{

		const int DEFAULT_CHAIN_DEPTH = 10;
		const int DEFAULT_NO_OF_CHAINS = 100;

		static void Main(string[] args)
		{
			Console.WriteLine("Welcome to Supply Chain Dummy Data Generator");
			Console.WriteLine("Choose for which kind of database you would like to generate dummy data:\n "
				+ "\tM\tMySQL\n"
				+ "\tN\tNeo4j\n");
			switch(Console.ReadKey().Key)
			{
				case ConsoleKey.M:
					HandleMySQL();
					break;
				case ConsoleKey.N:
					HandleNeo4j();
					break;
			}
		}

		private static void HandleNeo4j()
		{
			throw new NotImplementedException();
		}

		private static void HandleMySQL()
		{
			int numberOfChains = ReadInt("Please enter the number of chains:", DEFAULT_NO_OF_CHAINS);
			int chainDepth = ReadInt("Please enter the chain depth:", DEFAULT_CHAIN_DEPTH);
			GenerateMySQLDummyData(chainDepth, numberOfChains);
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

		private static void GenerateMySQLDummyData(int chainDepth, int numberOfChains)
		{
			Console.WriteLine("Generating dummy data for MySQL..");
			Console.ReadLine();
		}

		void GenerateSuppliers(int numberOfSuppliers)
		{
			// generate a number of suppliers and fill the neccessary fields
		}

		void GenerateActivities()
		{
			// generate a number of activities and fill the necessary fields
		}

		void GenerateProducts()
		{
			// generate a number of products and fill the necessary fields
		}

		void GenerateSupplierActivityProductRelations()
		{
			/*	1.	grab a non-used supplier (with activity retailer) and pick them as the first tier -> supplies to nobody
				2.	
				
			*/
		}

	}
}
