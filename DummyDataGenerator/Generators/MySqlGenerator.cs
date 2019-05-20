using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using DummyDataGenerator.Connectors;
using MySql.Data;
using MySql.Data.MySqlClient;

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

        public override void GenerateData(Configuration config)
        {
			RefreshDatabaseSchema();
			GenerateSuppliers(config.NumberOfSuppliers);
			GenerateActivities(config.NumberOfActivities);
			GenerateProducts(config.NumberOfProducts);
			/*GenerateProductHierarchies(config.NumberOfChains);
			GenerateSuppliesRelation();
			AssignSuppliesRelationToProducts();*/
			Console.WriteLine("Done..");
        }

		private void GenerateSuppliesRelation()
		{
			
		}

		private void AssignSuppliesRelationToProducts()
		{
			
		}

		private void GenerateProductHierarchies(int numberOfChains)
		{

		}

		private void GenerateProducts(int numberOfProducts)
		{
			for (int i = 0; i < numberOfProducts; i++)
			{
				string statement = "INSERT INTO products(name) VALUES(" + "'Product #" + i.ToString() + "'";
				MySqlCommand com = new MySqlCommand(statement, connector.Connection);
				com.ExecuteNonQuery();
			}
		}

		private void GenerateActivities(int numberOfActivities)
		{
			for (int i = 0; i < numberOfActivities; i++)
			{
				string statement = "INSERT INTO activities(name) VALUES(" + "'Activity #" + i.ToString() + "'";
				MySqlCommand com = new MySqlCommand(statement, connector.Connection);
				com.ExecuteNonQuery();
			}
		}

		private void GenerateSuppliers(int numberOfSuppliers)
		{
			for (int i = 0; i < numberOfSuppliers; i++)
			{
				string statement = "INSERT INTO organizations(name) VALUES(" + "'Supplier #" + i.ToString() + "'";
				MySqlCommand com = new MySqlCommand(statement, connector.Connection);
				com.ExecuteNonQuery();
			}
		}

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
    }

}
