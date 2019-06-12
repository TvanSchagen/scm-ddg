using DummyDataGenerator.Connectors;
using Neo4j.Driver.V1;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;

namespace DummyDataGenerator
{
    class Neo4jGenerator : Database
    {

		Neo4jConnector connector;

        public override void InitializeConnection()
        {
			connector = Neo4jConnector.Instance();
        }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="config"></param>
        public override void GenerateData(Configuration config)
        {
			RefreshSchema();
			int[] tlOrgs = GenerateTopLevelOrganizations(config.NumberOfTopLevelSuppliers);
			int[] orgs = GenerateOrganizations(config.NumberOfSuppliers);
			// int[] acts = GenerateActivities(config.NumberOfActivities);
			GenerateProductTrees(tlOrgs, config.NumberOfProducts, config.ChainDepth, config.ChainBreadth);
			AddOrganizationsAndActivitiesToProductTree(config.NumberOfActivities, orgs);
			// AddConstraints();
			AddMetaData(config);
			Console.WriteLine("Done..");
		}

		/// <summary>
		/// 
		/// </summary>
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

		/// <summary>
		/// 
		/// </summary>
		/// <param name="organizations"></param>
		/// <returns></returns>
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

		/// <summary>
		/// 
		/// </summary>
		/// <param name="organizations"></param>
		/// <returns></returns>
		private int[] GenerateOrganizations(int organizations)
		{
			List<int> result = new List<int>();
			var watch = System.Diagnostics.Stopwatch.StartNew();
			using (ISession session = connector.Connection.Session())
			{
				for (int i = 0; i < organizations; i++)
				{
					string statement = "CREATE (n:Organization { name: 'Organization #" + i + "' }) RETURN ID(n) AS id";
					IStatementResult res = session.WriteTransaction(tx => tx.Run(statement));
					IRecord record = res.Peek();
					int id = record["id"].As<int>();
					result.Add(id);
				}
			}
			watch.Stop();
			Console.WriteLine("Added organizations in " + watch.ElapsedMilliseconds + "ms" + "(" + (watch.ElapsedMilliseconds / 1000) + "s)");
			return result.ToArray();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="activities"></param>
		/// <returns></returns>
		/*private int[] GenerateActivities(int activities)
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
		}*/

		/// <summary>
		/// 
		/// </summary>
		/// <param name="organizationIdentifiers"></param>
		/// <param name="productsPerSupplier"></param>
		/// <param name="depth"></param>
		/// <param name="breadthPerLevel"></param>
		private void GenerateProductTrees(int[] organizationIdentifiers, int productsPerSupplier, int depth, int breadthPerLevel)
		{
			var watchTotal = System.Diagnostics.Stopwatch.StartNew();
			// for each top level supplier
			for (int i = 0; i < organizationIdentifiers.Length; i++)
			{
				Console.WriteLine("Generating for Organization: " + (i + 1));

				// for each of their products
				for (int j = 0; j < productsPerSupplier; j++)
				{
					var watch = System.Diagnostics.Stopwatch.StartNew();
					int topLevelId = 0;

					// generate the top level product
					using (ISession session = connector.Connection.Session())
					{
						string statement = "CREATE (n:Product { name: 'TopLevelProduct #o" + (i + 1) + "-p" + (j + 1) + "' }) RETURN ID(n) AS id";
						IStatementResult res = session.WriteTransaction(tx => tx.Run(statement));
						IRecord record = res.Peek();
						topLevelId = record["id"].As<int>();
					}

					List<int> previousResults = new List<int>();

					// for the depth of the chain
					for (int k = 0; k < depth; k++)
					{
						// in the first pass, generate the products for our top level product
						if (k == 0)
						{
							previousResults.Add(topLevelId);
							previousResults = GenerateProductRowAndRelations(i, previousResults.ToArray(), breadthPerLevel);

						}
						// in subsequent passes, take the previous row of products and generate a new underlying row for all of them
						else
						{
							previousResults = GenerateProductRowAndRelations(i, previousResults.ToArray(), breadthPerLevel);
						}

					}
					watch.Stop();
					Console.WriteLine("Took " + watch.ElapsedMilliseconds + "ms " + "(" + (watch.ElapsedMilliseconds / 1000) + "s) " +
						"to generate products and relations for product " + (j + 1) + " for organisation " + (i + 1));
				}
			}
			watchTotal.Stop();
			Console.WriteLine("Took " + watchTotal.ElapsedMilliseconds + "ms " + "(" + (watchTotal.ElapsedMilliseconds / 1000) + "s) in total");
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="chainId"></param>
		/// <param name="parentProductIdentifiers"></param>
		/// <param name="breadthPerLevel"></param>
		/// <returns></returns>
		private List<int> GenerateProductRowAndRelations(int chainId, int[] parentProductIdentifiers, int breadthPerLevel)
		{
			List<int> childProductsCreated = new List<int>();

			for (int i = 0; i < parentProductIdentifiers.Length; i++)
			{
				for (int j = 0; j < breadthPerLevel; j++)
				{
					int childProductId;
					// first insert the product
					using (ISession session = connector.Connection.Session())
					{
						string statement = "CREATE (n:Product { name: 'Product #c" + (chainId + 1) + "-p" + (i + 1) + "-p" + (j + 1) + "' }) RETURN ID(n) AS id";
						IStatementResult res = session.WriteTransaction(tx => tx.Run(statement));
						IRecord record = res.Peek();
						childProductId = record["id"].As<int>();
					}
					// add it to the products created list
					childProductsCreated.Add(childProductId);

					// then create the relation for the product hierarchy
					using (ISession session = connector.Connection.Session())
					{
						string statement = "MATCH (p:Product), (c:Product)" +
											" WHERE ID(p) = " + parentProductIdentifiers[i] +
											" AND ID(c) =" + childProductId +
											" CREATE (p)-[:CONSISTS_OF]->(c)";
						session.WriteTransaction(tx => tx.Run(statement));
					}
				}
			}
			return childProductsCreated;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="activityIds"></param>
		/// <param name="organizationIds"></param>
		private void AddOrganizationsAndActivitiesToProductTree(int numberOfActivities, int[] organizationIds)
		{
			var watch = System.Diagnostics.Stopwatch.StartNew();
			List<int> productIds = new List<int>();
			using (ISession session = connector.Connection.Session())
			{
				string statement =	"MATCH (n:Product)" +
									" WHERE n.name STARTS WITH 'Product'" +
									" RETURN ID(n) AS id";
				IStatementResult res = session.WriteTransaction(tx => tx.Run(statement));
				foreach (IRecord r in res)
				{
					productIds.Add(r["id"].As<int>());
				}
			}
			
			int[] productIdsArray = productIds.ToArray();

			for (int i = 0; i < productIdsArray.Length; i++)
			{
				using (ISession session = connector.Connection.Session())
				{
					int productId = productIdsArray[i];
					//int activityId = activityIds[i % activityIds.Length];
					int organizationId = organizationIds[i % organizationIds.Length];
					string statement = "MATCH (p:Product), (o:Organization)" +
										" WHERE ID(p) = " + productId +
										" AND ID(o) = " + organizationId +
										" CREATE (o)-[:ACTIVITY_" + (i % numberOfActivities) + "]->(p)";
					//" CREATE (o)-[:CARRIES_OUT]->(a:Activity { name:'Activity # " + i + "' })-[:PRODUCES]->(p)";
					session.WriteTransaction(tx => tx.Run(statement));
				}
			}
			watch.Stop();
			Console.WriteLine("Took " + watch.ElapsedMilliseconds + "ms " + "(" + (watch.ElapsedMilliseconds / 1000) + "s) to add organizations and activities");
		}

		/// <summary>
		/// 
		/// </summary>
		private void AddConstraints()
		{
			using(ISession session = connector.Connection.Session())
			{
				string statement = "ADD INDEX ON :Organization(name)";
				session.WriteTransaction(tx => tx.Run(statement));
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="config"></param>
		private void AddMetaData(Configuration config)
		{
			using (ISession session = connector.Connection.Session())
			{
				string statement = "CREATE (n:DbMeta) " +
									"SET n.chainDepth = " + config.ChainDepth +
									", n.chainBreadth = " + config.ChainBreadth +
									", n.numberOfActivities = " + config.NumberOfActivities +
									", n.numberOfProducts = " + config.NumberOfProducts +
									", n.numberOfSuppliers = " + config.NumberOfSuppliers +
									", n.numberOfTopLevelSuppliers = " + config.NumberOfTopLevelSuppliers;
				session.WriteTransaction(tx => tx.Run(statement));
			}
		}

		public override void CloseConnection()
        {
			connector?.Close();
        }
    }
}
