using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechnoPackaginListTracking.Dto.Common
{
    public class AppConfiguration
    {
   
            [Required]
            public string DocsUploadPath { get; set; }


            public AppConfiguration()
            {
                DocsUploadPath = string.Empty;
            }

            public AppConfiguration(string docsUploadPath)
            {
                DocsUploadPath = docsUploadPath;
            }

            public AppConfiguration(Dictionary<string, string> values)
            {
                DocsUploadPath = values[Constants.Keys.DocsUploadPath] ?? String.Empty;
            }

            public Dictionary<string, string> ToSettingsDictionary()
            {
                var result = new Dictionary<string, string>();

                if (DocsUploadPath != null)
                    result.Add(Constants.Keys.DocsUploadPath, DocsUploadPath);

                return result;

            }
        
    }
}
