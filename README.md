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
public readonly partial record struct NonEmptyString(string Value)
{
    public static implicit operator NonEmptyString(string value) => new(value);

    public static explicit operator string(NonEmptyString value) => value.Value;

    public static NonEmptyString NewNonEmptyString(string value) => new(value);

    public override string ToString() => Value.ToString();
}
```

You can add additional logic to your complex type id, for example you can force validation about not null or empty content

```csharp
    [ComplexType] 
    public readonly partial record struct NonEmptyString 
    { 
        public string Value { get; } = !string.IsNullOrWhiteSpace(Value) ? Value : 
            throw new ArgumentException("Value cannot be empty", nameof(Value));
    }

    [ComplexType]
    public readonly partial record struct Email 
    {
        internal static string emailPattern = @"^\w+([-+.']\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*$";
        public string Value { get; } = 
            Regex.IsMatch(Value, emailPattern, RegexOptions.IgnoreCase) ? Value : throw new InvalidCastException();
    }
    ...

    Email email = "suject@infowaresoluciones.com";
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
        public decimal Value { get; } = Value >= 0 ? Value: throw new ArgumentOutOfRangeException(nameof(Value));
    }
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

The new type is decorated with a TypeConverter and a JsonConverter automatically

```csharp
    // Auto generated code
    [TypeConverter(typeof(AccountBalanceTypeConverter))]
    [System.Text.Json.Serialization.JsonConverter(typeof(AccountBalanceJsonConverter))]
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
        public decimal Value { get; } = Value >= 0 ? Value: throw new ArgumentOutOfRangeException(nameof(Value));
    }
```
