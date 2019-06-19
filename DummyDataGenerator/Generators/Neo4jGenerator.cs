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
			int[] topLevelOrganizations = GenerateTopLevelOrganizations(config.NumberOfTopLevelSuppliers);
			int[] organizations = GenerateOrganizations(config.NumberOfSuppliers);
			int[] locations = GenerateLocations(config.NumberOfSuppliers);
			GenerateProductTrees(topLevelOrganizations, config.NumberOfProducts, config.ChainDepth, config.ChainBreadth);
			AddOrganizationsAndActivitiesToProductTree(organizations, locations);
			// AddConstraints();
			AddMetaData(config);
			Console.WriteLine("Program completed.");
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
					string statement = InsertOrganization(true, i);
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
					string statement = InsertOrganization(false, i);
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

		private int[] GenerateLocations(int locations)
		{
			List<int> result = new List<int>();
			var watch = System.Diagnostics.Stopwatch.StartNew();
			using (ISession session = connector.Connection.Session())
			{
				for (int i = 0; i < locations; i++)
				{
					string statement = InsertLocation(i);
					IStatementResult res = session.Run(statement);
					IRecord record = res.Peek();
					int id = record["id"].As<int>();
					result.Add(id);
				}
			}
			watch.Stop();
			Console.WriteLine("Added locations in " + watch.ElapsedMilliseconds + "ms" + "(" + (watch.ElapsedMilliseconds / 1000) + "s)");
			return result.ToArray();
		}

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
						//string statement = "CREATE (n:Product { name: 'TopLevelProduct #o" + (i + 1) + "-p" + (j + 1) + "' }) RETURN ID(n) AS id";
						string statement = InsertProduct(true, i * productsPerSupplier + j);
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
						// string statement = "CREATE (n:Product { name: 'Product #c" + (chainId + 1) + "-p" + (i + 1) + "-p" + (j + 1) + "' }) RETURN ID(n) AS id";
						string statement = InsertProduct(false, parentProductIdentifiers[i] * 2 + j);
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
		/// <param name="organizationIds"></param>
		/// <param name="locationIds"></param>
		private void AddOrganizationsAndActivitiesToProductTree(int[] organizationIds, int[] locationIds)
		{
			var watch = System.Diagnostics.Stopwatch.StartNew();
			List<int> productIds = new List<int>();
			using (ISession session = connector.Connection.Session())
			{
				string statement = "MATCH (n:Product)" +
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
					int organizationId = organizationIds[i % organizationIds.Length];
					int locationId = locationIds[i % locationIds.Length];
					string statement = "MATCH (p:Product), (o:Organization), (l:Location)" +
										" WHERE ID(p) = " + productId +
										" AND ID(o) = " + organizationId +
										" AND ID(l) = " + locationId +
										" CREATE (o)-[:PERFORMS]->(a:Activity " + InsertActivity(i) + ")," +
										" (a)-[:PRODUCES]->(p)," +
										" (a)-[:LOCATED_AT]->(l)";
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


		public string InsertProduct(bool isTopLevel, int id)
		{
			id = id % products.Count;
			string result = "CREATE (n:Product { " +
				string.Format("GUID: \"{0}\", name: \"{1}\", description: \"{2}\", ean : \"{3}\", category: \"{4}\", sub_category: \"{5}\", created: datetime('{6}'), last_updated: datetime('{7}')",
					products[id].GUID,
					isTopLevel ? "Top Level Product " + products[id].Name : products[id].Name,
					products[id].Description,
					products[id].EANCode,
					products[id].Category,
					products[id].SubCategory,
					new LocalDateTime(products[id].CreatedDate),
					new LocalDateTime(products[id].LastUpdatedDate)
				) +
				"}) RETURN ID(n) AS id";
			return result;
		}

		public string InsertActivity(int id)
		{
			id = id % activities.Count;
			string result = "{ " +
				string.Format("GUID: \"{0}\", name: \"{1}\", description: \"{2}\", created: datetime('{3}'), last_updated: datetime('{4}')",
					activities[id].GUID,
					activities[id].Name,
					activities[id].Description,
					new LocalDateTime(activities[id].CreatedDate),
					new LocalDateTime(activities[id].LastUpdatedDate)
				) +
				"}";
			return result;
		}

		public string InsertOrganization(bool isTopLevel, int id)
		{
			id = id % products.Count;
			string result = "CREATE (n:Organization { " +
				string.Format("GUID: \"{0}\", name: \"{1}\", description: \"{2}\", ein : \"{3}\", number_of_employees: {4}, email_address: \"{5}\", website: \"{6}\", created: datetime('{7}'), last_updated: datetime('{8}')",
					organizations[id].GUID,
					isTopLevel ? "Top Level Organization " + organizations[id].Name : organizations[id].Name,
					organizations[id].Description,
					organizations[id].EmployerIdentificationNumber,
					organizations[id].NumberOfEmployees,
					organizations[id].EmailAddress,
					organizations[id].WebsiteURL,
					new LocalDateTime(organizations[id].CreatedDate),
					new LocalDateTime(organizations[id].LastUpdatedDate)
				) +
				"}) RETURN ID(n) AS id";
			return result;
		}

		public string InsertLocation(int id)
		{
			id = id % locations.Count;
			string result = "CREATE (n:Location { " +
				string.Format("GUID: \"{0}\", latitude: {1}, longtitude: {2}, country: \"{3}\", postal_code: \"{4}\", province: \"{5}\", city: \"{6}\", street: \"{7}\", created: datetime('{8}'), last_updated: datetime('{9}')",
					locations[id].GUID,
					locations[id].Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture),
					locations[id].Longtitude.ToString(System.Globalization.CultureInfo.InvariantCulture),
					locations[id].Country,
					locations[id].PostalCode,
					locations[id].Province,
					locations[id].City,
					locations[id].Street,
					new LocalDateTime(locations[id].CreatedDate),
					new LocalDateTime(locations[id].LastUpdatedDate)
				) +
				"}) RETURN ID(n) AS id";
			return result;
		}

	}
}
