    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    namespace KSEFinvoiceSender.Configuration;

    public class ConfigSingleton
    {
        private static readonly Lazy<ConfigSingleton> _instance =
            new Lazy<ConfigSingleton>(() => new ConfigSingleton());

        public static ConfigSingleton Instance => _instance.Value;

        public DBConfig dbConfig { get; private set; }
        public KSeFConfig ksefConfig { get; private set; }
        private ConfigSingleton()
        {
            dbConfig = ConfigLoader.LoadSection<DBConfig>("Database");
            ksefConfig = ConfigLoader.LoadSection<KSeFConfig>("KSeF");
        }
    }
