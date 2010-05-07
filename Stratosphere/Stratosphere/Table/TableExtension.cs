﻿// Copyright (c) 2010 7Clouds

using System.Collections.Generic;
using System.Linq;

namespace Stratosphere.Table
{
    public static class TableExtension
    {
        public static void Erase(this ITable table)
        {
            List<string> itemNames = new List<string>();

            using (IReader reader = table.Select(new string[] { "itemName()" }, null))
            {
                ReadingState state;

                while ((state = reader.Read()) != ReadingState.End)
                {
                    if (state == ReadingState.EmptyItem ||
                        state == ReadingState.Item)
                    {
                        itemNames.Add(reader.ItemName);
                    }
                }
            }

            itemNames.ForEach(n => table.Delete(n, w => w.DeleteItem()));
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
            using (IReader reader = table.Select(attributeNames, condition))
            {
                ReadingState state;
                T itemData = default(T);
                string itemName = null;

                while ((state = reader.Read()) != ReadingState.End)
                {
                    if (state == ReadingState.EmptyItem ||
                        state == ReadingState.Item)
                    {
                        if (itemName != null)
                        {
                            yield return new KeyValuePair<string, T>(itemName, itemData);

                            itemName = null;
                            itemData = default(T);
                        }

                        if (state == ReadingState.EmptyItem)
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
    }
}
