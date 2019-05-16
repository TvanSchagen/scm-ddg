using System;
using System.Collections.Generic;
using System.Text;

namespace DummyDataGenerator
{
    class Neo4j : Database
    {
        public override void CloseConnection()
        {
            throw new NotImplementedException();
        }

        public override void GenerateData(Configuration config)
        {
            throw new NotImplementedException();
        }

        public override void InitializeConnection()
        {
            throw new NotImplementedException();
        }
    }
}
