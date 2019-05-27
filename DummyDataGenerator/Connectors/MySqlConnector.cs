using System;
using System.Collections.Generic;
using System.Text;
using MySql.Data;
using MySql.Data.MySqlClient;

namespace DummyDataGenerator.Connectors
{

	public class MySqlConnector
	{

		private MySqlConnection conn = null;
		public MySqlConnection Connection
		{
			get { return conn; }
		}

		private static MySqlConnector _instance = null;
		public static MySqlConnector Instance()
		{
			if (_instance == null)
				_instance = new MySqlConnector();
			return _instance;
		}

		private MySqlConnector()
		{
			if (Connection == null)
			{
				string connstring = string.Format("Server=192.168.178.94; database={0}; UID={1}; password={2}", Program.MYSQL_DATABASE_NAME, Program.MYSQL_USER, Program.MYSQL_PASSWORD);
				try
				{
					conn = new MySqlConnection(connstring);
					conn.Open();
					Console.WriteLine("Opened MySql connection with status: " + conn.State.ToString());
				}
				catch (MySqlException e)
				{
					Console.WriteLine(e);
				}
			}
		}

		public void Close()
		{
			conn?.Close();
		}
	}
}
