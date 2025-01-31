﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using DuckDB.NET.Data;
using FluentAssertions;
using Xunit;

namespace DuckDB.NET.Test;

public class DuckDBDataReaderTestAllTypes : DuckDBTestBase
{
    private readonly DuckDBDataReader reader;

    public DuckDBDataReaderTestAllTypes(DuckDBDatabaseFixture db) : base(db)
    {
        Command.CommandText = "Select * from test_all_types(use_large_enum = true)";
        reader = Command.ExecuteReader();
        reader.Read();
    }

    private void VerifyDataStruct<T>(string columnName, int columnIndex, IReadOnlyList<T> data, Type providerSpecificType = null, bool readProviderSpecificValue = false) where T : struct
    {
        reader.GetOrdinal(columnName).Should().Be(columnIndex);
        reader.GetProviderSpecificFieldType(columnIndex).Should().Be(providerSpecificType ?? typeof(T));

        (readProviderSpecificValue ? reader.GetProviderSpecificValue(columnIndex) : reader.GetValue(columnIndex)).Should().Be(data[0]);
        reader.GetFieldValue<T>(columnIndex).Should().Be(data[0]);

        reader.Read();

        (readProviderSpecificValue ? reader.GetProviderSpecificValue(columnIndex) : reader.GetValue(columnIndex)).Should().Be(data[1]);
        reader.GetFieldValue<T>(columnIndex).Should().Be(data[1]);

        reader.Read();

        reader.IsDBNull(columnIndex).Should().Be(true);

        reader.GetValue(columnIndex).Should().Be(DBNull.Value);
        reader.GetFieldValue<T?>(columnIndex).Should().Be(null);
        reader.GetProviderSpecificValue(columnIndex).Should().Be(DBNull.Value);

        reader.Invoking(r => r.GetFieldValue<T>(columnIndex)).Should().Throw<InvalidCastException>();
    }

    private void VerifyDataClass<T>(string columnName, int columnIndex, IReadOnlyList<T> data) where T : class
    {
        reader.GetOrdinal(columnName).Should().Be(columnIndex);
        reader.GetProviderSpecificFieldType(columnIndex).Should().Be(typeof(T));

        reader.GetValue(columnIndex).Should().Be(data[0]);
        reader.GetFieldValue<T>(columnIndex).Should().Be(data[0]);

        reader.Read();

        reader.GetValue(columnIndex).Should().Be(data[1]);
        reader.GetFieldValue<T>(columnIndex).Should().Be(data[1]);

        reader.Read();

        reader.IsDBNull(columnIndex).Should().Be(true);
        reader.Invoking(r => r.GetFieldValue<T>(columnIndex)).Should().Throw<InvalidCastException>();
    }

    private void VerifyDataList<T>(string columnName, int columnIndex, IReadOnlyList<List<T?>> data) where T : struct
    {
        reader.GetOrdinal(columnName).Should().Be(columnIndex);
        reader.GetProviderSpecificFieldType(columnIndex).Should().Be(typeof(List<T>));

        reader.GetFieldValue<List<T?>>(columnIndex).Should().BeEquivalentTo(data[0]);

        reader.Read();

        reader.GetFieldValue<List<T?>>(columnIndex).Should().BeEquivalentTo(data[1]);

        reader.Read();

        reader.IsDBNull(columnIndex).Should().Be(true);
        reader.Invoking(r => r.GetFieldValue<List<T>>(columnIndex)).Should().Throw<InvalidCastException>();
    }

    private void VerifyDataListClass<T>(string columnName, int columnIndex, IReadOnlyList<List<T>> data) where T : class
    {
        reader.GetOrdinal(columnName).Should().Be(columnIndex);

        var fieldValue = reader.GetFieldValue<List<T>>(columnIndex);
        fieldValue.Should().BeEquivalentTo(data[0]);

        reader.Read();

        reader.GetFieldValue<List<T>>(columnIndex).Should().BeEquivalentTo(data[1]);

        reader.Read();

        reader.IsDBNull(columnIndex).Should().Be(true);
        reader.Invoking(r => r.GetFieldValue<T>(columnIndex)).Should().Throw<InvalidCastException>();
    }

    [Fact]
    public void ReadBool()
    {
        VerifyDataStruct("bool", 0, new List<bool> { false, true });
    }

    [Fact]
    public void ReadTinyInt()
    {
        VerifyDataStruct("tinyint", 1, new List<sbyte> { sbyte.MinValue, sbyte.MaxValue });
    }

    [Fact]
    public void ReadSmallInt()
    {
        VerifyDataStruct("smallint", 2, new List<short> { short.MinValue, short.MaxValue });
    }

    [Fact]
    public void ReadInt()
    {
        VerifyDataStruct("int", 3, new List<int> { int.MinValue, int.MaxValue });
    }

    [Fact]
    public void ReadBigInt()
    {
        VerifyDataStruct("bigint", 4, new List<long> { long.MinValue, long.MaxValue });
    }

    [Fact]
    public void ReadHugeInt()
    {
        VerifyDataStruct("hugeint", 5, new List<BigInteger>
        {
            BigInteger.Parse("-170141183460469231731687303715884105728"),
            BigInteger.Parse("170141183460469231731687303715884105727")
        }, typeof(DuckDBHugeInt));
    }

    [Fact]
    public void ReadUTinyInt()
    {
        VerifyDataStruct("utinyint", 7, new List<byte> { 0, byte.MaxValue });
    }

    [Fact]
    public void ReadUSmallInt()
    {
        VerifyDataStruct("usmallint", 8, new List<ushort> { 0, ushort.MaxValue });
    }

    [Fact]
    public void ReadUInt()
    {
        VerifyDataStruct("uint", 9, new List<uint> { 0, uint.MaxValue });
    }

    [Fact]
    public void ReadUBigInt()
    {
        VerifyDataStruct("ubigint", 10, new List<ulong> { 0, ulong.MaxValue });
    }

    [Fact]
    public void ReadDate()
    {
        VerifyDataStruct("date", 11, new List<DuckDBDateOnly>
        {
            new(-5877641, 6, 25),
            new(5881580, 7, 10)
        }, typeof(DuckDBDateOnly), true);
    }

    [Fact]
    public void ReadTime()
    {
        VerifyDataStruct("time", 12, new List<DuckDBTimeOnly>
        {
            new(0,0,0),
            new(24, 0, 0,0)
        }, typeof(DuckDBTimeOnly), true);
    }

    [Fact]
    public void ReadTimeStamp()
    {
        VerifyDataStruct("timestamp", 13, new List<DuckDBTimestamp>
        {
            new(new(-290308, 12, 22), new(0,0,0)),
            new(new(294247, 1, 10), new(4,0,54,775806))
        }, typeof(DuckDBTimestamp), true);
    }

    [Fact]
    public void ReadTimeStampS()
    {
        VerifyDataStruct("timestamp_s", 14, new List<DuckDBTimestamp>
        {
            new(new(-290308, 12, 22), new(0,0,0)),
            new(new(294247, 1, 10), new(4,0,54))
        }, readProviderSpecificValue: true);
    }

    [Fact]
    public void ReadTimeStampMS()
    {
        VerifyDataStruct("timestamp_ms", 15, new List<DuckDBTimestamp>
        {
            new(new(-290308, 12, 22), new(0,0,0)),
            new(new(294247, 1, 10), new(4,0,54,775000))
        }, readProviderSpecificValue: true);
    }

    [Fact]
    public void ReadTimeStampNS()
    {
        VerifyDataStruct("timestamp_ns", 16, new List<DateTime>
        {
            new DateTime(1677, 09, 21, 0,12,43).AddTicks(145225 * 10),
            new DateTime (2262, 04, 11, 23,47,16).AddTicks(854775 * 10)
        }, typeof(DuckDBTimestamp));
    }

    [Fact(Skip = "These dates can't be expressed by DateTime or is unsupported by this library")]
    public void ReadTimeTZ()
    {
        VerifyDataStruct("time_tz", 17, new List<DuckDBTimestamp>
        {
            new(new(-290308, 12, 22), new(0,0,0)),
            new(new(294247, 1, 10), new(4,0,54,775806))
        });
    }

    [Fact]
    public void ReadTimeStampTZ()
    {
        VerifyDataStruct("timestamp_tz", 18, new List<DuckDBTimestamp>
        {
            new(new(-290308, 12, 22), new(0,0,0)),
            new(new(294247, 1, 10), new(4,0,54,775806))
        }, typeof(DuckDBTimestamp), true);
    }

    [Fact]
    public void ReadFloat()
    {
        VerifyDataStruct("float", 19, new List<float> { float.MinValue, float.MaxValue });
    }

    [Fact]
    public void ReadDouble()
    {
        VerifyDataStruct("double", 20, new List<double> { double.MinValue, double.MaxValue });
    }

    [Fact]
    public void ReadDecimal1()
    {
        VerifyDataStruct("dec_4_1", 21, new List<decimal> { -999.9m, 999.9m });
    }

    [Fact]
    public void ReadDecimal2()
    {
        VerifyDataStruct("dec_9_4", 22, new List<decimal> { -99999.9999m, 99999.9999m });
    }

    [Fact]
    public void ReadDecimal3()
    {
        VerifyDataStruct("dec_18_6", 23, new List<decimal> { -999999999999.999999m, 999999999999.999999m });
    }

    [Fact]
    public void ReadDecimal4()
    {
        VerifyDataStruct("dec38_10", 24, new List<decimal> { -9999999999999999999999999999.9999999999m, 9999999999999999999999999999.9999999999m });
    }

    [Fact]
    public void ReadGuid()
    {
        VerifyDataStruct("uuid", 25, new List<Guid> { Guid.Parse("00000000-0000-0000-0000-000000000000"), Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff") });
    }

    [Fact]
    public void ReadInterval()
    {
        VerifyDataStruct("interval", 26, new List<DuckDBInterval>
        {
            new(),
            new(999,999,999999999),
        }, typeof(DuckDBInterval), true);
    }

    [Fact]
    public void ReadString()
    {
        VerifyDataClass("varchar", 27, new List<string> { "🦆🦆🦆🦆🦆🦆", "goo\0se" });
    }

    [Fact]
    public void ReadBlob()
    {
        var columnIndex = 28;
        reader.GetOrdinal("blob").Should().Be(columnIndex);
        reader.GetProviderSpecificFieldType(columnIndex).Should().Be(typeof(Stream));

        using (var stream = reader.GetStream(columnIndex))
        {
            var streamReader = new StreamReader(stream);

            streamReader.ReadToEnd().Should().Be("thisisalongblob\x00withnullbytes");
        }

        reader.Read();

        using (var stream = reader.GetStream(columnIndex))
        {
            var streamReader = new StreamReader(stream);

            streamReader.ReadToEnd().Should().Be(new string(new char[] { (char)0, (char)0, (char)0, (char)97 }));
        }

        reader.Read();

        reader.IsDBNull(columnIndex).Should().Be(true);
        reader.Invoking(r => r.GetStream(columnIndex)).Should().Throw<InvalidCastException>();
    }

    [Fact]
    public void ReadBit()
    {
        VerifyDataClass("bit", 29, new List<string> { "0010001001011100010101011010111", "10101" });
    }

    [Fact]
    public void ReadSmallEnum()
    {
        VerifyDataClass("small_enum", 30, new List<string> { "DUCK_DUCK_ENUM", "GOOSE" });
    }

    [Fact]
    public void ReadMediumEnum()
    {
        VerifyDataClass("medium_enum", 31, new List<string> { "enum_0", "enum_299" });
    }

    [Fact]
    public void ReadLargeEnum()
    {
        VerifyDataClass("large_enum", 32, new List<string> { "enum_0", "enum_69999" });
    }

    [Fact]
    public void ReadIntList()
    {
        VerifyDataList("int_array", 33, new List<List<int?>> { new(), new() { 42, 999, null, null, -42 } });
    }

    [Fact]
    public void ReadDoubleList()
    {
        VerifyDataList("double_array", 34, new List<List<double?>> { new(), new() { 42.0, double.NaN, double.PositiveInfinity, double.NegativeInfinity, null, -42.0 } });
    }

    [Fact]
    public void ReadDateList()
    {
        VerifyDataList("date_array", 35, new List<List<DuckDBDateOnly?>> { new(), new()
        {
            new DuckDBDateOnly(1970, 1, 1),
            new DuckDBDateOnly(5881580, 7, 11),
            new DuckDBDateOnly(-5877641, 6, 24),
            null,
            new DuckDBDateOnly(2022,5,12),
        } });
    }

    [Fact(Skip = "These dates can't be expressed by DateTime or is unsupported by this library")]
    public void ReadTimeStampList()
    {
        VerifyDataList("timestamp_array", 35, new List<List<DuckDBTimestamp?>> { new(), new()
        {
            new DuckDBTimestamp(new DuckDBDateOnly(1970, 1, 1), new DuckDBTimeOnly()),
            new DuckDBTimestamp (new DuckDBDateOnly(5881580, 7, 11), new DuckDBTimeOnly()),
            new DuckDBTimestamp (new DuckDBDateOnly(-5877641, 6, 24), new DuckDBTimeOnly()),
            null,
            new DuckDBTimestamp (new DuckDBDateOnly(2022, 5, 12), new DuckDBTimeOnly()),
        } });
    }

    [Fact(Skip = "These dates can't be expressed by DateTime or is unsupported by this library")]
    public void ReadTimeStampTZList()
    {
        VerifyDataList("timestamptz_array", 36, new List<List<DuckDBDateOnly?>> { new(), new()
        {
            new DuckDBDateOnly(1970, 1, 1),
            new DuckDBDateOnly(5881580, 7, 11),
            new DuckDBDateOnly(-5877641, 6, 24),
            null,
            new DuckDBDateOnly(2022,5,12),
        } });
    }

    [Fact]
    public void ReadStringList()
    {
        VerifyDataListClass("varchar_array", 38, new List<List<string>> { new(), new() { "🦆🦆🦆🦆🦆🦆", "goose", null, "" } });
    }

    [Fact]
    public void ReadNestedIntList()
    {
        var data = new List<int?>() { 42, 999, null, null, -42 };
        VerifyDataListClass("nested_int_array", 39, new List<List<List<int?>>> {new (), new()
        {
            new(),
            data,
            null,
            new(),
            data,
        } });
    }

    [Fact]
    public void ReadStruct()
    {
        var columnIndex = 40;
        reader.GetOrdinal("struct").Should().Be(columnIndex);
        reader.GetProviderSpecificFieldType(columnIndex).Should().Be(typeof(Dictionary<string, object>));

        reader.GetValue(columnIndex).Should().BeEquivalentTo(new Dictionary<string, object>() { { "a", null }, { "b", null } });
        reader.GetFieldValue<StructTest>(columnIndex).Should().BeEquivalentTo(new StructTest());

        reader.Read();

        reader.GetValue(columnIndex).Should().BeEquivalentTo(new Dictionary<string, object>() { { "a", 42 }, { "b", "🦆🦆🦆🦆🦆🦆" } });
        reader.GetFieldValue<StructTest>(columnIndex).Should().BeEquivalentTo(new StructTest()
        {
            A = 42,
            B = "🦆🦆🦆🦆🦆🦆"
        });

        reader.Read();

        reader.IsDBNull(columnIndex).Should().Be(true);

        reader.Invoking(r => r.GetFieldValue<StructTest>(columnIndex)).Should().Throw<InvalidCastException>();
    }

    [Fact]
    public void ReadStructOfArray()
    {
        var columnIndex = 41;
        reader.GetOrdinal("struct_of_arrays").Should().Be(columnIndex);

        reader.GetFieldValue<StructOfArrayTest>(columnIndex).Should().BeEquivalentTo(new StructOfArrayTest());

        reader.Read();

        reader.GetFieldValue<StructOfArrayTest>(columnIndex).Should().BeEquivalentTo(new StructOfArrayTest()
        {
            A = new() { 42, 999, null, null, -42 },
            B = new() { "🦆🦆🦆🦆🦆🦆", "goose", null, "" }
        });

        reader.Read();

        reader.IsDBNull(columnIndex).Should().Be(true);
        reader.Invoking(r => r.GetFieldValue<StructOfArrayTest>(columnIndex)).Should().Throw<InvalidCastException>();
    }

    [Fact]
    public void ReadArrayOfStructs()
    {
        var columnIndex = 42;
        reader.GetOrdinal("array_of_structs").Should().Be(columnIndex);

        reader.GetFieldValue<List<StructTest>>(columnIndex).Should().BeEquivalentTo(new List<StructTest>());

        reader.Read();

        reader.GetFieldValue<List<StructTest>>(columnIndex).Should().BeEquivalentTo(new List<StructTest>()
        {
            new(),
            new()
            {
                A = 42,
                B = "🦆🦆🦆🦆🦆🦆"
            },
            null
        });

        reader.Read();

        reader.IsDBNull(columnIndex).Should().Be(true);
        reader.Invoking(r => r.GetFieldValue<List<StructTest>>(columnIndex)).Should().Throw<InvalidCastException>();
    }

    [Fact]
    public void ReadMap()
    {
        var columnIndex = 43;
        reader.GetOrdinal("map").Should().Be(columnIndex);

        reader.GetValue(columnIndex).Should().BeEquivalentTo(new Dictionary<string, string>());
        reader.GetFieldValue<Dictionary<string, string>>(columnIndex).Should().BeEquivalentTo(new Dictionary<string, string>());

        reader.Read();

        reader.GetValue(columnIndex).Should().BeEquivalentTo(new Dictionary<string, string>() { { "key1", "🦆🦆🦆🦆🦆🦆" }, { "key2", "goose" } });
        reader.GetFieldValue<Dictionary<string, string>>(columnIndex).Should().BeEquivalentTo(new Dictionary<string, string>() { { "key1", "🦆🦆🦆🦆🦆🦆" }, { "key2", "goose" } });

        reader.Read();

        reader.IsDBNull(columnIndex).Should().Be(true);
        reader.Invoking(r => r.GetFieldValue<Dictionary<string, string>>(columnIndex)).Should().Throw<InvalidCastException>();
    }

    class StructTest
    {
        public int? A { get; set; }
        public string B { get; set; }
    }

    class StructOfArrayTest
    {
        public List<int?> A { get; set; }
        public List<string> B { get; set; }
    }
}