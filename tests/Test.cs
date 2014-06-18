using NUnit.Framework;
using System;
using yaml;

namespace tests {
	[TestFixture]
	public class Test {

		class TestSubKlass {
			public int answer;

			public string anotherAnswer { get; set; }
		}

		class TestKlass {
			public int john;
			public string other;

			public string props { get; set; }

			public TestSubKlass subClass;
		}

		[Test]
		public void TestCase () {
			var testData = "   john:34  \n subClass: \n\tanswer: 42 \n\t  anotherAnswer: '99' \nother: 'hejsan svejsan' \n props: 'hello,world'";
			var parser = new YamlParser();
			var o = new TestKlass();

			parser.Parse(o, testData);
			Assert.AreEqual(34, o.john);
			Assert.AreEqual("hejsan svejsan", o.other);
			Assert.AreEqual("hello,world", o.props);
			Assert.AreEqual(42, o.subClass.answer);
			Assert.AreEqual("99", o.subClass.anotherAnswer);
		}
	}
}

