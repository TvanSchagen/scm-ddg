using DummyDataGenerator.Connectors;
using DummyDataGenerator.Utils;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;

namespace DummyDataGenerator.Generators
{

	class SqlServerGenerator : Database
	{

		SqlServerConnector connector;

		public override void CloseConnection()
		{
			connector.Close();
		}

		public override void GenerateData(Configuration config, bool allowMultipleThreads)
		{
			RefreshDatabaseSchema();
			int[] tlOrgs = GenerateTopLevelOrganizations(config.NumberOfTopLevelSuppliers);
			GenerateOrganizations(config.NumberOfSuppliers);
			GenerateActivities(config.NumberOfActivities);
			GenerateLocations(config.NumberOfSuppliers);
			if (allowMultipleThreads) Logger.Warn("Multithread is not supported for this generator");
			GenerateProductTrees(tlOrgs, config.NumberOfProducts, config.ChainDepth, config.ChainBreadth);
			AddOrganizationsAndActivitiesToProductTree(config.NumberOfSuppliers, config.NumberOfTopLevelSuppliers, config.NumberOfActivities, config.ChainDepth, config.ChainBreadth);
			AddMetaData(config);
		}

		private void RefreshDatabaseSchema()
		{
			string statement = File.ReadAllText(Properties.Resources.tsql_generate_schema);
			try
			{
				using (SqlCommand cmd = new SqlCommand(statement, connector.Connection))
				{
					cmd.ExecuteNonQuery();
					Logger.Debug("Refreshed database schema..");
				}
			}
			catch (SqlException e)
			{
				Logger.Error("Error: " + e);
			}
		}

		private int[] GenerateTopLevelOrganizations(int numberOfTopLevelSuppliers)
		{
			List<int> result = new List<int>();
			var watch = System.Diagnostics.Stopwatch.StartNew();
			for (int i = 0; i < numberOfTopLevelSuppliers; i++)
			{
				// SqlCommand com = InsertOrganization(true, i);
				// com.ExecuteNonQuery();
				
			}
			watch.Stop();
			Logger.Info("Took " + watch.ElapsedMilliseconds + "ms " + "(" + (watch.ElapsedMilliseconds / 1000) + "s) " + " to generate " + result.Count + " top level organisations");
			return result.ToArray();
		}

		private void GenerateOrganizations(int numberOfSuppliers)
		{
			
		}

		private void GenerateActivities(int numberOfActivities)
		{
			
		}

		private void GenerateLocations(int numberOfSuppliers)
		{
			
		}

		private void GenerateProductTrees(int[] tlOrgs, int numberOfProducts, int chainDepth, int chainBreadth)
		{
			
		}

		private void AddOrganizationsAndActivitiesToProductTree(int numberOfSuppliers, int numberOfTopLevelSuppliers, int numberOfActivities, int chainDepth, int chainBreadth)
		{
			
		}

		private void AddMetaData(Configuration config)
		{
			
		}

		public override void InitializeConnection()
		{
			connector = SqlServerConnector.Instance();
		}
	}
}
