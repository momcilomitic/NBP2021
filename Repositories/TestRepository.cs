using Neo4j.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Neo4JTest.Repositories
{
    public class Person
    {
        public string Name { get; set; }

        public Person(string name)
        {
            Name = name;
        }
    }
    public interface ITestRepository
    {
        Task<IResultCursor> InsertName(string name);
    }
    public class TestRepository : ITestRepository
    {
        private readonly IDriver _driver;

        public TestRepository(/*IDriver driver*/)
        {
            //_driver = driver;
        }

        public async Task<IResultCursor> InsertName(string name)
        {
            //var session = _driver.AsyncSession(WithDatabase);
            var session = _driver.AsyncSession();
            try
            {
                return (IResultCursor)await session.ReadTransactionAsync(async transaction =>
                {
                    var cursor = await transaction.RunAsync(@"
                        MATCH (p:Person {name: 'Momcilo'}) RETURN p",  
                        new { name }
                    );

                    //return await cursor.SingleAsync(record => new Movie(
                    //    record["title"].As<string>(),
                    //    MapCast(record["cast"].As<List<IDictionary<string, object>>>())
                    //));

                    return await cursor.SingleAsync(record => new Person(record["name"].As<string>()));
                });
            }
            catch (Exception e)
            {

                throw;
            }
            finally
            {
                await session.CloseAsync();
            }
        }

        private static void WithDatabase(SessionConfigBuilder sessionConfigBuilder)
        {
            var neo4jVersion = System.Environment.GetEnvironmentVariable("NEO4J_VERSION") ?? "";
            if (!neo4jVersion.StartsWith("4"))
            {
                return;
            }

            sessionConfigBuilder.WithDatabase(Database());
        }
        private static string Database()
        {
            return System.Environment.GetEnvironmentVariable("NEO4J_DATABASE") ?? "movies";
        }
    }
}
