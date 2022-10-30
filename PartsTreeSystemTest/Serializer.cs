using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PartsTreeSystem;

namespace PartsTreeSystemTest
{
	class Serializer
	{
		[SetUp]
		public void Setup()
		{
		}

		[Test]
		public void Enum()
		{
			var env = new Environment();
			var before = new TestClassEnum();
			before.Value1 = BasicEnum1.B;

			var data = JsonSerializer.Serialize(before, env);
			var after = JsonSerializer.Deserialize<TestClassEnum>(data, env);

			Assert.AreEqual(before.Value1, after.Value1);
		}
	}
}