using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MySql.Data;
using MySql.Data.MySqlClient;

namespace DummyDataGenerator
{
    class MySqlDummy : Database
    {

        Connection conn;

        public override void InitializeConnection()
        {
            conn = Connection.Instance();
			conn.IsConnect();
        }

        public override void CloseConnection()
        {
            conn.Close();
        }

        public override void GenerateData(Configuration config)
        {
			RefreshDatabaseSchema();
			/*GenerateSuppliers(config.NumberOfSuppliers);
			GenerateActivities(config.NumberOfActivities);
			GenerateProducts(config.NumberOfProducts);
			GenerateProductHierarchies(config.NumberOfChains);
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
				string statement = "INSERT INTO organizations(name) VALUES(" + "'Product #" + i.ToString() + "'";
			}
		}

		private void GenerateActivities(int numberOfActivities)
		{
			for (int i = 0; i < numberOfActivities; i++)
			{
				string statement = "INSERT INTO organizations(name) VALUES(" + "'Activity #" + i.ToString() + "'";
			}
		}

		private void GenerateSuppliers(int numberOfSuppliers)
		{
			for (int i = 0; i < numberOfSuppliers; i++)
			{
				string statement = "INSERT INTO organizations(name) VALUES(" + "'Supplier #" + i.ToString() + "'";

			}
		}

		private void RefreshDatabaseSchema()
		{
			MySqlScript script = new MySqlScript(conn.Conn, File.ReadAllText("scm_test_v3_structure.sql"));
			try
			{
				Console.WriteLine("Executing refresh schema script..");
				script.Execute();
			}
			catch (MySqlException e)
			{
				Console.WriteLine(e);
			}
			
		}
    }

    public class Connection
    {
        private Connection()
        {
        }

        private string databaseName = "scm_test_dummy_generated_v2";
        public string DatabaseName
        {
            get { return databaseName; }
            set { databaseName = value; }
        }

        public string Password { get; set; }
        private MySqlConnection conn = null;
        public MySqlConnection Conn
        {
            get { return conn; }
        }

        private static Connection _instance = null;
        public static Connection Instance()
        {
            if (_instance == null)
                _instance = new Connection();
				Console.WriteLine("Created new connection");
            return _instance;
        }

        public bool IsConnect()
        {
            if (Conn == null)
            {
                if (String.IsNullOrEmpty(databaseName))
                    return false;
                string connstring = string.Format("Server=localhost; database={0}; UID=root; password=teun1996", databaseName);
				try
				{
					conn = new MySqlConnection(connstring);
					conn.Open();
					Console.WriteLine("Connection State: " + conn.State.ToString());
				}
				catch (MySqlException e)
				{
					Console.WriteLine(e);
				}
            }

            return true;
        }

        public void Close()
        {
            conn.Close();
        }
    }
}
