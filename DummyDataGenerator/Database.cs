using DummyDataGenerator.Utils;
using MySql.Data.MySqlClient;
using Neo4j.Driver.V1;
using System;
using System.Collections.Generic;

namespace DummyDataGenerator
{
    public abstract class Database
    {
		protected List<Organization> organizations;
		protected List<Product> products;
		protected List<Activity> activities;
		protected List<Location> locations;
        public abstract void InitializeConnection();

        public abstract void GenerateData(Configuration config);

        public abstract void CloseConnection();

		/// <summary>
		/// Fills the organizations, products and activity objects with data for the specified configuration
		/// </summary>
		/// <param name="conf"></param>
		public void GenerateFakeData(int numberOfFakeEntities)
		{
			var watch = System.Diagnostics.Stopwatch.StartNew();
			organizations = FakeDataHelper.GenerateOrganizations(numberOfFakeEntities);
			products = FakeDataHelper.GenerateProducts(numberOfFakeEntities);
			activities = FakeDataHelper.GenerateActivities(numberOfFakeEntities);
			locations = FakeDataHelper.GenerateLocations(numberOfFakeEntities);
			watch.Stop();
			Console.WriteLine("Took " + watch.ElapsedMilliseconds + "ms to generate " + products.Count + " products of" + " fake data to insert into database");
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
