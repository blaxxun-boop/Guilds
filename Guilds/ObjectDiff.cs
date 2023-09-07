using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace Guilds;

public static class ObjectDiff
{
	public static Dictionary<string[], object?> diff<T>(T old, T cur) where T : notnull
	{
		Dictionary<string[], object?> differences = new();
		diff(old, cur, typeof(T), new List<string>(), differences);
		return differences;
	}

	private static readonly MethodInfo hashSetDiff = AccessTools.DeclaredMethod(typeof(ObjectDiff), nameof(diffHashSets));
	private static void diffHashSets<T>(List<string> path, Dictionary<string[], object?> differences, HashSet<T> a, HashSet<T> b) where T : struct 
	{
		foreach (T value in a.Where(v => !b.Contains(v)))
		{
			path.Add(value.ToString());
			path.Add("0");
			differences.Add(path.ToArray(), value);
			path.RemoveAt(path.Count - 1);
			path.RemoveAt(path.Count - 1);
		}

		foreach (T value in b.Where(v => !a.Contains(v)))
		{
			path.Add(value.ToString());
			path.Add("1");
			differences.Add(path.ToArray(), value);
			path.RemoveAt(path.Count - 1);
			path.RemoveAt(path.Count - 1);
		}
	}

	private static void diff(object old, object cur, Type t, List<string> path, Dictionary<string[], object?> differences)
	{
		foreach (FieldInfo f in t.GetFields(BindingFlags.Instance | BindingFlags.Public))
		{
			object oldVal = f.GetValue(old);
			object curVal = f.GetValue(cur);
			if (f.FieldType == typeof(string) || f.FieldType.IsPrimitive || f.FieldType.IsEnum)
			{
				if (!oldVal.Equals(curVal))
				{
					path.Add(f.Name);
					differences.Add(path.ToArray(), curVal);
					path.RemoveAt(path.Count - 1);
				}
			}
			else if (f.FieldType.IsGenericType && typeof(HashSet<>) == f.FieldType.GetGenericTypeDefinition())
			{
				Type valueType = f.FieldType.GetGenericArguments()[0];
				path.Add(f.Name);
				hashSetDiff.MakeGenericMethod(valueType).Invoke(null, new[] { path, differences, oldVal, curVal });
				path.RemoveAt(path.Count - 1);
			}
			else if (typeof(IDictionary).IsAssignableFrom(f.FieldType))
			{
				Type valueType = f.FieldType.GetGenericArguments()[1];
				path.Add(f.Name);
				foreach (object removedKey in ((IDictionary)oldVal).Keys.Cast<object>().Except(((IDictionary)curVal).Keys.Cast<object>()))
				{
					path.Add(removedKey.ToString());
					differences.Add(path.ToArray(), null);
					path.RemoveAt(path.Count - 1);
				}

				foreach (object key in ((IDictionary)curVal).Keys)
				{
					path.Add(key.ToString());
					if (((IDictionary)oldVal).Contains(key))
					{
						diff(((IDictionary)oldVal)[key], ((IDictionary)curVal)[key], valueType, path, differences);
					}
					else
					{
						differences.Add(path.ToArray(), ((IDictionary)curVal)[key]);
					}

					path.RemoveAt(path.Count - 1);
				}

				path.RemoveAt(path.Count - 1);
			}
			else
			{
				path.Add(f.Name);
				diff(oldVal, curVal, f.FieldType, path, differences);
				path.RemoveAt(path.Count - 1);
			}
		}
	}

	public static void ApplyDiff<T>(ref T baseTarget, Dictionary<string[], object?> differences) where T : notnull
	{
		foreach (KeyValuePair<string[], object?> kv in differences)
		{
			List<FieldInfo> structRef = new();
			Type t = typeof(T);
			object target = baseTarget;
			object? lastContainer = null;
			object lastKey = null!;
			for (int i = 0; i < kv.Key.Length; ++i)
			{
				FieldInfo f = t.GetField(kv.Key[i]);
				if (i == kv.Key.Length - 1)
				{
					// terminal type
					if (structRef.Count == 0)
					{
						f.SetValue(target, kv.Value);
					}
					else
					{
						TypedReference typedRef = TypedReference.MakeTypedReference(target, structRef.ToArray());
						f.SetValueDirect(typedRef, kv.Value);
					}

					break;
				}

				if (f.FieldType.IsGenericType && typeof(HashSet<>) == f.FieldType.GetGenericTypeDefinition())
				{
					Type valueType = f.FieldType.GetGenericArguments()[0];
					string add = kv.Key[i + 2];
					typeof(HashSet<>).MakeGenericType(valueType).GetMethod(add == "1" ? "Add" : "Remove")!.Invoke(f.GetValue(target), new[] { kv.Value });
					break;
				}
					
				if (typeof(IDictionary).IsAssignableFrom(f.FieldType))
				{
					Type keyType = f.FieldType.GetGenericArguments()[0];
					object key = kv.Key[++i];
					if (keyType != typeof(string))
					{
						key = TypeDescriptor.GetConverter(keyType).ConvertFromString((string)key) ?? "";
					}

					IDictionary dict;
					if (structRef.Count == 0)
					{
						dict = (IDictionary)f.GetValue(target);
					}
					else
					{
						TypedReference typedRef = TypedReference.MakeTypedReference(target, structRef.ToArray());
						dict = (IDictionary)f.GetValueDirect(typedRef);
					}

					if (kv.Key.Length == i + 1)
					{
						if (kv.Value == null)
						{
							dict.Remove(key);
						}
						else
						{
							dict[key] = kv.Value;
						}
					}
					else if (!dict.Contains(key))
					{
						break;
					}
					else
					{
						target = dict[key];
						lastKey = key;
						lastContainer = dict;
						t = f.FieldType.GetGenericArguments()[1];
						structRef.Clear();
					}
				}
				else if (f.FieldType.IsValueType)
				{
					structRef.Add(f);
					t = f.FieldType;
				}
				else
				{
					if (structRef.Count == 0)
					{
						target = f.GetValue(target);
					}
					else
					{
						TypedReference typedRef = TypedReference.MakeTypedReference(target, structRef.ToArray());
						target = f.GetValueDirect(typedRef);
					}
					t = f.FieldType;
					structRef.Clear();
					lastContainer = target;
				}
			}

			if (lastContainer == null)
			{
				baseTarget = (T)target;
			}
			else if (lastContainer is IDictionary lastDict)
			{
				lastDict[lastKey] = target;
			}
		}
	}
}