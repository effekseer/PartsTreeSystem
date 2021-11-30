using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace PartsTreeSystem
{
	public class AccessKeyGroup
	{
		public AccessKey[] Keys = null;

		public override int GetHashCode()
		{
			var hash = 0;

			foreach (var key in Keys)
			{
				hash += key.GetHashCode();
			}

			return hash;
		}

		public override bool Equals(object obj)
		{
			var o = obj as AccessKeyGroup;
			if (o == null) return false;

			if (Keys.Length != o.Keys.Length)
				return false;

			for (int i = 0; i < Keys.Length; i++)
			{
				if (!Keys[i].Equals(o.Keys[i]))
					return false;
			}

			return true;
		}
	}

	public class AccessKey
	{
		public string Name;
		public int? Index;

		public override int GetHashCode()
		{
			if (Index.HasValue)
			{
				return Name.GetHashCode() + Index.Value.GetHashCode();
			}

			return Name.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			var o = obj as AccessKey;
			if (o is null)
				return false;

			return Name == o.Name && Index == o.Index;
		}

		public override string ToString()
		{
			if (Index.HasValue)
			{
				return Name + "[" + Index.Value + "]";
			}
			return Name;
		}
	}
}