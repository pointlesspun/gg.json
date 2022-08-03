using System;

namespace gg.json
{
    /// <summary>
    /// (Released under the MIT License (C) 2022 PointlessPun)
    /// </summary>
    public class JsonConfigException : Exception
    {
        public JsonConfigException()
        {
        }

        public JsonConfigException(string message)
            : base(message)
        {
        }
    }
}
