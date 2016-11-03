﻿//MIT,2016, WinterDev
using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace TypeMirror
{
    /// <summary>
    /// very base reflection helper
    /// </summary>
    static class SimpleReflectionHelper
    {
        public static Dictionary<string, T> GetEnumFields<T>()
        {
            Dictionary<string, T> results = new Dictionary<string, T>();
            Type typeOfSample = typeof(T);
            FieldInfo[] allFields = typeOfSample.GetFields(
                BindingFlags.Static |
                 BindingFlags.NonPublic | BindingFlags.Public);
            int j = allFields.Length;
            for (int i = 0; i < j; ++i)
            {
                FieldInfo f = allFields[i];
                object value = f.GetValue(null);
                if (value is T)
                {
                    results.Add(f.Name, (T)value);
                }
            }
            return results;
        }
        public static List<T> GetEnumValues<T>()
        {
            List<T> values = new List<T>();
            Type typeOfSample = typeof(T);
            FieldInfo[] allFields = typeOfSample.GetFields(
                BindingFlags.Static |
                 BindingFlags.NonPublic | BindingFlags.Public);
            int j = allFields.Length;
            for (int i = 0; i < j; ++i)
            {
                FieldInfo f = allFields[i];
                object value = f.GetValue(null);
                if (value is T)
                {
                    values.Add((T)value);
                }
            }
            return values;
        }
    }
}
