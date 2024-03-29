﻿using DummyDataGenerator.Connectors;
using DummyDataGenerator.Utils;
using Neo4j.Driver.V1;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace DummyDataGenerator.Evaluators
{
	class Neo4jEvaluator
	{
		protected Neo4jConnector connector;

		protected List<string> queries = new List<string>();

		const int NUMBER_OF_REPETITIONS = 10;

		string output;

		public void Execute()
		{
			AddQueries();
			OpenConnection();
			AddHeadersToOutput();
			for (int j = 0; j < queries.Count; j++)
			{
				ExecuteQueryAndRetrieveResult(j, queries[j], "");
			}
			CloseConnection();
			WriteOutputToFile("scm_gr_breadth2_depth14.csv");
		}

		private void AddQueries()
		{
			string path = @"../../../Queries/Cypher/";
			foreach (string file in Directory.EnumerateFiles(path, "*.cql"))
			{
				Console.WriteLine(file + ", path" + path);
				Logger.Info("Reading file " + file + "..");
				string output = "";
				string[] lines = File.ReadAllLines(file);
				foreach (string line in lines)
				{
					output += line + " ";
				}
				output = Regex.Replace(output, @"\s+", " ");
				Logger.Debug(output);
				queries.Add(output);
			}
		}

		private void ExecuteQueryAndRetrieveResult(int iteration, string query, string database)
		{
			Logger.Info("Executing and retrieving results for query: " + (iteration + 1));
			output += database + "query " + (iteration + 1);
			List<int> eventIds = new List<int>();
			int totaExecutionTime = 0;
			for (int i = 0; i < NUMBER_OF_REPETITIONS; i++)
			{
				using (ISession session = connector.Connection.Session())
				{
					IStatementResult res = session.WriteTransaction(tx => tx.Run(query));
					int responseTime = res.Summary.ResultAvailableAfter.Milliseconds + res.Summary.ResultConsumedAfter.Milliseconds;
					Logger.Debug("Available after: " + res.Summary.ResultAvailableAfter.Milliseconds + " Consumed after: " + res.Summary.ResultConsumedAfter.Milliseconds);
					totaExecutionTime += responseTime;
					output += ";" + responseTime;
				}
			}
			output += "\n";
			Logger.Info("Average execution time: " + totaExecutionTime / NUMBER_OF_REPETITIONS);
		}

		private void WriteOutputToFile(string filename)
		{
			string path = @"../../../../" + filename;
			using (FileStream fs = File.Create(path))
			{
				// adds the keys to the file
				Byte[] info = new UTF8Encoding(true).GetBytes(output);
				fs.Write(info, 0, info.Length);
			}
		}

		private void AddHeadersToOutput()
		{
			Logger.Info("Adding headers to output..");
			output += "query number";
			for (int i = 0; i < NUMBER_OF_REPETITIONS; i++)
			{
				output += ";run " + (i + 1);
			}
			output += "\n";
		}

		private void OpenConnection()
		{
			connector = Neo4jConnector.Instance();
		}

		private void CloseConnection()
		{
			Logger.Info("Closing connection..");
			connector.Close();
		}

	}
}
