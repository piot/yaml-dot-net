## Yaml dot net
Very lightweight yaml serializer and deserializer.

```csharp
using Piot.Yaml;

var someObject = new YourClass();

// Convert object to string
var serializedString = YamlSerializer.Serialize(someObject);

// ...and bring it back
var yourObject = YamlDeserializer.Deserialize<YourClass>(serializedString);
```

### Notes
Only supports a small subset of YAML 1.2:

* Basic support for float and integers.
* Hexadecimal. e.g. `0x3A`.
* Enums. `FirstEnum`  or multiple enum values for flags: `FirstChoice | Second`. Optionally it can be formatted as `FirstChoice,Second`
* Alternate serialized name using YamlProperty attribute:

    ```csharp
    public enum SomeEnum
    {
        FirstChoice,
        Second,
        [YamlProperty("_third")]
        Third
    }

    public struct SomeStruct
    {
        public int answer;

        [YamlProperty("anotherAnswer")]
        public string SomethingCompletelyDifferent;
    }
    ```

* List / Array

    ```csharp
    public struct SomeItem
    {
        public int x;
    }

    public struct RootStruct
    {
        public SomeItem[] items;
        public string somethingElse;
    }
    ```

    Can be deserialized from:

    ```yaml
    items:
      - x: 42
      - x: 98
    somethingElse: 'Hello, world!'
    ```

    Note that it is very picky about the exact indentation.

* Dictionary

    ```csharp
    public struct RootStruct
    {
        public Dictionary<int, SomeItem> lookup;
        public string somethingElse;
    }
    ```

    Can be deserialized from example:

    ```yaml
    lookup:
      2:
        x: 909
      -10:
        x: 1234
    somethingElse: 'Hello, world, again'
    ```
