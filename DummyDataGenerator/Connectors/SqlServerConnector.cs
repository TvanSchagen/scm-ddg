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
				builder.DataSource = "10.0.10.136";
				builder.UserID = "SA";
				builder.Password = "SQLServer2019";
				builder.InitialCatalog = "master";

				try
				{
					_connection = new SqlConnection(builder.ConnectionString);
					_connection.Open();
					Logger.Info("Created connection to SQL Server @ " + "10.0.10.136");
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
				Logger.Info("Closed connection to SQL Server @ " + "10.0.10.136");
			}
			catch (SqlException e)
			{
				Logger.Error(e.ToString());
			}

		}

	}
}
