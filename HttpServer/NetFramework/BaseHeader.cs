using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetFramework
{
    // Http报文
    public class BaseHeader
    {
        public string Body { get; set; }

        public Encoding Encoding { get; set; }

        public string Content_Type { get; set; }

        public string Content_Length { get; set; }

        public string Content_Encoding { get; set; }

        public string ContentLanguage { get; set; }

        public Dictionary<string, string> Headers { get; set; }

        protected string GetHeaderByKey(string fieldName)
        {
            if (string.IsNullOrEmpty(fieldName))
            {
                return null;
            }

            try
            {
                bool hasKey = Headers.ContainsKey(fieldName);
                if (!hasKey)
                {
                    return null;
                }
                return Headers[fieldName];
            }
            catch (Exception)
            {
                return null;
            }
        }

        protected void SetHeaderByKey(string fieldName, string value)
        {
            if (string.IsNullOrEmpty(fieldName))
            {
                return;
            }

            bool hasKey = Headers.ContainsKey(fieldName);
            if (!hasKey)
            {
                Headers.Add(fieldName, value);
            }
            Headers[fieldName] = value;
        }
    }
}
