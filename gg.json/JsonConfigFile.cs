using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace gg.json
{
    /// <summary>
    /// Methods to read a jsonconfig from file (json or xjsn).
    /// (Released under the MIT License (C) 2022 PointlessPun)
    /// </summary>
    public static class JsonConfigFile
    {
        /// <summary>
        /// File extension for the extended json format.
        /// </summary>
        public static readonly string XjsonExtension = ".xjsn";

        #region --- Public Methods ------------------------------------------------------------------------------------       

        /// <summary>
        /// Read a dictionary from the given path.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="typeAliases">optial list of tuples mapping an alias (string name) to a type.</param>
        /// <returns></returns>
        public static Dictionary<string, object> Read(string path, params (string name, Type type)[] typeAliases)
            => Read(path, JsonConfig.Options.Create(typeAliases));

        /// <summary>
        /// Reads an object of the given type path.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="path"></param>
        /// <param name="typeAliases">optial list of tuples mapping an alias (string name) to a type.</param>
        /// <returns></returns>
        public static T Read<T>(string path, params (string name, Type type)[] typeAliases)
            => Read<T>(path, JsonConfig.Options.Create(typeAliases));

        /// <summary>
        /// Reads an object of the given type path.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="path"></param>
        /// <param name="typeAliases">optial list of tuples mapping an alias (string name) to a type.</param>
        /// <returns></returns>
        public static T Read<T>(string path, Assembly assembly)
            => Read<T>(path, JsonConfig.Options.Create(assembly));

        /// <summary>
        /// Read a dictionary from the given path.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="options">options used to deserialize a json or xjsn file.</param>
        /// <returns></returns>
        public static Dictionary<string, object> Read(string path, JsonConfig.Options options) =>
            Path.GetExtension(path) == XjsonExtension
                ? JsonConfig.Deserialize<Dictionary<string, object>>(TranscribeXJsn<Dictionary<string, object>>(path, options), options)
                : ReadDictionary(path, options);

        /// <summary>
        /// Reads an object of the given type path.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fileName"></param>
        /// <param name="options">options used to deserialize a json or xjsn file.</param>
        /// <returns></returns>
        public static T Read<T>(string fileName, JsonConfig.Options options)
            => Path.GetExtension(fileName) == XjsonExtension
                ? JsonConfig.Deserialize<T>(TranscribeXJsn<T>(fileName, options), FillDefaultOptions<T>(options))
                : ReadJsonConfig<T>(fileName, FillDefaultOptions<T>(options));

        public static JsonConfig.Options FillDefaultOptions<T>(JsonConfig.Options options)
        {
            return (options == null || options.Aliases.Count == 0
                        ? JsonConfig.Options.Create(typeof(T).Assembly)
                        : options).AddDefaultAliases();
        }

        /// <summary>
        /// Read a configuration as a dictionary
        /// </summary>
        /// <param name="fileName">non null file name (path or partial path)</param>
        /// <param name="options">options used to deserialize the json string.</param>
        /// <returns></returns>
        public static Dictionary<string, object> ReadDictionary(string fileName, JsonConfig.Options options)
            => ReadJsonConfig<Dictionary<string, object>>(fileName, options);

        /// <summary>
        /// Read a json configuration and map it to the given type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fileName"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static T ReadJsonConfig<T>(string fileName, JsonConfig.Options options)
        {
            options.TryLog($"Reading config from {fileName}.");
            return JsonConfig.Deserialize<T>(File.ReadAllText(fileName), options);
        }

        #endregion

        #region --- Private Methods -----------------------------------------------------------------------------------        

        /// <summary>
        /// Transcribe a xjsn file to a pure json file
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fileName"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        private static string TranscribeXJsn<T>(string fileName, JsonConfig.Options options)
        {
            options.TryLog($"Reading & transcribing config from {fileName}.");
            
            var lines = File.ReadAllLines(fileName).ToList();
            var hasTypeDefined = false;
            var statementCount = 0;
            var typeTag = options == null || string.IsNullOrEmpty(options.TypeTag) ? JsonConfig.DefaultTypeTag : options.TypeTag;

            for (var i = 0; i < lines.Count;)
            {
                var l = lines[i].TrimStart();
                if (l.Length == 0 || (l.Length >= 2 && l[0] == '/' && l[1] == '/'))
                {
                    lines.RemoveAt(i);
                }
                else
                {
                    if (l.IndexOf(typeTag) == 1)
                    {
                        // the file contains something to the effect of __type at the top level
                        // no need to explicitely add it
                        hasTypeDefined = true;
                    }
                    else 
                    {
                        // something found, could be a json line. This is used to dsetermine whether or
                        // not we need to add a ',' at the end of the type 
                        statementCount++;
                    }

                    i++;
                }
            }

            lines.Insert(0, "{");

            // insert a type line if no type has been defined and the user is asking for a specific type via <T>
            if (typeof(T) != typeof(Dictionary<string, object>) && !hasTypeDefined)
            {
                options.TryLog($"Adding {typeTag}: {typeof(T)}.");
                lines.Insert(1, $"    \"{typeTag}\": \" {typeof(T).AssemblyQualifiedName}\"{(statementCount > 0 ? "," : "")}\n" );
            }

            lines.Add("}");

            return string.Join('\n', lines);
        }        

        #endregion
    }
}
