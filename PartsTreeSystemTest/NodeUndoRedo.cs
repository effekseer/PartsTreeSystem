using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PartsTreeSystem;

namespace PartsTreeSystemTest
{
	public class NodeUndoRedo
	{
		[SetUp]
		public void Setup()
		{
		}

		[Test]
		public void AddChild()
		{
			var env = new PartsTreeSystem.Environment();
			var commandManager = new CommandManager();
			var nodeTreeGroup = new NodeTreeAsset();
			nodeTreeGroup.Init(typeof(Node), env);
			var instance = Utility.CreateNodeFromNodeTreeGroup(nodeTreeGroup, env);

			commandManager.AddNode(nodeTreeGroup, instance, instance.Root.InstanceID, typeof(Node), env);

			commandManager.Undo(env);
			Assert.AreEqual(0, instance.Root.GetChildren().Count);

			commandManager.Redo(env);
			Assert.AreEqual(1, instance.Root.GetChildren().Count);
			Assert.IsTrue(instance.Root.GetChildren().First() != null);
		}

		[Test]
		public void Merging()
		{
			var env = new PartsTreeSystem.Environment();
			var commandManager = new CommandManager();
			var nodeTreeGroup = new NodeTreeAsset();
			nodeTreeGroup.Init(typeof(Node), env);
			var instance = Utility.CreateNodeFromNodeTreeGroup(nodeTreeGroup, env);

			commandManager.PushMergingBlock();
			commandManager.AddNode(nodeTreeGroup, instance, instance.Root.InstanceID, typeof(Node), env);
			commandManager.AddNode(nodeTreeGroup, instance, instance.Root.InstanceID, typeof(Node), env);
			commandManager.PopMergingBlock();

			commandManager.Undo(env);
			Assert.AreEqual(0, instance.Root.GetChildren().Count);

			commandManager.Redo(env);
			Assert.AreEqual(2, instance.Root.GetChildren().Count);
		}


		[Test]
		public void EditFieldPrimitive()
		{
			EditFieldTest<TestNodePrimitive>(true);
		}

		[Test]
		public void EditFieldList()
		{
			EditFieldTest<TestNode_ListValue>(false);
		}

		[Test]
		public void EditFieldListClass()
		{
			EditFieldTest<TestNode_ListClass>(false);
		}

		[Test]
		public void EditFieldListClassNotSerializable()
		{
			EditFieldTest<TestNode_List<TestClassNotSerializable>>(false);
		}


		void EditFieldTest<T>(bool canMergeChanges)
		{
			var env = new PartsTreeSystem.Environment();
			var random = new System.Random();
			var commandManager = new CommandManager();
			var nodeTreeGroup = new NodeTreeAsset();
			nodeTreeGroup.Init(typeof(T), env);
			var instance = Utility.CreateNodeFromNodeTreeGroup(nodeTreeGroup, env);

			commandManager.StartEditFields(nodeTreeGroup, instance, instance.Root, env);

			var assignedUnedit = Helper.AssignRandomField(random, true, ref instance.Root);

			commandManager.NotifyEditFields(instance.Root);

			commandManager.EndEditFields(instance.Root, env);

			commandManager.SetFlagToBlockMergeCommands();

			commandManager.StartEditFields(nodeTreeGroup, instance, instance.Root, env);

			var assignedEdit1 = Helper.AssignRandomField(random, true, ref instance.Root);

			commandManager.NotifyEditFields(instance.Root);

			commandManager.EndEditFields(instance.Root, env);

			commandManager.Undo(env);

			Helper.AreEqual(assignedUnedit, ref instance.Root);

			commandManager.Redo(env);

			Helper.AreEqual(assignedEdit1, ref instance.Root);

			commandManager.StartEditFields(nodeTreeGroup, instance, instance.Root, env);

			var assignedEdit2 = Helper.AssignRandomField(random, true, ref instance.Root);

			commandManager.NotifyEditFields(instance.Root);

			commandManager.EndEditFields(instance.Root, env);

			if (!canMergeChanges)
			{
				commandManager.SetFlagToBlockMergeCommands();
			}

			commandManager.StartEditFields(nodeTreeGroup, instance, instance.Root, env);

			var assignedEdit3 = Helper.AssignRandomField(random, true, ref instance.Root);

			commandManager.NotifyEditFields(instance.Root);

			commandManager.EndEditFields(instance.Root, env);

			commandManager.Undo(env);

			if (canMergeChanges)
			{
				Helper.AreEqual(assignedEdit1, ref instance.Root);

				commandManager.Redo(env);
			}
			else
			{
				Helper.AreEqual(assignedEdit2, ref instance.Root);

				commandManager.Redo(env);
			}

			Helper.AreEqual(assignedEdit3, ref instance.Root);
		}

		[Test]
		public void PrefabEditFieldPrimitive()
		{
			PrefabEditFieldTest<TestNodePrimitive>(true);
		}

		[Test]
		public void PrefabEditFieldList()
		{
			PrefabEditFieldTest<TestNode_ListValue>(false);
		}

		[Test]
		public void PrefabEditFieldListClass()
		{
			PrefabEditFieldTest<TestNode_ListClass>(false);
		}

		[Test]
		public void PrefabEditFieldListClassNotSerializable()
		{
			PrefabEditFieldTest<TestNode_List<TestClassNotSerializable>>(false);
		}

		void PrefabEditFieldTest<T>(bool canMergeChanges)
		{
			var env = new MultiNodeTreeEnvironment();
			var random = new System.Random();

			var nodeTreeGroupChild = new NodeTreeAsset();
			var nodeTreeGroup = new NodeTreeAsset();

			env.NodeTrees.Add("C:/test/Tree1", nodeTreeGroupChild);
			env.NodeTrees.Add("C:/test/Tree2", nodeTreeGroup);

			nodeTreeGroupChild.Init(typeof(T), env);
			var instanceChild = Utility.CreateNodeFromNodeTreeGroup(nodeTreeGroupChild, env);

			var commandManagerChild = new CommandManager();
			commandManagerChild.StartEditFields(nodeTreeGroupChild, instanceChild, instanceChild.Root, env);

			var assignedEditChild = Helper.AssignRandomField(random, true, ref instanceChild.Root);

			commandManagerChild.NotifyEditFields(instanceChild.Root);

			commandManagerChild.EndEditFields(instanceChild.Root, env);

			var id = nodeTreeGroup.Init(typeof(Node), env);
			nodeTreeGroup.AddNodeTreeGroup(id, nodeTreeGroupChild, env);
			var instance = Utility.CreateNodeFromNodeTreeGroup(nodeTreeGroup, env);

			{
				var node = instance.Root.GetChildren().First();
				Helper.AreEqual(assignedEditChild, ref node);
			}

			var commandManager = new CommandManager();

			commandManager.StartEditFields(nodeTreeGroup, instance, instance.Root, env);

			var assignedUnedit = Helper.AssignRandomField(random, true, ref instance.Root);

			commandManager.NotifyEditFields(instance.Root);

			commandManager.EndEditFields(instance.Root, env);

			commandManager.StartEditFields(nodeTreeGroup, instance, instance.Root.GetChildren().First(), env);

			var childTemp = instance.Root.GetChildren().First();
			var assignedChildUnedit = Helper.AssignRandomField(random, true, ref childTemp);

			commandManager.NotifyEditFields(instance.Root.GetChildren().First());

			commandManager.EndEditFields(instance.Root.GetChildren().First(), env);

			commandManager.SetFlagToBlockMergeCommands();

			commandManager.StartEditFields(nodeTreeGroup, instance, instance.Root, env);

			var assignedEdit1 = Helper.AssignRandomField(random, true, ref instance.Root);

			commandManager.NotifyEditFields(instance.Root);

			commandManager.EndEditFields(instance.Root, env);

			commandManager.StartEditFields(nodeTreeGroup, instance, instance.Root.GetChildren().First(), env);

			childTemp = instance.Root.GetChildren().First();
			var assignedChildEdit = Helper.AssignRandomField(random, true, ref childTemp);

			commandManager.NotifyEditFields(instance.Root.GetChildren().First());

			commandManager.EndEditFields(instance.Root.GetChildren().First(), env);

			commandManager.Undo(env);

			commandManager.Undo(env);

			Helper.AreEqual(assignedUnedit, ref instance.Root);

			childTemp = instance.Root.GetChildren().First();
			Helper.AreEqual(assignedChildUnedit, ref childTemp);

			commandManager.Redo(env);

			commandManager.Redo(env);

			Helper.AreEqual(assignedEdit1, ref instance.Root);

			childTemp = instance.Root.GetChildren().First();
			Helper.AreEqual(assignedChildEdit, ref childTemp);

			commandManager.StartEditFields(nodeTreeGroup, instance, instance.Root, env);

			var assignedEdit2 = Helper.AssignRandomField(random, true, ref instance.Root);

			commandManager.NotifyEditFields(instance.Root);

			commandManager.EndEditFields(instance.Root, env);

			commandManager.StartEditFields(nodeTreeGroup, instance, instance.Root, env);

			var assignedEdit3 = Helper.AssignRandomField(random, true, ref instance.Root);

			commandManager.NotifyEditFields(instance.Root);

			commandManager.EndEditFields(instance.Root, env);

			commandManager.Undo(env);

			if (canMergeChanges)
			{
				Helper.AreEqual(assignedEdit1, ref instance.Root);

				commandManager.Redo(env);
			}
			else
			{
				Helper.AreEqual(assignedEdit2, ref instance.Root);

				commandManager.Redo(env);
			}

			Helper.AreEqual(assignedEdit3, ref instance.Root);
		}
	}
}