using System;
using System.Collections.Generic;
using System.Reflection;

namespace gg.json
{
    public static partial class JsonConfig
    {
        /// <summary>
        /// Options used when parsing a json config string.
        /// </summary>
        public class Options
        {
            public enum LogLevel
            {
                Info,
                Warning,
                Error
            };

            /// <summary>
            /// Create new options and adds alias in the assembly of T.
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <returns></returns>
            public static Options Create<T>() where T : class => Create(typeof(T).Assembly);

            /// <summary>
            /// Create new options and adds the given aliases.
            /// </summary>
            /// <param name="objectTypes"></param>
            /// <returns></returns>
            public static Options Create(params (string name, Type type)[] objectTypes)
            {
                var options = new Options
                {
                    Aliases = new Dictionary<string, Type>()
                };

                foreach (var (name, type) in objectTypes)
                {
                    options.Aliases[name] = type;
                }

                return options;
            }

            /// <summary>
            /// Create new options and calls AddTypesInAssemby
            /// </summary>
            /// <param name="assembly"></param>
            /// <returns></returns>
            public static Options Create(Assembly assembly) =>
                new Options()
                {
                    Aliases = new Dictionary<string, Type>()
                }
                .AddTypesInAssembly(assembly);

            /// <summary>
            /// Shorthand / class aliases mapping to their respective Types.
            /// </summary>
            public Dictionary<string, Type> Aliases { get; set; }

            /// <summary>
            /// Tag used to identify an object type property in the json files.
            /// </summary>
            public string TypeTag { get; set; } = "__type";

            /// <summary>
            /// Character used to separate a property name from the type
            /// </summary>
            public char TypeSeparator { get; set; } = ':';

            /// <summary>
            /// Allow fully qualified types to create new instances (this being a security risk, is maybe
            /// not something your want).
            /// </summary>
            public bool AllowFullyQualifiedTypes { get; set; } = false;

            /// <summary>
            /// Sets the action to log messages from the JsonConfig read process.
            /// </summary>
            public Action<string, LogLevel> Log { get; set; } = null;                    

            /// <summary>
            /// All public concrete types in the given assembly to the Aliases 
            /// </summary>
            /// <param name="assembly"></param>
            /// <returns></returns>
            public Options AddTypesInAssembly(Assembly assembly)
            {
                foreach (var type in assembly.DefinedTypes)
                {
                    // only add types which are public, concrete or pass the typefilter 
                    if (!type.IsInterface
                        && !type.IsAbstract
                        && (type.IsPublic || type.IsNestedPublic))
                    {
                        Aliases[type.Name] = type;
                    }
                }

                return this;
            }

            /// <summary>
            /// Add all the basic types and their arrays
            /// </summary>
            /// <returns></returns>
            public Options AddDefaultAliases()
            {
                if (Aliases == null)
                {
                    Aliases = new Dictionary<string, Type>();
                }

                Aliases["int"] = typeof(int);
                Aliases["uint"] = typeof(uint);
                Aliases["float"] = typeof(float);
                Aliases["long"] = typeof(long);
                Aliases["ulong"] = typeof(ulong);
                Aliases["int[]"] = typeof(int[]);
                Aliases["float[]"] = typeof(float[]);
                Aliases["string[]"] = typeof(string[]);
                Aliases["double[]"] = typeof(double[]);
                Aliases["object[]"] = typeof(object[]);
                Aliases["bool[]"] = typeof(bool[]);
                Aliases["boolean[]"] = typeof(bool[]);
                Aliases["uint[]"] = typeof(uint[]);
                Aliases["long[]"] = typeof(long[]);
                Aliases["ulong[]"] = typeof(ulong[]);

                return this;
            }

            /// <summary>
            /// Attempts to write the string to the given log
            /// </summary>
            /// <param name="message"></param>
            /// <param name="level"></param>
            public void TryLog(string message, LogLevel level = LogLevel.Info)
            {
                Log?.Invoke(message, level);
            }
        }
    }
}
