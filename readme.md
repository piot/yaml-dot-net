## Yaml dot net
Very lightweight yaml serializer and deserializer.

```csharp
using yaml;

var someObject = new YourClass();

// Convert object to string
var serializedString = YamlSerializer.Serialize(someObject);

// ...and bring it back
var yourObject = YamlDeserializer.Deserialize<YourClass>(serializedString);
```

### Notes
* Only supports strings enclosed in `'`.
* Basic support for float and integers.
