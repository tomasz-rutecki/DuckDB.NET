﻿using DuckDB.NET.Data.Extensions;
using System;

namespace DuckDB.NET.Data.Internal.Reader;

internal class BooleanVectorDataReader : VectorDataReaderBase
{
    internal unsafe BooleanVectorDataReader(void* dataPointer, ulong* validityMaskPointer, DuckDBType columnType, string columnName) : base(dataPointer, validityMaskPointer, columnType, columnName)
    {
    }

    protected override T GetValidValue<T>(ulong offset, Type targetType)
    {
        if (DuckDBType != DuckDBType.Boolean)
        {
            return base.GetValidValue<T>(offset, targetType);
        }
        
        var value = GetFieldData<bool>(offset);
        return (T)(object)value; //JIT will optimize the casts at least for not nullable T
    }

    internal override object GetValue(ulong offset, Type targetType)
    {
        if (DuckDBType != DuckDBType.Boolean)
        {
            return base.GetValue(offset, targetType);
        }

        return GetFieldData<bool>(offset);
    }
}