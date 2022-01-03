using System;
using System.Collections.Generic;
using System.Text;
using PartsTreeSystem;
using System.Linq;

namespace PartsTreeSystemTest
{
	public class Node : INode
	{
		public int InstanceID { get; set; }

		[System.NonSerialized]
		public List<Node> Children = new List<Node>();

		public void AddChild(INode node)
		{
			Children.Add(node as Node);
		}

		public void RemoveChild(int instanceID)
		{
			Children.RemoveAll(_ => _.InstanceID == instanceID);
		}

		public void InsertChild(int index, INode node)
		{
			Children.Insert(index, node as Node);
		}

		public IReadOnlyCollection<INode> GetChildren()
		{
			return Children;
		}
	}

	class MultiNodeTreeEnvironment : PartsTreeSystem.Environment
	{
		public Dictionary<string, NodeTreeGroup> NodeTrees = new Dictionary<string, NodeTreeGroup>();

		public override Asset GetAsset(string path)
		{
			return NodeTrees[Utility.BackSlashToSlash(path)];
		}

		public override string GetAssetPath(Asset asset)
		{
			return NodeTrees.FirstOrDefault(_ => _.Value == asset).Key;
		}
	}

	class TestNodePrimitive : Node
	{
		public bool ValueBool;
		public byte ValueByte;
		public sbyte ValueSByte;
		public double ValueDobule;
		public float ValueFloat;
		public int ValueInt32;
		public uint ValueUInt32;
		public long ValueInt64;
		public short ValueInt16;
		public ushort ValueUInt16;
		public char ValueChar;
		public string ValueString;
	}

	class TestNodeRef : Node
	{
		public Node Ref;
		public List<Node> Refs;
	}

	class TestClassNotSerializable
	{
		public float A;
		public float B;
		public float C;
	}

}