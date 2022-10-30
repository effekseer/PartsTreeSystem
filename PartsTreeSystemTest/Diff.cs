using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PartsTreeSystem;

namespace PartsTreeSystemTest
{
	struct TestStruct1
	{
		public float A;
		public float B;
		public float C;
		public TestStruct2 St;
	}

	struct TestStruct2
	{
		public float A;
		public float B;
		public float C;
	}

	[System.Serializable]
	class TestClass1
	{
		public float A;
		public float B;
		public float C;
	}

	class TestList1
	{
		public System.Collections.Generic.List<int> Values = new System.Collections.Generic.List<int>();
	}

	class TestNodeStruct : Node
	{
		public TestStruct1 Struct1;
	}

	class TestProperty
	{
		[SerializeField]
		public float Value1 { get; set; }

		public float Value2 { get; set; }
	}

	class TestPrivate
	{
		[SerializeField]
		float value;

		public void SetValue(float v)
		{
			value = v;
		}
	}

	public class Diff
	{
		[SetUp]
		public void Setup()
		{

		}

		[Test]
		public void DiffPrimitive()
		{
			var env = new Environment();
			var v = new TestNodePrimitive();

			var before = new FieldState();
			before.Store(v, env);

			v.ValueInt32 = 5;

			var after = new FieldState();
			after.Store(v, env);

			{
				var diff = after.GenerateDifference(before);
				var pair = diff.Modifications.First();
				var group = pair.Target;
				var field = group.Keys[0];
				Assert.AreEqual(1, diff.Modifications.Count);
				Assert.AreEqual("ValueInt32", field.Name);
				Assert.AreEqual(5, pair.Value);
			}

			{
				var diff = before.GenerateDifference(after);
				var pair = diff.Modifications.First();
				var group = pair.Target;
				var field = group.Keys[0];
				Assert.AreEqual(1, diff.Modifications.Count);
				Assert.AreEqual("ValueInt32", field.Name);
				Assert.AreEqual(0, pair.Value);
			}
		}

		[Test]
		public void DiffStruct()
		{
			var env = new Environment();
			var v = new TestNodeStruct();
			v.Struct1.A = 1.0f;
			v.Struct1.St.B = 3.0f;

			var before = new FieldState();
			before.Store(v, env);
			v.Struct1.A = 2.0f;
			v.Struct1.St.B = 4.0f;

			var after = new FieldState();
			after.Store(v, env);

			var diff = before.GenerateDifference(after);
			Assert.AreEqual(2, diff.Modifications.Count);
			Assert.AreEqual(1.0f, diff.Modifications.First().Value);
		}

		[Test]
		public void DiffClass()
		{
			var env = new Environment();
			var v = new TestNodeClass();
			v.Class1_1 = new TestClass1();
			v.Class1_1.A = 1.0f;

			var before = new FieldState();
			before.Store(v, env);
			v.Class1_1.A = 2.0f;

			var after = new FieldState();
			after.Store(v, env);

			var diff = before.GenerateDifference(after);
			Assert.AreEqual(1, diff.Modifications.Count);
			Assert.AreEqual(1.0f, diff.Modifications.First().Value);
		}

		[Test]
		public void DiffList()
		{
			var env = new Environment();
			var v = new TestList1();

			var before = new FieldState();
			before.Store(v, env);
			v.Values.Add(1);

			var after = new FieldState();
			after.Store(v, env);

			var diff1 = before.GenerateDifference(after);
			Assert.AreEqual(1, diff1.Modifications.Count);

			var diff2 = after.GenerateDifference(before);
			Assert.AreEqual(2, diff2.Modifications.Count);
		}

		[Test]
		public void DiffProperty()
		{
			var env = new Environment();
			var v = new TestProperty();
			v.Value1 = 3;
			v.Value2 = 4;

			var before = new FieldState();
			before.Store(v, env);

			v.Value1 = 6;
			v.Value2 = 8;

			var after = new FieldState();
			after.Store(v, env);

			var diff = before.GenerateDifference(after);
			Assert.AreEqual(1, diff.Modifications.Count);
			Assert.AreEqual(3.0f, diff.Modifications.First().Value);
		}

		[Test]
		public void DiffPrivate()
		{
			var env = new Environment();
			var v = new TestPrivate();
			v.SetValue(2);

			var before = new FieldState();
			before.Store(v, env);
			v.SetValue(4);

			var after = new FieldState();
			after.Store(v, env);

			var diff = before.GenerateDifference(after);
			Assert.AreEqual(1, diff.Modifications.Count);
			Assert.AreEqual(2.0f, diff.Modifications.First().Value);
		}

		[Test]
		public void DiffEnum()
		{
			var env = new Environment();
			var v = new TestClassEnum();
			v.Value1 = BasicEnum1.B;

			var before = new FieldState();
			before.Store(v, env);
			v.Value1 = BasicEnum1.A;

			var after = new FieldState();
			after.Store(v, env);

			var diff = before.GenerateDifference(after);
			Assert.AreEqual(1, diff.Modifications.Count);
			Assert.AreEqual(BasicEnum1.B, diff.Modifications.First().Value);
		}
	}
}