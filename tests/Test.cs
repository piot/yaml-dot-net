using NUnit.Framework;
using System;
using yaml;

namespace tests {
	[TestFixture]
	public class Test {

		class TestSubKlass {
			public int answer;

			public string anotherAnswer { get; set; }

			public Object anotherObject { get; set; }
		}

		class TestKlass {
			public int john;
			public string other;

			public string props { get; set; }

			public TestSubKlass subClass;
		}

		[Test]
		public void TestParse () {
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

		[Test]
		public void TestWrite () {
			var o = new TestKlass();
			o.john = 34;
			o.subClass = new TestSubKlass();
			o.subClass.answer = 42;
			o.props = "props";

			o.other = "other";
			var writer = new YamlWriter();
			var output = writer.Write(o);
			Console.WriteLine("output:\n" + output);
		}
	}
}

