using System;
using System.Collections.Generic;
using System.Text;
using System.Data.SqlClient;
using DummyDataGenerator.Utils;
using DotNetEnv;

namespace DummyDataGenerator.Connectors
{
	class SqlServerConnector
	{
		private static SqlServerConnector _instance = null;

		private SqlConnection _connection;

		public SqlConnection Connection
		{
			get { return _connection; }
		}

		public static SqlServerConnector Instance()
		{
			if (_instance == null)
				_instance = new SqlServerConnector();
			return _instance;
		}

		private SqlServerConnector()
		{
			if (Connection == null)
			{
				SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();
				builder.DataSource = Env.GetString("SQLSERVER_HOST");
				builder.UserID = Env.GetString("SQLSERVER_USER");
				builder.Password = Env.GetString("SQLSERVER_PW");
				builder.InitialCatalog = Env.GetString("SQLSERVER_DB");

				try
				{
					_connection = new SqlConnection(builder.ConnectionString);
					_connection.Open();
					Logger.Info("Created connection to SQL Server @ " + Env.GetString("SQLSERVER_HOST"));
					Logger.Debug("State: " + _connection.State.ToString());
				}
				catch (SqlException e)
				{
					Logger.Error("Error: " + e);
				}
			}
		}

		public void Close()
		{
			try
			{
				_connection?.Close();
				Logger.Info("Closed connection to SQL Server @ " + Env.GetString("SQLSERVER_HOST"));
			}
			catch (SqlException e)
			{
				Logger.Error(e.ToString());
			}

		}

	}
}
