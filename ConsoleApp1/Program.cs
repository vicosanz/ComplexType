using ComplexType;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Newtonsoft.Json.Linq;
using System;
using System.Buffers;
using System.Buffers.Text;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using UnionOf;

var serializeOptions = new JsonSerializerOptions
{
    WriteIndented = true
};

var converter = new ComplexType.AutoConverter<CultureInfo, string>(x => x.Name, x => new CultureInfo(x));

var conv1 = converter.Convert(CultureInfo.CurrentCulture);
var conv2 = converter.Convert(conv1);
Console.WriteLine(conv1);
Console.WriteLine(conv2);

Console.WriteLine("Hello, World!");

DateSerial dateSerial = new(new(2021, 10, 10));
var jsonDateSerial = JsonSerializer.Serialize(dateSerial, serializeOptions);
Console.WriteLine(jsonDateSerial);

//CultureData culture2 = "xxx";

CultureData culture = "en-US";
Console.WriteLine(culture);
culture = CultureData.Create(CultureInfo.CurrentCulture);
Console.WriteLine(culture);

CultureData culture1 = CultureInfo.CurrentCulture;
Console.WriteLine(culture1);


NonEmptyString nonEmptyString2 = "sadasd";

CustomerIdx customerIdx = CustomerIdx.Create();
var jsonx = JsonSerializer.Serialize(customerIdx, serializeOptions);
Console.WriteLine(jsonx);
var customerIdx2 = JsonSerializer.Deserialize<CustomerIdx>(jsonx);
Console.WriteLine(customerIdx2);

Console.WriteLine($"Email \"none@sdasd.sadasd.com\" valid: {Email.ValidateErrOr("none@sdasd.sadasd.com").IsValid()}");

var paramEmail = "sada@sda.com";
Email.Validate(paramEmail);
Email email = "sad@asda.com";
Console.WriteLine(email);

var newcustomer = new Customer(Guid.NewGuid(), Ulid.NewUlid(), 100000.1m, 102.1m, dateSerial);

var json = JsonSerializer.Serialize(newcustomer, serializeOptions);
Console.WriteLine(json);

var json2 = Newtonsoft.Json.JsonConvert.SerializeObject(newcustomer, Newtonsoft.Json.Formatting.Indented, new CustomerIdNewtonsoftJsonConverter());
Console.WriteLine(json2);

var newcustomer2 = JsonSerializer.Deserialize<Customer>(json);
Console.WriteLine(newcustomer2);
public record Customer(CustomerId Id, AccountId AccountId, AccountBalance Balance, decimal ExtraCredit, DateSerial DateSerial);


[ComplexType([EnumAdditionalConverters.Dapper, EnumAdditionalConverters.EFCore, EnumAdditionalConverters.NewtonsoftJson])]
public readonly partial record struct CultureData : IComplexType<CultureInfo, string>
{
    private static readonly Dictionary<string, CultureInfo> AllCultures = CultureInfo.GetCultures(CultureTypes.AllCultures).ToDictionary(c => c.Name);

    public static CultureInfo Validate(CultureInfo value, [CallerArgumentExpression(nameof(value))] string? argumentName = null)
    {
        if (value == null)
        {
            throw new ArgumentException("Unable to parse Culture", argumentName);
        }
        return value;
    }

    public static AutoConverter<CultureInfo, string> Converter => new(x => x.Name, x => AllCultures[x]);
}

[ComplexType([EnumAdditionalConverters.Dapper, EnumAdditionalConverters.EFCore, EnumAdditionalConverters.NewtonsoftJson])]
public readonly partial record struct DateSerial : IComplexType<Tuple<int, int, int>, string>
{
    public static AutoConverter<Tuple<int, int, int>, string> Converter => new(x => JsonSerializer.Serialize(x), x => JsonSerializer.Deserialize<Tuple<int, int, int>>(x)!);
}

[ComplexType([EnumAdditionalConverters.Dapper, EnumAdditionalConverters.EFCore, EnumAdditionalConverters.NewtonsoftJson])]
public readonly partial record struct NonEmptyString
{
    public static string Validate(string value, [CallerArgumentExpression(nameof(value))] string? argumentName = null) => 
        string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Value cannot be null or empty", argumentName) : value;

}
[ComplexType([EnumAdditionalConverters.Dapper, EnumAdditionalConverters.EFCore, EnumAdditionalConverters.NewtonsoftJson])]
public readonly partial record struct NonEmptyStringErrOr
{
    public static ErrOr<string> ValidateErrOr(string value, [CallerArgumentExpression(nameof(value))] string? argumentName = null)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return new ArgumentException("Value cannot be null or empty", argumentName);
        }
        return value;
    }

    public static string Validate(string value, [CallerArgumentExpression(nameof(value))] string? argumentName = null)
    {
        var validate = ValidateErrOr(value, argumentName);
        return validate.IsFail(out Exception ex) ? throw ex : validate.ValueT0;
    }
}

[ComplexType([EnumAdditionalConverters.Dapper, EnumAdditionalConverters.EFCore, EnumAdditionalConverters.NewtonsoftJson])]
public readonly partial record struct Email
{
    public static ErrOr<string> ValidateErrOr(string value, [CallerArgumentExpression(nameof(value))] string? argumentName = null) =>
        EmailRegex().IsMatch(value) ? value : new ArgumentException("Invalid email", argumentName);

    public static string Validate(string value, [CallerArgumentExpression(nameof(value))] string? argumentName = null)
    {
        var validate = ValidateErrOr(value, argumentName);
        return validate.IsFail(out Exception ex) ? throw ex : validate.ValueT0;
    }

    [GeneratedRegex(@"^\w+([-+.']\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*$", RegexOptions.IgnoreCase)]
    private static partial Regex EmailRegex();
}


[ComplexType([EnumAdditionalConverters.Dapper, EnumAdditionalConverters.EFCore, EnumAdditionalConverters.NewtonsoftJson])]
public readonly partial record struct AccountBalance : IComplexType<decimal>
{
    public static decimal Validate(decimal value, [CallerArgumentExpression(nameof(value))] string? argumentName = null) => 
        value switch
        {
            < 0 => throw new ArgumentException("Value cannot be negative", argumentName),
            > 1000000 => throw new ArgumentException("Value cannot be greater than 1000000", argumentName),
            _ => value
        };
    //public static decimal ParseBase(string value) => decimal.Parse(value);
    //public static string ParseBase(decimal value) => value.ToString();
}

[ComplexType([EnumAdditionalConverters.Dapper, EnumAdditionalConverters.EFCore, EnumAdditionalConverters.NewtonsoftJson])]
public readonly partial record struct CustomerId : IComplexType<Guid>
{
}

[ComplexType([EnumAdditionalConverters.Dapper, EnumAdditionalConverters.EFCore, EnumAdditionalConverters.NewtonsoftJson])]
public readonly partial record struct AccountId : IComplexType<Ulid>
{
}

[ComplexType([EnumAdditionalConverters.Dapper, EnumAdditionalConverters.EFCore, EnumAdditionalConverters.NewtonsoftJson])]
public readonly partial record struct CustomerIdx : IComplexType<Ulid>
{
}

