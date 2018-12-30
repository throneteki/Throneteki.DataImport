namespace CrimsonDev.Throneteki.DataImport
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using CrimsonDev.Throneteki.Data;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;

    public class Program
    {
        public static async Task Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Specify the location of the throneteki game data");
                System.Environment.Exit(0);
            }

            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            var configuration = builder.Build();

            var serviceCollection = new ServiceCollection()
                .AddLogging()
                .AddTransient<DataImporter>()
                .AddOptions();

            if (configuration["General:DatabaseProvider"] == "mssql")
            {
                serviceCollection.AddDbContext<ThronetekiDbContext>(options =>
                    options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));
            }
            else
            {
                serviceCollection.AddDbContext<ThronetekiDbContext>(options =>
                    options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));
            }

            var serviceProvider = serviceCollection.BuildServiceProvider();
            var importer = serviceProvider.GetService<DataImporter>();

            await importer.ImportAsync(args[0]);
        }
    }
}
