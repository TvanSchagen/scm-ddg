using System;
using System.Collections.Generic;
using System.Text;

namespace DummyDataGenerator
{
    public abstract class Database
    {
        public abstract void InitializeConnection();

        public abstract void GenerateData(Configuration config);

        public abstract void CloseConnection();

    }

    public class Configuration
    {

        public int NumberOfChains { get; }

        public int ChainDepth { get; }

		public int NumberOfActivities { get; }

		public int NumberOfSuppliers { get; }

		public int NumberOfProducts { get; }

        public Configuration(int numberOfChains, int chainDepth, int numberOfActivities, int numberOfSuppliers, int numberOfProducts)
        {
            this.NumberOfChains = numberOfChains;
            this.ChainDepth = chainDepth;
			this.NumberOfSuppliers = numberOfSuppliers;
			this.NumberOfActivities = numberOfActivities;
			this.NumberOfProducts = numberOfProducts;
        }
    }
}
