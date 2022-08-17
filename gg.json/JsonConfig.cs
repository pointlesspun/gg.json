using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace gg.json
{
    /// <summary>
    /// Deserializes the objects a json configuration file. In order for this to work the following
    /// convention is applied:
    /// 
    /// * Only a limited set of types are supported, these include all the basic types (int, bool, string and so on),
    ///   arrays and dictionaries.
    ///   
    /// * Objects can be instantiated by either explicitely indicating their type or if they are concrete
    ///   properties of other objects. The type can be either the fully qualified assembly name or a short hand,
    ///   This short hand types must be declared when reading the configuration. The type name follows the name
    ///   of the property separated by a column eg: "SomeProperty:MyClass" or in case of unnamed values (in
    ///   for instance arrays) by just a colomn and the type eg "{ ":MyClass": {} }"
    ///   
    /// * For examples see the relevant documentation  
    /// 
    /// (Released under the MIT License (C) 2022 PointlessPun)
    /// </summary>
    public static partial class JsonConfig
    {

        #region --- Static Readonly Fields ----------------------------------------------------------------------------

        /// <summary>
        /// Character used to separate a property name from the type when no custom options are defined.
        /// </summary>
        public static readonly char DefaultTypeSeparator = ':';

        /// <summary>
        /// Tag used to specify an object when no custom options are defined.
        /// </summary>
        public static readonly string DefaultTypeTag = "__type";

        /// <summary>
        /// Optional property which can be added to a json config to see if its version matches.
        /// </summary>
        public static readonly string VersionTag = "__gg.json.version";

        /// <summary>
        /// Current version of this class, if a higher version is detected in the json an exception will be thrown
        /// when deserialization starts. Version numbers imply 
        /// [0] => major version, breaking changes with lower versions
        /// [1] => minor version, no breaking changes with lower versions
        /// </summary>
        public static readonly int[] Version = new int[] { 2, 0 };

        #endregion

        #region --- Public Methods ------------------------------------------------------------------------------------       

        /// <summary>
        /// Deserializes the json sstring and maps it to a value of T.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="jsonString"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static T Deserialize<T>(string jsonString, Options options = null) 
            => (T) Deserialize(jsonString, typeof(T), options);

        /// <summary>
        /// Deserializes the string and maps it to an object.
        /// </summary>
        /// <param name="jsonString">Json string</param>
        /// <param name="options">A lookup of known types of "Name" tot type. Name can be Type.Name, fully qualified
        /// or a reference in order to make the json.</param>
        /// <returns></returns>
        public static object Deserialize(string jsonString, Type targetType = null, Options options = null)
        {
            if (options != null)
            {
                options.TryLog($"Deserializing jsonString ... with target type {(targetType == null ? "null" : targetType.Name)}.");
            }

            var element = JsonSerializer.Deserialize<JsonElement>(jsonString);

            // test if a version is present, if so validate it
            if (element.ValueKind == JsonValueKind.Object)
            {
                if (element.TryGetProperty(VersionTag, out var version))
                {
                    ValidateVersion(version.GetString(), options);
                }
            }

            return targetType == null || targetType.IsInterface || targetType.IsAbstract || targetType == typeof(Dictionary<string, object>)
                    ? element.MapTo(options)
                    : element.MapValueTo(targetType, options);
        }

        /// <summary>
        /// Deserialize an array which has arrayElementType.
        /// </summary>
        /// <param name="element">A json element with valueKin JsonValueKind.Array.</param>
        /// <param name="arrayElementType">a non null type which is the element type of the array.</param>
        /// <param name="options">Optional argument with the deserialization options.</param>
        /// <returns>A non null array of size 0 or more.</returns>
        public static Array MapToArray(this JsonElement element, Type arrayElementType, Options options = null)
        {
            RequiresValueKind(element, JsonValueKind.Array, 
                                    "When mapping to an array, the element must be of JsonValueKind.Array.");
            RequiresNotNull(arrayElementType, "Must define the element type of the array.");

            var result = Array.CreateInstance(arrayElementType, element.GetArrayLength());
            var index = 0;

            foreach (var subElement in element.EnumerateArray())
            {
                result.SetValue(subElement.MapValueTo(arrayElementType, options), index);
                index++;
            }

            return result;
        }

        /// <summary>
        /// Deserialize an array which has arrayElementType T.
        /// </summary>
        /// <param name="element">A json element with valueKin JsonValueKind.Array.</param>
        /// <param name="options">Optional argument with the deserialization options.</param>
        /// <returns>A non null array of size 0 or more.</returns>
        public static T[] MapToArray<T>(JsonElement element, Options options = null) 
                => (T[]) element.MapToArray(typeof(T), options);

        /// <summary>
        /// Deserialize a json array to a collection of type T.
        /// </summary>
        /// <param name="element">A json element with valueKin JsonValueKind.Array.</param>
        /// <param name="options">Optional argument with the deserialization options.</param>
        /// <returns>A non null array of size 0 or more.</returns>
        public static T MapToCollection<T>(JsonElement element, Options options = null)
                => (T)element.MapToCollection(typeof(T), options);

        /// <summary>
        /// Deserialize an array made out of objects.
        /// </summary>
        /// <param name="element">JsonElement of the array type</param>
        /// <param name="options">Optional options for deserializing the array</param>
        /// <returns>The deserialized array (may have size 0).</returns>
        public static object[] MapToObjectArray(this JsonElement element, Options options = null)
        {
            RequiresValueKind(element, JsonValueKind.Array,
                                    "When mapping to an array, the element must be of JsonValueKind.Array.");

            var result = new object[element.GetArrayLength()];
            var index = 0;

            foreach (var subElements in element.EnumerateArray())
            {
                result[index] = subElements.MapValue(options);
                index++;
            }

            return result;
        }

        /// <summary>
        /// Map the element to the given type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="element"></param>
        /// <param name="options"></param>
        /// <returns>An object of type T.</returns>
        public static T MapToType<T>(this JsonElement element, Options options = null) 
            => (T) element.MapToType(typeof(T), options);

        /// <summary>
        /// Map the element to the given type
        /// </summary>
        /// <param name="element"></param>
        /// <param name="targetType">A concrete type</param>
        /// <param name="options"></param>
        /// <returns>An object of type targetType.</returns>
        /// <exception cref="JsonConfigException">Thrown when the targettype is an interface or abstract type.</exception>
        public static object MapToType(this JsonElement element, Type targetType, Options options = null)
        {
            // if the target defined and not an interface ?
            Requires(!(targetType.IsInterface || targetType.IsAbstract),
                $"Cannot instantiate an interface of type {targetType.Name}, use either an explicit typename or an alias (eg \"MyProperty:Alias\").");

            if (element.ValueKind == JsonValueKind.Array)
            {
                return element.MapToArray(targetType.GetElementType(), options);
            }

            return SetObjectProperties(Activator.CreateInstance(targetType), element.EnumerateObject(), options);
        }

        /// <summary>
        /// Maps the given element to an string / object dictionary.
        /// </summary>
        /// <param name="element"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static Dictionary<string, object> MapToDictionary(this JsonElement element, Options options = null)
        {
            RequiresValueKind(element, JsonValueKind.Object,
                                    "When mapping to a dictionary, the element must be of JsonValueKind.Object.");

            var result = new Dictionary<string, object>();
            var splitChar = options == null ? DefaultTypeSeparator : options.TypeSeparator;

            foreach (var keyValuePair in element.EnumerateObject())
            {
                // check if this is property with a defined type
                if (TrySplitNameAndType(keyValuePair.Name, splitChar,out (string key, string typeName) inlinedName))
                {
                    var type = ResolveType(inlinedName.typeName, options);
                    result[inlinedName.key] = keyValuePair.Value.MapToType(type, options);
                }
                else
                {
                    // take a best guess at mapping the object
                    result[keyValuePair.Name] = keyValuePair.Value.MapValue(options);
                }
            }

            return result;
        }

        /// <summary>
        /// Map the given element to a value of type T.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="element"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static T MapValue<T>(this JsonElement element, Options options = null) 
            => (T) element.MapValueTo(typeof(T), options);

        /// <summary>
        /// Map the element to the ValueKind specified by the element. If the ValueKind is a number, array or object
        /// try to map it to the given target type.
        /// </summary>
        /// <param name="element"></param>
        /// <param name="targetType"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        /// <exception cref="JsonConfigException"></exception>
        public static object MapValueTo(this JsonElement element, Type targetType, Options options = null)
        {
            RequiresNotNull(targetType, "Must define a target type to map the element to.");

            switch (element.ValueKind)
            {
                case JsonValueKind.String:
                    return element.GetString();
                case JsonValueKind.Number:
                    if (targetType == typeof(int))
                    {
                        return element.GetInt32();
                    }
                    else if (targetType == typeof(uint))
                    {
                        return element.GetUInt32();
                    }
                    else if (targetType == typeof(long))
                    {
                        return element.GetInt64();
                    }
                    else if (targetType == typeof(ulong))
                    {
                        return element.GetUInt64();
                    }
                    else if (targetType == typeof(float))
                    {
                        return element.GetSingle();
                    }
                    else
                    {
                        return element.GetDouble();
                    }
                case JsonValueKind.True:
                    return true;
                case JsonValueKind.False:
                    return false;
                case JsonValueKind.Array:                   
                    return element.MapToCollection(targetType, options);
                case JsonValueKind.Object:
                    return element.MapToType(targetType, options);
                case JsonValueKind.Null:
                    return null;
                default:
                    throw new JsonConfigException($"Unknown or unhandled element of type: {element.ValueKind}");
            }
        }

        /// <summary>
        /// Map an (array) element to a collection of the given target type.
        /// </summary>
        /// <param name="element"></param>
        /// <param name="targetType"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        /// <exception cref="JsonConfigException"></exception>
        private static object MapToCollection(this JsonElement element, Type targetType, Options options = null)
        {
            
            // no targettype or we're working with an array, try to build an array
            if (targetType == null || targetType.IsArray)
            {
                return element.MapToArray(targetType.GetElementType(), options);
            }
            
            // if it's a list, build a list (generic or 'plain' (?)).
            if (ImplementsCollection(targetType))
            {
                if (targetType.IsGenericType)
                {
                    var genericArgs = targetType.GetGenericArguments()[0];
                    var genericType = targetType.GetGenericTypeDefinition();
                    var instanceType = genericType.MakeGenericType(genericArgs);
                    var args = element.MapToArray(genericArgs, options);
                    return Activator.CreateInstance(instanceType, args);
                }
                else
                {
                    Array arrayValues = element.MapToObjectArray(options);
                    return Activator.CreateInstance(targetType, arrayValues);
                }
            }

            throw new JsonConfigException($"Unknown or unhandled target type: {targetType.Name}");
        }

        /// <summary>
        /// Checks if the type t implements ICollection or ICollection<>
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        private static bool ImplementsCollection(Type t)
        {
            return typeof(ICollection).IsAssignableFrom(t)
                || t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICollection<>));
        }

        /// <summary>
        /// Maps the element to its valuekind
        /// </summary>
        /// <param name="element"></param>
        /// <param name="options"></param>
        /// <returns>An object mathcing th valuekind </returns>
        /// <exception cref="JsonConfigException"></exception>
        public static object MapValue(this JsonElement element, Options options = null)
        {
            return element.ValueKind switch
            {
                JsonValueKind.String => element.GetString(),
                JsonValueKind.Number => element.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Array => MapToObjectArray(element, options),
                JsonValueKind.Object => element.MapTo(options),
                JsonValueKind.Null => null,
                _ => throw new JsonConfigException($"Unknown or unhandled element of type: {element.ValueKind}"),
            };
        }

        public static string GetTypeTag(this Options options) =>
            options == null || string.IsNullOrEmpty(options.TypeTag) ? DefaultTypeTag : options.TypeTag;

        #endregion

        #region --- Private Methods -----------------------------------------------------------------------------------        


        /// <summary>
        /// Tries to resolve the type as indicated by the typename. If options or aliases are defined, the type
        /// will be derived using Type.GetType
        /// </summary>
        /// <param name="typeName"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        private static Type ResolveType(string typeName, Options options)
        {
            if (HasAliases(options) && options.Aliases.TryGetValue(typeName, out var type))
            {
                return type;
            }

            if (options.AllowFullyQualifiedTypes)
            {
                return Type.GetType(typeName);
            }

            throw new JsonConfigException($"Trying to create fully qualified type {typeName} but the deserialization options forbid that.");
        }

        /// <summary>
        /// Checks if the options are defined and if it has aliases.
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        private static bool HasAliases(Options options) 
            => options != null && options.Aliases != null && options.Aliases.Count > 0;

        /// <summary>
        /// Test if the name contains a TypeSeparator (:) if so return true and the name split in
        /// a propertyName and typeName
        /// </summary>
        /// <param name="name"></param>
        /// <param name="inlinedName"></param>
        /// <returns></returns>
        private static bool TrySplitNameAndType(string name, char splitChar, 
                                                        out (string propertyName, string typeName) inlinedName)
        {
            var startIndex = name.IndexOf(splitChar);

            if (startIndex > 0)
            {
                inlinedName = (name.Substring(0, startIndex).Trim(), name.Substring(startIndex + 1).Trim());
                return true;
            }

            inlinedName = ("", "");
            return false;
        }

        /// <summary>
        /// Sets the properties of the given instance by assigning the valies of the properties in the 
        /// 'properties' enumerator.
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="properties"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        private static object SetObjectProperties(object instance, JsonElement.ObjectEnumerator properties, 
                                                                                            Options options = null)
        {
            var type = instance.GetType();
            var typeTag = options.GetTypeTag();
            var splitChar = options == null ? DefaultTypeSeparator : options.TypeSeparator;

            foreach (var property in properties)
            {
                // ignore the __type property
                if (property.Name.Trim() != typeTag)
                {
                    // check if there is an object type with a type declaration (ie "name:type" {...} )
                    if (property.Value.ValueKind == JsonValueKind.Object
                        && TrySplitNameAndType(property.Name, splitChar, out (string key, string typeName) inlinedName))
                    {
                        var targetType = ResolveType(inlinedName.typeName, options);
                        object value = property.Value.MapToType(targetType, options);

                        SetObjectProperty(instance, value, type, inlinedName.key, options);
                    }
                    else
                    {
                        var propertyInfo = type.GetProperty(property.Name);

                        if (propertyInfo != null)
                        {
                            propertyInfo.SetValue(instance, property.Value.MapValueTo(propertyInfo.PropertyType, options));
                        }
                        else
                        {
                            var fieldInfo = type.GetField(property.Name);

                            if (fieldInfo != null)
                            {
                                fieldInfo.SetValue(instance, property.Value.MapValueTo(fieldInfo.FieldType, options));
                            }
                            else if (options != null)
                            {
                                options.TryLog($"Warning could not resolve field with name: {property.Name}", Options.LogLevel.Warning);
                            }
                        }
                    }
                }
            }

            return instance;
        }

        private static void SetObjectProperty(object obj, object value, Type type, string propertyName, Options options = null)
        {
            var propertyInfo = type.GetProperty(propertyName);

            if (propertyInfo != null)
            {
                propertyInfo.SetValue(obj, value);
            }
            else
            {
                var fieldInfo = type.GetField(propertyName);

                if (fieldInfo != null)
                {
                    fieldInfo.SetValue(obj, value);
                }
                else if (options != null)
                {
                    options.TryLog($"Warning could not resolve property with name: {propertyName}", Options.LogLevel.Warning);
                }
            }
        }


        /// <summary>
        /// Attempts to map an element to an object if a property with the 'TypeTag' is defined.
        /// If not the element will be mapped to a dictionary.
        /// </summary>
        /// <param name="element"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        private static object MapTo(this JsonElement element, Options options = null)
        {
            var typeTag = GetTypeTag(options);

            // is a type declared inside the object properties?
            if (element.TryGetProperty(typeTag, out var typeElement))
            {
                var type = ResolveType(typeElement.GetString(), options);

                return SetObjectProperties(Activator.CreateInstance(type), element.EnumerateObject(), options);
            }
            // else treat it as a dictionary
            return element.MapToDictionary(options);
        }

        /// <summary>
        /// Check if the cur
        /// </summary>
        /// <param name="inputVersion"></param>
        /// <returns></returns>
        /// <exception cref="JsonConfigException"></exception>
        private static bool ValidateVersion(string inputVersion, Options options = null)
        {
            var parts = inputVersion.Split('.');

            if (parts.Length > 1 && int.TryParse(parts[0].Trim(), out int majorVersion))
            {
                if (majorVersion > Version[0])
                {
                    var message = $"Input document is a higher version {inputVersion} than the current version of this JsonConfig.";

                    if (options != null)
                    {
                        options.TryLog(message, Options.LogLevel.Error);
                    }

                    throw new JsonConfigException(message);
                }
                return true;    
            }

            if (options != null)
            {
                var warningMessage = $"Warning: cannot validate this version '${inputVersion}'. ";
                options.TryLog(warningMessage, Options.LogLevel.Warning);
            }

            return true;
        }

        #region -- Contracts ------------------------------------------------------------------------------------------

        [System.Diagnostics.Conditional("DEBUG")]
        private static void Requires(bool expression, string message)
        {
            if (!expression)
            {
                throw new JsonConfigException(message);
            }
        }

        [System.Diagnostics.Conditional("DEBUG")]
        private static void RequiresNotNull(object obj, string message)
        {
            if (obj == null)
            {
                throw new JsonConfigException(message);
            }
        }

        [System.Diagnostics.Conditional("DEBUG")]
        private static void RequiresValueKind(JsonElement element, JsonValueKind kind, string message)
        {
            if (element.ValueKind != kind)
            {
                throw new JsonConfigException(message);
            }
        }

        #endregion

        #endregion
    }
}
