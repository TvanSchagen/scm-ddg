using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using DummyDataGenerator.Connectors;
using MySql.Data.MySqlClient;

namespace DummyDataGenerator
{
	class MySqlGenerator : Database
	{

		protected MySqlConnector connector;

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
			GenerateLocations(config.NumberOfSuppliers);
			GenerateProductTrees(tlOrgs, config.NumberOfProducts, config.ChainDepth, config.ChainBreadth);
			AddOrganizationsAndActivitiesToProductTree(config.NumberOfSuppliers, config.NumberOfTopLevelSuppliers, config.NumberOfActivities, config.NumberOfProducts, config.ChainDepth, config.ChainBreadth);
			AddMetaData(config);
			Console.WriteLine("Program completed.");
		}

		/// <summary>
		/// Drops and creates the database, as to start off with a clean sheet
		/// </summary>
		protected void RefreshDatabaseSchema()
		{
			var watch = System.Diagnostics.Stopwatch.StartNew();
			MySqlScript script = new MySqlScript(connector.Connection, File.ReadAllText("../../../mysql_generate_schema.sql"));
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
			Console.WriteLine("Refreshed schema in " + watch.ElapsedMilliseconds + "ms " + "(" + (watch.ElapsedMilliseconds / 1000) + "s) ");

			// set autocommit to off
			string statement = "START TRANSACTION;";
			MySqlCommand com = new MySqlCommand(statement, connector.Connection);
			com.ExecuteNonQuery();
		}

		/// <summary>
		/// Generates a set number of top level organizations; organizations that do not supply any others
		/// </summary>
		/// <param name="organizations">The number of organiations</param>
		/// <returns>a list of generated organization ids</returns>
		protected int[] GenerateTopLevelOrganizations(int organizations)
		{
			List<int> result = new List<int>();
			var watch = System.Diagnostics.Stopwatch.StartNew();
			for (int i = 0; i < organizations; i++)
			{
				MySqlCommand com = InsertOrganization(true, i);
				
				try
				{
					com.ExecuteNonQuery();
				}
				catch (MySqlException e)
				{
					Console.WriteLine(e);
				}
				result.Add((int)com.LastInsertedId);
			}
			watch.Stop();
			Console.WriteLine("Took " + watch.ElapsedMilliseconds + "ms " + "(" + (watch.ElapsedMilliseconds / 1000) + "s) " + " to generate " + result.Count + " top level organisations");
			return result.ToArray();
		}

		/// <summary>
		/// Generatea a set number of organizations to be used during the tree generation
		/// </summary>
		/// <param name="organizations">The number of organiations</param>
		/// <returns>a list of generated organization ids</returns>
		protected int[] GenerateOrganizations(int organizations)
		{
			List<int> result = new List<int>();
			var watch = System.Diagnostics.Stopwatch.StartNew();
			for (int i = 0; i < organizations; i++)
			{
				MySqlCommand com = InsertOrganization(false, i);
				com.ExecuteNonQuery();
				result.Add((int)com.LastInsertedId);
			}
			watch.Stop();
			Console.WriteLine("Took " + watch.ElapsedMilliseconds + "ms " + "(" + (watch.ElapsedMilliseconds / 1000) + "s) " + " to generate " + result.Count + " organisations");
			return result.ToArray();
		}

		/// <summary>
		/// Generatea a set number of activities to be used during tree generation
		/// </summary>
		/// <param name="activites">The number of activities to be generated</param>
		/// <returns>a list of generated activity ids</returns>
		protected int[] GenerateActivities(int activites)
		{
			List<int> result = new List<int>();
			var watch = System.Diagnostics.Stopwatch.StartNew();
			for (int i = 0; i < activites; i++)
			{
				MySqlCommand com = InsertActivity(i);
				com.ExecuteNonQuery();
				result.Add((int)com.LastInsertedId);
			}
			watch.Stop();
			Console.WriteLine("Took " + watch.ElapsedMilliseconds + "ms " + "(" + (watch.ElapsedMilliseconds / 1000) + "s) " + " to generate " + result.Count + " activities");
			return result.ToArray();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="locations"></param>
		/// <returns></returns>
		protected int[] GenerateLocations(int locations)
		{
			List<int> result = new List<int>();
			var watch = System.Diagnostics.Stopwatch.StartNew();
			for (int i = 0; i < locations; i++)
			{
				MySqlCommand com = InsertLocation(i);
				com.ExecuteNonQuery();
				result.Add((int) com.LastInsertedId);
			}
			watch.Stop();
			Console.WriteLine("Took " + watch.ElapsedMilliseconds + "ms " + "(" + (watch.ElapsedMilliseconds / 1000) + "s) " + " to generate " + result.Count + " locations");
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
			var watchTotal = System.Diagnostics.Stopwatch.StartNew();
			// for each top level supplier
			for (int i = 0; i < organizationIdentifiers.Length; i++)
			{
				Console.WriteLine("Generating for Organization: " + (i+1));

				// for each of their products
				for (int j = 0; j < productsPerSupplier; j++)
				{
					var watch = System.Diagnostics.Stopwatch.StartNew();

					// generate the top level product
					//string statement = "INSERT INTO product(name) VALUES(" + "'Top Level Product #o" + (i + 1) + "-p" + (j + 1) + "');";
					MySqlCommand com = InsertProduct(true, i * productsPerSupplier + j);
					com.ExecuteNonQuery();
					int topLevelId = (int) com.LastInsertedId;

					List<int> previousResults = new List<int>();
					// for the depth of the chain
					for (int k = 0; k < depth; k++)
					{
						// in the first pass, generate the products for our top level product
						if (k == 0)
						{
							previousResults.Add(topLevelId);
							previousResults = GenerateProductRowAndRelations(i, k + 1, previousResults, breadthPerLevel);

						}
						// in subsequent passes, take the previous row of products and generate a new underlying row for all of them
						else
						{
							previousResults = GenerateProductRowAndRelations(i, k + 1, previousResults, breadthPerLevel);
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
		/// TO DO: check if we can speed this up using batches
		/// ---
		private List<int> GenerateProductRowAndRelations(int chainId, int depth, List<int> parentProductIdentifiers, int breadthPerLevel)
		{
			List<int> childProductsCreated = new List<int>();
			int i = 0;

			foreach (int item in parentProductIdentifiers)
			{
				for (int j = 0; j < breadthPerLevel; j++)
				{
					// first insert the product
					//string statement = "INSERT INTO product(name) VALUES(" + "'Product #c" + (chainId + 1) + "-d" + depth + "-p" + (i + 1) + "-b" + (j + 1) + "');";
					MySqlCommand com = InsertProduct(false, item * 2 + j);
					com.ExecuteNonQuery();
					int childProductId = (int) com.LastInsertedId;
					childProductsCreated.Add(childProductId);

					// then create the relation for the product hierarchy
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
		protected void AddOrganizationsAndActivitiesToProductTree(int numberOfOrganizations, int numberOfTopLevelOrganizations, int numberOfActivities, int numberOfProducts, int chainDepth, int chainBreadth)
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
				// if the count isn't returned properly, we calculate what the count should theoretically be
				count = numberOfTopLevelOrganizations * numberOfOrganizations * ((chainBreadth ^ chainDepth) - 1);
				Console.WriteLine("Could not return count, using default: " + count);
			}

			for (int i = 0; i < count; i++)
			{
				string statement2 = string.Format("INSERT INTO supplies(organization_id, activity_id, product_id, location_id) VALUES({0}, {1}, {2}, {3});",
					// add an offset of the toplevel suppliers, which are added first to the database, then modulo if the n.o. products outnumbers the n.o. organizations
					(i % numberOfOrganizations) + numberOfTopLevelOrganizations + 1, // numberOfOrganizations + 1 + numberOfTopLevelOrganizations,
					// modulo if the n.o.products outnumbers the n.o. activities
					i % numberOfActivities + 1,
					// use the iterator as the product id
					i + 1,
					// use the number of organizations for the location as well
					(i % numberOfOrganizations) + 1);
				MySqlCommand com2 = new MySqlCommand(statement2, connector.Connection);
				com2.ExecuteNonQuery();
			}

			watch.Stop();
			Console.WriteLine("Took " + watch.ElapsedMilliseconds + "ms " + "(" + (watch.ElapsedMilliseconds / 1000) + "s) to add organizations and activities to the product tree");
		}

		protected void AddLocations()
		{

		}

		/// <summary>
		/// Adds metadata to a separate table in the database
		/// </summary>
		/// <param name="config">The supplied configuration</param>
		protected void AddMetaData(Configuration config)
		{
			string statement = "DROP TABLE IF EXISTS `db_meta`; CREATE TABLE `db_meta` (`meta_name` VARCHAR(50), `meta_value` VARCHAR(50)) ENGINE=InnoDB;";
			MySqlCommand com = new MySqlCommand(statement, connector.Connection);
			com.ExecuteNonQuery();

			string statement2 = string.Format(
				"INSERT INTO db_meta(meta_name, meta_value) " +
				"VALUES('NumberOfActivities', '{0}'), " +
					"('NumberOfProductsPerTopLevelSupplier', '{1}'), " +
					"('NumberOfSuppliers', '{2}'), " +
					"('NumberOfTopLevelSuppliers', '{3}'), " +
					"('ChainBreadth', '{4}'), " +
					"('ChainDepth', '{5}')", 
				config.NumberOfActivities, 
				config.NumberOfProducts, 
				config.NumberOfSuppliers, 
				config.NumberOfTopLevelSuppliers, 
				config.ChainBreadth, 
				config.ChainDepth
			);
			MySqlCommand com2 = new MySqlCommand(statement2, connector.Connection);
			com2.ExecuteNonQuery();
			Console.WriteLine("Added metadata to the database");

			// commit changes
			string statement3 = "COMMIT;";
			MySqlCommand com3 = new MySqlCommand(statement3, connector.Connection);
			com3.ExecuteNonQuery();
		}

		public MySqlCommand InsertProduct(bool isTopLevel, int id)
		{
			// this prevents the id from ever going out of range of the list
			id = id % products.Count;
			MySqlCommand com = new MySqlCommand();
			com.CommandText = "INSERT INTO product(`GUID`, `name`, `description`, `ean`, `category`, `sub_category`, `created`, `last_updated`) VALUES(@GUID, @name, @description, @ean, @category, @sub_category, @created, @last_updated);";
			com.Connection = connector.Connection;
			com.Prepare();
			com.Parameters.AddWithValue("@GUID", products[id].GUID);
			com.Parameters.AddWithValue("@name", isTopLevel ? "Top Level Product " + products[id].Name : products[id].Name);
			com.Parameters.AddWithValue("@description", products[id].Description);
			com.Parameters.AddWithValue("@ean", products[id].EANCode);
			com.Parameters.AddWithValue("@category", products[id].Category);
			com.Parameters.AddWithValue("@sub_category", products[id].SubCategory);
			com.Parameters.AddWithValue("@created", products[id].CreatedDate);
			com.Parameters.AddWithValue("@last_updated", products[id].LastUpdatedDate);
			return com;
		}

		public MySqlCommand InsertActivity(int id)
		{
			// this prevents the id from ever going out of range of the list
			id = id % activities.Count;
			MySqlCommand com = new MySqlCommand();
			com.CommandText = "INSERT INTO activity(`GUID`, `name`, `description`, `created`, `last_updated`) VALUES(@GUID, @name, @description, @created, @last_updated);";
			com.Connection = connector.Connection;
			com.Prepare();
			com.Parameters.AddWithValue("@GUID", activities[id].GUID);
			com.Parameters.AddWithValue("@name", activities[id].Name);
			com.Parameters.AddWithValue("@description", activities[id].Description);
			com.Parameters.AddWithValue("@created", activities[id].CreatedDate);
			com.Parameters.AddWithValue("@last_updated", activities[id].LastUpdatedDate);
			return com;
		}

		public MySqlCommand InsertOrganization(bool isTopLevel, int id)
		{
			// this prevents the id from ever going out of range of the list
			id = id % organizations.Count;
			MySqlCommand com = new MySqlCommand();
			com.CommandText = "INSERT INTO organization(`GUID`, `name`, `description`, `ein`, `number_of_employees`, `email_address`, `website`, `created`, `last_updated`) VALUES(@GUID, @name, @description, @ein, @number_of_employees, @email_address, @website, @created, @last_updated);";
			com.Connection = connector.Connection;
			com.Prepare();
			com.Parameters.AddWithValue("@GUID", organizations[id].GUID);
			com.Parameters.AddWithValue("@name", isTopLevel ? "Top Level Organization " + organizations[id].Name : organizations[id].Name);
			com.Parameters.AddWithValue("@description", organizations[id].Description);
			com.Parameters.AddWithValue("@ein", organizations[id].EmployerIdentificationNumber);
			com.Parameters.AddWithValue("@number_of_employees", organizations[id].NumberOfEmployees);
			com.Parameters.AddWithValue("@email_address", organizations[id].EmailAddress);
			com.Parameters.AddWithValue("@website", organizations[id].WebsiteURL);
			com.Parameters.AddWithValue("@created", organizations[id].CreatedDate);
			com.Parameters.AddWithValue("@last_updated", organizations[id].LastUpdatedDate);
			return com;
		}

		public MySqlCommand InsertLocation(int id)
		{
			id = id % locations.Count;
			MySqlCommand com = new MySqlCommand();
			com.CommandText = "INSERT INTO location(`GUID`, `longtitude`, `latitude`, `country`, `postal_code`, `province`, `city`, `street`, `created`, `last_updated`) VALUES(@GUID, @longtitude, @latitude, @country, @postal_code, @province, @city, @street, @created, @last_updated);";
			com.Connection = connector.Connection;
			com.Prepare();
			com.Parameters.AddWithValue("@GUID", locations[id].GUID);
			com.Parameters.AddWithValue("@longtitude", locations[id].Longtitude);
			com.Parameters.AddWithValue("@latitude", locations[id].Latitude);
			com.Parameters.AddWithValue("@country", locations[id].Country);
			com.Parameters.AddWithValue("@postal_code", locations[id].PostalCode);
			com.Parameters.AddWithValue("@province", locations[id].Province);
			com.Parameters.AddWithValue("@city", locations[id].City);
			com.Parameters.AddWithValue("@street", locations[id].Street);
			com.Parameters.AddWithValue("@created", locations[id].CreatedDate);
			com.Parameters.AddWithValue("@last_updated", locations[id].LastUpdatedDate);
			Console.WriteLine(com.CommandText);
			return com;
		}

	}

}