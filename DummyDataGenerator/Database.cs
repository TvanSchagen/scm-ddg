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

		public int NumberOfTopLevelSuppliers { get; }

		public int NumberOfProducts { get; }

		public int ChainBreadth { get; }


		public Configuration(int numberOfChains, int chainDepth, int chainBreadth, int numberOfActivities, int numberOfSuppliers, int numberOfTopLevelSuppliers, int numberOfProducts)
        {
            this.NumberOfChains = numberOfChains;
            this.ChainDepth = chainDepth;
			this.ChainBreadth = chainBreadth;
			this.NumberOfSuppliers = numberOfSuppliers;
			this.NumberOfTopLevelSuppliers = numberOfTopLevelSuppliers;
			this.NumberOfActivities = numberOfActivities;
			this.NumberOfProducts = numberOfProducts;
        }

    }
}
