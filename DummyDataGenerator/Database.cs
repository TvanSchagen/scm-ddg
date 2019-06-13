using DummyDataGenerator.Utils;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;

namespace DummyDataGenerator
{
    public abstract class Database
    {
		List<Organization> organizations;
		List<Product> products;
		List<Activity> activities;

        public abstract void InitializeConnection();

        public abstract void GenerateData(Configuration config);

        public abstract void CloseConnection();

		public void GenerateFakeData(Configuration conf)
		{
			var watch = System.Diagnostics.Stopwatch.StartNew();
			organizations = FakeDataHelper.GenerateOrganizations(conf.NumberOfSuppliers);
			int numberOfProducts = conf.NumberOfTopLevelSuppliers * conf.NumberOfProducts * (conf.ChainBreadth ^ conf.ChainDepth - 1);
			products = FakeDataHelper.GenerateProducts(numberOfProducts);
			activities = FakeDataHelper.GenerateActivities(conf.NumberOfActivities);
			watch.Stop();
			Console.WriteLine("Took " + watch.ElapsedMilliseconds + "ms to generate " + products.Count + " products of" + " fake data to insert into database");
		}

		public MySqlCommand SqlInsertProduct(bool isTopLevel, int id, MySqlConnection conn)
		{
			// this prevents the id from ever going out of range of the list
			id = id % products.Count;
			MySqlCommand com = new MySqlCommand();
			com.CommandText = "INSERT INTO product(`GUID`, `name`, `description`, `ean`, `category`, `sub_category`, `created`, `last_updated`) VALUES(@GUID, @name, @description, @ean, @category, @sub_category, @created, @last_updated);";
			com.Connection = conn;
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

		public MySqlCommand SqlInsertActivity(int id, MySqlConnection conn)
		{
			// this prevents the id from ever going out of range of the list
			id = id % activities.Count;
			MySqlCommand com = new MySqlCommand();
			com.CommandText = "INSERT INTO activity(`GUID`, `name`, `description`, `created`, `last_updated`) VALUES(@GUID, @name, @description, @created, @last_updated);";
			com.Connection = conn;
			com.Prepare();
			com.Parameters.AddWithValue("@GUID", activities[id].GUID);
			com.Parameters.AddWithValue("@name", activities[id].Name);
			com.Parameters.AddWithValue("@description", activities[id].Description);
			com.Parameters.AddWithValue("@created", activities[id].CreatedDate);
			com.Parameters.AddWithValue("@last_updated", activities[id].LastUpdatedDate);
			return com;
		}

		public MySqlCommand SqlInsertOrganization(bool isTopLevel, int id, MySqlConnection conn)
		{
			// this prevents the id from ever going out of range of the list
			id = id % organizations.Count;
			MySqlCommand com = new MySqlCommand();
			com.CommandText = "INSERT INTO organization(`GUID`, `name`, `description`, `ein`, `number_of_employees`, `email_address`, `website`, `created`, `last_updated`) VALUES(@GUID, @name, @description, @ein, @number_of_employees, @email_address, @website, @created, @last_updated);";
			com.Connection = conn;
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

		public string CypherInsertProduct(bool isTopLevel, int id)
		{
			id = id % products.Count;
			string result = "CREATE (n:Product { " + 
				string.Format("GUID: \"{0}\", name: \"{1}\", description: \"{2}\", ean : \"{3}\", category: \"{4}\", sub_category: \"{5}\", created: \"{6}\", last_updated: \"{7}\"",
					products[id].GUID,
					isTopLevel ? "Top Level Product " + products[id].Name : products[id].Name,
					products[id].Description,
					products[id].EANCode,
					products[id].Category,
					products[id].SubCategory,
					products[id].CreatedDate,
					products[id].LastUpdatedDate
				) + 
				"}) RETURN ID(n) AS id";
			return result;
		}

		public string CypherInsertActivityProperties(int id)
		{
			id = id % activities.Count;
			string result = " { " +
				string.Format("GUID: \"{0}\", name: \"{1}\", description: \"{2}\", created: \"{3}\", last_updated: \"{4}\"",
					activities[id].GUID,
					activities[id].Name,
					activities[id].Description,
					activities[id].CreatedDate,
					activities[id].LastUpdatedDate
				) +
				"}";
			return result;
		}

		public string CypherInsertOrganization(bool isTopLevel, int id)
		{
			id = id % products.Count;
			string result = "CREATE (n:Organization { " +
				string.Format("GUID: \"{0}\", name: \"{1}\", description: \"{2}\", ein : \"{3}\", number_of_employees: \"{4}\", email_address: \"{5}\", website: \"{6}\", created: \"{7}\", last_updated: \"{8}\"",
					organizations[id].GUID,
					isTopLevel ? "Top Level Organization " + organizations[id].Name : organizations[id].Name,
					organizations[id].Description,
					organizations[id].EmployerIdentificationNumber,
					organizations[id].NumberOfEmployees,
					organizations[id].EmailAddress,
					organizations[id].WebsiteURL,
					organizations[id].CreatedDate,
					organizations[id].LastUpdatedDate
				) +
				"}) RETURN ID(n) AS id";
			return result;
		}

	}

	public class Configuration
    {

		public int ChainDepth { get; set; }

		public int NumberOfActivities { get; set; }

		public int NumberOfSuppliers { get; set; }

		public int NumberOfTopLevelSuppliers { get; set; }

		public int NumberOfProducts { get; set; }

		public int ChainBreadth { get; set; }


		public Configuration(int chainDepth, int chainBreadth, int numberOfActivities, int numberOfSuppliers, int numberOfTopLevelSuppliers, int numberOfProducts)
        {
            this.ChainDepth = chainDepth;
			this.ChainBreadth = chainBreadth;
			this.NumberOfSuppliers = numberOfSuppliers;
			this.NumberOfTopLevelSuppliers = numberOfTopLevelSuppliers;
			this.NumberOfActivities = numberOfActivities;
			this.NumberOfProducts = numberOfProducts;
        }

    }
}
