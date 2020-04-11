using System;
using System.Diagnostics;
using System.IO;
using log4net.Core;
using log4net.Util;

namespace Dot.LogStash
{
    /// <summary>
    /// https://www.elastic.co/guide/en/elasticsearch/reference/current/indices-templates.html
    /// </summary>
    public class TemplateInfo : IOptionHandler
    {
        public string Name { get; set; }
        public string FileName { get; set; }
        public bool IsValid { get; private set; }

        public TemplateInfo()
        {
            IsValid = false;
        }

        public void ActivateOptions()
        {
            if (string.IsNullOrEmpty(Name) || string.IsNullOrEmpty(FileName))
            {
                LogLog.Error(GetType(), "Template name or fileName is empty!");
                return;
            }

            FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, FileName);
            if (!File.Exists(FileName))
            {
                LogLog.Error(GetType(), string.Format("Could not find template file: {0}", FileName));
                return;
            }

            IsValid = true;
        }
    }
}