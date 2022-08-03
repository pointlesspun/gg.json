using System;

namespace gg.json
{
    /// <summary>
    /// (Attribution 2.0 Generic (CC BY 2.0))
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
