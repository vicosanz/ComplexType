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
                WriteToString();
            });
            WriteLine();
        }

        private void WriteTypeConverter()
        {
            WriteBrace($"public class {metadata.NameTyped}TypeConverter : TypeConverter", () =>
            {
                WriteLine($"private static readonly Type StringType = typeof(string);");
                if (metadata.InnerType != "string")
                {
                    WriteLine($"private static readonly Type InnerType = typeof({metadata.InnerType});");
                }
                WriteLine();
                WriteCanConvertFrom();
                WriteLine();
                WriteCanConvertTo();
            });
            WriteLine();
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

        private void WriteCanConvertFrom()
        {
            WriteLine($"public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType) => ");
            if (metadata.InnerType == "string")
            {
                WriteLine($"    sourceType == StringType || base.CanConvertFrom(context, sourceType);");
            }
            else
            {
                WriteLine($"    sourceType == StringType || sourceType == InnerType || base.CanConvertFrom(context, sourceType);");
            }
            WriteLine();
            WriteLine($"public override object? ConvertFrom(ITypeDescriptorContext? context,");
            WriteNested(() =>
            {
                WriteLine($"CultureInfo? culture, object value) => value switch");
                WriteNested("{", "};", () =>
                {
                    WriteLine($"{metadata.InnerType} g => new {metadata.NameTyped}(g),");
                    if (metadata.InnerType != "string")
                    {
                        WriteLine($"string stringValue => {metadata.NameTyped}.Parse(stringValue),");
                    }
                    WriteLine($"_ => base.ConvertFrom(context, culture, value),");
                });
            });
        }

        private void WriteCanConvertTo()
        {
            WriteLine($"public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType) =>");
            if (metadata.InnerType == "string")
            {
                WriteLine($"    destinationType == StringType || base.CanConvertTo(context, destinationType);");
            }
            else
            {
                WriteLine($"    destinationType == StringType || destinationType == InnerType || base.CanConvertTo(context, destinationType);");
            }
            WriteLine();
            WriteBrace($"public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)", () =>
            {
                WriteBrace($"if (value is {metadata.NameTyped} result)", () =>
                {
                    WriteBrace($"if (destinationType == StringType)", () =>
                    {
                        WriteLine($"return result.ToString();");
                    });
                    if (metadata.InnerType != "string")
                    {
                        WriteBrace($"if (destinationType == InnerType)", () =>
                        {
                            WriteLine($"return result.Value;");
                        });
                    }
                });
                WriteLine($"return base.ConvertTo(context, culture, value, destinationType);");
            });
        }

        private void WriteJsonRead()
        {
            WriteBrace($"public override {metadata.NameTyped} Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)", () =>
            {
                WriteBrace($"try", () =>
                {
                    if (metadata.InnerType == "string" || metadata.InnerType == "Guid" || metadata.InnerType == "Ulid")
                    {
                        WriteLine($"if (reader.TokenType != JsonTokenType.String) throw new JsonException(\"Expected string\");");
                    }
                    if (metadata.InnerType == "Ulid")
                    {
                        WriteBrace($"if (reader.HasValueSequence)", () =>
                        {
                            WriteLine($"var seq = reader.ValueSequence;");
                            WriteLine($"if (seq.Length != 26) throw new JsonException(\"{metadata.NameTyped} invalid: length must be 26\");");
                            WriteLine($"Span<byte> buf = stackalloc byte[26];");
                            WriteLine($"seq.CopyTo(buf);");
                            WriteLine($"Ulid.TryParse(buf, out var uid);");
                            WriteLine($"return new {metadata.NameTyped}(uid);");
                        });
                        WriteBrace($"else", () =>
                        {
                            WriteLine($"var buf = reader.ValueSpan;");
                            WriteLine($"if (buf.Length != 26) throw new JsonException(\"{metadata.NameTyped} invalid: length must be 26\");");
                            WriteLine($"Ulid.TryParse(buf, out var uid);");
                            WriteLine($"return new {metadata.NameTyped}(uid);");
                        });
                    }
                    else
                    {
                        if (metadata.InnerType == "string")
                        {
                            WriteLine($"return new {metadata.NameTyped}(reader.GetString()!);");
                        }
                        else
                        {
                            WriteLine($"{metadata.InnerType} result = default;");
                            if (metadata.IsPrimitive())
                            {
                                WriteLine($"ReadOnlySpan<byte> span = reader.HasValueSequence ? reader.ValueSequence.ToArray() : reader.ValueSpan;");
                                WriteLine($"if (Utf8Parser.TryParse(span, out result, out int bytesConsumed) && span.Length == bytesConsumed) return result;");
                            }
                            WriteLine($"if ({metadata.InnerType}.TryParse(reader.GetString()!, out result)) return result;");
                            WriteLine($"return default;");
                        }
                    }
                });
                if (metadata.InnerType == "Ulid")
                {
                    WriteBrace($"catch (IndexOutOfRangeException e)", () =>
                    {
                        WriteLine($"throw new JsonException(\"{metadata.NameTyped} invalid: length must be 26\", e);");
                    });
                }
                WriteBrace($"catch (OverflowException e)", () =>
                {
                    WriteLine($"throw new JsonException(\"{metadata.NameTyped} invalid: invalid character\", e);");
                });
            });
        }

        private void WriteJsonWrite()
        {
            WriteBrace($"public override void Write(Utf8JsonWriter writer, {metadata.NameTyped} value, JsonSerializerOptions options)", () =>
            {
                WriteLine($"JsonSerializer.Serialize(writer, value.Value, value.Value.GetType(), options);");
            });
        }

        private void WriteStatics()
        {
            WriteLine($"public static implicit operator {metadata.NameTyped}({metadata.InnerType} value) => new(value);");
            WriteLine();
            WriteLine($"public static explicit operator {metadata.InnerType}({metadata.NameTyped} value) => value.Value;");
            WriteLine();
            if (metadata.InnerType == "Guid" || metadata.InnerType == "Ulid")
            {
                WriteLine($"public static {metadata.NameTyped} Empty => new({metadata.InnerType}.Empty);");
                WriteLine();
                WriteLine($"public static {metadata.NameTyped} New{metadata.NameTyped}() => new({metadata.InnerType}.New{metadata.InnerType}());");
                WriteLine();
                WriteLine($"public bool IsEmpty => Value == {metadata.InnerType}.Empty;");
            }
            else
            {
                WriteLine($"public static {metadata.NameTyped} New{metadata.NameTyped}({metadata.InnerType} value) => new(value);");
            }
            WriteLine();
        }

        private void WriteToString()
        {
            WriteLine($"public override string ToString() => Value.ToString();");
            WriteLine();
            if (metadata.InnerType != "string")
            {
                WriteLine($"public static {metadata.NameTyped} Parse(string text) => new {metadata.NameTyped}({metadata.InnerType}.Parse(text));");
                WriteLine();
                WriteBrace($"public static bool TryParse(string text, out {metadata.NameTyped} value)", () =>
                {
                    WriteLine($"value = default;");
                    WriteBrace("try", () =>
                    {
                        WriteLine("value = Parse(text);");
                        WriteLine("return true;");
                    });
                    WriteBrace("catch", () =>
                    {
                        WriteLine("return false;");
                    });
                });
            }
        }

        private void WriteNewtonSoftJson()
        {
            WriteLine("//NewtonsoftJson Converter");
            WriteBrace($"public class {metadata.NameTyped}NewtonsoftJsonConverter : Newtonsoft.Json.JsonConverter", () =>
            {
                WriteLine($"public override bool CanConvert(System.Type type) => type == typeof({metadata.InnerType});");
                WriteLine();
                WriteLine($"public override void WriteJson(Newtonsoft.Json.JsonWriter writer, object? value, Newtonsoft.Json.JsonSerializer serializer) =>");
                WriteLine($"    serializer.Serialize(writer, value is {metadata.NameTyped} id ? id.Value.ToString() : null);");
                WriteLine();

                WriteBrace($"public override object? ReadJson(Newtonsoft.Json.JsonReader reader, System.Type objectType, object? existingValue, Newtonsoft.Json.JsonSerializer serializer)", () =>
                {
                    if (metadata.InnerType == "Ulid" || metadata.InnerType == "string")
                    {
                        WriteLine($"var value = serializer.Deserialize<string?>(reader);");
                    }
                    else
                    {
                        WriteLine($"var value = serializer.Deserialize<{metadata.InnerType}?>(reader);");
                    }
                    if (metadata.InnerType == "string")
                    {
                        WriteLine($"return value != null ? new {metadata.NameTyped}(value) : null!;");
                    }
                    else if (metadata.InnerType == "Ulid")
                    {
                        WriteLine($"return value != null ? {metadata.NameTyped}.Parse(value) : null;");
                    }
                    else
                    {
                        WriteLine($"return value.HasValue ? new {metadata.NameTyped}(value.Value) : null;");
                    }
                });
            });
            WriteLine();
        }

        private void WriteDapper()
        {
            WriteLine("//Dapper Converter");
            WriteBrace($"public partial class {metadata.NameTyped}DapperTypeHandler : Dapper.SqlMapper.TypeHandler<{metadata.NameTyped}>", () =>
            {
                WriteBrace($"public override void SetValue(System.Data.IDbDataParameter parameter, {metadata.NameTyped} value)", () =>
                {
                    WriteLine($"parameter.Value = value.Value;");
                });
                WriteLine();
                WriteLine($"public override {metadata.NameTyped} Parse(object value) => value switch");
                WriteNested("{", "};", () =>
                {
                    if (metadata.InnerType != "string")
                    {
                        if (metadata.InnerType != "Ulid")
                        {
                            WriteLine($"{metadata.InnerType} g => new {metadata.NameTyped}(g),");
                        }
                        WriteLine($"string text when !string.IsNullOrEmpty(text) && {metadata.InnerType}.TryParse(text, out var result) => new {metadata.NameTyped}(result),");
                    }
                    else
                    {
                        WriteLine($"string text => new {metadata.NameTyped}(text),");
                    }
                    WriteLine($"_ => throw new InvalidCastException($\"Unable to cast object of type {{value.GetType()}} to {metadata.NameTyped}\"),");
                });
            });
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
                            WriteLine($"id => id.ToString(),");
                            WriteLine($"value => new {metadata.NameTyped}({metadata.InnerType}.Parse(value)),");
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
                            WriteLine($"id => id.Value.ToByteArray(),");
                            WriteLine($"value => new {metadata.NameTyped}(new Ulid(value)),");
                            WriteLine($"defaultHints.With(mappingHints)");
                        });
                    });
                });
            }
            else
            {
                WriteBrace($"public partial class {metadata.NameTyped}{metadata.InnerType}ValueConverter : ValueConverter<{metadata.NameTyped}, {metadata.InnerType}>", () =>
                {
                    WriteLine($"public {metadata.NameTyped}{metadata.InnerType}ValueConverter() : this(null) {{ }}");
                    WriteLine();
                    WriteNested($"public {metadata.NameTyped}{metadata.InnerType}ValueConverter(ConverterMappingHints? mappingHints = null)", "{ }", () =>
                    {
                        WriteNested($": base(", ")", () =>
                        {
                            WriteLine($"id => id.Value,");
                            WriteLine($"value => new {metadata.NameTyped}(value),");
                            WriteLine($"mappingHints");
                        });
                    });
                });
            }
            WriteLine();
        }

    }
}