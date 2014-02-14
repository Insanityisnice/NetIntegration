using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetIntegration.Testing.Database;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetIntegration.Testing.UnitTests.Database
{
    [TestClass]
    public class DatabaseManagerTests
    {
        [TestMethod]
        public void DatabaseManager_DeployLocalDB()
        {
            DatabaseManager.SetDataDirectory();

            var connectionString = @"Data Source=(LocalDB)\v11.0;AttachDbFileName=|DataDirectory|\Northwind.mdf;Initial Catalog=Northwind;Integrated Security=True";
            var database = DatabaseManager.GetDatabase(connectionString);

            var dacPackageFile = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, @"..\..\..\NetIntegraton.Database.Northwind\bin\Debug\Database.Northwind.dacpac"));
            database.DeployLocalDB(dacPackageFile);
        }
    }
}