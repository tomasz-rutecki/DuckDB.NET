﻿using System;
using System.Collections;
using System.Collections.Generic;
using DuckDB.NET.Data.Extensions;

namespace DuckDB.NET.Data.Internal.Reader;

internal class ListVectorDataReader : VectorDataReaderBase
{
    private readonly VectorDataReaderBase listDataReader;

    internal unsafe ListVectorDataReader(IntPtr vector, void* dataPointer, ulong* validityMaskPointer, DuckDBType columnType, string columnName) : base(dataPointer, validityMaskPointer, columnType, columnName)
    {
        using var logicalType = NativeMethods.DataChunks.DuckDBVectorGetColumnType(vector);
        using var childType = NativeMethods.LogicalType.DuckDBListTypeChildType(logicalType);

        var type = NativeMethods.LogicalType.DuckDBGetTypeId(childType);

        var childVector = NativeMethods.DataChunks.DuckDBListVectorGetChild(vector);

        var childVectorData = NativeMethods.DataChunks.DuckDBVectorGetData(childVector);
        var childVectorValidity = NativeMethods.DataChunks.DuckDBVectorGetValidity(childVector);

        listDataReader = VectorDataReaderFactory.CreateReader(childVector, childVectorData, childVectorValidity, type, columnName);
    }

    protected override Type GetColumnType()
    {
        return typeof(List<>).MakeGenericType(listDataReader.ClrType);
    }

    protected override Type GetColumnProviderSpecificType()
    {
        return typeof(List<>).MakeGenericType(listDataReader.ProviderSpecificClrType);
    }

    internal override object GetValue(ulong offset, Type targetType)
    {
        if (DuckDBType == DuckDBType.List)
        {
            return GetList(offset, targetType);
        }

        return base.GetValue(offset, targetType);
    }

    private unsafe object GetList(ulong offset, Type returnType)
    {
        var listData = (DuckDBListEntry*)DataPointer + offset;

        var listType = returnType.GetGenericArguments()[0];

        var allowNulls = listType.AllowsNullValue(out var _, out var nullableType);

        var list = Activator.CreateInstance(returnType) as IList
                   ?? throw new ArgumentException($"The type '{returnType.Name}' specified in parameter {nameof(returnType)} cannot be instantiated as an IList.");

        //Special case for specific types to avoid boxing
        switch (list)
        {
            case List<int> theList:
                return BuildList<int>(theList);
            case List<int?> theList:
                return BuildList<int?>(theList);
            case List<float> theList:
                return BuildList<float>(theList);
            case List<float?> theList:
                return BuildList<float?>(theList);
            case List<double> theList:
                return BuildList<double>(theList);
            case List<double?> theList:
                return BuildList<double?>(theList);
            case List<decimal> theList:
                return BuildList<decimal>(theList);
            case List<decimal?> theList:
                return BuildList<decimal?>(theList);
        }

        var targetType = nullableType ?? listType;

        for (ulong i = 0; i < listData->Length; i++)
        {
            var childOffset = i + listData->Offset;
            if (listDataReader.IsValid(childOffset))
            {
                var item = listDataReader.GetValue(childOffset, targetType);
                list.Add(item);
            }
            else
            {
                if (allowNulls)
                {
                    list.Add(null);
                }
                else
                {
                    throw new NullReferenceException("The list contains null value");
                }
            }
        }

        return list;

        List<T> BuildList<T>(List<T> list)
        {
            for (ulong i = 0; i < listData->Length; i++)
            {
                var childOffset = i + listData->Offset;
                if (listDataReader.IsValid(childOffset))
                {
                    var item = listDataReader.GetValue<T>(childOffset);
                    list.Add(item);
                }
                else
                {
                    if (allowNulls)
                    {
                        list.Add(default!);
                    }
                    else
                    {
                        throw new NullReferenceException("The list contains null value");
                    }
                }
            }
            return list;
        }
    }

    public override void Dispose()
    {
        listDataReader.Dispose();
        base.Dispose();
    }
}