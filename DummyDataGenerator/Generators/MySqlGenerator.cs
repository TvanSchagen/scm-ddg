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
			int[] orgs = GenerateOrganizations(config.NumberOfSuppliers);
			int[] acts = GenerateActivities(config.NumberOfActivities);
			GenerateProductTrees(tlOrgs, config.NumberOfProducts, config.ChainDepth, config.ChainBreadth);
			/*
			AddOrganizationsAndActivitiesToProductTree();
			*/
			Console.WriteLine("Done..");
        }

		/// <summary>
		/// Drops and creates the database, as to start off with a clean sheet
		/// </summary>
		private void RefreshDatabaseSchema()
		{
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
				result.Add((int) com.LastInsertedId);
			}
			watch.Stop();
			Console.WriteLine("Took " + watch.ElapsedMilliseconds + "ms to generate " + result.Count + " top level organisations");
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
				result.Add((int) com.LastInsertedId);
			}
			watch.Stop();
			Console.WriteLine("Took " + watch.ElapsedMilliseconds + "ms to generate " + result.Count + " organisations");
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
			Console.WriteLine("Took " + watch.ElapsedMilliseconds + "ms to generate " + result.Count + " activities");
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
			var watch = System.Diagnostics.Stopwatch.StartNew();

			// for each top level supplier
			for (int i = 0; i < organizationIdentifiers.Length; i++)
			{
				Console.WriteLine("Generating for Organization: " + i);

				// for each of their products
				for (int j = 0; j < productsPerSupplier; j++)
				{
					//Console.WriteLine("Generating for product: " + j);

					// generate the top level product
					string statement = "INSERT INTO product(name) VALUES(" + "'Top Level Product #" + (j + 1).ToString() + "');";
					MySqlCommand com = new MySqlCommand(statement, connector.Connection);
					com.ExecuteNonQuery();
					int topLevelId = (int)com.LastInsertedId;

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
							// Console.WriteLine("Second loop {0}, {1}, {2} ", organizationIdentifiers[i], string.Join(",", previousResults), breadthPerLevel);
						}
						
					}
				}
			}

			watch.Stop();
			Console.WriteLine("Took " + watch.ElapsedMilliseconds + "ms to generate products and relations");
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
					string statement = "INSERT INTO product(name) VALUES(" + "'Product #c" + (chainId+) + "-p" + (i+1) + "-b" + (j+1) + "');";
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
		/// Adds organizations to all of the products
		/// </summary>
		private void AddOrganizationsAndActivitiesToProductTree()
		{

		}

    }

}