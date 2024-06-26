﻿using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Reflection;

namespace GiggleBook.Utilities;

public static class DataRowUtility
{
    public static T ToObject<T>(this DataRow dataRow)
    where T : new()
    {
        T item = new T();

        foreach (DataColumn column in dataRow.Table.Columns)
        {
            PropertyInfo property = GetProperty(typeof(T), column.ColumnName);

            if (property != null && dataRow[column] != DBNull.Value && dataRow[column].ToString() != "NULL")
            {
                property.SetValue(item, ChangeType(dataRow[column], property.PropertyType), null);
            }
        }

        return item;
    }

    private static PropertyInfo GetProperty(Type type, string attributeName)
    {
        attributeName = attributeName.Replace("_", "");

        PropertyInfo? property = type.GetProperty(attributeName, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public);

        if (property != null)
        {
            return property;
        }

        return type.GetProperties()
             .Where(p => p.IsDefined(typeof(DisplayAttribute), false) && p.GetCustomAttributes(typeof(DisplayAttribute), false).Cast<DisplayAttribute>().Single().Name == attributeName)
             .First();
    }

    public static object? ChangeType(object value, Type type)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition().Equals(typeof(Nullable<>)))
        {
            if (value == null)
            {
                return null;
            }

            return Convert.ChangeType(value, Nullable.GetUnderlyingType(type)!);
        }

        return Convert.ChangeType(value, type);
    }

}
