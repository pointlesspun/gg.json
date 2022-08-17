using gg.json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace gg.json.tests
{
    [TestClass]
    public class JsonConfigTests
    {
        class TestObject
        {
            public string f1 = "foo";

            public int P1 { get; set; } = 42;
        }
        class NestedTestObject
        {
            public bool f1 = true;

            public TestObject P1 { get; set; }
        }

        class TestArrayObj
        {
            public int[] array;
        }

        class TestListObj
        {
            public List<int> GenericIntList { get; set; }
            public ArrayList arrayList;
        }

        private static readonly JsonConfig.Options TestOptions = new JsonConfig.Options()
        {
            Aliases = new Dictionary<string, Type>()
            {
                { nameof(TestObject), typeof(TestObject) },
                { nameof(NestedTestObject), typeof(NestedTestObject) },
                { nameof(TestArrayObj), typeof(TestArrayObj) },
            }
        };


        [TestMethod]
        public void ToDotNetValueTests()
        {
            var jsonString = "{\"p1\": 1, \"p2\": 0.42, \"p3\": true}";
            var document = JsonSerializer.Deserialize<JsonElement>(jsonString);

            object value = document.GetProperty("p1").MapValue<int>();

            Assert.IsTrue(value is int);
            Assert.IsTrue((int)value == 1);

            value = document.GetProperty("p2").MapValue<float>();

            Assert.IsTrue(value is float);

            var floatValue = (float)value;

            Assert.IsTrue(Math.Abs(floatValue - 0.42f) < float.Epsilon);

            value = document.GetProperty("p3").MapValue<bool>();

            Assert.IsTrue(value is bool);
            Assert.IsTrue((bool)value == true);
        }

        [TestMethod]
        public void DeserializeObjectTests()
        {
            var jsonString = $"{{ \"{JsonConfig.DefaultTypeTag}\": \"TestObject\", \"f1\":\"bar\", \"P1\":-1 }}";

            var document = JsonSerializer.Deserialize<JsonElement>(jsonString);
            var value = document.MapToType<TestObject>(TestOptions);

            Assert.IsTrue(value != null);
            Assert.IsTrue(value.P1 == -1);
            Assert.IsTrue(value.f1 == "bar");
        }


        [TestMethod]
        public void DeserializeNestedObjectTests()
        {
            var jsonString =
                "{" +
                "  \"" + JsonConfig.DefaultTypeTag + "\": \"NestedTestObject\"," +
                "  \"f1\": false," +
                "  \"P1: TestObject\": { \"f1\":\"bar\",\"P1\":-1 }" +
                "}";

            var document = JsonSerializer.Deserialize<JsonElement>(jsonString);
            var value = document.MapToType<NestedTestObject>(options: TestOptions);

            Assert.IsTrue(value != null);
            Assert.IsTrue(value.P1 != null);
            Assert.IsTrue(value.P1.f1 == "bar");
            Assert.IsTrue(value.P1.P1 == -1);
            Assert.IsTrue(value.f1 == false);
        }

        [TestMethod]
        public void DeserializeArrayTests()
        {
            var objString = $"{{ \"{JsonConfig.DefaultTypeTag}\": \"TestObject\", \"f1\":\"bar\", \"P1\":-1 }}";
            var arrayString = $"[ {objString}, 1, \"foo\", false, [ 1, \"bar\", {objString} ]]";

            var document = JsonSerializer.Deserialize<JsonElement>(arrayString);
            var array = document.MapToObjectArray(options: TestOptions);

            Assert.IsTrue(array != null);
            Assert.IsTrue(array.Length == 5);
            Assert.IsTrue(array[0] is TestObject);
            Assert.IsTrue(array[1] is double);
            Assert.IsTrue(array[2] is string);
            Assert.IsTrue(array[3] is bool);
            Assert.IsTrue(array[4] is object[]);
            Assert.IsTrue(((object[])array[4]).Length == 3);
            Assert.IsTrue(((object[])array[4])[0].Equals(1.0));
            Assert.IsTrue(((object[])array[4])[1].Equals("bar"));
            Assert.IsTrue(((object[])array[4])[2] is TestObject);
        }

        [TestMethod]
        public void DeserializeTypeArrayTests()
        {
            var outerJsonString = $"[1,2,3]";

            var document = JsonSerializer.Deserialize<JsonElement>(outerJsonString);
            var array = JsonConfig.MapToArray<int>(document);

            Assert.IsTrue(array != null);
            Assert.IsTrue(array.Length == 3);
            Assert.IsTrue(array[0] == 1);
            Assert.IsTrue(array[1] == 2);
            Assert.IsTrue(array[2] == 3);
        }


        [TestMethod]
        public void DeserializeArrayObjectTests()
        {
            var jsonString = $"{{  \"{JsonConfig.DefaultTypeTag}\": \"TestArrayObj\", \"array\": [1,2,3] }}";

            var document = JsonSerializer.Deserialize<JsonElement>(jsonString);
            var obj = document.MapToType<TestArrayObj>(TestOptions);

            Assert.IsTrue(obj != null);
            Assert.IsTrue(obj.array.Length == 3);
            Assert.IsTrue(obj.array[0] == 1);
            Assert.IsTrue(obj.array[1] == 2);
            Assert.IsTrue(obj.array[2] == 3);
        }

        [TestMethod]
        public void DeserializeDictionaryObjectTest()
        {
            var builder = new StringBuilder().AppendJsonObject(properties: new (string name, object value)[] { ("foo", "bar"), ("number", 42) });
            var jsonString = builder.ToString();

            var document = JsonSerializer.Deserialize<JsonElement>(jsonString);
            var obj = document.MapToDictionary();

            Assert.IsTrue(obj != null);
            Assert.IsTrue(obj.Count == 2);
            Assert.IsTrue(obj["foo"].Equals("bar"));
            Assert.IsTrue(obj["number"].Equals(42.0));
        }

        [TestMethod]
        public void DeserializeShortHandTypeTestWithAlias()
        {
            var builder =
                new StringBuilder()
                    .AppendJsonObject(2, properties: new (string name, object value)[] { (JsonConfig.DefaultTypeTag, "TestObject"), ("f1", "bar"), ("P1", 42) });

            var jsonString = builder.ToString();

            var document = JsonSerializer.Deserialize<JsonElement>(jsonString);
            var obj = document.MapToType<TestObject>(options: TestOptions);

            Assert.IsTrue(obj != null);
            Assert.IsTrue(obj.P1 == 42);
            Assert.IsTrue(obj.f1 == "bar");
        }

        [TestMethod]
        public void DeserializeUntypedListTest()
        {
            var outerJsonString = $"[1,2,3]";

            var document = JsonSerializer.Deserialize<JsonElement>(outerJsonString);
            var list = JsonConfig.MapToCollection<ArrayList>(document);

            Assert.IsTrue(list != null);
            Assert.IsTrue(list.Count == 3);
            Assert.IsTrue(((double)list[0]) == 1);
            Assert.IsTrue(((double)list[1]) == 2);
            Assert.IsTrue(((double)list[2]) == 3);
        }

        [TestMethod]
        public void DeserializeTypedListTest()
        {
            var outerJsonString = $"[1,2,3]";

            var document = JsonSerializer.Deserialize<JsonElement>(outerJsonString);
            var list = JsonConfig.MapToCollection<List<int>>(document);

            Assert.IsTrue(list != null);
            Assert.IsTrue(list.Count == 3);
            Assert.IsTrue(list[0] == 1);
            Assert.IsTrue(list[1] == 2);
            Assert.IsTrue(list[2] == 3);
        }

        [TestMethod]
        public void DeserializeObjectWithListAndArrayTest()
        {
            var outerJsonString = $"{{ \"GenericIntList\": [1,2,3], \"arrayList\": [1, \"foo\", [1,2,3]] }}";

            var document = JsonSerializer.Deserialize<JsonElement>(outerJsonString);
            var obj = JsonConfig.MapToType<TestListObj>(document);

            Assert.IsTrue(obj != null);
            Assert.IsTrue(obj.GenericIntList.Count == 3);
            Assert.IsTrue(obj.arrayList.Count == 3);
        }
    }
}
