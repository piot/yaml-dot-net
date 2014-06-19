using NUnit.Framework;
using System;
using yaml;
using System.Xml;

namespace tests {

	public static class AssertEx {
		static string ObjectToString (object expected) {
			var x = new System.Xml.Serialization.XmlSerializer(expected.GetType());
			var writer = new System.IO.StringWriter();
			x.Serialize(writer, expected);
			writer.Close();
			return writer.ToString();
		}

		public static void AreEqualByXml (object expected, object actual) {
			var expectedString = ObjectToString(expected);
			var actualString = ObjectToString(actual);

			Assert.AreEqual(expectedString, actualString);
		}
	}

	[TestFixture]
	public class Test {

		public struct SomeStruct {
			public int inDaStruct;
		}

		public class TestSubKlass {
			public int answer;

			public string anotherAnswer { get; set; }

			public Object anotherObject { get; set; }
			public SomeStruct someStruct = new SomeStruct();
		}

		public class TestKlass {
			public int john;
			public string other;

			public string props { get; set; }

			public TestSubKlass subClass;
		}

		[Test]
		public void TestDeserialize () {
			var testData = "   john:34  \n subClass: \n\tanswer: 42 \n\t  anotherAnswer: '99' \nother: 'hejsan svejsan' \n props: 'hello,world'";
			var o = YamlDeserializer.Deserialize<TestKlass>(testData);
			Assert.AreEqual(34, o.john);
			Assert.AreEqual("hejsan svejsan", o.other);
			Assert.AreEqual("hello,world", o.props);
			Assert.AreEqual(42, o.subClass.answer);
			Assert.AreEqual("99", o.subClass.anotherAnswer);
		}

		[Test]
		public void TestSerialize () {
			var o = new TestKlass();
			o.john = 34;
			o.subClass = new TestSubKlass();
			o.subClass.answer = 42;
			o.subClass.someStruct.inDaStruct = 1;
			o.props = "props";
			// o.subClass.anotherObject = new Object();

			o.other = "other";
			var output = YamlSerializer.Serialize(o);

			var back = YamlDeserializer.Deserialize<TestKlass>(output);
			var backOutput = YamlSerializer.Serialize(back);
			AssertEx.AreEqualByXml(o, back);
			Assert.AreEqual(output, backOutput);
		}
	}
}

