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

		public virtual string GetRelativePath(string basePath, string path)
		{
			return Utility.GetRelativePath(basePath, path);
		}

		public virtual string GetAbsolutePath(string basePath, string path)
		{
			return Utility.GetAbsolutePath(basePath, path);
		}
	}

}