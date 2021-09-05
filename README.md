# filter-parser

Filter parser parses a filter string which is an approximate implementation of OData filter syntax.
http://docs.oasis-open.org/odata/odata/v4.0/errata02/os/complete/part2-url-conventions/odata-v4.0-errata02-os-part2-url-conventions-complete.html
The parsed filter object is an instance of a Criteria object which can be used to convert the filter into an SQL query.
Filter values are parameterised.

The parser takes in the raw filter value, a field resolver and a constant resolver.

The field resolver enables the conversion of the filter field name to the underlying sql table/alias and field.

The constant resolver enables the conversion of a string to a value, it is useful to convert enums to the underlying integer value.

Example usage with SqlKata library.

```csharp
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using Newtonsoft.Json;
using SqlKata.Execution;
using SqlKata.Compilers;
```

```csharp
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
}
else
{
    Console.WriteLine($"Raw filter: {filter}");
}
```

#### Operators

| Type | Operator | Syntax  |  |
|  ---  |  ---  |  ---  |  ---  |
| Whitespace | New Line |  |  |
| Binary | and | and |  |
| Binary | or | or |  |
| Logical | eq | {lhs} eq {rhs} |  |
| Logical | ex | ex ({rhs}) |  |
| Logical | ge | {lhs} ge {rhs} |  |
| Logical | gt | {lhs} gt {rhs} |  |
| Logical | le | {lhs} le {rhs} |  |
| Logical | lt | {lhs} lt {rhs} |  |
| Logical | not | not {rhs} |  |
| Arithmetic | add | {lhs} add {rhs} |  |
| Arithmetic | div | {lhs} div {rhs} |  |
| Arithmetic | mod | {lhs} mod {rhs} |  |
| Arithmetic | mul | {lhs} mul {rhs} |  |
| Arithmetic | sub | {lhs} sub {rhs} |  |
| Grouping | Open Group: ( | ( |  |
| Grouping | Close Group: ) | ) |  |
| Function | all | ? | **Not implemented** |
| Function | any | ? | **Not implemented** |
| Function | cast | ? | **Not implemented** |
| Function | ceiling | ceiling({lhs}) |  |
| Function | concat | concat({lhs},{rhs}) |  |
| Function | contains | contains({lhs},{rhs}) |  |
| Function | date | date({lhs}) | **Not implemented** |
| Function | day | day({lhs}) |  |
| Function | endswith | endswith({lhs},{rhs}) |  |
| Function | floor | floor({lhs}) |  |
| Function | fractionalseconds | fractionalseconds({lhs}) |  |
| Function | geo.distance | ? | **Not implemented** |
| Function | geo.intersects | ? | **Not implemented** |
| Function | geo.length | ? | **Not implemented** |
| Function | hour | hour({lhs}) |  |
| Function | indexof | indexof({lhs},{rhs}) |  |
| Function | isof | ? | **Not implemented** |
| Function | length | length({lhs}) |  |
| Function | maxdatetime | maxdatetime() |  |
| Function | mindatetime | mindatetime() |  |
| Function | minute | minute({lhs}) |  |
| Function | month | month({lhs}) |  |
| Function | now | now() |  |
| Function | round | round({lhs}) |  |
| Function | second | second({lhs}) |  |
| Function | startswith | startswith({lhs},{rhs}) |  |
| Function | substring | substring({lhs},{rhs}) substring({lhs},{rhs},{rhs1}) |  |
| Function | time | time({lhs}) | **Not implemented** |
| Function | tolower | tolower({lhs}) | **Not implemented** |
| Function | totaloffsetminutes | totaloffsetminutes({lhs}) | **Not implemented** |
| Function | totalseconds | totalseconds({lhs}) | **Not implemented** |
| Function | toupper | toupper({lhs}) | **Not implemented** |
| Function | trim | trim({lhs}) |  |
| Function | year | year({lhs}) |  |
