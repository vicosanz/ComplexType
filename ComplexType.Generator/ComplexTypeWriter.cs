namespace ComplexType.Generator
{
    public class ComplexTypeWriter(Metadata metadata) : AbstractWriter
    {
        public string GetCode()
        {
            WriteFile();
            return GeneratedText();
        }

        private void WriteFile()
        {
            AddUsings("System");
            AddUsings("System.ComponentModel");
            AddUsings("System.Globalization");
            AddUsings("System.Text.Json.Serialization");
            AddUsings("System.Text.Json");
            AddUsings("System.Buffers");
            AddUsings("System.Runtime.CompilerServices");
            if (metadata.AdditionalConverters.Any(x => x == 0))
            {
                AddUsings("Microsoft.EntityFrameworkCore.Storage.ValueConversion");
            }
            foreach (var @using in metadata.Usings)
            {
                Write(@using);
            }
            WriteLine();
            WriteLine("#nullable enable");
            WriteLine();

            if (!string.IsNullOrEmpty(metadata.Namespace))
            {
                WriteLine($"namespace {metadata.Namespace};");
            }
            WriteLine();
            WriteComplexType();
        }

        private void AddUsings(string assembly)
        {
            var assemblyUsing = $"using {assembly};";
            if (!metadata.Usings.Any(x => x.StartsWith(assemblyUsing, StringComparison.InvariantCultureIgnoreCase)))
            {
                WriteLine(assemblyUsing);
            }
        }

        private void WriteComplexType()
        {
            WriteMainRecordStruct();
            WriteTypeConverter();
            WriteJsonConverter();
            if (metadata.AdditionalConverters.Any(x => x == 0)) //efcore
            {
                WriteEfcore();
            }
            if (metadata.AdditionalConverters.Any(x => x == 1)) //Dapper
            {
                WriteDapper();
            }
            if (metadata.AdditionalConverters.Any(x => x == 2)) //NewtonSoftJson
            {
                WriteNewtonSoftJson();
            }
        }

        private void WriteMainRecordStruct()
        {
            WriteLine($"[TypeConverter(typeof({metadata.NameTyped}TypeConverter))]");
            WriteLine($"[System.Text.Json.Serialization.JsonConverter(typeof({metadata.NameTyped}JsonConverter))]");
            WriteBrace($"{metadata.Modifiers} record struct {metadata.NameTyped}({metadata.InnerType} Value)", () =>
            {
                WriteStatics();
            });
            WriteLine();
        }

        private void WriteStatics()
        {
            WriteLine($"public {metadata.InnerType} Value {{ get; }} = Validate(Value);");

            WriteLine();
            WriteLine($"public static implicit operator {metadata.NameTyped}({metadata.InnerType} value) => new(value);");
            WriteLine($"public static explicit operator {metadata.InnerType}({metadata.NameTyped} value) => value.Value;");
            WriteLine($"public static {metadata.NameTyped} Parse({metadata.InnerType} value) => new(value);");
            WriteBrace($"public static bool TryParse({metadata.InnerType} value, out {metadata.NameTyped} result)", () =>
            {
                WriteLine($"result = default;");
                WriteBrace("try", () =>
                {
                    WriteLine("result = Parse(value);");
                    WriteLine("return true;");
                });
                WriteBrace("catch", () =>
                {
                    WriteLine("return false;");
                });
            });
            if (metadata.InnerType == "string")
            {
                WriteLine($"public override string ToString() => Value;");
            }

            if (metadata.InnerType == "Guid" || metadata.InnerType == "Ulid")
            {
                WriteLine($"public static {metadata.NameTyped} Empty => new({metadata.InnerType}.Empty);");
                WriteLine();
                WriteLine($"public static {metadata.NameTyped} Create() => new({metadata.InnerType}.New{metadata.InnerType}());");
                WriteLine();
                WriteLine($"public bool IsEmpty => Value == {metadata.InnerType}.Empty;");
            }
            else
            {
                WriteLine($"public static {metadata.NameTyped} Create({metadata.InnerType} value) => new(value);");
            }

            if (metadata.BaseInnerType != null)
            {
                WriteLine();
                WriteLine($"public static implicit operator {metadata.NameTyped}({metadata.BaseInnerType} value) => new(Converter.Convert(value));");
                WriteLine($"public static explicit operator {metadata.BaseInnerType}({metadata.NameTyped} value) => Converter.Convert(value.Value);");
                WriteLine($"public static {metadata.NameTyped} Parse({metadata.BaseInnerType} value) => new(Converter.Convert(value));");
                WriteBrace($"public static bool TryParse({metadata.BaseInnerType} value, out {metadata.NameTyped} result)", () =>
                {
                    WriteLine($"result = default;");
                    WriteBrace("try", () =>
                    {
                        WriteLine("result = Converter.Convert(value);");
                        WriteLine("return true;");
                    });
                    WriteBrace("catch", () =>
                    {
                        WriteLine("return false;");
                    });
                });
                WriteLine($"public static {metadata.NameTyped} Create({metadata.BaseInnerType} value) => new(Converter.Convert(value));");

                if (!metadata.ConverterExist && metadata.IsInnerTypePrimitiveOrId() && metadata.IsBaseInnerTypePrimitiveOrId())
                {
                    WriteLine();
                    WriteLine($"public static AutoConverter<{metadata.InnerType}, {metadata.BaseInnerType}> Converter => new(x => x.ToString(), x=> {metadata.InnerType}.Parse(x));");
                }
                if (metadata.BaseInnerType == "string")
                {
                    WriteLine($"public override string ToString() => Converter.Convert(Value);");
                }
            }

            if (!metadata.ValidateExist)
            {
                WriteLine($"public static {metadata.InnerType} Validate({metadata.InnerType} value, [CallerArgumentExpression(nameof(value))] string? argumentName = null) => value;");
            }
        }

        private void WriteTypeConverter()
        {
            WriteBrace($"public class {metadata.NameTyped}TypeConverter : TypeConverter", () =>
            {
                WriteLine($"private static readonly Type InnerType = typeof({metadata.InnerType});");
                if (metadata.BaseInnerType != null)
                {
                    WriteLine($"private static readonly Type BaseInnerType = typeof({metadata.BaseInnerType});");
                }
                WriteCanConvertFrom();
                WriteCanConvertTo();
            });
            WriteLine();
        }

        private void WriteCanConvertFrom()
        {
            WriteLine();
            WriteLine($"public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType) => ");
            List<string> source = [];
            source.Add("sourceType == InnerType");
            
            if (metadata.BaseInnerType != null)
            {
                source.Add("sourceType == BaseInnerType");
            }
            source.Add("base.CanConvertFrom(context, sourceType);");
            WriteLine("    " + string.Join(" || ", source));
            
            WriteLine();
            WriteLine($"public override object? ConvertFrom(ITypeDescriptorContext? context,");
            WriteNested(() =>
            {
                WriteLine($"CultureInfo? culture, object value) => value switch");
                WriteNested("{", "};", () =>
                {
                    WriteLine($"{metadata.InnerType} i => {metadata.NameTyped}.Parse(i),");
                    if (metadata.BaseInnerType != null)
                    {
                        WriteLine($"{metadata.BaseInnerType} bi => {metadata.NameTyped}.Parse(bi),");
                    }
                    WriteLine($"_ => base.ConvertFrom(context, culture, value),");
                });
            });
        }

        private void WriteCanConvertTo()
        {
            WriteLine();
            WriteLine($"public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType) =>");
            List<string> source = [];
            source.Add("destinationType == InnerType");

            if (metadata.BaseInnerType != null)
            {
                source.Add("destinationType == BaseInnerType");
            }
            source.Add("base.CanConvertTo(context, destinationType);");
            WriteLine("    " + string.Join(" || ", source));
            WriteLine();
            WriteBrace($"public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)", () =>
            {
                WriteBrace($"if (value is {metadata.NameTyped} result)", () =>
                {
                    WriteBrace($"if (destinationType == InnerType)", () =>
                    {
                        WriteLine($"return result.Value;");
                    });
                    if (metadata.BaseInnerType != null)
                    {
                        WriteBrace($"if (destinationType == BaseInnerType)", () =>
                        {
                            WriteLine($"return {metadata.NameTyped}.Converter.Convert(result.Value);");
                        });
                    }
                });
                WriteLine($"return base.ConvertTo(context, culture, value, destinationType);");
            });
        }

        private void WriteJsonConverter()
        {
            WriteBrace($"public class {metadata.NameTyped}JsonConverter : JsonConverter<{metadata.NameTyped}>", () =>
            {
                WriteJsonRead();
                WriteJsonWrite();
            });
            WriteLine();
        }


        private void WriteJsonRead()
        {
            WriteLine($"public override {metadata.NameTyped} Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => JsonSerializer.Deserialize<{metadata.InnerType}>(ref reader, options)!;");
        }

        private void WriteJsonWrite()
        {
            WriteLine($"public override void Write(Utf8JsonWriter writer, {metadata.NameTyped} value, JsonSerializerOptions options) => JsonSerializer.Serialize(writer, value.Value, options);");
        }


        private void WriteEfcore()
        {
            WriteLine("//Efcore Converter");
            if (metadata.InnerType == "Ulid" || metadata.InnerType == "Guid")
            {
                WriteBrace($"public partial class {metadata.NameTyped}StringValueConverter : ValueConverter<{metadata.NameTyped}, string>", () =>
                {
                    WriteLine($"public {metadata.NameTyped}StringValueConverter() : this(null) {{ }}");
                    WriteLine();
                    WriteNested($"public {metadata.NameTyped}StringValueConverter(ConverterMappingHints? mappingHints = null)", "{ }", () =>
                    {
                        WriteNested($": base(", ")", () =>
                        {
                            WriteLine($"v => {metadata.NameTyped}.Converter.Convert(v.Value),");
                            WriteLine($"v => {metadata.NameTyped}.Parse(v),");
                            WriteLine($"mappingHints");
                        });
                    });
                });
                WriteLine();
            }

            if (metadata.InnerType == "Ulid")
            {
                WriteBrace($"public partial class {metadata.NameTyped}ByteArrayValueConverter : ValueConverter<{metadata.NameTyped}, byte[]>", () =>
                {
                    WriteLine($"private static readonly ConverterMappingHints defaultHints = new ConverterMappingHints(size: 16);");
                    WriteLine();
                    WriteLine($"public {metadata.NameTyped}ByteArrayValueConverter() : this(null) {{ }}");
                    WriteLine();
                    WriteNested($"public {metadata.NameTyped}ByteArrayValueConverter(ConverterMappingHints? mappingHints = null)", "{ }", () =>
                    {
                        WriteNested($": base(", ")", () =>
                        {
                            WriteLine($"v => v.Value.ToByteArray(),");
                            WriteLine($"v => new {metadata.NameTyped}(new Ulid(v)),");
                            WriteLine($"defaultHints.With(mappingHints)");
                        });
                    });
                });
            }
            else
            {
                var innerType = metadata.GetBaseInnerType();
                
                WriteBrace($"public partial class {metadata.NameTyped}{innerType}ValueConverter : ValueConverter<{metadata.NameTyped}, {innerType}>", () =>
                {
                    WriteLine($"public {metadata.NameTyped}{innerType}ValueConverter() : this(null) {{ }}");
                    WriteLine();
                    WriteNested($"public {metadata.NameTyped}{innerType}ValueConverter(ConverterMappingHints? mappingHints = null)", "{ }", () =>
                    {
                        WriteNested($": base(", ")", () =>
                        {
                            if (metadata.InnerType == "string")
                            {
                                WriteLine($"v => v.Value,");
                            }
                            else
                            {
                                WriteLine($"v => {metadata.NameTyped}.Converter.Convert(v.Value),");
                            }
                            WriteLine($"v => {metadata.NameTyped}.Parse(v),");
                            WriteLine($"mappingHints");
                        });
                    });
                });
            }
            WriteLine();
        }

        private void WriteDapper()
        {
            var innerType = metadata.GetBaseInnerType();
            WriteLine("//Dapper Converter");
            WriteBrace($"public partial class {metadata.NameTyped}DapperTypeHandler : Dapper.SqlMapper.TypeHandler<{metadata.NameTyped}>", () =>
            {
                WriteBrace($"public override void SetValue(System.Data.IDbDataParameter parameter, {metadata.NameTyped} value)", () =>
                {
                    if (metadata.InnerType == "string")
                    {
                        WriteLine($"parameter.Value = value.Value;");
                    }
                    else
                    {
                        WriteLine($"parameter.Value = {metadata.NameTyped}.Converter.Convert(value.Value);");
                    }
                });
                WriteLine();
                WriteLine($"public override {metadata.NameTyped} Parse(object value) => value switch");
                WriteNested("{", "};", () =>
                {
                    if (innerType != "Ulid")
                    {
                        if (metadata.InnerType == "string")
                        {
                            WriteLine($"{innerType} v => {metadata.NameTyped}.Parse(v),");
                        }
                        else
                        {
                            WriteLine($"{innerType} v => {metadata.NameTyped}.Converter.Convert(v),");
                        }
                    }
                    WriteLine($"_ => throw new InvalidCastException($\"Unable to cast object of type {{value.GetType()}} to {metadata.NameTyped}\"),");
                });
            });
        }

        private void WriteNewtonSoftJson()
        {
            var innerType = metadata.GetBaseInnerType();
            WriteLine("//NewtonsoftJson Converter");
            WriteBrace($"public class {metadata.NameTyped}NewtonsoftJsonConverter : Newtonsoft.Json.JsonConverter", () =>
            {
                WriteLine($"public override bool CanConvert(Type type) => type == typeof({metadata.NameTyped});");
                WriteLine();
                WriteLine($"public override void WriteJson(Newtonsoft.Json.JsonWriter writer, object? value, Newtonsoft.Json.JsonSerializer serializer) =>");
                WriteLine($"    serializer.Serialize(writer, value is {metadata.NameTyped} id ? id.Value : null);");
                WriteLine();

                WriteBrace($"public override object? ReadJson(Newtonsoft.Json.JsonReader reader, Type objectType, object? existingValue, Newtonsoft.Json.JsonSerializer serializer)", () =>
                {
                    if (innerType == "Ulid" || innerType == "string")
                    {
                        WriteLine($"var value = serializer.Deserialize<string?>(reader);");
                    }
                    else
                    {
                        WriteLine($"var value = serializer.Deserialize<{innerType}?>(reader);");
                    }

                    if (innerType == "string" || innerType == "Ulid")
                    {
                        WriteLine($"return value == null ? null : (object){metadata.NameTyped}.Parse(value);");
                    }
                    else
                    {
                        WriteLine($"return value.HasValue ? (object){metadata.NameTyped}.Parse(value.Value) : null;");
                    }
                });
            });
            WriteLine();
        }
    }
}