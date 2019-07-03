using DummyDataGenerator.Connectors;
using DummyDataGenerator.Utils;
using Neo4j.Driver.V1;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Threading;

namespace DummyDataGenerator
{
    class Neo4jGenerator : Database
    {

		Neo4jConnector connector;

		/// <summary>
		/// Initializes the singleton connection
		/// </summary>
		public override void InitializeConnection()
        {
			connector = Neo4jConnector.Instance();
        }

		/// <summary>
		/// Generates the data with the specified parameters of the configuration
		/// </summary>
		/// <param name="config">The configuration that holds the paremeters for the data</param>
		public override void GenerateData(Configuration config, bool allowMultipleThreads)
        {
			RefreshSchema();
			int[] topLevelOrganizations = GenerateTopLevelOrganizations(config.NumberOfTopLevelSuppliers);
			int[] organizations = GenerateOrganizations(config.NumberOfSuppliers);
			int[] locations = GenerateLocations(config.NumberOfSuppliers);
			if (allowMultipleThreads)
			{
				mt_GenerateProductTrees(topLevelOrganizations, config.NumberOfProducts, config.ChainDepth, config.ChainBreadth, locations);
			}
			else
			{
				GenerateProductTrees(topLevelOrganizations, config.NumberOfProducts, config.ChainDepth, config.ChainBreadth, locations);
			}
			AddOrganizationsAndActivitiesToProductTree(organizations, locations);
			// AddConstraints();
			AddMetaData(config);
			Logger.Info("Program completed.");
		}

		/// <summary>
		/// Deletes all nodes and relationships from the connected graph
		/// </summary>
		#warning There is no option for dropping a database in Neo4j, when the graph is very large, this way of deleting can take a long time
		private void RefreshSchema()
		{
			var watch = System.Diagnostics.Stopwatch.StartNew();
			using (ISession session = connector.Connection.Session())
			{
				string statement = "MATCH (n) DETACH DELETE n";
				IStatementResult res = session.Run(statement);
			}
			watch.Stop();
			Logger.Info("Refreshed schema in " + watch.ElapsedMilliseconds + "ms" + "(" + (watch.ElapsedMilliseconds / 1000) + "s)");
		}

		/// <summary>
		/// Generates a set number of top level organizations; organizations that do not supply any others
		/// </summary>
		/// <param name="organizations">The number of organiations</param>
		/// <returns>a list of generated organization ids</returns>
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
			Logger.Info("Added top level organizations in " + watch.ElapsedMilliseconds + "ms" + "(" + (watch.ElapsedMilliseconds / 1000) + "s)");
			return result.ToArray();
		}

		/// <summary>
		/// Generates a set number of organizations; they can be located anwywhere in the chain
		/// </summary>
		/// <param name="organizations">The number of organiations</param>
		/// <returns>a list of generated organization ids</returns>
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
			Logger.Info("Added organizations in " + watch.ElapsedMilliseconds + "ms" + "(" + (watch.ElapsedMilliseconds / 1000) + "s)");
			return result.ToArray();
		}

		/// <summary>
		/// Generate a set number of locations to be used during the tree generation
		/// </summary>
		/// <param name="organizations">The number of locations</param>
		/// <returns>a list of generated locations ids</returns>
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
			Logger.Info("Added locations in " + watch.ElapsedMilliseconds + "ms" + "(" + (watch.ElapsedMilliseconds / 1000) + "s)");
			return result.ToArray();
		}

		/// <summary>
		/// Used to pass data along to a thread, when in multi threaded mode
		/// </summary>
		struct ProductTreeData
		{
			public ProductTreeData(int org, int productsPerSupplier, int depth, int breadthPerLevel, int orgIter, int prodIter, int locationId)
			{
				this.org = org;
				this.productsPerSupplier = productsPerSupplier;
				this.depth = depth;
				this.breadthPerLevel = breadthPerLevel;
				this.orgIter = orgIter;
				this.prodIter = prodIter;
				this.locationId = locationId;
			}

			public int org { get; }
			public int productsPerSupplier { get; }
			public int depth { get; }
			public int breadthPerLevel { get; }
			public int orgIter;
			public int prodIter;
			public int locationId;
		}

		/// <summary>
		/// Multi threaded way to generate all of the product trees necessary (amount of top level suppliers * amount of products) 
		/// starts with the top level products, and then proceeds with all branches in the tree
		/// </summary>
		/// <param name="organizationIds">The id's of the top level organizations</param>
		/// <param name="productsPerSupplier">The number of products each supplier should get</param>
		/// <param name="depth">The depth of each chain (in how many branches does each product split up)</param>
		/// <param name="breadthPerLevel">The breadth of each chain per level; the number of products to generate for the product above</param>
		/// <param name="locations">The location ids to be used</param>
		private void mt_GenerateProductTrees(int[] organizationIds, int productsPerSupplier, int depth, int breadthPerLevel, int[] locations)
		{
			var watch = System.Diagnostics.Stopwatch.StartNew();
			List<Thread> threads = new List<Thread>();
			List<ProductTreeData> data = new List<ProductTreeData>();

			for(int i = 0; i < organizationIds.Length; i++)
			{
				for (int j = 0; j < productsPerSupplier; j++)
				{
					threads.Add(new Thread(mt_GenerateProductTree));
					data.Add(new ProductTreeData(organizationIds[i], productsPerSupplier, depth, breadthPerLevel, i, j, locations[i]));
				}
			}

			for(int i = 0; i < threads.Count; i++)
			{
				threads[i].Start(data[i]);
				Logger.Debug("Starting thread " + i + "..");
			}

			for (int i = 0; i < threads.Count; i++)
			{
				threads[i].Join();
			}
			watch.Stop();
			Logger.Info("Took " + watch.ElapsedMilliseconds + "ms " + "(" + (watch.ElapsedMilliseconds / 1000) + "s) to generate products and relations");

		}

		/// <summary>
		/// Generates one product tree, to be used with a single thread of the multi-threaded way of generating product trees 
		/// </summary>
		/// <param name="data">ProductTree data</param>
		private void mt_GenerateProductTree(object data)
		{
			ProductTreeData d = (ProductTreeData) data;
			int topLevelId = 0;

			// generate the top level product
			using (ISession session = connector.Connection.Session())
			{
				//string statement = "CREATE (n:Product { name: 'TopLevelProduct #o" + (i + 1) + "-p" + (j + 1) + "' }) RETURN ID(n) AS id";
				string statement = InsertProduct(true, d.orgIter * d.productsPerSupplier + d.prodIter);
				IStatementResult res = session.WriteTransaction(tx => tx.Run(statement));
				IRecord record = res.Peek();
				topLevelId = record["id"].As<int>();
			}

			// add the top level product to the top level supplier
			using (ISession session = connector.Connection.Session())
			{
				string statement = "MATCH (p:Product), (o:Organization), (l:Location)" +
									" WHERE ID(p) = " + topLevelId +
									" AND ID(o) = " + d.org +
									" AND ID(l) = " + d.locationId +
									" CREATE (o)-[:PERFORMS]->(a:Activity " + InsertActivity(d.orgIter) + ")," +
									" (a)-[:PRODUCES]->(p)," +
									" (a)-[:LOCATED_AT]->(l)" +
									" MERGE (o)-[:HAS_LOCATION]->(l)";
				IStatementResult res = session.WriteTransaction(tx => tx.Run(statement));
			}

			List<int> previousResults = new List<int>();

			// for the depth of the chain
			for (int k = 0; k < d.depth; k++)
			{
				// in the first pass, generate the products for our top level product
				if (k == 0)
				{
					previousResults.Add(topLevelId);
					previousResults = GenerateProductRowAndRelations(previousResults.ToArray(), d.breadthPerLevel);

				}
				// in subsequent passes, take the previous row of products and generate a new underlying row for all of them
				else
				{
					previousResults = GenerateProductRowAndRelations(previousResults.ToArray(), d.breadthPerLevel);
				}

			}
		}

		/// <summary>
		/// Generates all of the product trees necessary (amount of top level suppliers * amount of products) 
		/// starts with the top level products, and then proceeds with all branches in the tree
		/// </summary>
		/// <param name="organizationIdentifiers">The id's of the top level organizations</param>
		/// <param name="productsPerSupplier">The number of products each supplier should get</param>
		/// <param name="depth">The depth of each chain (in how many branches does each product split up)</param>
		/// <param name="breadthPerLevel">The breadth of each chain per level; the number of products to generate for the product above</param>
		/// <param name="locations">The location ids to be used</param>
		private void GenerateProductTrees(int[] organizationIdentifiers, int productsPerSupplier, int depth, int breadthPerLevel, int[] locationIdentifiers)
		{
			var watchTotal = System.Diagnostics.Stopwatch.StartNew();
			// for each top level supplier
			for (int i = 0; i < organizationIdentifiers.Length; i++)
			{
				Logger.Debug("Generating for Organization: " + (i + 1));

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

					// add the top level product to the top level supplier
					using (ISession session = connector.Connection.Session())
					{
						string statement = "MATCH (p:Product), (o:Organization), (l:Location)" +
											" WHERE ID(p) = " + topLevelId +
											" AND ID(o) = " + organizationIdentifiers[i] +
											" AND ID(l) = " + locationIdentifiers[i] +
											" CREATE (o)-[:PERFORMS]->(a:Activity " + InsertActivity(i) + ")," +
											" (a)-[:PRODUCES]->(p)," +
											" (a)-[:LOCATED_AT]->(l)" +
											" MERGE (o)-[:HAS_LOCATION]->(l)";
						IStatementResult res = session.WriteTransaction(tx => tx.Run(statement));
						IRecord record = res.Peek();
					}

					List<int> previousResults = new List<int>();

					// for the depth of the chain
					for (int k = 0; k < depth; k++)
					{
						// in the first pass, generate the products for our top level product
						if (k == 0)
						{
							previousResults.Add(topLevelId);
							previousResults = GenerateProductRowAndRelations(previousResults.ToArray(), breadthPerLevel);

						}
						// in subsequent passes, take the previous row of products and generate a new underlying row for all of them
						else
						{
							previousResults = GenerateProductRowAndRelations(previousResults.ToArray(), breadthPerLevel);
						}

					}
					watch.Stop();
					Logger.Info("Took " + watch.ElapsedMilliseconds + "ms " + "(" + (watch.ElapsedMilliseconds / 1000) + "s) " +
						"to generate products and relations for product " + (j + 1) + " for organisation " + (i + 1));
				}
			}
			watchTotal.Stop();
			Logger.Info("Took " + watchTotal.ElapsedMilliseconds + "ms " + "(" + (watchTotal.ElapsedMilliseconds / 1000) + "s) in total");
		}

		/// <summary>
		/// Generates a single row of products for the tree, given the list of parent product ids
		/// </summary>
		/// <param name="parentProductIdentifiers">the list of parent products for which a number of child products has to be generated</param>
		/// <param name="breadthPerLevel">the number of child products that have to be generated per parent product</param>
		/// <returns>a list of the id's of the child products that have been created</returns>
		private List<int> GenerateProductRowAndRelations(int[] parentProductIdentifiers, int breadthPerLevel)
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
		/// Adds organizations and activities to all the products in the database
		/// </summary>
		/// <param name="numberOfOrganizations">the total number of organizations specified</param>
		/// <param name="numberOfTopLevelOrganizations">the total number of activities specified</param>
		/// <param name="numberOfActivities">the total number of products specified</param>
		/// <param name="chainDepth">the chain depth specified</param>
		/// <param name="chainBreadth">the chain breadth specified</param>
		private void AddOrganizationsAndActivitiesToProductTree(int[] organizationIds, int[] locationIds)
		{
			var watch = System.Diagnostics.Stopwatch.StartNew();
			List<int> productIds = new List<int>();
			Logger.Debug("Gettings product IDs to create relations to organizations..");
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
			Logger.Debug("Retrieved " + productIdsArray.Length + " products");

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
										// we need to make sure not to assign the top level products twice
										" AND NOT p.name STARTS WITH 'Top Level Product'" +
										" CREATE (o)-[:PERFORMS]->(a:Activity " + InsertActivity(i) + ")," +
										" (a)-[:PRODUCES]->(p)," +
										" (a)-[:LOCATED_AT]->(l)" +
										// and not create the has location relationship multiple times
										"MERGE (o)-[:HAS_LOCATION]->(l)";
					//" CREATE (o)-[:CARRIES_OUT]->(a:Activity { name:'Activity # " + i + "' })-[:PRODUCES]->(p)";
					session.WriteTransaction(tx => tx.Run(statement));
				}
			}
			watch.Stop();
			Logger.Info("Took " + watch.ElapsedMilliseconds + "ms " + "(" + (watch.ElapsedMilliseconds / 1000) + "s) " + "(" + (watch.ElapsedMilliseconds / 1000 / 60) + "m) to add organizations and activities");
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
