/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Peter Bjorklund. All rights reserved. https://github.com/piot/yaml-dot-net
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using System;
using Piot.Yaml;
using NUnit.Framework;

namespace tests
{
	public static class AssertEx
	{
		static string ObjectToString(object expected)
		{
			var x = new System.Xml.Serialization.XmlSerializer(expected.GetType());
			var writer = new System.IO.StringWriter();
			x.Serialize(writer, expected);
			writer.Close();
			return writer.ToString();
		}

		public static void AreEqualByXml(object expected, object actual)
		{
			var expectedString = ObjectToString(expected);
			var actualString = ObjectToString(actual);

			Assert.AreEqual(expectedString, actualString);
		}
	}

	[TestFixture]
	public class Test
	{
		public struct SomeClass
		{
			public uint inDaStruct;
		}

		public enum SomeEnum
		{
			FirstChoice,
			Second,
			[YamlProperty("_third")] Third
		}

		public struct TestSubKlass
		{
			public int answer;

			[YamlProperty("anotherAnswer")] public string AnOTHerAnswer { get; set; }

			public SomeClass someClass;
			public SomeEnum someEnum;
			public float f;
		}

		public struct TestKlass
		{
			public int john;
			public string other;

			[YamlProperty("props_custom")] public string props { get; set; }
			public bool isItTrue;

			public TestSubKlass subClass;
		}

		public struct TestIntKlass
		{
			public uint someInt;
			public int somethingElse;
		}

		public class TestStandaloneIntKlass
		{
			public uint someInt;
			public int somethingElse;
		}


		[Test]
		public void TestDeserialize()
		{
			var testData =
				"john:34  \nsubClass: \n  answer: 42 \n  anotherAnswer: '99'\nother: 'hejsan svejsan' \nprops_custom: 'hello,world'";
			var o = YamlDeserializer.Deserialize<TestKlass>(testData);
			Assert.AreEqual(34, o.john);
			Assert.AreEqual("hejsan svejsan", o.other);
			Assert.AreEqual("hello,world", o.props);
			Assert.AreEqual(42, o.subClass.answer);
			Assert.AreEqual("99", o.subClass.AnOTHerAnswer);
		}

		[Test]
		public void TestDeserializeWithSpacesBeforeColon()
		{
			var testData = @"
john:34  
subClass  : 
  answer: 42 
  anotherAnswer       : '99'
  someClass:
    inDaStruct:           2

other: hejsan svejsan

props_custom: 'hello,world'
";
			var o = YamlDeserializer.Deserialize<TestKlass>(testData);
			Assert.AreEqual(34, o.john);
			Assert.AreEqual("hejsan svejsan", o.other);
			Assert.AreEqual("hello,world", o.props);
			Assert.AreEqual(42, o.subClass.answer);
			Assert.AreEqual("99", o.subClass.AnOTHerAnswer);
		}

		[Test]
		public void TestEnum()
		{
			var testData = @"
john:34  
subClass  : 
  answer: 42 
  anotherAnswer       : '99'
  someEnum: Second
  someClass:
    inDaStruct:           2

other: hejsan svejsan

props_custom: 'hello,world'
";
			var o = YamlDeserializer.Deserialize<TestKlass>(testData);
			Assert.AreEqual(SomeEnum.Second, o.subClass.someEnum);
			Assert.AreEqual(34, o.john);
			Assert.AreEqual("hejsan svejsan", o.other);
			Assert.AreEqual("hello,world", o.props);
			Assert.AreEqual(42, o.subClass.answer);
			Assert.AreEqual("99", o.subClass.AnOTHerAnswer);
		}

		[Test]
		public void TestEnumWithDifferentName()
		{
			var testData = @"
john:34  
subClass  : 
  answer: 42 
  anotherAnswer       : '99'
  someEnum: _third
  someClass:
    inDaStruct:           2

other: hejsan svejsan

props_custom: 'hello,world'
";
			var o = YamlDeserializer.Deserialize<TestKlass>(testData);
			Assert.AreEqual(SomeEnum.Third, o.subClass.someEnum);
			Assert.AreEqual(34, o.john);
			Assert.AreEqual("hejsan svejsan", o.other);
			Assert.AreEqual("hello,world", o.props);
			Assert.AreEqual(42, o.subClass.answer);
			Assert.AreEqual("99", o.subClass.AnOTHerAnswer);
		}


		[Test]
		public void TestDeserializeString()
		{
			var testData = "anotherAnswer: \"example\"";
			var o = YamlDeserializer.Deserialize<TestSubKlass>(testData);
			Assert.AreEqual("example", o.AnOTHerAnswer);
		}

		[Test]
		public void TestDeserializeString2()
		{
			var testData = "anotherAnswer: example";
			var o = YamlDeserializer.Deserialize<TestSubKlass>(testData);
			Assert.AreEqual("example", o.AnOTHerAnswer);
		}

		[Test]
		public void TestAutomaticScalar()
		{
			var testData = @"
other: example
subClass:
  anotherAnswer: yes";
			var o = YamlDeserializer.Deserialize<TestKlass>(testData);
			Assert.AreEqual("example", o.other);
			Assert.AreEqual("yes", o.subClass.AnOTHerAnswer);
		}


		[Test]
		public void TestAutomaticScalarWithSpaces()
		{
			var testData = @"
other: example
subClass:
  anotherAnswer: yes";
			var o = YamlDeserializer.Deserialize<TestKlass>(testData);
			Assert.AreEqual("example", o.other);
			Assert.AreEqual("yes", o.subClass.AnOTHerAnswer);
		}

		[Test]
		public void TestIntegerToString()
		{
			var testData = "anotherAnswer: 0x232323";
			var o = YamlDeserializer.Deserialize<TestSubKlass>(testData);
			Assert.AreEqual("2302755", o.AnOTHerAnswer);
		}

		[Test]
		public void TestHexInteger()
		{
			var testData = "someInt: 0xffa800\n  ";
			var o = YamlDeserializer.Deserialize<TestIntKlass>(testData);
			Assert.AreEqual(16754688, o.someInt);
		}

		[Test]
		public void TestStandaloneHexInteger()
		{
			var testData = "someInt: 0xffa800\n  ";
			var o = YamlDeserializer.Deserialize<TestStandaloneIntKlass>(testData);
			Assert.AreEqual(16754688, o.someInt);
		}

		[Test]
		public void TestInteger()
		{
			var testData = "someInt: 1";
			var o = YamlDeserializer.Deserialize<TestIntKlass>(testData);
			Assert.AreEqual(1, o.someInt);
		}


		[Test]
		public void TestInteger2()
		{
			var testData = "someInt: 1\nsomethingElse: 5 ";
			var o = YamlDeserializer.Deserialize<TestIntKlass>(testData);
			Assert.AreEqual(1, o.someInt);
			Assert.AreEqual(5, o.somethingElse);
		}

		[Test]
		public void TestIntegerWithComment()
		{
			var testData = "someInt: 1 # some comment here";
			var o = YamlDeserializer.Deserialize<TestIntKlass>(testData);
			Assert.AreEqual(1, o.someInt);
		}

		[Test]
		public void TestSerialize()
		{
			var o = new TestKlass();
			o.john = 34;
			o.subClass = new TestSubKlass();
			o.subClass.answer = 42;
			o.subClass.f = -22.42f;
			o.subClass.someClass.inDaStruct = 1;
			o.props = "props";
			o.isItTrue = true;

			o.other = "other";
			var output = YamlSerializer.Serialize(o);
			Console.WriteLine("Output:{0}", output);
			var back = YamlDeserializer.Deserialize<TestKlass>(output);
			var backOutput = YamlSerializer.Serialize(back);
			AssertEx.AreEqualByXml(o, back);
			Assert.AreEqual(output, backOutput);
		}

		public struct SomeColor
		{
			public string color;
		};

		[Test]
		public void TestString()
		{
			var s = "color:    'kind of red'";
			var c = YamlDeserializer.Deserialize<SomeColor>(s);
			Assert.AreEqual("kind of red", c.color);
		}
	}
}