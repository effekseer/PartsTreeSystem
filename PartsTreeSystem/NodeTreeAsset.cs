using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace PartsTreeSystem
{
	class NodeTreeAssetInternalData
	{
		public List<NodeTreeBase> Bases = new List<NodeTreeBase>();

		public string Serialize(Environment env)
		{
			return JsonSerializer.Serialize(this, env);
		}

		public static NodeTreeAssetInternalData Deserialize(string json, Environment env)
		{
			return JsonSerializer.Deserialize<NodeTreeAssetInternalData>(json, env);
		}
	}

	public class NodeTreeAsset : Asset
	{
		internal NodeTreeAssetInternalData InternalData = new NodeTreeAssetInternalData();
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

		internal void AssignID(Dictionary<int, int> idRemapper, INode node)
		{
			Action<INode> assignID = null;

			assignID = (n) =>
			{
				if (idRemapper.ContainsKey(n.InstanceID))
				{
					return;
				}

				var newID = GenerateGUID();
				idRemapper.Add(n.InstanceID, newID);
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

			AssignID(nodeTreeBase.IDRemapper, node);

			nodeTreeBase.ParentID = parentInstanceID;

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

		public int AddNodeTreeGroup(int parentInstanceID, NodeTreeAsset nodeTreeGroup, Environment env)
		{
			var node = Utility.CreateNodeFromNodeTreeGroup(nodeTreeGroup, env);

			var nodeTreeBase = new NodeTreeBase();

			nodeTreeBase.Template = env.GetRelativePath(env.GetAssetPath(this), env.GetAssetPath(nodeTreeGroup));

			AssignID(nodeTreeBase.IDRemapper, node.Root);

			nodeTreeBase.ParentID = parentInstanceID;

			InternalData.Bases.Add(nodeTreeBase);

			return node.Root.InstanceID;
		}


		public string Copy(int instanceID, Environment env)
		{
			if (instanceID < 0)
			{
				return null;
			}

			var nodeBase = InternalData.Bases.FirstOrDefault(_ => _.IDRemapper.ContainsValue(instanceID));
			if (nodeBase == null)
			{
				return null;
			}

			var collectedBases = CollectChildren(nodeBase);

			return JsonSerializer.Serialize(collectedBases, env);
		}

		public void Paste(string data, int instanceID, Environment env)
		{
			List<NodeTreeBase> nodeTreeBases = JsonSerializer.Deserialize<List<NodeTreeBase>>(data, env);
			if (nodeTreeBases == null)
			{
				return;
			}


			// TODO
		}

		public bool CanRemoveNode(int instanceID, Environment env)
		{
			var nodeBase = InternalData.Bases.FirstOrDefault(_ => _.IDRemapper.ContainsValue(instanceID));
			if (nodeBase == null)
			{
				return false;
			}

			// TODO : Refactor
			var rootNode = Utility.CreateNode(this, nodeBase, env);
			if (rootNode == null)
			{
				return false;
			}

			if (nodeBase.IDRemapper[rootNode.InstanceID] != instanceID)
			{
				return false;
			}

			return true;
		}

		public bool RemoveNode(int instanceID, Environment env)
		{
			if (!CanRemoveNode(instanceID, env))
			{
				return false;
			}

			var nodeBase = InternalData.Bases.FirstOrDefault(_ => _.IDRemapper.ContainsValue(instanceID));
			List<NodeTreeBase> collectedBases = CollectChildren(nodeBase);

			foreach (var r in collectedBases)
			{
				InternalData.Bases.Remove(r);
			}

			return true;
		}

		private List<NodeTreeBase> CollectChildren(NodeTreeBase nodeBase)
		{
			var collectedBases = new List<NodeTreeBase>();
			collectedBases.Add(nodeBase);

			bool changing = true;

			while (changing)
			{
				changing = false;

				foreach (var b in InternalData.Bases)
				{
					if (collectedBases.Contains(b))
					{
						continue;
					}

					if (collectedBases.Any(_ => _.IDRemapper.ContainsKey(b.ParentID)))
					{
						changing = true;
						collectedBases.Add(b);
					}
				}
			}

			return collectedBases;
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
						if (difference != null)
						{
							b.Differences[instanceID] = difference;
						}
						else
						{
							b.Differences.Remove(instanceID);
						}
					}
					else
					{
						if (difference != null)
						{
							b.Differences.Add(instanceID, difference);
						}
					}
				}
			}
		}

		public string Serialize(Environment env)
		{
			var json = InternalData.Serialize(env);
			var internalData = NodeTreeAssetInternalData.Deserialize(json, env);

			RemoveUnusedVariables(internalData, env);

			return internalData.Serialize(env);
		}

		public static NodeTreeAsset Deserialize(string json, Environment env)
		{
			var prefab = new NodeTreeAsset();

			prefab.InternalData = NodeTreeAssetInternalData.Deserialize(json, env);

			return prefab;
		}

		void RemoveUnusedVariables(NodeTreeAssetInternalData internalData, Environment env)
		{
			var nodeIDs = new HashSet<int>();
			foreach (var nodeBase in internalData.Bases)
			{
				var rootNode = Utility.CreateNode(this, nodeBase, env);
				foreach (var d in nodeBase.Differences)
				{
					var removingTargets = new List<AccessKeyGroup>();

					foreach (var m in d.Value.Modifications)
					{
						var o = (object)rootNode;
						if (Difference.GetAndCreateObjectHierarchy(ref o, m) == null)
						{
							removingTargets.Add(m.Target);
						}
					}

					foreach (var t in removingTargets)
					{
						d.Value.Remove(t);
					}
				}


				var remapResult = Utility.RemapID(nodeBase.IDRemapper, null, rootNode, null);

				foreach (var unused in remapResult.UnusedIDs)
				{
					nodeBase.IDRemapper.Remove(unused);
				}


				Action<INode> visitNodes = null;

				visitNodes = (n) =>
				{
					nodeIDs.Add(n.InstanceID);
					foreach (var child in n.GetChildren())
					{
						visitNodes(child);
					}
				};

				visitNodes(rootNode);
			}

			nodeIDs.Add(-1);

			internalData.Bases.RemoveAll(_ => _.Differences.Count == 0 && !nodeIDs.Contains(_.ParentID));
		}
	}
}