/*----------------------------------------------------------------------------------------------------------
 *  Copyright (c) Peter Bjorklund. All rights reserved. https://github.com/piot/yaml-dot-net
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *--------------------------------------------------------------------------------------------------------*/

using System;
using Piot.Yaml;
using NUnit.Framework;
using System.Collections.Generic;

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

		public struct SomeItem
		{
			public int x;
			public int y;

			public override string ToString()
			{
				return $"[SomeItem {x}, {y}]";
			}
		}

		[Flags]
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
			public SomeItem[] someItems;
			public Dictionary<int, SomeItem> lookup;
			public int[] integers;


			public override string ToString()
			{
				return $"[TestSubKlass {lookup}]";
			}

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
		public void TestEnumFlags()
		{
			var testData = @"
john:34  
subClass  : 
  answer: 42 
  anotherAnswer       : '99'
  someEnum: Second | FirstChoice
  someClass:
    inDaStruct:           2

other: hejsan svejsan

props_custom: 'hello,world'
";
			var o = YamlDeserializer.Deserialize<TestKlass>(testData);
			Assert.AreEqual(SomeEnum.Second | SomeEnum.FirstChoice, o.subClass.someEnum);
			Assert.AreEqual(34, o.john);
			Assert.AreEqual("hejsan svejsan", o.other);
			Assert.AreEqual("hello,world", o.props);
			Assert.AreEqual(42, o.subClass.answer);
			Assert.AreEqual("99", o.subClass.AnOTHerAnswer);
		}

		[Test]
		public void TestEnumFlagsWithComma()
		{
			var testData = @"
john:34  
subClass  : 
  answer: 42 
  anotherAnswer       : '99'
  someEnum: Second,FirstChoice
  someClass:
    inDaStruct:           2

other: hejsan svejsan

props_custom: 'hello,world'
";
			var o = YamlDeserializer.Deserialize<TestKlass>(testData);
			Assert.AreEqual(SomeEnum.Second | SomeEnum.FirstChoice, o.subClass.someEnum);
			Assert.AreEqual(34, o.john);
			Assert.AreEqual("hejsan svejsan", o.other);
			Assert.AreEqual("hello,world", o.props);
			Assert.AreEqual(42, o.subClass.answer);
			Assert.AreEqual("99", o.subClass.AnOTHerAnswer);
		}

		[Test]
		public void TestEnumFlagsWithDifferentName()
		{
			var testData = @"
john:34  
subClass  : 
  answer: 42 
  anotherAnswer       : '99'
  someEnum: Second | _third
  someClass:
    inDaStruct:           2

other: hejsan svejsan

props_custom: 'hello,world'
";
			var o = YamlDeserializer.Deserialize<TestKlass>(testData);
			Assert.AreEqual(SomeEnum.Second | SomeEnum.Third, o.subClass.someEnum);
			Assert.AreEqual(34, o.john);
			Assert.AreEqual("hejsan svejsan", o.other);
			Assert.AreEqual("hello,world", o.props);
			Assert.AreEqual(42, o.subClass.answer);
			Assert.AreEqual("99", o.subClass.AnOTHerAnswer);
		}

		[Test]
		public void TestDeserializeWithList()
		{
			var testData = @"
john:34  
subClass  : 
  answer: 42 
  anotherAnswer       : '99'
  someItems:
    - x: 23
    - x: 42
  someClass:
    inDaStruct:           2

other: hejsan svejsan

props_custom: 'hello,world'
";
			var o = YamlDeserializer.Deserialize<TestKlass>(testData);
			Assert.AreEqual(2, o.subClass.someItems.Length);
			Assert.AreEqual(23, o.subClass.someItems[0].x);
			Assert.AreEqual(42, o.subClass.someItems[1].x);
			Assert.AreEqual(34, o.john);
			Assert.AreEqual("hejsan svejsan", o.other);
			Assert.AreEqual("hello,world", o.props);
			Assert.AreEqual(42, o.subClass.answer);
			Assert.AreEqual("99", o.subClass.AnOTHerAnswer);
		}


		[Test]
		public void TestDeserializeWithMinimalList()
		{
			var testData = @"
subClass: 
  someItems:
  - x: 23
  - x: 42
";
			var o = YamlDeserializer.Deserialize<TestKlass>(testData);
			Assert.AreEqual(2, o.subClass.someItems.Length);
			Assert.AreEqual(23, o.subClass.someItems[0].x);
			Assert.AreEqual(42, o.subClass.someItems[1].x);
		}


		[Test]
		public void TestDeserializeWithMinimalList2()
		{
			var testData = @"
subClass: 
  someItems:
    -     x: 399
    -     x: 42
";
			var o = YamlDeserializer.Deserialize<TestKlass>(testData);
			Assert.AreEqual(2, o.subClass.someItems.Length);
			Assert.AreEqual(399, o.subClass.someItems[0].x);
			Assert.AreEqual(42, o.subClass.someItems[1].x);
		}


		[Test]
		public void TestDeserializeWithMinimalListNoIndent()
		{
			var testData = @"
subClass: 
  someItems:
  - x: 399
  - x: 42
";
			var o = YamlDeserializer.Deserialize<TestKlass>(testData);
			Assert.AreEqual(2, o.subClass.someItems.Length);
			Assert.AreEqual(399, o.subClass.someItems[0].x);
			Assert.AreEqual(42, o.subClass.someItems[1].x);
		}

		[Test]
		public void TestDeserializeWithMinimalIntegerListIndent()
		{
			var testData = @"
subClass: 
  integers:
    - 0
    - 00
    - 10
    - -20
    - 399
    - 42
";
			var o = YamlDeserializer.Deserialize<TestKlass>(testData);
			Assert.AreEqual(6, o.subClass.integers.Length);
			Assert.AreEqual(0, o.subClass.integers[0]);
			Assert.AreEqual(0, o.subClass.integers[1]);
			Assert.AreEqual(10, o.subClass.integers[2]);
			Assert.AreEqual(-20, o.subClass.integers[3]);
			Assert.AreEqual(399, o.subClass.integers[4]);
			Assert.AreEqual(42, o.subClass.integers[5]);
		}

		public struct BoatCollection
		{
			public Boat[] boats;
		}

		public struct Boat
		{
			public string id;
			public string description;

			public Seat[] seats;
		}


		public struct Seat
		{
			public string id;
		}


		[Test]
		public void TestDeserializeWithMinimalIntegerSubTask()
		{
			var testData = @"
boats:
  - id: firstBoat
    description: 'This is an interesting boat'
    seats:
    - id: seat1
    - id: seat2
    - id: seat3

  - id: anotherBoat
    seats:
      - id: seatb1
      - id: seatb2
      - id: seatb3
";
			var o = YamlDeserializer.Deserialize<BoatCollection>(testData);
			Assert.AreEqual(2, o.boats.Length);
			Assert.AreEqual("seat1", o.boats[0].seats[0].id);
			Assert.AreEqual("seat2", o.boats[0].seats[1].id);
			Assert.AreEqual("seatb3", o.boats[1].seats[2].id);
		}

		[Test]
		public void TestDeserializeWithMinimalIntegerListNoIndent()
		{
			var testData = @"
subClass: 
  integers:
    - 399
    - -42
";
			var o = YamlDeserializer.Deserialize<TestKlass>(testData);
			Assert.AreEqual(2, o.subClass.integers.Length);
			Assert.AreEqual(399, o.subClass.integers[0]);
			Assert.AreEqual(-42, o.subClass.integers[1]);
		}


		[Test]
		public void TestDeserializeWithMinimalListAndDictionary()
		{
			var testData = @"
props: 'props'
john: 34
other: 'other'
isItTrue: true
subClass:
  anotherAnswer: '42'
  answer: 42
  someClass:
    inDaStruct: 1
  someEnum: FirstChoice
  someItems:
    - x: 399
      y: -1024
    - x: 42
  lookup:
    2:
      x: 909
      y: -1234
  f: -22.42
";
			var o = YamlDeserializer.Deserialize<TestKlass>(testData);
			Assert.AreEqual(2, o.subClass.someItems.Length);
			Assert.AreEqual(399, o.subClass.someItems[0].x);
			Assert.AreEqual(42, o.subClass.someItems[1].x);
			Assert.AreEqual(-22.42, o.subClass.f, 0.0001f);
			Assert.AreEqual(909, o.subClass.lookup[2].x);
			Assert.AreEqual(-1234, o.subClass.lookup[2].y);
		}


		[Test]
		public void TestDeserializeWithWrongHyphenList()
		{
			var testData = @"
john:34  
subClass  : 
  answer: 42 
  anotherAnswer       : '99'
  someItems:
 - x: 23
 - x: 42
  someClass:
    inDaStruct:           2

other: hejsan svejsan

props_custom: 'hello,world'
";
			var o = Assert.Throws<IndentationException>(() => YamlDeserializer.Deserialize<TestKlass>(testData));
		}

		[Test]
		public void TestDeserializeWithDictionary()
		{
			var testData = @"
subClass: 
  lookup:
    2:
      x: 42
    3:
      x: 101   
other: hejsan svejsan
";
			var o = YamlDeserializer.Deserialize<TestKlass>(testData);
			Assert.AreEqual(42, o.subClass.lookup[2].x);
			Assert.AreEqual(101, o.subClass.lookup[3].x);
			Assert.AreEqual("hejsan svejsan", o.other);
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
			Assert.AreEqual(0xffa800, o.someInt);
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

		//[Test]
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
			o.subClass.someItems = new SomeItem[1];
			o.subClass.lookup = new();

			o.other = "other";
			var output = YamlSerializer.Serialize(o);
			Console.WriteLine("Output:{0}", output);
			var back = YamlDeserializer.Deserialize<TestKlass>(output);
			var backOutput = YamlSerializer.Serialize(back);
			AssertEx.AreEqualByXml(o, back);
			Assert.AreEqual(output, backOutput);
		}

		//[Test]
		public void TestSerializeWithList()
		{
			var o = new TestKlass();
			o.john = 34;
			o.subClass = new TestSubKlass();
			o.subClass.answer = 42;
			o.subClass.f = -22.42f;
			o.subClass.someClass.inDaStruct = 1;
			o.props = "props";
			o.isItTrue = true;
			o.subClass.someItems = new SomeItem[]
			{
				new() { x = 399 },
				new() { x = 42 }
			};

			o.subClass.lookup = new()
			{
				{ 2, new() { x = 909 } }
			};

			o.other = "other";
			var output = YamlSerializer.Serialize(o);
			Console.WriteLine("Output:{0}", output);
			var back = YamlDeserializer.Deserialize<TestKlass>(output);
			var backOutput = YamlSerializer.Serialize(back);
			AssertEx.AreEqualByXml(o, back);
			Assert.AreEqual(output, backOutput);
		}


		//[Test]
		public void TestSerializeWithDictionary()
		{
			var o = new TestKlass();
			o.john = 34;
			o.subClass = new TestSubKlass();
			o.subClass.answer = 42;
			o.subClass.f = -22.42f;
			o.subClass.someClass.inDaStruct = 1;
			o.props = "props";
			o.isItTrue = true;
			o.subClass.someItems = new SomeItem[]
			{
				new() { x = 399 },
				new() { x = 42 }
			};

			o.subClass.lookup = new Dictionary<int, SomeItem>()
			{
				{
					1, new()
					{
						x = -101
					}
				},
				{
					-1, new()
					{
						x = 99
					}
				}
			};

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