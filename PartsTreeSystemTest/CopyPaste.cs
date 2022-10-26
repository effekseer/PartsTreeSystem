using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PartsTreeSystem;

namespace PartsTreeSystemTest
{
	class CopyPaste
	{
		[Test]
		public void OneNestedTreeTest()
		{
			{
				OneNestedTreeTestParameter param;

				param.rootChildrenCount = 2;
				param.copiedChildCount = 1;
				param.edittedIndex = 0;
				param.pasteedIndex = 1;

				OneNestedTreeTest(param);
			}

			{
				OneNestedTreeTestParameter param;

				param.rootChildrenCount = 3;
				param.copiedChildCount = 1;
				param.edittedIndex = 0;
				param.pasteedIndex = 1;

				OneNestedTreeTest(param);
			}

			{
				OneNestedTreeTestParameter param;

				param.rootChildrenCount = 3;
				param.copiedChildCount = 1;
				param.edittedIndex = 0;
				param.pasteedIndex = 2;

				OneNestedTreeTest(param);
			}

			{
				OneNestedTreeTestParameter param;

				param.rootChildrenCount = 3;
				param.copiedChildCount = 2;
				param.edittedIndex = 0;
				param.pasteedIndex = 2;

				OneNestedTreeTest(param);
			}
		}

		struct OneNestedTreeTestParameter
		{
			public int rootChildrenCount;
			public int copiedChildCount;
			public int edittedIndex;
			public int pasteedIndex;
		}

		void OneNestedTreeTest(in OneNestedTreeTestParameter param)
		{
			int rootChildrenCount = param.rootChildrenCount;
			int copiedChildCount = param.copiedChildCount;
			int edittedIndex = param.edittedIndex;
			int pasteedIndex = param.pasteedIndex;

			const int edittedValue = 32;

			var env = new Environment();
			var commandManager = new CommandManager();
			var nodeTreeGroup = new NodeTreeAsset();
			nodeTreeGroup.Init(typeof(TestNodePrimitive), env);
			var instance = Utility.CreateNodeFromNodeTreeGroup(nodeTreeGroup, env);

			List<int> rootChildrenIds = new List<int>();

			for (int i = 0; i < rootChildrenCount; i++)
			{
				var id = commandManager.AddNode(nodeTreeGroup, instance, instance.Root.InstanceID, typeof(TestNodePrimitive), env);
				rootChildrenIds.Add(id);
			}

			for (int i = 0; i < copiedChildCount; i++)
			{
				commandManager.AddNode(nodeTreeGroup, instance, rootChildrenIds[0], typeof(TestNodePrimitive), env);
			}

			{
				var editNode = instance.Root.GetChildren().ElementAt(0).GetChildren().ElementAt(edittedIndex) as TestNodePrimitive;
				commandManager.StartEditFields(nodeTreeGroup, instance, editNode, env);
				editNode.ValueInt32 = edittedValue;
				commandManager.NotifyEditFields(editNode);
				commandManager.EndEditFields(editNode, env);
			}

			var data = nodeTreeGroup.Copy(rootChildrenIds[0], env);

			commandManager.Paste(nodeTreeGroup, instance, rootChildrenIds[pasteedIndex], data, env);
			nodeTreeGroup.Paste(data, rootChildrenIds[1], env);

			Assert.AreEqual(instance.Root.GetChildren().ElementAt(pasteedIndex).GetChildren().Count, copiedChildCount);
			Assert.AreEqual((instance.Root.GetChildren().ElementAt(pasteedIndex).GetChildren().ElementAt(edittedIndex) as TestNodePrimitive).ValueInt32, edittedValue);

			commandManager.Undo(env);
			Assert.AreEqual(instance.Root.GetChildren().ElementAt(pasteedIndex).GetChildren().Count, 0);

			commandManager.Redo(env);
			Assert.AreEqual(instance.Root.GetChildren().ElementAt(pasteedIndex).GetChildren().Count, copiedChildCount);
			Assert.AreEqual((instance.Root.GetChildren().ElementAt(pasteedIndex).GetChildren().ElementAt(edittedIndex) as TestNodePrimitive).ValueInt32, edittedValue);
		}
	}
}