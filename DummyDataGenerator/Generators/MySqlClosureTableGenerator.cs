using DummyDataGenerator.Utils;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.IO;

namespace DummyDataGenerator.Generators
{
	class MySqlClosureTableGenerator : MySqlGenerator
	{

		/// <summary>
		/// Generates the data with the specified parameters of the configuration
		/// </summary>
		/// <param name="config">The configuration that holds the paremeters for the data</param>
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
			Console.WriteLine("Program done, press enter to continue");
		}

		/// <summary>
		/// Drops and creates the database, as to start off with a clean sheet
		/// </summary>
		private new void RefreshDatabaseSchema()
		{
			var watch = System.Diagnostics.Stopwatch.StartNew();
			MySqlScript script = new MySqlScript(connector.Connection, File.ReadAllText(Properties.Resources.mysql_generate_schema_ct));
			try
			{
				Logger.Debug("Executing refresh schema script..");
				script.Execute();
			}
			catch (MySqlException e)
			{
				Logger.Error("An error ocurred executing the refresh schema script: " + e);
			}
			finally
			{
				watch.Stop();

			}
			Logger.Info("Refreshed schema in " + watch.ElapsedMilliseconds + "ms " + "(" + (watch.ElapsedMilliseconds / 1000) + "s) ");

			// set autocommit to off
			string statement = "START TRANSACTION;";
			MySqlCommand com = new MySqlCommand(statement, connector.Connection);
			com.ExecuteNonQuery();
		}

		/// <summary>
		/// Generates all of the product trees necessary (amount of top level suppliers * amount of products) 
		/// starts with the top level products, and then proceeds with all branches in the tree
		/// </summary>
		/// <param name="organizationIdentifiers">The id's of the top level organizations</param>
		/// <param name="productsPerSupplier">The number of products each supplier should get</param>
		/// <param name="depth">The depth of each chain (in how many branches does each product split up)</param>
		/// <param name="breadthPerLevel">The breadth of each chain per level; the number of products to generate for the product above</param>
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
					// string statement = "INSERT INTO product(name) VALUES(" + "'Top Level Product #o" + (i + 1) + "-p" + (j + 1) + "');";
					MySqlCommand com = InsertProduct(true, i * productsPerSupplier + j);
					com.ExecuteNonQuery();
					int topLevelId = (int)com.LastInsertedId;

					// make sure the top level suppliers supply the top level product
					string statement = "INSERT INTO supplies(organization_id, product_id, activity_id, location_id) VALUES(" + organizationIdentifiers[i] + ", " + topLevelId + ", " + (i + 1) + ", " + (i + 1) + ")";
					Console.WriteLine(statement);
					MySqlCommand com2 = new MySqlCommand(statement, connector.Connection);
					com2.ExecuteNonQuery();

					// add it to the hierarchy
					string statement2 = string.Format("INSERT INTO consists_of(parent_product_id, child_product_id, path_length) VALUES({0}, {0}, 0)", topLevelId);
					MySqlCommand com3 = new MySqlCommand(statement2, connector.Connection);
					com3.ExecuteNonQuery();

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
							previousResults = GenerateProductRowAndRelations(null, previousResults, breadthPerLevel, connector.Connection, k);
							allResults.AddRange(previousResults);

						}
						// in subsequent passes, take the previous row of products and generate a new underlying row for all of them
						else
						{
							previousResults = GenerateProductRowAndRelations(allResults, previousResults, breadthPerLevel, connector.Connection, k);
							allResults.AddRange(previousResults);
						}

					}
					watch.Stop();
					Console.WriteLine("Took " + watch.ElapsedMilliseconds + "ms " + "(" + (watch.ElapsedMilliseconds / 1000) + "s) " +
						"to generate products and relations for product " + (j + 1) + " for organisation " + (i + 1));
				}
			}
			watchTotal.Stop();
			Console.WriteLine("Took " + watchTotal.ElapsedMilliseconds + "ms " + "(" + (watchTotal.ElapsedMilliseconds / 1000) + "s)" + "(" + (watchTotal.ElapsedMilliseconds / 1000 / 60) + "m) in total");
		}

		/// <summary>
		/// Adds a single row to a single product hierarchy
		/// </summary>
		/// <param name="parentProductIdentifiers">the list of all previous parent products for which a number of child products has to be generated</param>
		/// <param name="immediateParentProductIdentifiers">the list of parent products for which a number of child products has to be generated</param>
		/// <param name="breadthPerLevel">the number of child products that have to be generated per parent product</param>
		/// <returns>a list of the id's of the child products that have been created</returns>
		private List<int> GenerateProductRowAndRelations(List<int> allParentProductIdentifiers, List<int> immediateParentProductIdentifiers, int breadthPerLevel, MySqlConnection c, int depth)
		{
			List<int> childProductsCreated = new List<int>();
			int i = 0;

			foreach (int item in immediateParentProductIdentifiers)
			{
				for (int j = 0; j < breadthPerLevel; j++)
				{
					// first insert the product
					// string statement = "INSERT INTO product(name) VALUES(" + "'Product #c" + (chainId + 1) + "-d" + depth + "-p" + (i + 1) + "-b" + (j + 1) + "');";
					MySqlCommand com = InsertProduct(false, item * 2 + j);
					com.ExecuteNonQuery();
					// get the id back from the database
					int childProductId = (int)com.LastInsertedId;
					// and add it to the child products list
					childProductsCreated.Add(childProductId);
					
					// then for the tree structure, add a reference to itself
					string statement2 = "INSERT INTO consists_of(parent_product_id, child_product_id, path_length) VALUES(" + childProductId + "," + childProductId + ", + 0); ";
					MySqlCommand com2 = new MySqlCommand(statement2, c);
					com2.ExecuteNonQuery();
					
					// then add a row for the reference to the immediate parent
					string statement3 = "INSERT INTO consists_of(parent_product_id, child_product_id, path_length) VALUES(" + immediateParentProductIdentifiers[i] + "," + childProductId + ", " + 1 + ");";
					MySqlCommand com3 = new MySqlCommand(statement3, c);
					com3.ExecuteNonQuery();

					List<int> productIds = new List<int>();
					if (allParentProductIdentifiers != null)
					{
						// first, retrieve the parent products and their path lengths
						List<int> ids = new List<int>();
						List<int> pathlengths = new List<int>();
						string stmt = string.Format(@"WITH RECURSIVE cte ( ppid, cpid, pl ) AS ( 
													SELECT parent.id, child.id, path_length FROM consists_of 
													JOIN product AS parent ON consists_of.parent_product_id = parent.id 
													JOIN product AS child ON consists_of.child_product_id = child.id 
													WHERE child.id = {0} 
													UNION ALL 
													SELECT parent.id, child.id, path_length FROM consists_of 
													JOIN product AS parent ON consists_of.parent_product_id = parent.id
													JOIN product AS child ON consists_of.child_product_id = child.id 
													JOIN cte ON cte.ppid = child.id WHERE consists_of.child_product_id <> consists_of.parent_product_id ) 
													SELECT ppid, MAX(pl) AS pl FROM cte GROUP BY ppid ORDER BY ppid DESC"
											, childProductId);
						MySqlCommand cmd = new MySqlCommand(stmt, connector.Connection);
						MySqlDataReader reader = cmd.ExecuteReader();
						while (reader.Read())
						{
							int ppid = reader.GetInt16(0);
							ids.Add(ppid);
							int pl = reader.GetInt16(1);
							pathlengths.Add(pl);
						}
						reader.Close();

						// skip the first two id's, because they the first one is referencing themselves, and the second one was already inserted before (initial parent > child relation)
						for (int k = 2; k < ids.Count; k++)
						{
							// inserto into hierarchy
							string statement5 = string.Format("INSERT INTO consists_of(parent_product_id, child_product_id, path_length) VALUES(" + ids[k] + "," + childProductId + ", " + (pathlengths[k] + 1) +");");
							MySqlCommand com5 = new MySqlCommand(statement5, c);
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
