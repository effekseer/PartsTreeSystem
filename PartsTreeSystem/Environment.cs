using System;
using System.Collections.Generic;
using System.Text;

namespace PartsTreeSystem
{
	public class Environment
	{
		public virtual Type GetType(string typeName)
		{
			return Type.GetType(typeName);
		}

		public virtual string GetTypeName(Type type)
		{
			return type.AssemblyQualifiedName;
		}

		public virtual Asset GetAsset(string path)
		{
			return null;
		}

		public virtual string GetAssetPath(Asset asset)
		{
			return null;
		}
	}

}