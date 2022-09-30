using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PartsTreeSystem;

namespace PartsTreeSystemTest
{
	class SaveLoad
	{
		[SetUp]
		public void Setup()
		{

		}

		[Test]
		public void SaveLoadPrimitive()
		{
			SaveLoadTest<TestNodePrimitive>();
		}

		[Test]
		public void SaveLoadList()
		{
			SaveLoadTest<TestNode_ListValue>();
		}

		[Test]
		public void SaveLoadListClass()
		{
			SaveLoadTest<TestNode_ListClass>();
		}

		[Test]
		public void SaveLoadListClassNotSerializable()
		{
			SaveLoadTest<TestNode_List<TestClassNotSerializable>>();
		}

		void SaveLoadTest<T>()
		{
			var env = new Environment();
			var random = new System.Random();
			var commandManager = new CommandManager();
			var nodeTreeGroup = new NodeTreeAsset();
			nodeTreeGroup.Init(typeof(T), env);

			var instance = Utility.CreateNodeFromNodeTreeGroup(nodeTreeGroup, env);

			commandManager.StartEditFields(nodeTreeGroup, instance, instance.Root, env);

			var state = Helper.AssignRandomField(random, false, ref instance.Root);

			commandManager.NotifyEditFields(instance.Root);
			commandManager.EndEditFields(instance.Root, env);

			var json = nodeTreeGroup.Serialize(env);
			var nodeTreeGroup2 = NodeTreeAsset.Deserialize(json, env);
			var instance2 = Utility.CreateNodeFromNodeTreeGroup(nodeTreeGroup2, env);

			Assert.True(Helper.IsValueEqual(instance, instance2));
		}

		[Test]
		public void SaveLoadRef()
		{
			var env = new Environment();
			var random = new System.Random();
			var commandManager = new CommandManager();
			var nodeTreeGroup = new NodeTreeAsset();
			nodeTreeGroup.Init(typeof(TestNodeRef), env);

			var instance = Utility.CreateNodeFromNodeTreeGroup(nodeTreeGroup, env);

			commandManager.StartEditFields(nodeTreeGroup, instance, instance.Root, env);

			(instance.Root as TestNodeRef).Ref = instance.Root as Node;
			(instance.Root as TestNodeRef).Refs = new List<Node>();
			(instance.Root as TestNodeRef).Refs.Add(instance.Root as Node);

			commandManager.NotifyEditFields(instance.Root);
			commandManager.EndEditFields(instance.Root, env);

			var json = nodeTreeGroup.Serialize(env);

			var nodeTreeGroup2 = NodeTreeAsset.Deserialize(json, env);
			var instance2 = Utility.CreateNodeFromNodeTreeGroup(nodeTreeGroup2, env);

			var instanceRoot = instance2.Root as TestNodeRef;

			Assert.True(Helper.IsValueEqual(instanceRoot, instanceRoot.Ref));
			Assert.True(Helper.IsValueEqual(instanceRoot, instanceRoot.Refs[0]));
			Assert.True(Helper.IsValueEqual(instance, instance2));
		}

		[Test]
		public void SaveLoadMultiTree()
		{
			var env = new MultiNodeTreeEnvironment();
			var random = new System.Random();
			var commandManager = new CommandManager();
			var nodeTreeGroup1 = new NodeTreeAsset();
			var nodeTreeGroup2 = new NodeTreeAsset();

			env.NodeTrees.Add("C:/test/Tree1", nodeTreeGroup1);
			env.NodeTrees.Add("C:/test/Tree2", nodeTreeGroup2);

			var id1 = nodeTreeGroup1.Init(typeof(TestNodeRef), env);
			var id2 = nodeTreeGroup2.Init(typeof(TestNodeRef), env);

			var instance = Utility.CreateNodeFromNodeTreeGroup(nodeTreeGroup1, env);

			nodeTreeGroup1.AddNodeTreeGroup(id1, nodeTreeGroup2, env);

			PartsTreeSystem.Utility.RebuildNodeTree(nodeTreeGroup1, instance, env);

			commandManager.StartEditFields(nodeTreeGroup1, instance, instance.Root, env);

			(instance.Root as TestNodeRef).Ref = instance.Root as Node;
			(instance.Root as TestNodeRef).Refs = new List<Node>();
			(instance.Root as TestNodeRef).Refs.Add(instance.Root as Node);

			commandManager.NotifyEditFields(instance.Root);
			commandManager.EndEditFields(instance.Root, env);

			var json = nodeTreeGroup1.Serialize(env);

			var nodeTreeGroup_Deserialized = NodeTreeAsset.Deserialize(json, env);

			// TODO : Better implimentation
			env.NodeTrees["C:/test/Tree1"] = nodeTreeGroup_Deserialized;
			var instance2 = Utility.CreateNodeFromNodeTreeGroup(nodeTreeGroup_Deserialized, env);

			var instanceRoot = instance2.Root as TestNodeRef;

			Assert.True(Helper.IsValueEqual(instanceRoot, instanceRoot.Ref));
			Assert.True(Helper.IsValueEqual(instanceRoot, instanceRoot.Refs[0]));
			Assert.True(Helper.IsValueEqual(instance, instance2));
		}

		[Test]
		public void SaveLoadProperty()
		{
			var env = new Environment();
			var random = new System.Random();
			var commandManager = new CommandManager();
			var nodeTreeGroup = new NodeTreeAsset();
			nodeTreeGroup.Init(typeof(TestNodeProprety), env);

			var instance = Utility.CreateNodeFromNodeTreeGroup(nodeTreeGroup, env);

			commandManager.StartEditFields(nodeTreeGroup, instance, instance.Root, env);

			(instance.Root as TestNodeProprety).Value1 = 1;
			(instance.Root as TestNodeProprety).Value2 = 2;

			commandManager.NotifyEditFields(instance.Root);
			commandManager.EndEditFields(instance.Root, env);

			var json = nodeTreeGroup.Serialize(env);

			var nodeTreeGroup2 = NodeTreeAsset.Deserialize(json, env);
			var instance2 = Utility.CreateNodeFromNodeTreeGroup(nodeTreeGroup2, env);

			var instanceRoot = instance2.Root as TestNodeProprety;

			Assert.True(Helper.IsValueEqual(instanceRoot.Value1, (instance.Root as TestNodeProprety).Value1));
			Assert.True(Helper.IsValueEqual(instanceRoot.Value2, 0.0f));
		}

		[Test]
		public void SaveLoadPrivate()
		{
			var env = new Environment();
			var random = new System.Random();
			var commandManager = new CommandManager();
			var nodeTreeGroup = new NodeTreeAsset();
			nodeTreeGroup.Init(typeof(TestNodePrivate), env);

			var instance = Utility.CreateNodeFromNodeTreeGroup(nodeTreeGroup, env);

			commandManager.StartEditFields(nodeTreeGroup, instance, instance.Root, env);

			(instance.Root as TestNodePrivate).SetValue(1.0f);

			commandManager.NotifyEditFields(instance.Root);
			commandManager.EndEditFields(instance.Root, env);

			var json = nodeTreeGroup.Serialize(env);

			var nodeTreeGroup2 = NodeTreeAsset.Deserialize(json, env);
			var instance2 = Utility.CreateNodeFromNodeTreeGroup(nodeTreeGroup2, env);

			var instanceRoot = instance2.Root as TestNodePrivate;

			Assert.True(Helper.IsValueEqual(instanceRoot.GetValue(), 1.0f));
		}

		[Test]
		public void SaveLoadCustomAsset()
		{
			var env = new CustomAssetEnvironment();

			var ca1 = new CustomAsset();
			var ca2 = new CustomAsset();
			ca2.AssetRef = ca1;

			env.CustomAssets.Add("CA1", ca1);
			env.CustomAssets.Add("CA2", ca2);


			var random = new System.Random();
			var commandManager = new CommandManager();
			var nodeTreeAsset = new NodeTreeAsset();
			nodeTreeAsset.Init(typeof(TestNodeCustomAsset), env);
			env.NodeTrees.Add("Tree1", nodeTreeAsset);

			var instance = Utility.CreateNodeFromNodeTreeGroup(nodeTreeAsset, env);

			commandManager.StartEditFields(nodeTreeAsset, instance, instance.Root, env);

			(instance.Root as TestNodeCustomAsset).AssetRef = ca2;

			commandManager.NotifyEditFields(instance.Root);
			commandManager.EndEditFields(instance.Root, env);

			var json = nodeTreeAsset.Serialize(env);

			var nodeTreeGroup2 = NodeTreeAsset.Deserialize(json, env);
			var instance2 = Utility.CreateNodeFromNodeTreeGroup(nodeTreeGroup2, env);

			var instanceRoot = instance2.Root as TestNodeCustomAsset;

			Assert.True(Helper.IsValueEqual(instanceRoot.AssetRef, ca2));

			{
				var json_ca2 = JsonSerializer.Serialize(ca2, env);
				var cs2_des = JsonSerializer.Deserialize<CustomAsset>(json_ca2, env);

				Assert.True(Helper.IsValueEqual(cs2_des.AssetRef, ca1));
			}
		}
	}
}