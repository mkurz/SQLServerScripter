using System;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using System.Text;
using System.Data.SqlClient;
using Microsoft.SqlServer.Management.Common;
using System.IO;

/// <summary>
/// Based on https://stackoverflow.com/a/11655269/810109
/// </summary>
public class SQLServerScripter
{

    public static void Main(string[] args)
    {
        var dataSource = args.Length > 0 ? args[0] : "localhost"; // 172.17.0.5,1433 192.168.56.1,1435, mydb.database.windows.net,1433
        var dbName = args.Length > 1 ? args[1] : "master";
        var user = args.Length > 2 ? args[2] : "sa";
        var outputFolderSql = args.Length > 3 ? args[3] : "";

        Console.WriteLine("Password for user {0} for database {1} at {2}: ", user, dbName, dataSource);
        var password = GetPassword();

        SqlServerVersion targetServerVersion = SqlServerVersion.Version140;
        DatabaseEngineEdition targetDatabaseEngineEdition = DatabaseEngineEdition.Standard;
        DatabaseEngineType targetDatabaseEngineType = DatabaseEngineType.Standalone;

        String outputFileBase = dbName + "@" + @dataSource + "_" + DateTime.Now.ToString("yyyy-MM-dd_hh-mm-ss");
        String outputFileSql = outputFileBase + ".sql";
        String outputFilePathSql = Path.Combine(outputFolderSql, outputFileSql);

        Console.WriteLine("Connecting to database... ");

        var connectionString = new SqlConnectionStringBuilder
        {
            PersistSecurityInfo = true, // Needed - see https://github.com/Microsoft/sqltoolsservice/issues/372#issuecomment-307190530
            DataSource = @dataSource,
            UserID = user,
            Password = password,
            InitialCatalog = dbName,
            PacketSize = 4096, // default is 8000, however 4096 gives much better performance when accessing azure database
            //IntegratedSecurity = true, // Log in using SQL authentication or Windows authentication
            //MultipleActiveResultSets = false,
            //Encrypt = false,
            //TrustServerCertificate = false,
        };
        var sqlConnection = new SqlConnection(connectionString.ConnectionString);
        var serverConnection = new ServerConnection(sqlConnection);
        var sqlServer = new Server(serverConnection);
        //sqlServer.ConnectionContext.BatchSeparator = "GO";

        // Improve performance by just loading fields we need
        sqlServer.SetDefaultInitFields(typeof(StoredProcedure), "IsSystemObject");
        sqlServer.SetDefaultInitFields(typeof(Table), "IsSystemObject");
        sqlServer.SetDefaultInitFields(typeof(View), "IsSystemObject");
        sqlServer.SetDefaultInitFields(typeof(UserDefinedFunction), "IsSystemObject");
        sqlServer.SetDefaultInitFields(typeof(Trigger), "IsSystemObject");
        sqlServer.SetDefaultInitFields(typeof(SqlAssembly), "IsSystemObject");
        sqlServer.SetDefaultInitFields(typeof(Default), false);
        sqlServer.SetDefaultInitFields(typeof(Rule), false);
        sqlServer.SetDefaultInitFields(typeof(UserDefinedAggregate), false);
        sqlServer.SetDefaultInitFields(typeof(Synonym), false);
        sqlServer.SetDefaultInitFields(typeof(Sequence), false);
        sqlServer.SetDefaultInitFields(typeof(SecurityPolicy), false);
        sqlServer.SetDefaultInitFields(typeof(UserDefinedDataType), false);
        sqlServer.SetDefaultInitFields(typeof(XmlSchemaCollection), false);
        sqlServer.SetDefaultInitFields(typeof(UserDefinedType), false);
        sqlServer.SetDefaultInitFields(typeof(UserDefinedTableType), false);
        sqlServer.SetDefaultInitFields(typeof(PartitionScheme), false);
        sqlServer.SetDefaultInitFields(typeof(PartitionFunction), false);
        sqlServer.SetDefaultInitFields(typeof(PlanGuide), false);
        sqlServer.SetDefaultInitFields(typeof(FullTextCatalog), false);

        var db = sqlServer.Databases[dbName];

        Console.WriteLine("Connected!");

        Console.WriteLine("Generating script for database " + dbName + ". This may take a while...");

        // ### First script "CREATE DATABASE ..."
        var createDbScriptOptions = DefaultOptions(targetServerVersion, targetDatabaseEngineEdition, targetDatabaseEngineType);
        createDbScriptOptions.FileName = outputFilePathSql;
        createDbScriptOptions.ScriptData = false;
        createDbScriptOptions.WithDependencies = false;
        db.Script(createDbScriptOptions);

        Console.WriteLine("Collecting objects...");

        // ### Now script all the objects within the database
        var urns = new UrnCollection();
        Console.WriteLine("...Tables");
        foreach (Table obj in db.Tables)
        {
            if (!obj.IsSystemObject)
            {
                Console.WriteLine(" -> " + obj.Name);
                urns.Add(obj.Urn);
            }
        }
        Console.WriteLine("...Views");
        foreach (View obj in db.Views)
        {
            if (!obj.IsSystemObject)
            {
                Console.WriteLine(" -> " + obj.Name);
                urns.Add(obj.Urn);
            }
        }
        Console.WriteLine("...UserDefinedFunctions");
        foreach (UserDefinedFunction obj in db.UserDefinedFunctions)
        {
            if (!obj.IsSystemObject)
            {
                Console.WriteLine(" -> " + obj.Name);
                urns.Add(obj.Urn);
            }
        }
        Console.WriteLine("...Defaults");
        foreach (Default obj in db.Defaults)
        {
            Console.WriteLine(" -> " + obj.Name);
            urns.Add(obj.Urn);
        }
        Console.WriteLine("...Rules");
        foreach (Rule obj in db.Rules)
        {
            Console.WriteLine(" -> " + obj.Name);
            urns.Add(obj.Urn);
        }
        Console.WriteLine("...Triggers");
        foreach (Trigger obj in db.Triggers)
        {
            if (!obj.IsSystemObject)
            {
                Console.WriteLine(" -> " + obj.Name);
                urns.Add(obj.Urn);
            }
        }
        Console.WriteLine("...UserDefinedAggregates");
        foreach (UserDefinedAggregate obj in db.UserDefinedAggregates)
        {
            Console.WriteLine(" -> " + obj.Name);
            urns.Add(obj.Urn);
        }
        Console.WriteLine("...Synonyms");
        foreach (Synonym obj in db.Synonyms)
        {
            Console.WriteLine(" -> " + obj.Name);
            urns.Add(obj.Urn);
        }
        Console.WriteLine("...Sequences");
        foreach (Sequence obj in db.Sequences)
        {
            Console.WriteLine(" -> " + obj.Name);
            urns.Add(obj.Urn);
        }
        Console.WriteLine("...SecurityPolicies");
        foreach (SecurityPolicy obj in db.SecurityPolicies)
        {
            Console.WriteLine(" -> " + obj.Name);
            urns.Add(obj.Urn);
        }
        Console.WriteLine("...UserDefinedDataTypes");
        foreach (UserDefinedDataType obj in db.UserDefinedDataTypes)
        {
            Console.WriteLine(" -> " + obj.Name);
            urns.Add(obj.Urn);
        }
        Console.WriteLine("...XmlSchemaCollections");
        foreach (XmlSchemaCollection obj in db.XmlSchemaCollections)
        {
            Console.WriteLine(" -> " + obj.Name);
            urns.Add(obj.Urn);
        }
        Console.WriteLine("...UserDefinedTypes");
        foreach (UserDefinedType obj in db.UserDefinedTypes)
        {
            Console.WriteLine(" -> " + obj.Name);
            urns.Add(obj.Urn);
        }
        Console.WriteLine("...UserDefinedTableTypes");
        foreach (UserDefinedTableType obj in db.UserDefinedTableTypes)
        {
            Console.WriteLine(" -> " + obj.Name);
            urns.Add(obj.Urn);
        }
        Console.WriteLine("...PartitionSchemes");
        foreach (PartitionScheme obj in db.PartitionSchemes)
        {
            Console.WriteLine(" -> " + obj.Name);
            urns.Add(obj.Urn);
        }
        Console.WriteLine("...PartitionFunctions");
        foreach (PartitionFunction obj in db.PartitionFunctions)
        {
            Console.WriteLine(" -> " + obj.Name);
            urns.Add(obj.Urn);
        }
        Console.WriteLine("...PlanGuides");
        foreach (PlanGuide obj in db.PlanGuides)
        {
            Console.WriteLine(" -> " + obj.Name);
            urns.Add(obj.Urn);
        }
        Console.WriteLine("...Assemblies");
        foreach (SqlAssembly obj in db.Assemblies)
        {
            if (!obj.IsSystemObject)
            {
                Console.WriteLine(" -> " + obj.Name);
                urns.Add(obj.Urn);
            }
        }
        Console.WriteLine("...StoredProcedures");
        foreach (StoredProcedure obj in db.StoredProcedures)
        {
            if (!obj.IsSystemObject)
            {
                Console.WriteLine(" -> " + obj.Name);
                urns.Add(obj.Urn);
            }
        }
        var ftUrns = new UrnCollection();
        Console.WriteLine("...FullTextCatalogs");
        foreach (FullTextCatalog obj in db.FullTextCatalogs)
        {
            Console.WriteLine(" -> " + obj.Name);
            ftUrns.Add(obj.Urn);
        }

        var scripter = new Scripter(sqlServer)
        {
            PrefetchObjects = true, // some sources suggest this may speed things up
        };
        scripter.Options = DefaultOptions(targetServerVersion, targetDatabaseEngineEdition, targetDatabaseEngineType);
        scripter.Options.FileName = outputFilePathSql;
        scripter.ScriptingProgress += (o, e) => Console.WriteLine("Scripting: {0}", e.Current);
        scripter.ScriptingError += (o, e) => Console.WriteLine("Error: {0}", e.Current);
        scripter.Options.WithDependencies = false;
        scripter.EnumScript(ftUrns);
        scripter.Options.WithDependencies = true;
        scripter.EnumScript(urns);

        Console.WriteLine("Scripting finished! See File:");
        Console.WriteLine(outputFilePathSql);

        // And now we also could create a backup (but be aware the file is saved ON THE SERVER
        // (Maybe we could save it locally somehow via DeviceType.Pipe, DeviceType.Url, DeviceType.VirtualDevice, etc...)
        //String outputFileBak = outputFileBase + ".bak";
        //String outputFilePathBak = outputFileBak;
        //Console.WriteLine();
        //Console.WriteLine("And now we create a backup...");
        //var backup = new Backup
        //{
        //    Action = BackupActionType.Database,
        //    Database = dbName
        //};
        //backup.Devices.Add(new BackupDeviceItem(outputFilePathBak, DeviceType.File));
        //backup.Initialize = true;
        //backup.Checksum = true;
        //backup.ContinueAfterError = false;
        //backup.Incremental = false;
        //backup.PercentCompleteNotification = 1;
        //backup.LogTruncation = BackupTruncateLogType.Truncate;
        //backup.PercentComplete += (s, e) => Console.WriteLine("Backup: {0}", e.Message);
        //backup.Complete += (s, e) => Console.WriteLine("{0} See File: {1}", e.Error.Message, outputFilePathBak);
        //backup.SqlBackupAsync(sqlServer);

        // ----

        // An alternative would be using the internal class ScriptPublis‌​hWizard via reflection (because we can't access internal stuff)
        // ###################################################################
        //new Microsoft.SqlServer.Management.SqlScriptPublish.ScriptPublis‌​hWizard();
        //var assembly = Assembly.LoadFrom(@"C:\Program Files (x86)\Microsoft SQL Server\140\Tools\Binn\ManagementStudio\Microsoft.SqlServer.Management.SqlScriptPublishUI.dll");

        // ----

        // Bonus: Display all the configuration options.
        // ###################################################################
        //foreach (ConfigProperty p in sqlServer.Configuration.Properties)
        //{
        //    Console.WriteLine(p.DisplayName + " " + p.ConfigValue + " " + p.RunValue);
        //}
        //Console.WriteLine("There are " + sqlServer.Configuration.Properties.Count.ToString() + " configuration options.");
        ////Display the maximum and minimum values for ShowAdvancedOptions.
        //int min = sqlServer.Configuration.ShowAdvancedOptions.Minimum;
        //int max = sqlServer.Configuration.ShowAdvancedOptions.Maximum;
        //Console.WriteLine("Minimum and Maximum values are " + min + " and " + max + ".");

        sqlConnection.Close();
    }

    /// <summary>
    /// See https://msdn.microsoft.com/en-us/library/microsoft.sqlserver.management.smo.scriptingoptions_properties.aspx
    /// </summary>
    /// <param name="options">ScriptingOptions to modify - if null a new ScriptingOptions object will be used instead.</param>
    /// <returns>A ScriptingOptions with default values set</returns>
    private static ScriptingOptions DefaultOptions(SqlServerVersion targetServerVersion, DatabaseEngineEdition targetDatabaseEngineEdition, DatabaseEngineType targetDatabaseEngineType)
    {
        var o = new ScriptingOptions();

        // General
        o.AllowSystemObjects = false;
        o.DdlHeaderOnly = false;
        o.DdlBodyOnly = false;

        // Here you could disable scripting absolute file paths
        o.NoFileGroup = false;
        o.IncludeFullTextCatalogRootPath = true;

        // Output
        o.ScriptBatchTerminator = true;
        o.BatchSize = 100;
        o.NoCommandTerminator = false;
        o.AnsiFile = false;
        o.Encoding = Encoding.UTF8;
        o.ToFileOnly = true;

        // "General" from the SSMS UI
        o.AnsiPadding = false;
        o.AppendToFile = true;
        o.IncludeIfNotExists = false;
        o.ContinueScriptingOnError = false;
        o.ConvertUserDefinedDataTypesToBaseType = false;
        o.WithDependencies = true;
        o.IncludeHeaders = true;
        o.DriIncludeSystemNames = false;
        // Include unsupported statements - doesn't exist...
        o.SchemaQualify = true;
        o.Bindings = false;
        o.NoCollation = true;
        o.DriDefaults = true;
        o.ScriptDrops = false;
        o.ExtendedProperties = true;
        o.TargetServerVersion = targetServerVersion;
        o.TargetDatabaseEngineEdition = targetDatabaseEngineEdition;
        o.TargetDatabaseEngineType = targetDatabaseEngineType;
        o.LoginSid = false;
        o.Permissions = false;
        o.ScriptOwner = false;
        o.Statistics = false;
        o.IncludeDatabaseContext = true;
        o.ScriptSchema = true;
        o.ScriptData = true;

        // "Table view options" from the SSMS UI
        o.ChangeTracking = false;
        o.DriChecks = true;
        o.ScriptDataCompression = false;
        o.DriForeignKeys = true;
        o.FullTextIndexes = true;
        o.Indexes = true;
        o.PrimaryObject = true;
        o.Triggers = true;
        o.DriUniqueKeys = true;

        o.NoIdentities = true;
        o.IncludeDatabaseRoleMemberships = false;
        o.NoMailProfilePrincipals = true;
        o.NoMailProfileAccounts = true;
        o.NoExecuteAs = true;

        o.TimestampToBinary = false;
        o.NoFileStreamColumn = false;
        o.NoFileStream = false;
        o.NoViewColumns = false;
        o.NoVardecimal = false;
        o.EnforceScriptingOptions = false;
        o.OptimizerData = false;
        o.NoXmlNamespaces = false;
        o.NoTablePartitioningSchemes = false;
        o.NoIndexPartitioningSchemes = false;
        o.NonClusteredIndexes = true;
        o.ClusteredIndexes = true;
        o.XmlIndexes = true;
        o.SchemaQualifyForeignKeysReferences = true;
        o.FullTextStopLists = true;
        o.FullTextCatalogs = true;
        o.NoAssemblies = false;

        o.AgentAlertJob = true;
        o.AgentNotify = true;
        o.AgentJobId = true;

        o.Default = true;

        o.DriAll = true;
        o.DriAllConstraints = true;
        o.DriAllKeys = true;
        o.DriIndexes = true;
        o.DriPrimaryKey = true;
        o.DriClustered = true;
        o.DriNonClustered = true;
        o.DriWithNoCheck = false;

        return o;
    }

    private static String GetPassword()
    {
        string pass = "";
        ConsoleKeyInfo key;

        do
        {
            key = Console.ReadKey(true);

            // Backspace Should Not Work
            if (key.Key != ConsoleKey.Backspace && key.Key != ConsoleKey.Enter && !char.IsControl(key.KeyChar))
            {
                pass += key.KeyChar;
                Console.Write("*");
            }
            else
            {
                if (key.Key == ConsoleKey.Backspace && pass.Length > 0)
                {
                    pass = pass.Substring(0, (pass.Length - 1));
                    Console.Write("\b \b");
                }
            }
        }
        // Stops Receving Keys Once Enter is Pressed
        while (key.Key != ConsoleKey.Enter);

        Console.WriteLine();

        return pass;
    }
}