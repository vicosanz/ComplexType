# ComplexType
C# Implementation of Complex Type Id made easy. Avoid primitive obsession using this package.

ComplexType [![NuGet Badge](https://buildstats.info/nuget/ComplexType)](https://www.nuget.org/packages/ComplexType/)

ComplexType.Generator [![NuGet Badge](https://buildstats.info/nuget/ComplexType.Generator)](https://www.nuget.org/packages/ComplexType.Generator/)

[![publish to nuget](https://github.com/vicosanz/ComplexType/actions/workflows/main.yml/badge.svg)](https://github.com/vicosanz/ComplexType/actions/workflows/main.yml)


## Buy me a coffee
If you want to reward my effort, :coffee: https://www.paypal.com/paypalme/vicosanzdev?locale.x=es_XC


All complex typed ids are source generated, you must create a record struct in this ways:

Using attribute decorating a record struct

```csharp
    [ComplexType] 
    public readonly partial record struct NonEmptyString { }
```

The generator will create a partial record struct of the same name

```csharp
// Auto generated code
[TypeConverter(typeof(NonEmptyStringTypeConverter))]
[System.Text.Json.Serialization.JsonConverter(typeof(NonEmptyStringJsonConverter))]
public readonly partial record struct NonEmptyString(string Value)
{
    public string Value { get; } = Validate(Value);

    public static implicit operator NonEmptyString(string value) => new(value);
    public static explicit operator string(NonEmptyString value) => value.Value;
    public static NonEmptyString Parse(string value) => new(value);
    public static bool TryParse(string value, out NonEmptyString result)
    {
        result = default;
        try
        {
            result = Parse(value);
            return true;
        }
        catch
        {
            return false;
        }
    }
    public override string ToString() => Value;
    public static NonEmptyString Create(string value) => new(value);
}
```

The new type is decorated with a TypeConverter and a JsonConverter automatically

```csharp
    // Auto generated code
    [TypeConverter(typeof(AccountBalanceTypeConverter))]
    [System.Text.Json.Serialization.JsonConverter(typeof(AccountBalanceJsonConverter))]
```

You can add additional logic to your complex type id, for example you can force validation about not null or empty content

```csharp
    [ComplexType] 
    public readonly partial record struct NonEmptyString 
    { 
        public static string Validate(string value, [CallerArgumentExpression(nameof(value))] string? argumentName = null)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Value cannot be null or empty", argumentName);
            }
            return value;
        }
    }

    [ComplexType]
    public readonly partial record struct Email 
    {
        public static string Validate(string value, [CallerArgumentExpression(nameof(value))] string? argumentName = null) =>
            EmailRegex().IsMatch(value) ? value : throw new ArgumentException("Invalid email", argumentName);
    
        [GeneratedRegex(@"^\w+([-+.']\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*$", RegexOptions.IgnoreCase)]
        private static partial Regex EmailRegex();
    }
    ...

    Email email = "suject@infowaresoluciones.com";
```

Use an ErrOr type to catch an error without using try catch
```csharp
    // Add this package to your project
    //     <PackageReference Include="UnionOf" Version="1.0.11" />

    [ComplexType]
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
    ...
    Console.WriteLine($"Email \"none@sdasd.sadasd.com\" valid: {Email.ValidateErrOr("none@sdasd.sadasd.com").IsValid()}");

    // Output
    // Email "none@sdasd.sadasd.com" valid: True
```

Create additional converters to popular packages like efcore, dapper and newtonsoftjson

```csharp
    [ComplexType([EnumAdditionalConverters.Dapper, EnumAdditionalConverters.EFCore, EnumAdditionalConverters.NewtonsoftJson])]
    public readonly partial record struct NonEmptyString 
```

Change the inner type to any primitive

```csharp
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
    }
```

Encapsulate incompatible or complex types, you must assign a primitive type and ParseBase in order to work with Dapper and EfCore.
```csharp
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
        //Convertibility between complex type (CultureInfo) and primitive (string)
        public static CultureInfo ParseBase(string value) => AllCultures.GetValueOrDefault(value)!;
        public static string ParseBase(CultureInfo value) => value.Name;
    }


    ...
    var culture = new CultureData("es-ES");
    CultureData myCulture = CultureInfo.CurrentCulture;
```

You can create strongly typed ids as Guid or Ulid

```csharp
    [ComplexType([EnumAdditionalConverters.Dapper, EnumAdditionalConverters.EFCore, EnumAdditionalConverters.NewtonsoftJson])]
    public readonly partial record struct CustomerId : IComplexType<Guid>
    {
    }

    [ComplexType([EnumAdditionalConverters.Dapper, EnumAdditionalConverters.EFCore, EnumAdditionalConverters.NewtonsoftJson])]
    public readonly partial record struct AccountId : IComplexType<Ulid>
    {
    }
```

You can serialize and deserialize without problems


```csharp
    var newcustomer = new Customer(Guid.NewGuid(), 102.1m);

    var serializeOptions = new JsonSerializerOptions
    {
        WriteIndented = true
    };
    var json = JsonSerializer.Serialize(newcustomer, serializeOptions);
    Console.WriteLine(json);
    var newcustomer2 = JsonSerializer.Deserialize<Customer>(json);
    Console.WriteLine(newcustomer2);




    public record Customer(Guid Id, AccountBalance Balance);

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
    }
```
