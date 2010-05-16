﻿// Copyright (c) 2010 7Clouds

using System.Collections.Generic;
using System.Linq;

namespace Stratosphere.Table
{
    public static class TableExtension
    {
        public static void Erase(this ITable table)
        {
            string[] itemNames = table.Select().ToArray();

            foreach (string itemName in itemNames)
            {
                table.Delete(itemName, w => w.DeleteItem());
            }
        }

        public static void Set<T>(this ITable table, string itemName, T itemData)
            where T : IEnumerable<KeyValuePair<string, string>>, new()
        {
            if (!string.IsNullOrEmpty(itemName))
            {
                KeyValuePair<string, string>[] putAttributes = itemData.Where(
                    pair => !string.IsNullOrEmpty(pair.Value)).ToArray();

                string[] deleteAttributeKeys = itemData.Where(
                    pair => string.IsNullOrEmpty(pair.Value)).Select(pair => pair.Key).ToArray();

                if (putAttributes.Length != 0)
                {
                    table.Put(itemName, w =>
                    {
                        foreach (KeyValuePair<string, string> attribute in putAttributes)
                        {
                            w.ReplaceAttribute(attribute.Key, attribute.Value);
                        }
                    });
                }

                if (deleteAttributeKeys.Length != 0)
                {
                    table.Delete(itemName, w =>
                    {
                        foreach (string key in deleteAttributeKeys)
                        {
                            w.DeleteAttribute(key);
                        }
                    });
                }
            }
        }

        public static IEnumerable<KeyValuePair<string, T>> Get<T>(this ITable table)
            where T : IDictionary<string, string>, new()
        {
            return Get<T>(table, null);
        }

        public static IEnumerable<KeyValuePair<string, T>> Get<T>(this ITable table, Condition condition)
            where T : IDictionary<string, string>, new()
        {
            return Get<T>(table, new string[] { }, condition);
        }

        public static IEnumerable<KeyValuePair<string, T>> Get<T>(this ITable table, IEnumerable<string> attributeNames, Condition condition)
            where T : IDictionary<string, string>, new()
        {
            return Get<T>(table, attributeNames, condition, false);
        }

        public static IEnumerable<KeyValuePair<string, T>> Get<T>(this ITable table, IEnumerable<string> attributeNames, Condition condition, bool withConsistency)
            where T : IDictionary<string, string>, new()
        {
            using (IReader reader = table.Select(attributeNames, condition, withConsistency))
            {
                T itemData = default(T);
                string itemName = null;

                while (reader.Read())
                {
                    if (reader.Position == ReaderPosition.EmptyItem ||
                        reader.Position == ReaderPosition.Item)
                    {
                        if (itemName != null)
                        {
                            yield return new KeyValuePair<string, T>(itemName, itemData);

                            itemName = null;
                            itemData = default(T);
                        }

                        if (reader.Position == ReaderPosition.EmptyItem)
                        {
                            yield return new KeyValuePair<string, T>(reader.ItemName, new T());

                            continue;
                        }
                        else
                        {
                            itemName = reader.ItemName;
                            itemData = new T();
                        }
                    }

                    itemData[reader.AttributeName] = reader.AttributeValue;
                }

                if (itemName != null)
                {
                    yield return new KeyValuePair<string, T>(itemName, itemData);
                }
            };
        }

        public static IEnumerable<KeyValuePair<string, string>> Select(this ITable table, string attributeName)
        {
            return Select(table, attributeName, null);
        }

        public static IEnumerable<KeyValuePair<string, string>> Select(this ITable table, string attributeName, Condition condition)
        {
            return Select(table, attributeName, condition, false);
        }

        public static IEnumerable<KeyValuePair<string, string>> Select(this ITable table, string attributeName, Condition condition, bool withConsistency)
        {
            using (IReader reader = table.Select(new string[] { attributeName }, condition, withConsistency))
            {
                while (reader.Read())
                {
                    if (reader.Position != ReaderPosition.EmptyItem)
                    {
                        yield return new KeyValuePair<string, string>(reader.ItemName, reader.AttributeValue);
                    }
                }
            }
        }

        public static IReader Select(this ITable table, IEnumerable<string> attributeNames, Condition condition)
        {
            return table.Select(attributeNames, condition, false);
        }

        public static IEnumerable<string> Select(this ITable table)
        {
            return Select(table, (Condition)null);
        }

        public static IEnumerable<string> Select(this ITable table, Condition condition)
        {
            return Select(table, condition, false);
        }

        public static IEnumerable<string> Select(this ITable table, Condition condition, bool withConsistency)
        {
            using (IReader reader = table.Select(new string[] { ItemNameAttribute }, condition, withConsistency))
            {
                while (reader.Read())
                {
                    yield return reader.ItemName;
                }
            }
        }

        public static long SelectCount(this ITable table)
        {
            return SelectCount(table, null);
        }

        public static long SelectCount(this ITable table, Condition condition)
        {
            return SelectCount(table, condition, false);
        }

        public static long SelectCount(this ITable table, Condition condition, bool withConsistency)
        {
            using (IReader reader = table.Select(new string[] { CountAttribute }, condition, withConsistency))
            {
                if (reader.Read())
                {
                    return long.Parse(reader.AttributeValue);
                }
            }

            return 0;
        }

        public const string ItemNameAttribute = "itemName()";
        public const string CountAttribute = "count(*)";
    }
}
