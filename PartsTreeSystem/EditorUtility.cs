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
					ArrayUtility.SetValueToIndex(parent, value, index.Value);
				}
			}
		}

		int currentIndex = -1;
		List<Element> elements = new List<Element>(8);

		public string[] Names { get => elements.Take(currentIndex + 1).Select(_ => _.GetName()).ToArray(); }

		public System.Reflection.FieldInfo[] FieldInfos { get => elements.Take(currentIndex + 1).Select(_ => _.fieldInfo).ToArray(); }

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
				return ArrayUtility.GetValueWithIndex(elm.parent, elm.index.Value);
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
				ArrayUtility.SetValueToIndex(elm.parent, value, elm.index.Value);
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
}