using DummyDataGenerator.Connectors;
using System;
using System.Collections.Generic;
using System.Text;

namespace DummyDataGenerator
{
    class Neo4jGenerator : Database
    {

		Neo4jConnector connector;

        public override void InitializeConnection()
        {
			connector = Neo4jConnector.Instance();
        }

        public override void GenerateData(Configuration config)
        {
            throw new NotImplementedException();
        }

        public override void CloseConnection()
        {
			connector?.Close();
        }
    }
}
