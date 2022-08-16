using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace gg.json.tests
{
    [TestClass]
    public class JsonConfigFileTests
    {
        // the following are some 'quirky' classes used in the integration tests/examples.
        public interface IPerson
        {
            public float Age { get; set; }

            public string Name { get; set; }
        }

        public class Citizen : IPerson
        {
            public string Name { get; set; }

            public float Age { get; set; }

            public IPerson AlterEgo { get; set; }
        }

        public class Hero : IPerson
        {
            public string Name { get; set; } = "some hero...";

            private int secretId = 42;

            public float Age { get; set; }

            public int SecretId => secretId;

            private string SecretDescription { get; set; }

            public string RevealSecretDescription => SecretDescription;

            public Hero ReportsTo { get; set; }
        }

        /// <summary>
        /// Load a json file with some POD data. 
        /// </summary>
        [TestMethod]
        public void ReadSimpleDictionaryTest()
        {
            var configFileDictionary = JsonConfigFile.Read("data/plainData.json");

            Assert.IsTrue(configFileDictionary["positiveNumber"].Equals(42.0));

            // since the type of the number is not known, it will default to double.
            Assert.IsTrue(configFileDictionary["positiveNumber"].GetType() == typeof(double));

            Assert.IsTrue(configFileDictionary["negativeNumber"].Equals(-42.0));
            Assert.IsTrue(configFileDictionary["negativeNumber"].GetType() == typeof(double));

            Assert.IsTrue(configFileDictionary["positiveFraction"].Equals(42.42));
            Assert.IsTrue(configFileDictionary["positiveFraction"].GetType() == typeof(double));

            Assert.IsTrue(configFileDictionary["negativeFraction"].Equals(-42.42));
            Assert.IsTrue(configFileDictionary["negativeFraction"].GetType() == typeof(double));

            Assert.IsTrue(configFileDictionary["booleanTrue"].Equals(true));
            Assert.IsTrue(configFileDictionary["booleanFalse"].Equals(false));

            Assert.IsTrue(configFileDictionary["string"].Equals("foo"));
        }

        /// <summary>
        /// Load an  Xjson file with some POD data. 
        /// </summary>
        [TestMethod]
        public void ReadXJsonDictionaryTest()
        {
            // the values are the same as ReadSimpleDictionaryTest
            var configFileDictionary = JsonConfigFile.Read("data/plainData.xjsn");

            Assert.IsTrue(configFileDictionary["positiveNumber"].Equals(42.0));

            // since the type of the number is not known, it will default to double.
            Assert.IsTrue(configFileDictionary["positiveNumber"].GetType() == typeof(double));

            Assert.IsTrue(configFileDictionary["negativeNumber"].Equals(-42.0));
            Assert.IsTrue(configFileDictionary["negativeNumber"].GetType() == typeof(double));

            Assert.IsTrue(configFileDictionary["positiveFraction"].Equals(42.42));
            Assert.IsTrue(configFileDictionary["positiveFraction"].GetType() == typeof(double));

            Assert.IsTrue(configFileDictionary["negativeFraction"].Equals(-42.42));
            Assert.IsTrue(configFileDictionary["negativeFraction"].GetType() == typeof(double));

            Assert.IsTrue(configFileDictionary["booleanTrue"].Equals(true));
            Assert.IsTrue(configFileDictionary["booleanFalse"].Equals(false));

            Assert.IsTrue(configFileDictionary["string"].Equals("foo"));
        }


        /// <summary>
        /// Demonstrates how to read an object from an xjson file. Because this config file
        /// contains an explicit type tag (the assembly qualified name of the Hero class)
        /// it can be cast to the hero type.
        /// </summary>
        [TestMethod]
        public void ReadXJsonObjectTest()
        {
            var dataObject = JsonConfigFile.Read<Hero>("data/heroWithType.json");

            Assert.IsNotNull(dataObject);

            Assert.IsTrue(dataObject.Name == "James");

            // this should still be secret 
            Assert.IsTrue(dataObject.SecretId == 42);

            // igore the epsilon
            Assert.IsTrue(dataObject.Age > 43.0 && dataObject.Age < 43.2);

            // secret description is private so won't be affected either
            Assert.IsTrue(dataObject.RevealSecretDescription == null);

            // Reports to no one
            Assert.IsTrue(dataObject.ReportsTo == null);
        }

        /// <summary>
        /// This should fail since we read with the default options having the fully qualified type
        /// creation set to false. 
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(JsonConfigException))]
        public void ReadXJsonObjectWithDisallowedExplicitTypeTest()
        {
            JsonConfigFile.Read("data/heroWithType.json");

            Assert.Fail();
        }

        [TestMethod]
        public void ReadXJsonObjectWithExplicitTypeTest()
        {
            var options = JsonConfig.Options
                            .Create((nameof(Hero), typeof(Hero)))
                            .AddDefaultAliases();

            options.AllowFullyQualifiedTypes = true;

            var hero = JsonConfigFile.Read<IPerson>("data/heroWithType.json", options);

            Assert.IsTrue(hero is Hero);
        }

        /// <summary>
        /// Demonstrates how to read an object from an xjson file.
        /// Unlike ReadXJsonObjectTest an alias lookup is used in order
        /// to be able to use more readable names in the config. Furthermore
        /// the object in config contains a nested object (of type PlainDataObject).
        /// This object will be automatically instantiated as well.
        /// </summary>
        [TestMethod]
        public void ReadXJsonObjectWithAliasTest()
        {
            var alias = ("Hero", typeof(Hero));
            var dataObject = JsonConfigFile.Read<Hero>("data/heroWithAlias.json", alias);

            Assert.IsNotNull(dataObject);

            Assert.IsTrue(dataObject.Name == "Robin");
            Assert.IsTrue(dataObject.Age == 22);

            Assert.IsTrue(dataObject.ReportsTo != null);
            Assert.IsTrue(dataObject.ReportsTo.Name == "Batman");
        }

        /// <summary>
        /// Demonstrates how to read an object from an xjson file. 
        /// </summary>
        [TestMethod]
        public void ReadXJsonObjectWithoutTypeTest()
        {
            var dataObject = JsonConfigFile.Read<Hero>("data/heroWithoutType.xjsn");

            Assert.IsNotNull(dataObject);

            Assert.IsTrue(dataObject.Name == "James");

            // this should still be secret 
            Assert.IsTrue(dataObject.SecretId == 42);

            // igore the epsilon
            Assert.IsTrue(dataObject.Age > 43.0 && dataObject.Age < 43.2);

            // secret description is private so won't be affected either
            Assert.IsTrue(dataObject.RevealSecretDescription == null);
        }

#if DEBUG
        /// <summary>
        /// This demonstrates what happens if we're trying to automatically instantiate 
        /// an interface property. Which is to say: an exception will be thrown as 
        /// an interface cannot be instantiated (automatically).
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(JsonConfigException))]
        public void ReadXJsonObjectAttemptingToInstantiateAnInterfaceTest()
        {

            var heroAlias = (nameof(Hero), typeof(Hero));
            var citizenAlias = (nameof(Citizen), typeof(Citizen));
            var dataObject = JsonConfigFile.Read<Citizen>("data/objectWithInterface.xjsn", heroAlias, citizenAlias);

            Assert.Fail();
    }
#endif

        /// <summary>
        /// Read an object which explicitely specifies what object to instantiate for a property with an interface
        /// signature.
        /// </summary>
        [TestMethod]
        public void ReadXJsonObjectWithExplicitInstantiationTest()
        {
            var heroAlias = (nameof(Hero), typeof(Hero));
            var citizen = JsonConfigFile.Read<Citizen>("data/objectWithInterfaceFixed.xjsn", heroAlias);

            Assert.IsNotNull(citizen);
            Assert.IsNotNull(citizen.AlterEgo);
            Assert.IsTrue(citizen.AlterEgo is Hero);
            Assert.IsTrue(((Hero)citizen.AlterEgo).Name == "Batman");
        }

        /// <summary>
        /// Read an object which explicitely specifies what object to instantiate for a property with an interface
        /// signature.
        /// </summary>
        [TestMethod]
        public void ReadXJsonObjectsInterfaceTest()
        {
            var citizen = JsonConfigFile.Read<IPerson>("data/objectWithInterfaceFixed.xjsn");

            Assert.IsNotNull(citizen);
            Assert.IsTrue(citizen is Citizen);
            Assert.IsTrue(((Citizen)citizen).AlterEgo != null);
            Assert.IsTrue(((Citizen)citizen).AlterEgo is Hero);
        }


        /// <summary>
        /// Read an object which explicitely specifies what object to instantiate for a property with an interface
        /// signature.
        /// </summary>
        [TestMethod]
        public void ReadJsonObjectsInterfaceTest()
        {
            var citizen = JsonConfigFile.Read<Citizen>("data/objectWithInterfaceFixed.json");

            Assert.IsNotNull(citizen);
            Assert.IsTrue(citizen is Citizen);
            Assert.IsTrue(citizen.AlterEgo != null);
            Assert.IsTrue(citizen.AlterEgo is Hero);
        }


        /// <summary>
        /// </summary>
        [TestMethod]
        public void ReadJsonObjectWithArraysTest()
        {
            var citizenAlias = (nameof(Citizen), typeof(Citizen));
            var intAlias = ("int[]", typeof(int[]));
            var dictionary = JsonConfigFile.Read("data/arrays.json", intAlias, citizenAlias);

            Assert.IsNotNull(dictionary);
            Assert.IsTrue(dictionary["objectArray"] is object[]);
            Assert.IsTrue(dictionary["intArray"] is int[]);

            var objectArray = dictionary["objectArray"] as object[];
            var intArray = dictionary["intArray"] as int[];

            // numbers will always default to double unless specified
            Assert.IsTrue(objectArray[0].Equals(1.0));
            Assert.IsTrue(objectArray[1].Equals("two"));
            Assert.IsTrue(objectArray[2] is Citizen);
            Assert.IsTrue(objectArray[3] is object[]);

            for (var i = 0; i < intArray.Length; i++)
            {
                Assert.IsTrue(intArray[i] == i);
            }
        }

        [TestMethod]
        public void ReadJsonObjectWithAndOlderVersionTest()
        {
            var dictionary = JsonConfigFile.Read("data/oldversion.json");

            // all fine...
            Assert.IsNotNull(dictionary);
            Assert.AreEqual(dictionary["data"], "foo");
        }

        [TestMethod]
        [ExpectedException(typeof(JsonConfigException))]
        public void ReadJsonObjectWithAndNewVersionTest()
        {
            JsonConfigFile.Read("data/newversion.json");

            // should never get here
            Assert.Fail();
        }

        /// <summary>
        /// Bare bones test to check if 
        /// </summary>
        [TestMethod]
        public void LogTest()
        {
            var options = JsonConfig.Options.Create<Citizen>();
            var logs = new List<string>();

            options.Log = (str, lvl) => logs.Add(str);

            var citizen = JsonConfigFile.Read<Citizen>("data/objectWithInterfaceFixed.json", options);

            Assert.IsTrue(citizen != null);
            Assert.IsTrue(logs.Count > 0);
        }
    }
}
