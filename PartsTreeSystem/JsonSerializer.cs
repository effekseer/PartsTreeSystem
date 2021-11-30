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

				return o;
			}
		}

		static object ConvertJsonToCS(Type type, JToken token)
		{
			if (token is JObject jobj)
			{
				var dst = Activator.CreateInstance(type);

				var fields = type.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

				foreach (var field in fields)
				{
					if (!jobj.ContainsKey(field.Name))
					{
						continue;
					}

					field.SetValue(dst, ConvertJsonToCS(field.FieldType, jobj[field.Name]));
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
			else if (token is JToken jtoken)
			{
				if (jtoken.Type == JTokenType.Integer)
				{
					return Convert.ChangeType(jtoken.Value<object>(), type);
				}
				else if (jtoken.Type == JTokenType.Float)
				{
					return jtoken.Value<double>();
				}
				else if (jtoken.Type == JTokenType.String)
				{
					return jtoken.Value<string>();
				}
				else if (jtoken.Type == JTokenType.Boolean)
				{
					return jtoken.Value<bool>();
				}
				return null;
			}

			return null;
		}
	}
}