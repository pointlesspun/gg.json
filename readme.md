# gg.json an Extensible Json Reader (v1.0)

gg.json provides a fairly easy way to deserialize Json into C# classes, somewhat (if not entirely) similar to JsonConvert 
in NewtonSoft's Json deserializer with a minor twist. This document attempts to outline the intended use of this tool in an not too incoherent manner.

Install:
```
dotnet add package gg.json --version 1.0.2
```

Released under [the MIT License, (C)2022 PointlessPun ](https://opensource.org/licenses/MIT) 

## Key features

* Native .net core System.Text.Json reader, so no additional dependencies on other libraries. 
* No need to annotate classes with 'JsonProperties'.
* Ability to specify classes to deserialize to.
* Ability to deserialize objects to interfaces.
* Supports an extended format to add documentation by transcribing from the extended format to plain json.
* Comes with a much requested way to express versions (no one asked for this).

## Limitations

* No references to other objects. Properties which refer to objects are always owned by the declaring class.
* Limited collection support: array types & dictionaries are the only collections supported (Lists, Sets will come soon).
* Not very informative error messages, in case of an errors.
* Objects must have a default / parameter less constructor.
* Security risk: objects will be instantiated, i.e. code will be invoked and run to moment a json file is read. Make sure
  you know what you instantiate and what json you are reading.

## Examples

As the code changes quickly documentation has a tendency to become stale. Therefore it is recommended to refer to the [integration tests] (https://github.com/pointlesspun/gg.json/blob/master/gg.json.tests/JsonConfigFileTests.cs) for the most up to date samples. 

That being said, let's get the basics out of the way and then let's get on with the actual examples. 

The gg.json functionality comes by means of three (largely static) classes:

* `JsonConfig` which provides various methods to deserialize `JsonElements` into `C#`.
* `JsonConfig.Options` options with which the deserialization process is controlled.
* `JsonConfigFile` builds upon `JsonConfig` and provides easy to use `.json` or `.gg.json` file reading.

For the most part we will be using the `JsonConfigFile` class.

### Reading 'plain' Json

The most simple use case is to read a json file via the `JsonConfigFile.Read` method. This
will parse the json and return a `Dictionary<string, object>` as shown below.

```json
{
  "positiveNumber": 42,
  "negativeNumber": -42,
  "positiveFraction": 42.42,
  "negativeFraction": -42.42,
  "booleanTrue": true,
  "booleanFalse": false,
  "string": "foo"
}
```

```csharp
public void ReadSimpleDictionaryTest()
{
    var configFileDictionary = JsonConfigFile.Read("jsonconfig/data/plainData.json");

    Assert.IsTrue(configFileDictionary["positiveNumber"].Equals(42.0));

    // since the type of the number is not known, it will default to double.
    Assert.IsTrue(configFileDictionary["positiveNumber"].GetType() == typeof(double));

    ...

    Assert.IsTrue(configFileDictionary["string"].Equals("foo"));
}
```

### Reading .xjsn

The .XJSON format is a json variation which offers two additional features to json object files
(json files starting with '{' and ending with '}'):

* The ability to add single line comments.
* No need to add the '{' and '}' characters.

The xjsn form previous json example could look like this:

```json
// Example of an xjson file with simple data types.

"positiveNumber": 42,
"negativeNumber": -42,
"positiveFraction": 42.42,
"negativeFraction": -42.42,
"booleanTrue": true,
"booleanFalse": false,
"string": "foo"
```

Reading an xjson file is the same as json files:

```csharp
public void ReadXJsonDictionaryTest()
{
    // the values are the same as ReadSimpleDictionaryTest
    var configFileDictionary = JsonConfigFile.Read("jsonconfig/data/plainData.xjsn");

    Assert.IsTrue(configFileDictionary["positiveNumber"].Equals(42.0));

    ...
}
```

### Reading a specific type

The examples given so far provide some bare bones functionality by deserializing to a Dictionary.
However the reader is capable of something more powerful. Let's assume we have the following types:

```csharp
public interface IPerson
{
    public float Age { get; set; }

    public string Name { get; set; }
}

public class Citizen : IPerson
{
    ...
    public IPerson AlterEgo { get; set; }
}

public class Hero : IPerson
{
    ...

    private int secretId = 42;

    public int SecretId => secretId;

    public Hero ReportsTo { get; set; }
}
```

And the corresponding json:

```json
{
	"Name": "Bruce",
	"Age": 48,
	"AlterEgo: Hero": {
		"name": "Batman",
		"Age": 48
	}
}
```

We can read this json by simply calling read with the expected type (Citizen), ie:

```csharp
public void ReadBruceWaynesFile()
{
    var citizen = JsonConfigFile.Read<Citizen>("bruce.json");

    Assert.IsNotNull(citizen);
    Assert.IsTrue(citizen is Citizen);
    Assert.IsTrue(((Citizen)citizen).AlterEgo != null);
    Assert.IsTrue(((Citizen)citizen).AlterEgo is Hero);
}
```

Now the careful reader may notice something interesting in the class definition of citizen: the `AlterEgo` property is defined as an interface (`IPerson`), yet XSJN somehow 'knows' to turn this interface into the concrete type of a `Hero`. The "magic" of this result has two parts:

* The declaration of the AlterEgo property.
* The instantiation of the right object corresponding to said declaration.

IF you're only interested in using gg.json, the only thing you should know are the following:

* In order to specify the type of an object, gg.json checks if the type of the property is a concrete type (ie not an interface nor an abstract class).
* If the property is not a concrete type, gg.json looks for the typename following a colon (':') the name declaration. In case of the previous json, you will find the type, `Hero`, after the property name `AlterEgo`, ie `"AlterEgo: Hero"`.
* You can specify the concrete classes gg.json can use by adding 'options' to the read call, eg `JsonConfigFile.Read<Citizen>("bruce.json", new JsonConfig.Options()`), but more on this later.
* If no options are provided, gg.json will create these options for you based on a reasonable guess of the concrete classes it might come across. This guess consists of a number of common default types (eg `int`, `float[]`, `ulong[]`) and public concrete types found in the assembly of the `Citizen` class. `Hero` is part of the latter, so gg.json knows how to create an object of type `Hero`.

If you're interested in other use cases, specifying the types or the limitations of gg.json read on...

### Declaring types explicitly 

Let's take a look at the following example, which is similar to the previous cs example, except for one minor detail:

```csharp
public void ReadPerson()
{
    var citizen = JsonConfigFile.Read<IPerson>("explicitBruce.json");

    Assert.IsNotNull(citizen);
    Assert.IsTrue(citizen is Citizen);
    Assert.IsTrue(((Citizen)citizen).AlterEgo != null);
    Assert.IsTrue(((Citizen)citizen).AlterEgo is Hero);
}
```

As you can see the object is cast to the `IPerson` interface. This would "normally" (for a given definition of normal) not be possible as the deserializer needs to know what type the json file needs to be converted to. gg.json however is on the lookup for a `__type` property. This property is a key/value (string) pair where the value is the type of the enclosing object. This type can be an assembly qualified type or a more readable 'alias'. Eg

```json
{
	"__type": "gg.framework.tests.jsonconfig.JsonConfigFileTests+Hero, gg.framework.tests, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",

	"Name": "James",
	"Age": 43.1
}
```
Note that while out of the box gg.json supports this approach, it is not by default enabled for security reasons. If you want to use fully qualified types to be read and instantiated, create custom options and set the `AllowFullyQualifiedTypes` property to true (at your own peril).

Another approach is to use a more readable Type alias:

```json
// .xjsn example of an object which tries to define an interface-property using an aliased type. 

    "__type": "Citizen",
    "Name": "Bruce",
    "Age": 32,
    "AlterEgo: Hero": {
        "Name": "Batman",
        "Age": 48
    }
```

The use case for introducing a type inside the json file is to have fully selfcontained, self describing pieces of data. I'm not sure what the market for these types of data looks like but I'm just going to assume it's huge and profitable or if not there is some artistic benefit. In case you're wondering if this `__type` property shows up in your object somehow, don't worry: if XSJN can't find a property or field, or the property or field is not public it will be ignored.

In general the expectation is that you'll want to use the aliased types, so let's look at setting up the options. XJSN offers various methods of creating some best guess default options and aliases out of the box, eg by giving one or more type tuples at the end of the `Read` method:

```csharp
   var heroAlias = ("Hero", typeof(Hero));
   var citizenAlias = ("Citizen", typeof(Citizen));
   var dataObject = JsonConfigFile.Read<IPerson>("heroFile.json", heroAlias, citizenAlias);
```

If that is still not enough control, you can create Options, customize them to your heart's content and pass them on to the `Read` method, eg:

```csharp

    var options = JsonConfig.Options
                    .Create((nameof(Hero), typeof(Hero)))
                    .AddDefaultAliases();

    // allow for bad stuff to happen ?
    options.AllowFullyQualifiedTypes = true;

    var hero = JsonConfigFile.Read<IPerson>("jsonconfig/data/heroWithType.json", options);

    ... do some heroic things
```

There are a couple of features, quirks and details you should know about (eg `__version`) but if you came this far, you probably have an ok to good grasp on what's going on and can figure it out yourself.

## Background / Motivation

(Insert a captivating story about my passion of working with json files, reinventing the wheel and the dismay of having to deal with the shortcomings of other json tools.)

## "Roadmap"

* v1.1 - Adding support for error logging.
* v2.0 - Support for Lists<>, HashSets<> and Dictionaries
* v3.0 - References
* v4.0 - Includes (Fetch)


