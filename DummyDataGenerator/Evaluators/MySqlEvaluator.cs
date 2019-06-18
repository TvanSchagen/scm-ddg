using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using DotNetEnv;
using DummyDataGenerator.Connectors;
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
			foreach (string d in databases)
			{
				ConfigurePerformanceSchema(d);
				CaptureDatabaseSize(d);
				for (int j = 0; j < queries.Count; j++)
				{
					ExecuteQuery(j, queries[j], NUMBER_OF_REPETITIONS, d);
					RetrieveResult(j, queries[j], NUMBER_OF_REPETITIONS);
				}
			}
			CloseConnection();
			WriteOutputToFile("mysql_eval.csv");
		}

		private void AddDatabases()
		{
			databases.Add("scm_al_breadth2_depth2");
			databases.Add("scm_al_breadth2_depth4");
			databases.Add("scm_al_breadth2_depth6");
			databases.Add("scm_al_breadth2_depth8");
			databases.Add("scm_al_breadth2_depth10");
			databases.Add("scm_al_breadth2_depth12");
			databases.Add("scm_al_breadth2_depth14");
		}


		private void AddQueries()
		{
			queries.Add("SELECT * FROM product");
		}

		private void CaptureDatabaseSize(string database)
		{
			string statement = string.Format("USE {0}; SELECT sum(round(((data_length + index_length) / 1024 / 1024), 2))  as 'size_mb' FROM information_schema.TABLES WHERE table_schema = '{0}'", database);
			MySqlCommand command = new MySqlCommand(statement, connector.Connection);
			decimal size = (decimal)command.ExecuteScalar();
			output += database + " (" + size + " MB) ";
		}

		private void ConfigurePerformanceSchema(string database)
		{
			string statement = string.Format(
				@"USE {0};
				UPDATE performance_schema.setup_actors SET ENABLED = 'NO', HISTORY = 'NO' WHERE HOST = '%' AND USER = '%';
				INSERT IGNORE INTO performance_schema.setup_actors(HOST,USER,ROLE,ENABLED,HISTORY) VALUES('%', '{1}', '%', 'YES', 'YES');
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
			Console.WriteLine("Executing query: " + (queryNumber + 1) + " " + NUMBER_OF_REPETITIONS + " times on database: " + database);
			string stmt = string.Format(query);
			MySqlCommand cmd = new MySqlCommand(stmt, connector.Connection);
			for (int i = 0; i < repetitions; i++)
			{
				cmd.ExecuteNonQuery();
			}
		}

		private void RetrieveResult(int queryNumber, string query, int repetitions)
		{
			Console.WriteLine("Retrieving results for query: " + (queryNumber + 1));
			output += "query " + (queryNumber + 1);
			List<int> eventIds = new List<int>();
			float totaExecutionTime = 0.0f;

			string statement = string.Format("SELECT EVENT_ID, TRUNCATE(TIMER_WAIT/1000000000000,6) as Duration, SQL_TEXT FROM performance_schema.events_statements_history_long WHERE SQL_TEXT LIKE '%{0}%'", query);
			MySqlCommand command = new MySqlCommand(statement, connector.Connection);
			MySqlDataReader reader = command.ExecuteReader();
			try
			{
				while (reader.Read())
				{
					int id = reader.GetInt16(0);
					float responseTime = reader.GetFloat(1);
					eventIds.Add(id);
					totaExecutionTime += responseTime;
					output += ";" + responseTime;
				}
				output += "\n";
			}
			catch (MySqlException e)
			{
				Console.WriteLine(e);
			}
			finally
			{
				reader.Close();
			}

			Console.WriteLine("Average execution time: " + totaExecutionTime / repetitions);

			foreach (int i in eventIds)
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
			}

		}

		private void OpenConnection()
		{
			connector = MySqlConnector.Instance();
		}

		private void CloseConnection()
		{
			// remove performance profiler for the user
			/*string statement = string.Format("UPDATE performance_schema.setup_actors SET ENABLED = 'NO', HISTORY = 'NO' WHERE HOST = '%' AND USER = '{0}';", Env.GetString("MYSQL_USER"));
			MySqlCommand command = new MySqlCommand(statement, connector.Connection);
			command.ExecuteNonQuery();*/
			// then close the connection
			connector.Close();
		}

		private void AddHeadersToOutput()
		{
			output += "database (size) and query number";
			for (int i = 0; i < NUMBER_OF_REPETITIONS; i++)
			{
				output += ";run " + (i + 1);
			}
			output += "\n";
		}

		private void WriteOutputToFile(string fileName)
		{
			string path = @"../../../../" + fileName + ".csv";
			using (FileStream fs = File.Create(path))
			{
				// adds the keys to the file
				Byte[] info = new UTF8Encoding(true).GetBytes(output);
				fs.Write(info, 0, info.Length);
			}
		}

	}
}
