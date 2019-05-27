using DummyDataGenerator.Connectors;
using Neo4j.Driver.V1;
using System;
using System.Collections.Generic;
using System.Text;

namespace DummyDataGenerator
{
    class Neo4jGenerator : Database
    {

		Neo4jConnector connector;

        public override void InitializeConnection()
        {
			connector = Neo4jConnector.Instance();
        }

        public override void GenerateData(Configuration config)
        {
			RefreshSchema();
			int[] tlOrgs = GenerateTopLevelOrganizations(config.NumberOfTopLevelSuppliers);
			GenerateOrganizations(config.NumberOfSuppliers);
			GenerateActivities(config.NumberOfActivities);
			GenerateProductTrees(tlOrgs, config.NumberOfProducts, config.ChainDepth, config.ChainBreadth);
			AddOrganizationsAndActivitiesToProductTree(config.NumberOfSuppliers, config.NumberOfTopLevelSuppliers, config.NumberOfActivities, config.NumberOfProducts);
			AddConstraints();
			AddMetaData(config);
			Console.WriteLine("Done..");
		}

		private void RefreshSchema()
		{
			var watch = System.Diagnostics.Stopwatch.StartNew();
			using (ISession session = connector.Connection.Session())
			{
				string statement = "MATCH (n) DETACH DELETE n";
				IStatementResult res = session.Run(statement);
			}
			watch.Stop();
			Console.WriteLine("Refreshed schema in " + watch.ElapsedMilliseconds + "ms" + "(" + (watch.ElapsedMilliseconds / 1000) + "s)");
		}

		private int[] GenerateTopLevelOrganizations(int organizations)
		{
			var watch = System.Diagnostics.Stopwatch.StartNew();
			List<int> result = new List<int>();
			using(ISession session = connector.Connection.Session())
			{
				for (int i = 0; i < organizations; i++)
				{
					string statement = "CREATE (n:Organization { name: 'Top Level Organization #" + i + "' }) RETURN ID(n) AS id";
					IStatementResult res = session.Run(statement);
					IRecord record = res.Peek();
					int id = record["id"].As<int>();
					result.Add(id);
				}
			}
			watch.Stop();
			Console.WriteLine("Added top level organizations in " + watch.ElapsedMilliseconds + "ms" + "(" + (watch.ElapsedMilliseconds / 1000) + "s)");
			return result.ToArray();
		}

		private int[] GenerateOrganizations(int organizations)
		{
			List<int> result = new List<int>();
			var watch = System.Diagnostics.Stopwatch.StartNew();
			using (ISession session = connector.Connection.Session())
			{
				for (int i = 0; i < organizations; i++)
				{
					string statement = "CREATE (n:Organization { name: 'Organization #" + i + "' }) RETURN ID(n) AS id";
					IStatementResult res = session.Run(statement);
					IRecord record = res.Peek();
					int id = record["id"].As<int>();
					result.Add(id);
				}
			}
			watch.Stop();
			Console.WriteLine("Added organizations in " + watch.ElapsedMilliseconds + "ms" + "(" + (watch.ElapsedMilliseconds / 1000) + "s)");
			return result.ToArray();
		}

		private int[] GenerateActivities(int activities)
		{
			List<int> result = new List<int>();
			var watch = System.Diagnostics.Stopwatch.StartNew();
			using (ISession session = connector.Connection.Session())
			{
				for (int i = 0; i < activities; i++)
				{
					string statement = "CREATE (n:Activity { name: 'Activity #" + i + "' }) RETURN ID(n) AS id";
					IStatementResult res = session.Run(statement);
					IRecord record = res.Peek();
					int id = record["id"].As<int>();
					result.Add(id);
				}
			}
			watch.Stop();
			Console.WriteLine("Added activities organizations in " + watch.ElapsedMilliseconds + "ms" + "(" + (watch.ElapsedMilliseconds / 1000) + "s)");
			return result.ToArray();
		}

		private void GenerateProductTrees(int[] tlOrgs, int numberOfProducts, int chainDepth, int chainBreadth)
		{
			
		}

		private void AddOrganizationsAndActivitiesToProductTree(int numberOfSuppliers, int numberOfTopLevelSuppliers, int numberOfActivities, int numberOfProducts)
		{
			
		}

		private void AddConstraints()
		{

		}

		private void AddMetaData(Configuration config)
		{

		}

		public override void CloseConnection()
        {
			connector?.Close();
        }
    }
}
