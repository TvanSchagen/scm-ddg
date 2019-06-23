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
		private void AddDatabases()
		{
			databases.Add("scm_ct_breadth2_depth2");
			databases.Add("scm_ct_breadth2_depth4");
			databases.Add("scm_ct_breadth2_depth6");
			databases.Add("scm_ct_breadth2_depth8");
			databases.Add("scm_ct_breadth2_depth10");
			databases.Add("scm_ct_breadth2_depth12");
			databases.Add("scm_ct_breadth2_depth14");
		}

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
