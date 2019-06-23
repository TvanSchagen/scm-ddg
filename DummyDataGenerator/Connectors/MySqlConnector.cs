using System;
using System.Collections.Generic;
using System.Text;
using MySql.Data;
using MySql.Data.MySqlClient;
using DotNetEnv;
using DummyDataGenerator.Utils;

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
				string connstring = string.Format(
					"Server={0}; database={1}; UID={2}; password={3}; default command timeout=60;", 
					Env.GetString("MYSQL_HOST"), 
					Env.GetString("MYSQL_DB"),
					Env.GetString("MYSQL_USER"),
					Env.GetString("MYSQL_PW")
				);
				try
				{
					conn = new MySqlConnection(connstring);
					conn.Open();
					Logger.Info("Created connection to MySQL @ " + Env.GetString("MYSQL_HOST"));
				}
				catch (MySqlException e)
				{
					Logger.Error(e.ToString());
				}
			}
		}

		[Obsolete("Currently does not work as intended, do not use")]
		public MySqlConnection NewConnection()
		{
			MySqlConnection c = null;
			string connstring = string.Format(
				"Server={0}; database={1}; UID={2}; password={3}",
				Env.GetString("MYSQL_HOST"),
				Env.GetString("MYSQL_DB"),
				Env.GetString("MYSQL_USER"),
				Env.GetString("MYSQL_PW")
			);
			try
			{
				c = new MySqlConnection(connstring);
				c.Open();
				Logger.Info("Created connection to MySQL @ " + Env.GetString("MYSQL_HOST"));
			}
			catch (MySqlException e)
			{
				Console.WriteLine(e);
			}
			return c;
		}

		public void Close()
		{
			try
			{
				conn?.Close();
				Logger.Info("Closed connection to MySQL @ " + Env.GetString("MYSQL_HOST"));
			}
			catch (MySqlException e)
			{
				Logger.Error(e.ToString());
			}
			
		}
	}
}
