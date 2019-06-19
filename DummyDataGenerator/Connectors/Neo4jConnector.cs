using DotNetEnv;
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
				driver = GraphDatabase.Driver(Env.GetString("NEO4J_HOST"), 
					AuthTokens.Basic(
						Env.GetString("NEO4J_USER"), 
						Env.GetString("NEO4J_PW"))
					);
				Console.WriteLine("\nOpened Neo4j connection");
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
