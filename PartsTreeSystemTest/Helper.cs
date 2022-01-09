using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;

namespace PartsTreeSystemTest
{
	public class Helper
	{
		public static PartsTreeSystem.INode GetChild(PartsTreeSystem.INode node, IEnumerable<int> indexes)
		{
			foreach (var index in indexes)
			{
				node = node.GetChildren().ElementAt(index);
			}

			return node;
		}

		class Visitor
		{
			Dictionary<object, System.Tuple<object, object>> instancePairs = new Dictionary<object, System.Tuple<object, object>>();

			public bool IsPairRegistered(object o1, object o2)
			{
				if (instancePairs.TryGetValue(o1, out var value))
				{
					if (value.Item1 == o1 && value.Item2 == o2)
					{
						return true;
					}
				}

				return false;
			}

			public bool IsVisited(object o)
			{
				return instancePairs.ContainsKey(o);
			}

			public void RegisterPairs(object o1, object o2)
			{
				instancePairs.Add(o1, System.Tuple.Create(o1, o2));
			}
		}

		static bool IsValueEqual(object v1, object v2, Visitor visitor)
		{
			if (v1 == null && v2 == null)
			{
				return true;
			}
			else if (v1 == null || v2 == null)
			{
				return false;
			}

			if (v1.GetType() != v2.GetType())
			{
				return false;
			}

			var type = v1.GetType();

			if (type.IsValueType)
			{
				return v1.Equals(v2);
			}
			else
			{
				if (visitor.IsPairRegistered(v1, v2))
				{
					return true;
				}

				if (visitor.IsVisited(v1))
				{
					return false;
				}

				visitor.RegisterPairs(v1, v2);
			}

			if (type.IsSubclassOf(typeof(IList)))
			{
				var vl1 = v1 as IList;
				var vl2 = v2 as IList;

				if (vl1.Count != vl2.Count)
				{
					return false;
				}

				for (int i = 0; i < vl1.Count; i++)
				{
					if (!IsValueEqual(vl1[i], vl2[i], visitor))
					{
						return false;
					}
				}
			}
			else
			{
				foreach (var field in type.GetFields())
				{
					var fv1 = field.GetValue(v1);
					var fv2 = field.GetValue(v2);

					if (!IsValueEqual(fv1, fv2, visitor))
					{
						return false;
					}
				}

				foreach (var prop in type.GetProperties())
				{
					var pms = prop.GetGetMethod().GetParameters();
					if (pms.Length > 0)
					{
						// TODO support
						continue;
					}

					var fv1 = prop.GetValue(v1);
					var fv2 = prop.GetValue(v2);

					if (!IsValueEqual(fv1, fv2, visitor))
					{
						return false;
					}
				}
			}

			return true;
		}



		public static bool IsValueEqual(object v1, object v2)
		{
			return IsValueEqual(v1, v2, new Visitor());
		}

		public static void AreEqual<T>(Dictionary<System.Reflection.FieldInfo, object> states, ref T o)
		{
			foreach (var kv in states)
			{
				if (kv.Key.FieldType.IsPrimitive)
				{
					var value = kv.Key.GetValue(o);
					Assert.AreEqual(value, kv.Value);
				}
				else if (kv.Value is IList kvvl)
				{
					var value = kv.Key.GetValue(o) as IList;
					Assert.AreEqual(value.Count, kvvl.Count);

					for (int i = 0; i < value.Count; i++)
					{
						IsValueEqual(value[i], kvvl[i]);
					}
				}
				else
				{
					var value = kv.Key.GetValue(o);
					IsValueEqual(value, kv.Value);
				}
			}
		}

		public static object CreateRandomData(System.Random random, bool avoidDefault, System.Type type)
		{
			if (type == typeof(bool))
			{
				if (avoidDefault)
				{
					return true;
				}

				return random.Next(2) == 0 ? false : true;
			}
			else if (type == typeof(byte))
			{
				byte t = 0;
				do
				{
					t = (byte)random.Next(byte.MinValue, byte.MaxValue + 1);
				}
				while (avoidDefault && t == default(byte));

				return t;
			}
			else if (type == typeof(sbyte))
			{
				sbyte t = 0;
				do
				{
					t = (sbyte)random.Next(sbyte.MinValue, sbyte.MaxValue + 1);
				}
				while (avoidDefault && t == default(sbyte));

				return t;
			}
			else if (type == typeof(short))
			{
				short t = 0;
				do
				{
					t = (short)random.Next(short.MinValue, short.MaxValue + 1);
				}
				while (avoidDefault && t == default(short));

				return t;
			}
			else if (type == typeof(ushort))
			{
				ushort t = 0;
				do
				{
					t = (ushort)random.Next(ushort.MinValue, ushort.MaxValue + 1);
				}
				while (avoidDefault && t == default(ushort));

				return t;
			}
			else if (type == typeof(int))
			{
				int t = 0;
				do
				{
					t = random.Next();
				}
				while (avoidDefault && t == default(int));

				return t;
			}
			else if (type == typeof(uint))
			{
				uint t = 0;
				do
				{
					var bytes = new byte[4];
					random.NextBytes(bytes);
					t = System.BitConverter.ToUInt32(bytes);
				}
				while (avoidDefault && t == default(uint));

				return t;
			}
			else if (type == typeof(long))
			{
				long t = 0;
				do
				{
					var bytes = new byte[8];
					random.NextBytes(bytes);
					t = System.BitConverter.ToInt64(bytes);
				}
				while (avoidDefault && t == default(long));

				return t;
			}
			else if (type == typeof(float))
			{
				float t = 0;
				do
				{
					t = (float)random.NextDouble();
				}
				while (avoidDefault && t == default(float));

				return t;
			}
			else if (type == typeof(double))
			{
				double t = 0;
				do
				{
					t = (double)random.NextDouble();
				}
				while (avoidDefault && t == default(double));

				return t;
			}
			else if (type == typeof(char))
			{
				char t = default(char);
				do
				{
					t = (char)random.Next(char.MinValue, char.MaxValue + 1);
				}
				while (avoidDefault && t == default(char));

				return t;
			}
			else if (type == typeof(string))
			{
				return System.Guid.NewGuid().ToString();
			}
			else if (type.IsEnum)
			{
				var values = type.GetEnumValues();

				if (avoidDefault)
				{
					return values.GetValue(random.Next(1, values.Length));
				}

				return values.GetValue(random.Next(0, values.Length));
			}
			else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
			{
				var ret = (IList)type.GetConstructor(System.Type.EmptyTypes).Invoke(null);
				var elmType = type.GenericTypeArguments[0];
				var count = random.Next(0, 10);

				for (int i = 0; i < count; i++)
				{
					ret.Add(CreateRandomData(random, avoidDefault, elmType));
				}

				return ret;
			}
			else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
			{
				var ret = (IDictionary)type.GetConstructor(System.Type.EmptyTypes).Invoke(null);
				var keyType = type.GenericTypeArguments[0];
				var valueType = type.GenericTypeArguments[1];

				var count = random.Next(0, 10);

				for (int i = 0; i < count; i++)
				{
					var key = CreateRandomData(random, avoidDefault, keyType);

					if (ret.Contains(key))
					{
						continue;
					}

					ret.Add(key, CreateRandomData(random, avoidDefault, valueType));
				}

				return ret;
			}
			else if (type.IsArray)
			{
				var count = random.Next(0, 10);
				var ret = System.Array.CreateInstance(type.GetElementType(), count);

				for (int i = 0; i < count; i++)
				{
					ret.SetValue(CreateRandomData(random, avoidDefault, type.GetElementType()), i);
				}
			}
			else if (type.GetCustomAttributes(true).OfType<System.SerializableAttribute>().Any())
			{
				var constructor = type.GetConstructor(new System.Type[0]);
				if (constructor != null)
				{
					var inst = constructor.Invoke(null);
					AssignRandomField(random, avoidDefault, ref inst);
					return inst;
				}
			}
			return null;
		}

		public static Dictionary<System.Reflection.FieldInfo, object> AssignRandomField<T>(System.Random random, bool avoidDefault, ref T o)
		{
			var assigned = new Dictionary<System.Reflection.FieldInfo, object>();

			foreach (var field in o.GetType().GetFields())
			{
				if (field.GetCustomAttributes(true).OfType<System.NonSerializedAttribute>().Any())
				{
					continue;
				}

				var value = CreateRandomData(random, avoidDefault, field.FieldType);
				if (value != null)
				{
					if (field.FieldType.IsGenericType && field.FieldType.GetGenericTypeDefinition() == typeof(List<>))
					{
						var dst = field.GetValue(o) as IList;
						if (dst == null)
						{
							dst = field.FieldType.GetConstructor(new System.Type[0]).Invoke(null) as IList;
							field.SetValue(o, dst);
						}
						dst.Clear();
						var src = value as IList;
						for (int i = 0; i < src.Count; i++)
						{
							dst.Add(src[i]);
						}
						assigned.Add(field, value);
					}
					else if (field.FieldType.IsGenericType && field.FieldType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
					{
						var dst = field.GetValue(o) as IDictionary;
						dst.Clear();
						var src = value as IDictionary;
						foreach (var key in src.Keys)
						{
							dst.Add(key, src[key]);
						}

						assigned.Add(field, value);
					}
					else
					{
						field.SetValue(o, value);
						assigned.Add(field, value);
					}
				}
			}

			return assigned;
		}
	}
}