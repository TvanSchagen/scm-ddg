using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using DummyDataGenerator.Connectors;
using MySql.Data;
using MySql.Data.MySqlClient;
// add fakedata generator

namespace DummyDataGenerator
{
	class MySqlGenerator : Database
	{

		MySqlConnector connector;

		/// <summary>
		/// Initializes the singleton connection
		/// </summary>
		public override void InitializeConnection()
		{
			connector = MySqlConnector.Instance();
		}

		/// <summary>
		/// Closes the singleton connection
		/// </summary>
		public override void CloseConnection()
		{
			connector.Close();
		}

		/// <summary>
		/// Generates the data with the specified parameters of the configuration
		/// </summary>
		/// <param name="config">The configuration that holds the paremeters for the data</param>
		public override void GenerateData(Configuration config)
		{
			RefreshDatabaseSchema();
			int[] tlOrgs = GenerateTopLevelOrganizations(config.NumberOfTopLevelSuppliers);
			GenerateOrganizations(config.NumberOfSuppliers);
			GenerateActivities(config.NumberOfActivities);
			GenerateProductTrees(tlOrgs, config.NumberOfProducts, config.ChainDepth, config.ChainBreadth);
			AddOrganizationsAndActivitiesToProductTree(config.NumberOfSuppliers, config.NumberOfTopLevelSuppliers, config.NumberOfActivities, config.NumberOfProducts);
			AddMetaData(config);
			Console.WriteLine("Done..");
		}

		/// <summary>
		/// Drops and creates the database, as to start off with a clean sheet
		/// </summary>
		private void RefreshDatabaseSchema()
		{
			var watch = System.Diagnostics.Stopwatch.StartNew();
			MySqlScript script = new MySqlScript(connector.Connection, File.ReadAllText("mysql_generate_schema.sql"));
			try
			{
				Console.WriteLine("Executing refresh schema script..");
				script.Execute();
			}
			catch (MySqlException e)
			{
				Console.WriteLine("An error ocurred executing the refresh schema script: " + e);
			}
			finally
			{
				watch.Stop();
				
			}
			Console.WriteLine("Refreshed schema in " + watch.ElapsedMilliseconds + "ms" + "(" + (watch.ElapsedMilliseconds / 1000) + "s) " +);
		}

		/// <summary>
		/// Generates a set number of top level organizations; organizations that do not supply any others
		/// </summary>
		/// <param name="organizations">The number of organiations</param>
		/// <returns>a list of generated organization ids</returns>
		private int[] GenerateTopLevelOrganizations(int organizations)
		{
			List<int> result = new List<int>();
			var watch = System.Diagnostics.Stopwatch.StartNew();
			for (int i = 0; i < organizations; i++)
			{
				string statement = "INSERT INTO organization(name) VALUES(" + "'TopLevelOrganization #" + (i + 1).ToString() + "');";
				MySqlCommand com = new MySqlCommand(statement, connector.Connection);
				com.ExecuteNonQuery();
				result.Add((int)com.LastInsertedId);
			}
			watch.Stop();
			Console.WriteLine("Took " + watch.ElapsedMilliseconds + "ms" + "(" + (watch.ElapsedMilliseconds / 1000) + "s) " + " to generate " + result.Count + " top level organisations");
			return result.ToArray();
		}

		/// <summary>
		/// Generatea a set number of organizations to be used during the tree generation
		/// </summary>
		/// <param name="organizations">The number of organiations</param>
		/// <returns>a list of generated organization ids</returns>
		private int[] GenerateOrganizations(int organizations)
		{
			List<int> result = new List<int>();
			var watch = System.Diagnostics.Stopwatch.StartNew();
			for (int i = 0; i < organizations; i++)
			{
				string statement = "INSERT INTO organization(name) VALUES(" + "'Organization #" + (i + 1).ToString() + "');";
				MySqlCommand com = new MySqlCommand(statement, connector.Connection);
				com.ExecuteNonQuery();
				result.Add((int)com.LastInsertedId);
			}
			watch.Stop();
			Console.WriteLine("Took " + watch.ElapsedMilliseconds + "ms" + "(" + (watch.ElapsedMilliseconds / 1000) + "s) " + " to generate " + result.Count + " organisations");
			return result.ToArray();
		}

		/// <summary>
		/// Generatea a set number of activities to be used during tree generation
		/// </summary>
		/// <param name="activites">The number of activities to be generated</param>
		/// <returns>a list of generated activity ids</returns>
		private int[] GenerateActivities(int activites)
		{
			List<int> result = new List<int>();
			var watch = System.Diagnostics.Stopwatch.StartNew();
			for (int i = 0; i < activites; i++)
			{
				string statement = "INSERT INTO activity(name) VALUES(" + "'Activity #" + (i + 1).ToString() + "');";
				MySqlCommand com = new MySqlCommand(statement, connector.Connection);
				com.ExecuteNonQuery();
				result.Add((int)com.LastInsertedId);
			}
			watch.Stop();
			Console.WriteLine("Took " + watch.ElapsedMilliseconds + "ms" + "(" + (watch.ElapsedMilliseconds / 1000) + "s) " + " to generate " + result.Count + " activities");
			return result.ToArray();
		}

		/// <summary>
		/// Generates the product trees for specified organizations
		/// </summary>
		/// <param name="organizationIdentifiers">The id's of the top level organizations</param>
		/// <param name="productsPerSupplier">The number of products each supplier should get</param>
		/// <param name="depth">The depth of each chain</param>
		/// <param name="breadthPerLevel">The breadth of each chain per level; the number of products to generate for the product above</param>
		private void GenerateProductTrees(int[] organizationIdentifiers, int productsPerSupplier, int depth, int breadthPerLevel)
		{

			// for each top level supplier
			for (int i = 0; i < organizationIdentifiers.Length; i++)
			{
				Console.WriteLine("Generating for Organization: " + i);

				// for each of their products
				for (int j = 0; j < productsPerSupplier; j++)
				{
					var watch = System.Diagnostics.Stopwatch.StartNew();
					//Console.WriteLine("Generating for product: " + j);

					// generate the top level product
					string statement = "INSERT INTO product(name) VALUES(" + "'Top Level Product #o" + (i + 1) + "-p" + (j + 1).ToString() + "');";
					MySqlCommand com = new MySqlCommand(statement, connector.Connection);
					com.ExecuteNonQuery();
					int topLevelId = (int) com.LastInsertedId;

					List<int> previousResults = new List<int>();
					// for the depth of the chain
					for (int k = 0; k < depth; k++)
					{
						//Console.WriteLine("Generating for depth: " + k);

						// in the first pass, generate the products for our top level product
						if (k == 0)
						{
							previousResults.Add(topLevelId);
							// Console.WriteLine("First loop {0}, {1}, {2}", organizationIdentifiers[i], string.Join(",", previousResults), breadthPerLevel);
							previousResults = GenerateProductRowAndRelations(i, previousResults, breadthPerLevel);

						}
						// in subsequent passes, take the previous row of products and generate a new underlying row for all of them
						else
						{
							previousResults = GenerateProductRowAndRelations(i, previousResults, breadthPerLevel);
							// Console.WriteLine("Second loop {0}, {1}, {2}", organizationIdentifiers[i], string.Join(",", previousResults), breadthPerLevel);
						}

					}
					watch.Stop();
					Console.WriteLine("Took " + watch.ElapsedMilliseconds + "ms" + "(" + (watch.ElapsedMilliseconds / 1000) + "s) " +
						"to generate products and relations for product " + j + " for organisation " + i);
				}
			}

		}

		/// <summary>
		/// Adds a single row to a single product hierarchy
		/// </summary>
		/// <param name="chainId">an identifier for a chain, to more clearly identify the products associated to this chain in the database</param>
		/// <param name="parentProductIdentifiers">the list of parent products for which a number of child products has to be generated</param>
		/// <param name="breadthPerLevel">the number of child products that have to be generated per parent product</param>
		/// <returns>a list of the id's of the child products that have been created</returns>
		private List<int> GenerateProductRowAndRelations(int chainId, List<int> parentProductIdentifiers, int breadthPerLevel)
		{
			// Console.WriteLine("Generating for " + chainId + ", parents: " +  string.Join(",", parentProductIdentifiers));
			List<int> childProductsCreated = new List<int>();
			// Console.WriteLine("parentProductIds length: " + parentProductIdentifiers.Count);
			int i = 0;
			foreach (int item in parentProductIdentifiers)
			{
				//Console.WriteLine("Generating for Parent Product: " + parentProductIdentifiers[i]);

				for (int j = 0; j < breadthPerLevel; j++)
				{
					//Console.WriteLine("Generating for breadth " + j);

					// generate product
					string statement = "INSERT INTO product(name) VALUES(" + "'Product #c" + (chainId + 1) + "-p" + (i + 1) + "-b" + (j + 1) + "');";
					MySqlCommand com = new MySqlCommand(statement, connector.Connection);
					com.ExecuteNonQuery();
					int childProductId = (int)com.LastInsertedId;
					// Console.WriteLine("Added product " + childProductId);
					childProductsCreated.Add(childProductId);
					// Console.WriteLine("List length " + childProductsCreated.Count);

					// add the product into the hierarchy
					string statement2 = "INSERT INTO consists_of(parent_product_id, child_product_id) VALUES(" + parentProductIdentifiers[i] + "," + childProductId + ");";
					MySqlCommand com2 = new MySqlCommand(statement2, connector.Connection);
					com2.ExecuteNonQuery();
				}
				i++;
			}
			return childProductsCreated;
		}

		/// <summary>
		/// Adds organizations and activities to all the products in the database
		/// </summary>
		/// <param name="numberOfOrganizations">the total number of organizations specified</param>
		/// <param name="numberOfActivites">the total number of activities specified</param>
		/// <param name="numberOfProducts">the total number of products specified</param>
		private void AddOrganizationsAndActivitiesToProductTree(int numberOfOrganizations, int numberOfTopLevelOrganizations, int numberOfActivites, int numberOfProducts)
		{
			var watch = System.Diagnostics.Stopwatch.StartNew();
			int count = 0;
			string statement = "SELECT COUNT(*) FROM product";
			MySqlCommand com = new MySqlCommand(statement, connector.Connection);
			object result = com.ExecuteScalar();
			if (result != null)
			{
				count = Convert.ToInt32(result);
			}
			else
			{
				throw new Exception("Could not return count!");
			}

			for (int i = 0; i < count; i++)
			{
				string statement2 = string.Format("INSERT INTO supplies(organization_id, activity_id, product_id) VALUES({0}, {1}, {2});",
					// add an offset of the toplevel suppliers, which are added first to the database, then modulo if the n.o. products outnumbers the n.o. organizations
					((i + numberOfTopLevelOrganizations) % numberOfOrganizations) + 1,
					// modulo if the n.o.products outnumbers the n.o. activities
					(i % numberOfActivites) + 1,
					// use the iterator as the product id
					i + 1);
				//Console.WriteLine(statement2);
				MySqlCommand com2 = new MySqlCommand(statement2, connector.Connection);
				com2.ExecuteNonQuery();
			}
			watch.Stop();
			Console.WriteLine("Added organizations and activities to the product tree in " + watch.ElapsedMilliseconds + "ms" + "(" + (watch.ElapsedMilliseconds / 1000) + "s) ");
		}

		/// <summary>
		/// Adds metadata to a separate table in the database
		/// </summary>
		/// <param name="config">The supplied configuration</param>
		void AddMetaData(Configuration config)
		{
			string statement = "DROP TABLE IF EXISTS `db_meta`; CREATE TABLE `db_meta` (`meta_name` VARCHAR(50), `meta_value` VARCHAR(50)) ENGINE=InnoDB;";
			MySqlCommand com = new MySqlCommand(statement, connector.Connection);
			com.ExecuteNonQuery();

			string statement2 = string.Format("INSERT INTO db_meta(meta_name, meta_value) VALUES('NumberOfActivities', '{0}'), ('NumberOfProductsPerTopLevelSupplier', '{1}'), ('NumberOfSuppliers', '{2}'), ('NumberOfTopLevelSuppliers', '{3}'), ('ChainBreadth', '{4}'), ('ChainDepth', '{5}')", 
				config.NumberOfActivities, 
				config.NumberOfProducts, 
				config.NumberOfSuppliers, 
				config.NumberOfTopLevelSuppliers, 
				config.ChainBreadth, 
				config.ChainDepth);
			MySqlCommand com2 = new MySqlCommand(statement2, connector.Connection);
			com2.ExecuteNonQuery();
		}

	}

}