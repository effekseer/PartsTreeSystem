using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace PartsTreeSystem
{
	public class ElementGetterSetterArray
	{
		class Element
		{
			public System.Reflection.FieldInfo fieldInfo;
			public int? index;
			public object parent;

			public void Reset()
			{
				fieldInfo = null;
				index = null;
				parent = null;
			}

			public string GetName()
			{
				if (fieldInfo != null)
				{
					return fieldInfo.Name;
				}
				else if (index.HasValue)
				{
					return index.Value.ToString();
				}

				return string.Empty;
			}

			public void SetValue(object value)
			{
				if (fieldInfo != null)
				{
					fieldInfo.SetValue(parent, value);
				}
				else if (index.HasValue)
				{
					EditorUtility.SetValueToIndex(parent, value, index.Value);
				}
			}
		}

		int currentIndex = -1;
		List<Element> elements = new List<Element>(8);

		public string[] Names { get => elements.Take(currentIndex + 1).Select(_ => _.GetName()).ToArray(); }

		public void Push(object o, System.Reflection.FieldInfo fieldInfo)
		{
			var elm = PushElement();
			elm.parent = o;
			elm.fieldInfo = fieldInfo;
		}

		public void Push(object o, int index)
		{
			var elm = PushElement();
			elm.parent = o;
			elm.index = index;
		}

		Element PushElement()
		{
			currentIndex += 1;
			if (elements.Count <= currentIndex)
			{
				elements.Add(new Element());
			}
			else
			{
				elements[currentIndex].Reset();
			}

			return elements[currentIndex];
		}

		public void Pop()
		{
			currentIndex--;
		}

		public string GetName()
		{
			var elm = elements[currentIndex];
			return elm.GetName();
		}

		public object GetValue()
		{
			var elm = elements[currentIndex];

			if (elm.fieldInfo != null)
			{
				return elm.fieldInfo.GetValue(elm.parent);
			}
			else if (elm.index.HasValue)
			{
				return EditorUtility.GetValueWithIndex(elm.parent, elm.index.Value);
			}

			return null;
		}

		public void SetValue(object value)
		{
			var elm = elements[currentIndex];
			elm.SetValue(value);

			if (elm.fieldInfo != null)
			{
				elm.fieldInfo.SetValue(elm.parent, value);
			}
			else if (elm.index.HasValue)
			{
				EditorUtility.SetValueToIndex(elm.parent, value, elm.index.Value);
			}

			for (int i = currentIndex; i > 0; i--)
			{
				if (elements[i].parent.GetType().IsClass)
				{
					break;
				}
				elements[i - 1].SetValue(elements[i].parent);
			}
		}
	}

	public class EditorUtility
	{
		public static void ResizeList(IList list, int count)
		{
			while (list.Count < count)
			{
				list.Add(CreateDefaultValue(list.GetType().GetGenericArguments()[0]));
			}

			while (list.Count > count)
			{
				list.RemoveAt(list.Count - 1);
			}
		}

		public static object GetValueWithIndex(object target, int index)
		{
			foreach (var pi in target.GetType().GetProperties())
			{
				if (pi.GetIndexParameters().Length != 1)
				{
					continue;
				}

				return pi.GetValue(target, new object[] { index });
			}
			return null;
		}

		public static bool SetValueToIndex(object target, object value, int index)
		{
			foreach (var pi in target.GetType().GetProperties())
			{
				if (pi.GetIndexParameters().Length != 1)
				{
					continue;
				}

				pi.SetValue(target, value, new object[] { index });
				return true;
			}
			return false;
		}

		public static object CreateDefaultValue(Type type)
		{
			if (type.IsValueType)
			{
				return Activator.CreateInstance(type);
			}
			else
			{
				var constructor = type.GetConstructor(new Type[] { });
				if (constructor == null)
				{
					return null;
				}

				return constructor.Invoke(null);
			}
		}
	}
}