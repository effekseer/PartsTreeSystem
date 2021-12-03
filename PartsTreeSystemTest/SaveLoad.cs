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
			var env = new PartsTreeSystem.Environment();
			var random = new System.Random();
			var commandManager = new CommandManager();
			var nodeTreeGroup = new NodeTreeGroup();
			nodeTreeGroup.Init(typeof(T), env);

			var instance = Utility.CreateNodeFromNodeTreeGroup(nodeTreeGroup, env);

			commandManager.StartEditFields(nodeTreeGroup, instance, instance.Root, env);

			var state = Helper.AssignRandomField(random, false, ref instance.Root);

			commandManager.NotifyEditFields(instance.Root);
			commandManager.EndEditFields(instance.Root, env);

			var json = nodeTreeGroup.Serialize(env);
			var nodeTreeGroup2 = NodeTreeGroup.Deserialize(json);
			var instance2 = Utility.CreateNodeFromNodeTreeGroup(nodeTreeGroup2, env);

			Assert.True(Helper.IsValueEqual(instance, instance2));
		}

		[Test]
		public void SaveLoadRef()
		{
			var env = new PartsTreeSystem.Environment();
			var random = new System.Random();
			var commandManager = new CommandManager();
			var nodeTreeGroup = new NodeTreeGroup();
			nodeTreeGroup.Init(typeof(TestNodeRef), env);

			var instance = Utility.CreateNodeFromNodeTreeGroup(nodeTreeGroup, env);

			commandManager.StartEditFields(nodeTreeGroup, instance, instance.Root, env);

			(instance.Root as TestNodeRef).Ref = instance.Root as Node;
			(instance.Root as TestNodeRef).Refs = new List<Node>();
			(instance.Root as TestNodeRef).Refs.Add(instance.Root as Node);

			commandManager.NotifyEditFields(instance.Root);
			commandManager.EndEditFields(instance.Root, env);

			var json = nodeTreeGroup.Serialize(env);

			var nodeTreeGroup2 = NodeTreeGroup.Deserialize(json);
			var instance2 = Utility.CreateNodeFromNodeTreeGroup(nodeTreeGroup2, env);

			var instanceRoot = instance2.Root as TestNodeRef;

			Assert.True(Helper.IsValueEqual(instanceRoot, instanceRoot.Ref));
			Assert.True(Helper.IsValueEqual(instanceRoot, instanceRoot.Refs[0]));
			Assert.True(Helper.IsValueEqual(instance, instance2));
		}
	}
}