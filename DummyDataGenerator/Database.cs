using System.Runtime.Serialization;
using Newtonsoft.Json;

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
		[JsonProperty("cd")]
		public int ChainDepth { get; set; }

		[JsonProperty("noa")]
		public int NumberOfActivities { get; set; }

		[JsonProperty("nos")]
		public int NumberOfSuppliers { get; set; }

		[JsonProperty("notls")]
		public int NumberOfTopLevelSuppliers { get; set; }

		[JsonProperty("nop")]
		public int NumberOfProducts { get; set; }

		[JsonProperty("cb")]
		public int ChainBreadth { get; set; }


		public Configuration(int chainDepth, int chainBreadth, int numberOfActivities, int numberOfSuppliers, int numberOfTopLevelSuppliers, int numberOfProducts)
        {
            this.ChainDepth = chainDepth;
			this.ChainBreadth = chainBreadth;
			this.NumberOfSuppliers = numberOfSuppliers;
			this.NumberOfTopLevelSuppliers = numberOfTopLevelSuppliers;
			this.NumberOfActivities = numberOfActivities;
			this.NumberOfProducts = numberOfProducts;
        }

    }
}
