using DummyDataGenerator.Connectors;
using MySql.Data.MySqlClient;
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;

namespace DummyDataGenerator.Generators
{
	class MySqlClosureTableGenerator : MySqlGenerator
	{

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
			AddOrganizationsAndActivitiesToProductTree(config.NumberOfSuppliers, config.NumberOfTopLevelSuppliers, config.NumberOfActivities, config.NumberOfProducts, config.ChainDepth, config.ChainBreadth);
			AddMetaData(config);
			Console.WriteLine("Program done, press enter to continue");
		}

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

					// generate the top level product
					string statement = "INSERT INTO product(name) VALUES(" + "'Top Level Product #o" + (i + 1) + "-p" + (j + 1) + "');";
					MySqlCommand com = new MySqlCommand(statement, connector.Connection);
					com.ExecuteNonQuery();
					int topLevelId = (int)com.LastInsertedId;

					// add it to the hierarchy
					string statement2 = string.Format("INSERT INTO consists_of(parent_product_id, child_product_id) VALUES({0}, {0})", topLevelId);
					MySqlCommand com2 = new MySqlCommand(statement2, connector.Connection);
					com2.ExecuteNonQuery();

					// this list represents the products in the previous row of the current hierarchy
					List<int> previousResults = new List<int>();
					// this list represents all the products in the current hierarchy
					List<int> allResults = new List<int>();

					// for the depth of the chain
					for (int k = 0; k < depth; k++)
					{
						// in the first pass, generate the products for our top level product
						if (k == 0)
						{
							// add the top level id as our previous row
							previousResults.Add(topLevelId);
							// also add the top level id to all results (because it isn't returned as childs when generating the product row and relations)
							allResults.Add(topLevelId);
							previousResults = GenerateProductRowAndRelations(i, k + 1, null, previousResults, breadthPerLevel);
							allResults.AddRange(previousResults);

						}
						// in subsequent passes, take the previous row of products and generate a new underlying row for all of them
						else
						{
							previousResults = GenerateProductRowAndRelations(i, k + 1, allResults, previousResults, breadthPerLevel);
							allResults.AddRange(previousResults);
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
		/// Adds a single row to a single product hierarchy
		/// </summary>
		/// <param name="chainId">an identifier for a chain, to more clearly identify the products associated to this chain in the database</param>
		/// <param name="parentProductIdentifiers">the list of parent products for which a number of child products has to be generated</param>
		/// <param name="breadthPerLevel">the number of child products that have to be generated per parent product</param>
		/// <returns>a list of the id's of the child products that have been created</returns>
		/// ---
		/// TO DO: fix a bug -> currently, when generating chains each child level adds relations for all the parent products, even if they are in another branch of the tree
		/// ---
		private List<int> GenerateProductRowAndRelations(int chainId, int depth, List<int> allParentProductIdentifiers, List<int> immediateParentProductIdentifiers, int breadthPerLevel)
		{
			List<int> childProductsCreated = new List<int>();
			int i = 0;

			foreach (int item in immediateParentProductIdentifiers)
			{
				for (int j = 0; j < breadthPerLevel; j++)
				{
					// first insert the product
					string statement = "INSERT INTO product(name) VALUES(" + "'Product #c" + (chainId + 1) + "-d" + depth + "-p" + (i + 1) + "-b" + (j + 1) + "');";
					MySqlCommand com = new MySqlCommand(statement, connector.Connection);
					com.ExecuteNonQuery();
					// get the id back from the database
					int childProductId = (int)com.LastInsertedId;
					// and add it to the child products list
					childProductsCreated.Add(childProductId);
					
					// then for the tree structure, add a reference to itself
					string statement2 = "INSERT INTO consists_of(parent_product_id, child_product_id) VALUES(" + childProductId + "," + childProductId + "); ";
					MySqlCommand com2 = new MySqlCommand(statement2, connector.Connection);
					com2.ExecuteNonQuery();
					
					// then add a row for the reference to the immediate parent
					string statement3 = "INSERT INTO consists_of(parent_product_id, child_product_id) VALUES(" + immediateParentProductIdentifiers[i] + "," + childProductId + ");";
					MySqlCommand com3 = new MySqlCommand(statement3, connector.Connection);
					com3.ExecuteNonQuery();

					List<int> productIds = new List<int>();
					if (allParentProductIdentifiers != null)
					{
						// select all of our parent products for the created child product
						string statement4 = string.Format("WITH RECURSIVE cte ( ppid, cpid ) AS ( SELECT parent.id, child.id FROM consists_of JOIN product AS parent ON consists_of.parent_product_id = parent.id JOIN product AS child ON consists_of.child_product_id = child.id WHERE child.id = {0} UNION ALL SELECT parent.id, child.id FROM consists_of JOIN product AS parent ON consists_of.parent_product_id = parent.id JOIN product AS child ON consists_of.child_product_id = child.id JOIN cte ON cte.ppid = child.id WHERE consists_of.child_product_id <> consists_of.parent_product_id ) SELECT GROUP_CONCAT(DISTINCT ppid ORDER BY ppid DESC SEPARATOR ',') AS ids FROM cte ", childProductId);
						MySqlCommand com4 = new MySqlCommand(statement4, connector.Connection);
						string list = (string) com4.ExecuteScalar();
						productIds = list.Split(',').Select(Int32.Parse).ToList();
						// Console.WriteLine("Parent Product Ids: " + list + " for child " + childProductId);

						// skip the first two id's, because they the first one is referencing themselves, and the second one was already inserted before (initial parent > child relation)
						for (int k = 2; k < productIds.Count; k++)
						{
							// inserto into hierarchy
							string statement5 = string.Format("INSERT INTO consists_of(parent_product_id, child_product_id) VALUES(" + productIds[k] + "," + childProductId + ");");
							// Console.WriteLine("Inserting: " + productIds[k]);
							MySqlCommand com5 = new MySqlCommand(statement5, connector.Connection);
							com5.ExecuteNonQuery();
						}
					}
				
				}
				i++;
			}
			return childProductsCreated;
		}

	}
}
