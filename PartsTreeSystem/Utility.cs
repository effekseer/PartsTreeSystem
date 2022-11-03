using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace PartsTreeSystem
{
	public class ArrayUtility
	{
		public static Array ResizeArray(Array list, int count)
		{
			var elmType = list.GetType().GetElementType();
			var array = Array.CreateInstance(elmType, count);

			for (int i = 0; i < Math.Min(list.Length, count); i++)
			{
				array.SetValue(list.GetValue(i), i);
			}

			for (int i = list.Length; i < count; i++)
			{
				array.SetValue(Utility.CreateDefaultValue(elmType), i);
			}

			return array;
		}

		public static void ResizeList(IList list, int count)
		{
			var elmType = list.GetType().GetGenericArguments()[0];
			while (list.Count < count)
			{
				list.Add(Utility.CreateDefaultValue(elmType));
			}

			while (list.Count > count)
			{
				list.RemoveAt(list.Count - 1);
			}
		}

		public static object GetValueWithIndex(object target, int index)
		{
			if (target is Array array)
			{
				return array.GetValue(index);
			}

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
			if (target is Array array)
			{
				array.SetValue(value, index);
				return true;
			}

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
	}

	public class Utility
	{
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

		internal static INode CreateNode(NodeTreeAsset nodeTreeAsset, NodeTreeBase nodeTreeBase, Environment env)
		{
			INode node = null;

			if (nodeTreeBase.BaseType != null)
			{
				var nodeType = env.GetType(nodeTreeBase.BaseType);

				var constructor = nodeType.GetConstructor(Type.EmptyTypes);
				node = (INode)constructor.Invoke(null);
			}
			else if (nodeTreeBase.Template != null)
			{
				var path = env.GetAbsolutePath(env.GetAssetPath(nodeTreeAsset), nodeTreeBase.Template);
				var baseNodeTreeAsset = env.GetAsset(path) as NodeTreeAsset;

				var nodeTree = CreateNodeFromNodeTreeGroup(baseNodeTreeAsset, env);
				node = nodeTree.Root;
			}
			else
			{
				throw new InvalidOperationException();
			}

			return node;
		}

		internal class RemapResult
		{
			public List<int> UnusedIDs = new List<int>();
		}

		internal static RemapResult RemapID(Dictionary<int, int> idRemapper, NodeTreeAsset nodeTreeGroup, INode node, Dictionary<int, INode> idToNode)
		{
			var result = new RemapResult();

			result.UnusedIDs.AddRange(idRemapper.Keys);

			Action<INode> applyID = null;

			applyID = (n) =>
			{
				if (idRemapper.ContainsKey(n.InstanceID))
				{
					result.UnusedIDs.RemoveAll(_ => _ == n.InstanceID);
					n.InstanceID = idRemapper[n.InstanceID];
				}
				else
				{
					if (nodeTreeGroup != null)
					{
						nodeTreeGroup.AssignID(idRemapper, n);
					}
				}

				if (idToNode != null)
				{
					idToNode.Add(n.InstanceID, n);
				}

				foreach (var child in n.GetChildren())
				{
					applyID(child);
				}
			};

			applyID(node);

			return result;
		}

		public static string GetRelativePath(string basePath, string path)
		{
			Func<string, string> escape = (string s) =>
			{
				return s.Replace("%", "%25");
			};

			Func<string, string> unescape = (string s) =>
			{
				return s.Replace("%25", "%");
			};

			Uri basepath = new Uri(escape(basePath));
			Uri targetPath = new Uri(escape(path));
			return unescape(Uri.UnescapeDataString(basepath.MakeRelativeUri(targetPath).ToString()));
		}
		public static string GetAbsolutePath(string basePath, string path)
		{
			if (string.IsNullOrEmpty(path))
			{
				return string.Empty;
			}

			var basePath_ecs = new Uri(basePath, UriKind.Relative);
			var path_ecs = new Uri(path, UriKind.Relative);
			var basePath_slash = BackSlashToSlash(basePath_ecs.ToString());
			var basePath_uri = new Uri(basePath_slash, UriKind.Absolute);
			var path_uri = new Uri(path_ecs.ToString(), UriKind.Relative);
			var targetPath = new Uri(basePath_uri, path_uri);
			var ret = targetPath.LocalPath.ToString();
			return ret;
		}

		public static string BackSlashToSlash(string input)
		{
			return input.Replace("\\", "/");
		}

		public static void RebuildNodeTree(NodeTreeAsset nodeTreeGroup, NodeTree nodeTree, Environment env)
		{
			var nt = CreateNodeFromNodeTreeGroup(nodeTreeGroup, env);
			nodeTree.Root = nt.Root;
		}

		public static NodeTree CreateNodeFromNodeTreeGroup(NodeTreeAsset nodeTreeGroup, Environment env)
		{
			var idToNode = new Dictionary<int, INode>();

			var parentIdToChild = new List<Tuple<int, INode>>();

			var baseToNode = new Dictionary<NodeTreeBase, INode>();

			foreach (var b in nodeTreeGroup.InternalData.Bases)
			{
				var node = CreateNode(nodeTreeGroup, b, env);

				RemapID(b.IDRemapper, nodeTreeGroup, node, idToNode);

				parentIdToChild.Add(Tuple.Create(b.ParentID, node));

				baseToNode.Add(b, node);
			}

			INode rootNode = null;

			foreach (var pc in parentIdToChild)
			{
				if (idToNode.ContainsKey(pc.Item1))
				{
					var parent = idToNode[pc.Item1];
					parent.AddChild(pc.Item2);
				}
				else
				{
					rootNode = pc.Item2;
				}
			}

			var ret = new NodeTree();
			ret.Root = rootNode;

			foreach (var b in nodeTreeGroup.InternalData.Bases)
			{
				foreach (var difference in b.Differences)
				{
					Func<int, INode, INode> findNode = null;

					findNode = (int id, INode n) =>
					{
						if (n.InstanceID == id)
						{
							return n;
						}

						foreach (var child in n.GetChildren())
						{
							var found = findNode(id, child);
							if (found != null)
							{
								return found;
							}
						}

						return null;
					};

					var node = baseToNode[b];

					var targetNode = findNode(difference.Key, node);
					var target = (object)targetNode;
					Difference.ApplyDifference(ref target, difference.Value, nodeTreeGroup, ret, env);
				}

			}

			return ret;
		}
	}
}