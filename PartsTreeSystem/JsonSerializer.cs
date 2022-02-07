using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PartsTreeSystem
{
	class JsonSerializer
	{
		public static string Serialize(object o)
		{
			return JsonConvert.SerializeObject(ConvertCSToJson(o));
		}

		public static T Deserialize<T>(string json)
		{
			var token = JsonConvert.DeserializeObject(json);

			return (T)ConvertJsonToCS(typeof(T), token as JToken);
		}

		static Type RemoveNullable(Type type)
		{
			if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
			{
				return type.GetGenericArguments()[0];
			}
			return type;
		}

		static JToken ConvertCSToJson(object value)
		{
			if (value is null)
			{
				return null;
			}
			else if (value is string)
			{
				return (string)value;
			}
			else if (value.GetType().IsPrimitive)
			{
				return JToken.FromObject(value);
			}
			else if (value.GetType().IsArray)
			{
				var a = new JArray();

				var v = (Array)value;

				for (int i = 0; i < v.Length; i++)
				{
					a.Add(ConvertCSToJson(v.GetValue(i)));
				}

				return a;
			}
			else if (value is IList)
			{
				var a = new JArray();

				var v = (IList)value;

				for (int i = 0; i < v.Count; i++)
				{
					a.Add(ConvertCSToJson(v[i]));
				}

				return a;
			}
			else if (value is IDictionary)
			{
				var v = (IDictionary)value;
				var o = new JArray();

				foreach (var key in v.Keys)
				{
					var elm = new JArray();
					elm.Add(ConvertCSToJson(key));
					elm.Add(ConvertCSToJson(v[key]));
					o.Add(elm);
				}

				return o;
			}
			else
			{
				var o = new JObject();

				var fields = value.GetType().GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);


				foreach (var field in fields)
				{
					var fv = field.GetValue(value);

					o.Add(field.Name, ConvertCSToJson(fv));
				}

				var properties = value.GetType().GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

				foreach (var property in properties)
				{
					var pv = property.GetValue(value);

					o.Add(property.Name, ConvertCSToJson(pv));
				}

				return o;
			}
		}

		static object ConvertJsonToCS(Type type, JToken token)
		{
			type = RemoveNullable(type);

			if (token is JObject jobj)
			{
				var dst = Activator.CreateInstance(type);

				var fields = FieldState.GetFields(type);
				if (fields != null)
				{
					foreach (var field in fields)
					{
						if (!jobj.ContainsKey(field.Name))
						{
							continue;
						}

						field.SetValue(dst, ConvertJsonToCS(field.FieldType, jobj[field.Name]));
					}

				}

				var properties = FieldState.GetProperties(type);
				if (properties != null)
				{
					foreach (var property in properties)
					{
						if (!jobj.ContainsKey(property.Name))
						{
							continue;
						}

						property.SetValue(dst, ConvertJsonToCS(property.PropertyType, jobj[property.Name]));
					}
				}
				return dst;
			}
			else if (token is JArray jarray)
			{
				var count = jarray.Count;

				if (type.IsArray)
				{
					var dst = Array.CreateInstance(type.GetElementType(), count);
					for (int i = 0; i < jarray.Count; i++)
					{
						dst.SetValue(ConvertJsonToCS(type.GetElementType(), jarray[i]), i);
					}
					return dst;
				}
				else if (type.GetInterfaces().Contains(typeof(IDictionary)))
				{
					var dst = Activator.CreateInstance(type) as IDictionary;

					var keyType = type.GenericTypeArguments[0];
					var valueType = type.GenericTypeArguments[1];

					for (int i = 0; i < jarray.Count; i++)
					{
						var jkv = jarray[i] as JArray;
						var key = ConvertJsonToCS(keyType, jkv[0]);
						var value = ConvertJsonToCS(valueType, jkv[1]);
						dst.Add(key, value);
					}

					return dst;
				}
				else if (type.GetInterfaces().Contains(typeof(IList)))
				{
					var dst = Activator.CreateInstance(type) as IList;
					for (int i = 0; i < jarray.Count; i++)
					{
						dst.Add(ConvertJsonToCS(dst.GetType().GenericTypeArguments[0], jarray[i]));
					}
					return dst;
				}
				else
				{
					return null;
				}
			}
			else if (token is JValue jvalue)
			{
				if (jvalue.Type == JTokenType.Integer)
				{
					if (type == typeof(Byte) ||
						type == typeof(SByte) ||
						type == typeof(UInt16) ||
						type == typeof(Int16) ||
						type == typeof(UInt32) ||
						type == typeof(Int32) ||
						type == typeof(Int64))
					{
						return jvalue.ToObject(type);
					}
					return jvalue.Value;
				}
				else if (jvalue.Type == JTokenType.Float)
				{
					return jvalue.Value<double>();
				}
				else if (jvalue.Type == JTokenType.String)
				{
					return jvalue.Value<string>();
				}
				else if (jvalue.Type == JTokenType.Boolean)
				{
					return jvalue.Value<bool>();
				}
				return null;
			}

			return null;
		}
	}
}