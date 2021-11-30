using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace PartsTreeSystem
{
	public class NodeTree : IAssetInstanceRoot
	{
		public INode Root;

		public IInstanceID? FindInstance(int id)
		{
			return FindInstance(Root, id);
		}

		public INode FindParent(int id)
		{
			return FindParent(Root, id);
		}

		IInstanceID? FindInstance(INode node, int id)
		{
			if (node.InstanceID == id)
			{
				return node;
			}

			foreach (var child in node.GetChildren())
			{
				var result = FindInstance(child, id);
				if (result != null)
				{
					return result;
				}
			}

			return null;

		}

		INode FindParent(INode parent, int id)
		{
			if (parent.GetChildren().Any(_ => _.InstanceID == id))
			{
				return parent;
			}

			foreach (var child in parent.GetChildren())
			{
				var result = FindParent(child, id);
				if (result != null)
				{
					return result;
				}
			}

			return null;
		}
	}

	class NodeTreeBase
	{
		public string BaseType;

		public string Template;

		public Dictionary<int, int> IDRemapper = new Dictionary<int, int>();

		public Dictionary<int, Difference> Differences = new Dictionary<int, Difference>();

		public int ParentID;

		public int RootID = -1;
	}

	class NodeTreeGroupInternalData
	{
		public List<NodeTreeBase> Bases = new List<NodeTreeBase>();

		public string Serialize()
		{
			return JsonSerializer.Serialize(this);
		}

		public static NodeTreeGroupInternalData Deserialize(string json)
		{
			return JsonSerializer.Deserialize<NodeTreeGroupInternalData>(json);
		}
	}

	public class NodeTreeGroup : Asset
	{
		internal NodeTreeGroupInternalData InternalData = new NodeTreeGroupInternalData();
		int GenerateGUID()
		{
			var rand = new Random();
			while (true)
			{
				var id = rand.Next(0, int.MaxValue);

				if (InternalData.Bases.Find(_ => _.IDRemapper.Values.Contains(id)) == null)
				{
					return id;
				}
			}
		}

		internal void AssignID(NodeTreeBase nodeTreeBase, INode node)
		{
			Action<INode> assignID = null;

			assignID = (n) =>
			{
				if (nodeTreeBase.IDRemapper.ContainsKey(n.InstanceID))
				{
					return;
				}

				var newID = GenerateGUID();
				nodeTreeBase.IDRemapper.Add(n.InstanceID, newID);
				n.InstanceID = newID;

				foreach (var child in n.GetChildren())
				{
					assignID(child);
				}
			};

			assignID(node);
		}

		int AddNodeInternal(int parentInstanceID, string typeName, Environment env)
		{
			var nodeType = env.GetType(typeName);
			var constructor = nodeType.GetConstructor(Type.EmptyTypes);
			var node = (INode)constructor.Invoke(null);

			var nodeTreeBase = new NodeTreeBase();
			nodeTreeBase.BaseType = typeName;

			AssignID(nodeTreeBase, node);

			nodeTreeBase.ParentID = parentInstanceID;
			nodeTreeBase.RootID = node.InstanceID;

			InternalData.Bases.Add(nodeTreeBase);

			return node.InstanceID;
		}

		public int Init(Type nodeType, Environment env)
		{
			return AddNodeInternal(-1, env.GetTypeName(nodeType), env);
		}

		public int AddNode(int parentInstanceID, Type nodeType, Environment env)
		{
			if (parentInstanceID < 0)
			{
				return -1;
			}

			return AddNodeInternal(parentInstanceID, env.GetTypeName(nodeType), env);
		}

		public int AddNodeTreeGroup(int parentInstanceID, NodeTreeGroup nodeTreeGroup, Environment env)
		{
			var node = Utility.CreateNodeFromNodeTreeGroup(nodeTreeGroup, env);

			var nodeTreeBase = new NodeTreeBase();

			nodeTreeBase.Template = Utility.GetRelativePath(env.GetAssetPath(this), env.GetAssetPath(nodeTreeGroup));

			AssignID(nodeTreeBase, node.Root);

			nodeTreeBase.ParentID = parentInstanceID;
			nodeTreeBase.RootID = node.Root.InstanceID;

			InternalData.Bases.Add(nodeTreeBase);

			return node.Root.InstanceID;
		}

		public bool RemoveNode(int instanceID)
		{
			var removed = InternalData.Bases.Where(_ => _.RootID == instanceID).FirstOrDefault();
			if (removed == null)
			{
				return false;
			}

			var removingNodes = new List<NodeTreeBase>();
			removingNodes.Add(removed);

			bool changing = true;

			while (changing)
			{
				changing = false;

				foreach (var b in InternalData.Bases)
				{
					if (removingNodes.Contains(b))
					{
						continue;
					}

					if (removingNodes.Any(_ => _.IDRemapper.ContainsKey(b.ParentID)))
					{
						changing = true;
						removingNodes.Add(b);
					}
				}
			}

			foreach (var r in removingNodes)
			{
				InternalData.Bases.Remove(r);
			}

			return true;
		}

		internal override Difference GetDifference(int instanceID)
		{
			foreach (var b in InternalData.Bases)
			{
				if (b.Differences.ContainsKey(instanceID))
				{
					return b.Differences[instanceID];
				}
			}

			return null;
		}

		internal override void SetDifference(int instanceID, Difference difference)
		{
			foreach (var b in InternalData.Bases)
			{
				if (b.IDRemapper.Values.Contains(instanceID))
				{
					if (b.Differences.ContainsKey(instanceID))
					{
						b.Differences[instanceID] = difference;
					}
					else
					{
						b.Differences.Add(instanceID, difference);
					}
				}
			}
		}

		public string Serialize()
		{
			return InternalData.Serialize();
		}

		public static NodeTreeGroup Deserialize(string json)
		{
			var prefab = new NodeTreeGroup();

			prefab.InternalData = NodeTreeGroupInternalData.Deserialize(json);

			return prefab;
		}
	}
}