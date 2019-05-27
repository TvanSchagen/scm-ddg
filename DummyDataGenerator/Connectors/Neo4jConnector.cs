using Neo4j.Driver.V1;
using System;
using System.Collections.Generic;
using System.Text;

namespace DummyDataGenerator.Connectors
{
	class Neo4jConnector
	{

		private static Neo4jConnector _instance = null;

		public static Neo4jConnector Instance()
		{
			if (_instance == null)
				_instance = new Neo4jConnector();
			return _instance;
		}

		public IDriver Connection
		{
			get { return driver; }
		}

		private readonly IDriver driver;

		private Neo4jConnector()
		{
			try
			{
				driver = GraphDatabase.Driver("bolt://localhost:7687", AuthTokens.Basic(Program.NEO4J_USERNAME, Program.NEO4J_PASSWORD));
				Console.WriteLine("Opened Neo4j connection");
			}
			catch (Neo4jException e)
			{
				Console.WriteLine(e);
			}
			
		}

		public void Close()
		{
			driver?.Dispose();
		}

	}
}
