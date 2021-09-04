using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using Newtonsoft.Json;
using SqlKata.Execution;
using SqlKata.Compilers;

namespace JimTaylor1974.FilterParser.ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var makrdown = Operator.DocumentAsMarkdown(false);
            Console.WriteLine(makrdown);
            var html = Operator.DocumentAsHtml();
            Console.WriteLine(html);

            // Order, OrderType, Offer
            // Order.Status: Placed, InPacking, InTransit, Delivered
            // OrderType.Type: Standard, Prime

            var fields = new Dictionary<string, Tuple<Identifier, Type>>(StringComparer.OrdinalIgnoreCase)
            {
                { "Id", Tuple.Create(new Identifier("Order", "Id"), typeof(Guid)) },
                { "Status", Tuple.Create(new Identifier("Order", "Status"), typeof(int)) },
                { "StatusChangedOnUtc", Tuple.Create(new Identifier("Order", "StatusChangedOnUtc"), typeof(DateTime)) },
                { "CreatedOnUtc", Tuple.Create(new Identifier("Order", "CreatedOnUtc"), typeof(DateTime)) },
                { "CustomerName", Tuple.Create(new Identifier("Order", "CustomerName"), typeof(string)) },
                { "Address", Tuple.Create(new Identifier("Order", "Address"), typeof(string)) },
                { "Type", Tuple.Create(new Identifier("OrderType", "Type"), typeof(int)) },
                { "Name", Tuple.Create(new Identifier("OrderType", "Name"), typeof(string)) },
                { "Description", Tuple.Create(new Identifier("OrderType", "Description"), typeof(string)) },
                { "Offer", Tuple.Create(new Identifier("o", "Name"), typeof(string)) }
            };

            var constants = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                { "Placed", 0 },
                { "InPacking", 1 },
                { "InTransit", 2 },
                { "Delivered", 3 },
                { "Standard", 0 },
                { "Prime", 1 }
            };

            FilterField ResolveField(string value)
            {
                if (fields.ContainsKey(value))
                {
                    var (identifier, type) = fields[value];
                    return new FilterField { Identifier = identifier, Type = type };
                }

                return null;
            }

            object ResolveConstant(string value)
            {
                if (constants.ContainsKey(value))
                {
                    return constants[value];
                }

                return null;
            }

            var filter = "(status eq InTransit or status eq Delivered) and substring(CustomerName, 1, 3) eq 'Jim'";

            if (Criteria.TryParse(filter, ResolveField, ResolveConstant, out var criteria))
            {
                Console.WriteLine(criteria.ToString(Syntax.Sql));

                var parameters = criteria.GetAllParameters().ToArray();

                foreach (var parameter in parameters)
                {
                    Console.WriteLine($"{parameter.Name}: {parameter.Value}");
                }

                /*
                // NOTE: Example use with SqlKata ...

                var connectionString = "Data Source=.;Initial Catalog=JimTaylor1974FilterParserExampleData;Integrated Security=true;";

                var sqlConnection = new SqlConnection(connectionString);
                sqlConnection.Open();

                var db = new QueryFactory(sqlConnection, new SqlServerCompiler());
                var sqlKataQuery = db
                    .Query("Order")
                    .Join("OrderType", "OrderType.Id", "Order.OrderTypeId")
                    .LeftJoin("Offer AS o", "o.Id", "Order.OfferId")
                    .Select(
                        "Order.{Id, Status, StatusChangedOnUtc, CreatedOnUtc, CustomerName, Address}",
                        "OrderType.{Type, Name, Description}",
                        "o.Name AS Offer"
                    );

                var sql = criteria.ToString(Syntax.SqlKata);
                var bindings = parameters.Select(p => p.Value).ToArray();
                sqlKataQuery = sqlKataQuery.WhereRaw(sql, bindings);

                var orders = sqlKataQuery.Get();

                foreach (var order in orders)
                {
                    Console.WriteLine(JsonConvert.SerializeObject(order, Formatting.Indented));
                }
                */
            }
            else
            {
                Console.WriteLine($"Raw filter: {filter}");
            }

            Console.WriteLine("DONE");
            Console.ReadLine();
        }
    }
}
