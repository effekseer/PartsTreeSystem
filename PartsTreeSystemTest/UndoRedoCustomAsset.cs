using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PartsTreeSystem;

namespace PartsTreeSystemTest
{
	public class UndoRedoCustomAsset
	{
		class CustomAssetPrimitive : Asset
		{
			public int Value1;
		}


		[SetUp]
		public void Setup()
		{
		}

		[Test]
		public void Primitive()
		{
			var env = new PartsTreeSystem.Environment();
			var random = new System.Random();
			var commandManager = new CommandManager();

			var customAsset = new CustomAssetPrimitive();
			customAsset.Value1 = 1;

			commandManager.StartEditFields(customAsset, env);

			customAsset.Value1 = 2;
			commandManager.NotifyEditFields(customAsset);

			commandManager.EndEditFields(customAsset, env);

			commandManager.Undo(env);

			Assert.AreEqual(customAsset.Value1, 1);

			commandManager.Redo(env);

			Assert.AreEqual(customAsset.Value1, 2);
		}
	}
}