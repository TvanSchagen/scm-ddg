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

        public override void InitializeConnection()
        {
			connector = MySqlConnector.Instance();
        }

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
		/// <param name="organizations"></param>
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
		/// <param name="organizations"></param>
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
				Console.WriteLine("Generating for Organization: " + organizationIdentifiers[i]);

				// for each of their products
				for (int j = 0; j < productsPerSupplier; j++)
				{
					Console.WriteLine("Generating for product: " + j);

					// generate the top level product
					string statement = "INSERT INTO product(name) VALUES(" + "'Top Level Product #" + (j + 1).ToString() + "');";
					MySqlCommand com = new MySqlCommand(statement, connector.Connection);
					com.ExecuteNonQuery();
					int topLevelId = (int)com.LastInsertedId;

					// for the depth of the chain
					for (int k = 0; k < depth; k++)
					{
						Console.WriteLine("Generating for depth: " + k);

						int[] topLeveLProductId = new int[1];
						topLeveLProductId[0] = topLevelId;

						List<int> previousResults = new List<int>();

						// in the first pass, generate the products for our top level product
						if (k == 0)
						{
							Console.WriteLine("First loop");
							previousResults = GenerateProductRowAndRelations(organizationIdentifiers[i], topLeveLProductId, breadthPerLevel);
						}
						// in subsequent passes, take the previous row of products and generate a new underlying row for all of them
						else
						{
							previousResults = GenerateProductRowAndRelations(organizationIdentifiers[i], previousResults.ToArray(), breadthPerLevel);
						}
						
					}
				}
			}

			watch.Stop();
			Console.WriteLine("Took " + watch.ElapsedMilliseconds + "ms to generate products and relations");
		}

		/// <summary>
		/// Adds a single row of products in a product tree
		/// </summary>
		/// <param name="parentProductIdentifier"></param>
		private List<int> GenerateProductRowAndRelations(int chainId, int[] parentProductIdentifiers, int breadthPerLevel)
		{
			List<int> childProductsCreated = new List<int>();
			for (int i = 0; i < parentProductIdentifiers.Length; i++)
			{
				Console.WriteLine("Generating for Parent Product: " + parentProductIdentifiers[i]);

				for (int j = 0; j < breadthPerLevel; j++)
				{
					Console.WriteLine("Generating for breadth " + j);

					// generate product
					string statement = "INSERT INTO product(name) VALUES(" + "'Product #" + chainId + "-" + i + "-" + j + "');";
					MySqlCommand com = new MySqlCommand(statement, connector.Connection);
					com.ExecuteNonQuery();
					int childProductId = (int)com.LastInsertedId;
					childProductsCreated.Add(childProductId);

					// add the product into the hierarchy
					string statement2 = "INSERT INTO consists_of(parent_product_id, child_product_id) VALUES(" + parentProductIdentifiers[i] + "," + childProductId + ");";
					MySqlCommand com2 = new MySqlCommand(statement2, connector.Connection);
					com2.ExecuteNonQuery();
				}
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