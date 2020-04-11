using System;
using System.Collections.Generic;
using Dot.LogStash.Authentication;
using Dot.LogStash.Configuration;

namespace Dot.LogStash
{
    public interface IElasticsearchClient : IDisposable
    {
        ServerDataCollection Servers { get; }
        bool Ssl { get; }
        bool AllowSelfSignedServerCert { get; }
        AuthenticationMethodChooser AuthenticationMethod { get; set; }
        void PutTemplateRaw(string templateName, string rawBody);
        void IndexBulk(IEnumerable<InnerBulkOperation> bulk);
        void IndexBulkAsync(IEnumerable<InnerBulkOperation> bulk);
    }
}