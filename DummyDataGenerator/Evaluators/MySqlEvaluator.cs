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

		/// <summary>
		/// Runs the performance evaluator for all added databases and queries, and outputs a file with the results
		/// </summary>
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

		// add databases you want to evaluate here
		private void AddDatabases()
		{
			databases.Add("scm_al_b2_d10");
		}

		/// <summary>
		/// Reads all .sql files from the directory and adds them to the list
		/// </summary>
		#warning due to limitations (of query length) of the MySQL performance schema, q<number> must be contained somewhere in the query in order to recognize it as this particular query
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

		/// <summary>
		/// Returns the size of the specified database in megabytes
		/// </summary>
		/// <param name="database">The name of the database</param>
		private void CaptureDatabaseSize(string database)
		{
			string statement = string.Format("USE {0}; SELECT sum(round(((data_length + index_length) / 1024 / 1024), 2))  as 'size_mb' FROM information_schema.TABLES WHERE table_schema = '{0}'", database);
			MySqlCommand command = new MySqlCommand(statement, connector.Connection);
			decimal size = (decimal)command.ExecuteScalar();
			output += database + " (" + size + " MB) ";
		}

		/// <summary>
		/// Enabled the performance schema on MySQL in order to capture the query response times
		/// </summary>
		/// <param name="database">The name of the database</param>
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

		/// <summary>
		/// Executes a query on the database a specified number of times
		/// </summary>
		/// <param name="queryNumber">Number of the query</param>
		/// <param name="query">The actual SQL code to be executed</param>
		/// <param name="repetitions">Number of times the query is executed</param>
		/// <param name="database"></param>
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

		/// <summary>
		/// Gets the result from the enabled performance schema for the specified query, using 'q' and the number of the query (example 'q1')
		/// </summary>
		/// <param name="queryNumber"></param>
		/// <param name="query"></param>
		/// <param name="repetitions"></param>
		#warning due to limitations (of query length) of the MySQL performance schema, q<number> must be contained somewhere in the query in order to recognize it as this particular query
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

			// gets individual response times

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

		/// <summary>
		/// Disables the performance schema for the specified database
		/// </summary>
		/// <param name="database">Name of the database</param>
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

		/// <summary>
		/// Adds headers to the output file based on the no. of repetitions
		/// </summary>
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

		/// <summary>
		/// Writes the results to a file in csv format
		/// </summary>
		/// <param name="fileName"></param>
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
