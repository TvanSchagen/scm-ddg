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
	class MySqlClosureTableEvaluator : MySqlEvaluator
	{
		// add the databases you want to evaluate here
		private void AddDatabases()
		{
			databases.Add("scm_ct_b2_d10");
		}

		/// <summary>
		/// Reads all .sql files from the directory and adds them to the list
		/// </summary>
		#warning due to limitations (of query length) of the MySQL performance schema, q<number> must be contained somewhere in the query in order to recognize it as this particular query
		private void AddQueries()
		{
			string path = @"../../../Queries/SQL/Closure Table/";
			foreach (string file in Directory.EnumerateFiles(path, "*.sql"))
			{
				Logger.Debug("Reading file " + file + "..");
				string output = "";
				string[] lines = File.ReadAllLines(path + file);
				foreach (string line in lines)
				{
					output += line + " ";
				}
				output = Regex.Replace(output, @"\s+", " ");
				queries.Add(output);
				Logger.Debug("Closing file " + file + "..");
			}
		}

	}
}
