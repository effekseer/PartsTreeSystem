﻿using System;
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
		public Dictionary<string, NodeTreeAsset> NodeTrees = new Dictionary<string, NodeTreeAsset>();

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

	enum BasicEnum1
	{
		A,
		B,
		C,
	}

	class TestClassEnum
	{
		public BasicEnum1 Value1 = BasicEnum1.A;
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

	class TestNodeProprety : Node
	{
		[SerializeField]
		public float Value1 { get; set; }

		public float Value2 { get; set; }
	}

	class TestNodePrivate : Node
	{
		[SerializeField]
		float value = 0.0f;

		public float GetValue() { return value; }

		public void SetValue(float value) { this.value = value; }
	}

	class CustomAsset : Asset
	{
		public CustomAsset AssetRef;
	}

	class TestNodeCustomAsset : Node
	{
		public CustomAsset AssetRef;
	}
	class CustomAssetEnvironment : PartsTreeSystem.Environment
	{
		public Dictionary<string, NodeTreeAsset> NodeTrees = new Dictionary<string, NodeTreeAsset>();

		public Dictionary<string, CustomAsset> CustomAssets = new Dictionary<string, CustomAsset>();

		public override Asset GetAsset(string path)
		{
			var key = Utility.BackSlashToSlash(path);
			if (NodeTrees.ContainsKey(key))
			{
				return NodeTrees[key];
			}

			if (CustomAssets.ContainsKey(key))
			{
				return CustomAssets[key];
			}

			return null;
		}

		public override string GetAssetPath(Asset asset)
		{
			var nodeTree = NodeTrees.FirstOrDefault(_ => _.Value == asset);
			var customAsset = CustomAssets.FirstOrDefault(_ => _.Value == asset);

			if (nodeTree.Key != null)
			{
				return nodeTree.Key;
			}

			if (customAsset.Key != null)
			{
				return customAsset.Key;
			}

			return string.Empty;
		}
	}
}