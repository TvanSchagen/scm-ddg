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

		public int ChainDepth { get; set; }

		public int NumberOfActivities { get; set; }

		public int NumberOfSuppliers { get; set; }

		public int NumberOfTopLevelSuppliers { get; set; }

		public int NumberOfProducts { get; set; }

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
