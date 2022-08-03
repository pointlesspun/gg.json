using System;
using System.Text;

namespace gg.json
{
    /// <summary>
    /// Extension methods to the string builder class
    /// (Released under the MIT License (C) 2022 PointlessPun)
    /// </summary>
    public static class StringBuilderExtensions
    {
        /// <summary>
        /// Append the string to builder adding indention number of spaces on the left.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="indentation"></param>
        /// <param name="str"></param>
        /// <returns></returns>
        public static StringBuilder Append(this StringBuilder builder, int indentation, string str)
        {
            return builder.Append("".PadLeft(indentation)).Append(str);
        }

        /// <summary>
        /// Append the line to builder adding indention number of spaces on the left.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="indentation"></param>
        /// <param name="str"></param>
        /// <returns></returns>
        public static StringBuilder AppendLine(this StringBuilder builder, int indentation, string str)
        {
            return builder.Append("".PadLeft(indentation)).AppendLine(str);
        }

        /// <summary>
        /// Create a json object from the given properties.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="indentation"></param>
        /// <param name="objectName"></param>
        /// <param name="properties"></param>
        /// <returns></returns>
        public static StringBuilder AppendJsonObject(this StringBuilder builder, int indentation = 0, string objectName = null, (string name, object value)[] properties = null)
        {
            if (String.IsNullOrEmpty(objectName))
            {
                builder.AppendLine(indentation, "{");
            }
            else
            {
                builder.AppendLine(indentation + 2, $"\"{objectName}\": {{");
            }

            if (properties != null)
            {

                for (var i = 0; i < properties.Length; i++)
                {
                    var (name, value) = properties[i];

                    if (value is string)
                    {
                        if (name[0] == '\\')
                        {
                            builder.Append(indentation + 4, $"\"{name.Substring(1)}\": {value}");
                        }
                        else
                        {
                            builder.Append(indentation + 4, $"\"{name}\": \"{value}\"");
                        }
                    }
                    else if (value is bool)
                    {
                        builder.Append(indentation + 4, $"\"{name}\": {value.ToString().ToLower()}");
                    }
                    else
                    {
                        builder.Append(indentation + 4, $"\"{name}\": {value}");
                    }

                    if (i < properties.Length - 1)
                    {
                        builder.AppendLine(",");
                    }
                    else
                    {
                        builder.AppendLine("");
                    }
                }
            }

            return builder.AppendLine(indentation, "}");
        }
    }
}
