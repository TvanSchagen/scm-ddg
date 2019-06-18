using System;
using System.Collections.Generic;
using System.Text;
using DotNetEnv;
using DummyDataGenerator.Connectors;
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
			queries.Add("SELECT * FROM product");
		}

	}
}
