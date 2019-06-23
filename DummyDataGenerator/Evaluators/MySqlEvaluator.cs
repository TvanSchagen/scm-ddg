using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using DotNetEnv;
using DummyDataGenerator.Connectors;
using DummyDataGenerator.Utils;
using MySql.Data.MySqlClient;

namespace DummyDataGenerator.Evaluators
{
	class MySqlEvaluator
	{

		const int NUMBER_OF_REPETITIONS = 10;

		protected MySqlConnector connector;

		string output = "";

		protected List<string> databases = new List<string>();
		protected List<string> queries = new List<string>();

		public void Execute()
		{
			AddDatabases();
			AddQueries();
			OpenConnection();
			AddHeadersToOutput();
			foreach (string db in databases)
			{
				EnablePerformanceSchema(db);
				for (int j = 0; j < queries.Count; j++)
				{
					CaptureDatabaseSize(db);
					ExecuteQuery(j + 1, queries[j], NUMBER_OF_REPETITIONS, db);
					RetrieveResult(j + 1, queries[j], NUMBER_OF_REPETITIONS);
				}
				DisablePerformanceSchema(db);
			}
			CloseConnection();
			WriteOutputToFile("mysql_eval.csv");
		}

		private void AddDatabases()
		{
			databases.Add("v2_scm_al_breadth2_depth2");
			databases.Add("v2_scm_al_breadth2_depth4");
			databases.Add("v2_scm_al_breadth2_depth6");
			databases.Add("v2_scm_al_breadth2_depth8");
			databases.Add("v2_scm_al_breadth2_depth10");
			databases.Add("v2_scm_al_breadth2_depth12");
			databases.Add("v2_scm_al_breadth2_depth14");
		}

		private void AddQueries()
		{
			string path = @"../../../Queries/SQL/Adjacency List/";
			foreach(string file in Directory.EnumerateFiles(path, "*.sql"))
			{
				Logger.Debug("Reading file " + file + "..");
				string output = "";
				string[] lines = File.ReadAllLines(path + file);
				foreach(string line in lines)
				{
					output += line + " ";
				}
				output = Regex.Replace(output, @"\s+", " ");
				Logger.Debug(output);
				queries.Add(output);
			}
		}

		private void CaptureDatabaseSize(string database)
		{
			string statement = string.Format("USE {0}; SELECT sum(round(((data_length + index_length) / 1024 / 1024), 2))  as 'size_mb' FROM information_schema.TABLES WHERE table_schema = '{0}'", database);
			MySqlCommand command = new MySqlCommand(statement, connector.Connection);
			decimal size = (decimal)command.ExecuteScalar();
			output += database + " (" + size + " MB) ";
		}

		private void EnablePerformanceSchema(string database)
		{
			Logger.Debug("Enabling performance schema");
			string statement = string.Format(
				@"USE {0};
				SET SQL_SAFE_UPDATES = 0;
				UPDATE performance_schema.setup_actors SET ENABLED = 'YES', HISTORY = 'YES' WHERE HOST = '%' AND USER = '%';
				
				UPDATE performance_schema.setup_instruments SET ENABLED = 'YES', TIMED = 'YES' WHERE NAME LIKE '%statement/%';
				UPDATE performance_schema.setup_instruments SET ENABLED = 'YES', TIMED = 'YES' WHERE NAME LIKE '%stage/%'; 
				UPDATE performance_schema.setup_consumers SET ENABLED = 'YES' WHERE NAME LIKE '%events_statements_%'; 
				UPDATE performance_schema.setup_consumers SET ENABLED = 'YES' WHERE NAME LIKE '%events_stages_%';
				TRUNCATE performance_schema.events_statements_history_long", 
				database, 
				Env.GetString("MYSQL_USER")
			);
			MySqlCommand command = new MySqlCommand(statement, connector.Connection);
			command.ExecuteNonQuery();
		}

		private void ExecuteQuery(int queryNumber, string query, int repetitions, string database)
		{
			Logger.Info("Executing query: " + queryNumber + " " + NUMBER_OF_REPETITIONS + " times on database: " + database);
			string stmt = string.Format(query);
			MySqlCommand cmd = new MySqlCommand(stmt, connector.Connection);
			for (int i = 0; i < repetitions; i++)
			{
				cmd.ExecuteNonQuery();
			}
		}

		private void RetrieveResult(int queryNumber, string query, int repetitions)
		{
			Logger.Info("Retrieving results for query: " + (queryNumber));
			output += "query " + (queryNumber + 1);
			List<int> eventIds = new List<int>();
			float totaExecutionTime = 0.0f;

			MySqlCommand com = new MySqlCommand();
			com.CommandText = string.Format("SELECT EVENT_ID, TRUNCATE(TIMER_WAIT/1000000000000,6) as Duration, SQL_TEXT FROM performance_schema.events_statements_history_long WHERE SQL_TEXT LIKE '%{0}%'", "q" + queryNumber);
			com.Connection = connector.Connection;
			//com.Prepare();
			//com.Parameters.AddWithValue("@query", query);
			MySqlDataReader reader = com.ExecuteReader();
			try
			{
				while (reader.Read())
				{
					//int id = reader.GetInt16(0);
					float responseTime = reader.GetFloat(1);
					Logger.Debug("Response time for query " + queryNumber + " is " + responseTime);
					//eventIds.Add(id);
					totaExecutionTime += responseTime;
					output += ";" + responseTime;
				}
				output += "\n";
			}
			catch (MySqlException e)
			{
				Logger.Error("Error: " +e);
			}
			finally
			{
				reader.Close();
			}

			Logger.Info("Average execution time: " + totaExecutionTime / repetitions);

			/*foreach (int i in eventIds)
			{
				string statement2 = string.Format("SELECT event_name AS Stage, TRUNCATE(TIMER_WAIT/1000000000000,6) AS Duration FROM performance_schema.events_stages_history_long WHERE NESTING_EVENT_ID = {0}", i);
				MySqlCommand command2 = new MySqlCommand(statement2, connector.Connection);
				MySqlDataReader reader2 = command2.ExecuteReader();
				try
				{
					while (reader2.Read())
					{
						// if we want to extract sub timings we can do it here
					}
				}
				catch (MySqlException e)
				{
					Console.WriteLine(e);
				}
				finally
				{
					reader2.Close();
				}
			}*/

		}

		private void OpenConnection()
		{
			connector = MySqlConnector.Instance();

		}

		private void CloseConnection()
		{
			Logger.Info("Closing connection..");
			connector.Close();
		}

		private void DisablePerformanceSchema(string database)
		{
			Logger.Debug("Disabling performance schema");
			string statement = string.Format(
				@"USE {0};
				SET SQL_SAFE_UPDATES = 0;
				DELETE FROM performance_schema.setup_actors WHERE ENABLED = 'YES' AND HISTORY = 'YES' AND HOST = '%' AND USER = '{1}' AND ROLE = '%';
				TRUNCATE performance_schema.events_statements_history_long",
				database, Env.GetString("MYSQL_USER"));
			MySqlCommand command = new MySqlCommand(statement, connector.Connection);
			command.ExecuteNonQuery();
		}

		private void AddHeadersToOutput()
		{
			Logger.Debug("Adding headers to file..");
			output += "database (size) and query number";
			for (int i = 0; i < NUMBER_OF_REPETITIONS; i++)
			{
				output += ";run " + (i + 1);
			}
			output += "\n";
		}

		private void WriteOutputToFile(string fileName)
		{
			Logger.Debug("Writing output to file..");
			try
			{
				string path = @"../../../../" + fileName;
				using (FileStream fs = File.Create(path))
				{
					// adds the keys to the file
					Byte[] info = new UTF8Encoding(true).GetBytes(output);
					fs.Write(info, 0, info.Length);
				}
			} catch (Exception e)
			{
				Logger.Error("Error: " + e);
			}
		}

	}
}
