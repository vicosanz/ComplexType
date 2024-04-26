using ComplexType;
using System.Buffers.Text;
using System.Text.Json;
using System.Text.RegularExpressions;


Console.WriteLine("Hello, World!");


Email email = "sad@asda.com";

var newcustomer = new Customer(Guid.NewGuid(), Ulid.NewUlid(), 102.1m, 102.1m);

var serializeOptions = new JsonSerializerOptions
{
    WriteIndented = true
};
var json = JsonSerializer.Serialize(newcustomer, serializeOptions);
Console.WriteLine(json);
var newcustomer2 = JsonSerializer.Deserialize<Customer>(json);
Console.WriteLine(newcustomer2);
public record Customer(CustomerId Id, AccountId AccountId, AccountBalance Balance, decimal ExtraCredit);


[ComplexType]
public readonly partial record struct Email 
{
    internal static string emailPattern = @"^\w+([-+.']\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*$";
    public string Value { get; } = 
        Regex.IsMatch(Value, emailPattern, RegexOptions.IgnoreCase) ? Value : throw new InvalidCastException();
}


[ComplexType([EnumAdditionalConverters.Dapper, EnumAdditionalConverters.EFCore, EnumAdditionalConverters.NewtonsoftJson])]
public readonly partial record struct AccountBalance : IComplexType<decimal>
{
    public decimal Value { get; } = Value >= 0 ? Value: throw new ArgumentOutOfRangeException(nameof(Value));
    //    public string Value { get; } = !string.IsNullOrWhiteSpace(Value) ? Value : throw new ArgumentException("Value cannot be empty", nameof(Value));
}

[ComplexType([EnumAdditionalConverters.Dapper, EnumAdditionalConverters.EFCore, EnumAdditionalConverters.NewtonsoftJson])]
public readonly partial record struct CustomerId : IComplexType<Guid>
{
}

[ComplexType([EnumAdditionalConverters.Dapper, EnumAdditionalConverters.EFCore, EnumAdditionalConverters.NewtonsoftJson])]
public readonly partial record struct AccountId : IComplexType<Ulid>
{
}
