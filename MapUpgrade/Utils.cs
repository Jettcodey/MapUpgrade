// Originally developed by Ardot66
// Modified and maintained by Jettcodey
// Licensed under the MIT License

using UnityEngine;
using System;
using System.Reflection;
using HarmonyLib;
using System.Collections.Generic;

namespace Ardot.Jettcodey.REPO.MapUpgrade;

public static class Utils
{
	private static readonly Dictionary<Type, Dictionary<string, FieldInfo>> _fieldCache = new Dictionary<Type, Dictionary<string, FieldInfo>>();

	public static object Get<O>(this O obj, string field)
	{
		return GetField<O>(field).GetValue(obj);
	}

	public static T Get<T, O>(this O obj, string field)
	{
		return (T)Get(obj, field);
	}

	public static void Set<T>(this T obj, string field, object value)
	{
		GetField<T>(field).SetValue(obj, value);
	}

	public static FieldInfo GetField<T>(string field)
	{
		Type type = typeof(T);

		if (!_fieldCache.TryGetValue(type, out var fields))
		{
			fields = new Dictionary<string, FieldInfo>();
			_fieldCache[type] = fields;
		}

		if (!fields.TryGetValue(field, out FieldInfo fieldInfo))
		{
			fieldInfo = AccessTools.Field(type, field);
			fields[field] = fieldInfo;
		}

		return fieldInfo;
	}

	public static bool IsHost()
	{
		return SemiFunc.IsMasterClientOrSingleplayer();
	}

	public static void ForObjectsInTree(Transform root, Predicate<Transform> action)
	{
		var stack = new Stack<Transform>();
		stack.Push(root);

		while (stack.Count > 0)
		{
			Transform transform = stack.Pop();

			if (!action(transform))
				continue;

			for (int x = transform.childCount - 1; x >= 0; x--)
			{
				stack.Push(transform.GetChild(x));
			}
		}
	}
}