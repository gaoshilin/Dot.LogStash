﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using Dot.LogStash.Authentication;
using Dot.LogStash.Configuration;
using Dot.LogStash.Extensions;
using Dot.LogStash.LogEventFactory;
using Dot.LogStash.SmartFormatters;
using log4net.Appender;
using log4net.Core;
using log4net.Util;

namespace Dot.LogStash
{
    public class ElasticSearchAppender : AppenderSkeleton, ILogEventFactoryParams
    {
        private List<InnerBulkOperation> _bulk = new List<InnerBulkOperation>();
        private IElasticsearchClient _client;
        private LogEventSmartFormatter _indexName;
        private LogEventSmartFormatter _indexType;
        private TolerateCallsBase _tolerateCalls;

        private readonly Timer _timer;

        public FixFlags FixedFields { get; set; }
        public bool SerializeObjects { get; set; }

        [Obsolete]
        public string DocumentIdSource
        {
            set
            {
                IndexOperationParams.AddParameter(new IndexOperationParam("_id", string.Format("{0}{1}{2}", "%{", value, "}" )));
            }
        }

        public IndexOperationParamsDictionary IndexOperationParams { get; set; }
        public int BulkSize { get; set; }
        public int BulkIdleTimeout { get; set; }
        public int TimeoutToWaitForTimer { get; set; }

        public int TolerateLogLogInSec
        {
            set => _tolerateCalls = TolerateCallsFactory.Create(value);
        }

        // elastic configuration
        public string Server { get; set; }
        public int Port { get; set; }
        public string Path { get; set; }
        public ServerDataCollection Servers { get; set; }
        public int ElasticSearchTimeout { get; set; }
        public bool Ssl { get; set; }
        public bool AllowSelfSignedServerCert { get; set; }
        public AuthenticationMethodChooser AuthenticationMethod { get; set; }
        public bool IndexAsync { get; set; }
        public TemplateInfo Template { get; set; }
        public ElasticAppenderFilters ElasticFilters { get; set; }
        public ILogEventFactory LogEventFactory { get; set; }
        public bool DropEventsOverBulkLimit { get; set; }
        [Obsolete]
        public string BasicAuthUsername { get; set; }
        [Obsolete]
        public string BasicAuthPassword { get; set; }
        public string LogEventFactoryType { get; set; }

        public string IndexName
        {
            set { _indexName = value; }
            get { return _indexName.ToString();  }
        }

        public string IndexType
        {
            set { _indexType = value; }
            get { return _indexType.ToString(); }
        }

        public ElasticSearchAppender()
        {
            FixedFields = FixFlags.Partial;
            SerializeObjects = true;

            BulkSize = 2000;
            BulkIdleTimeout = 5000;
            DropEventsOverBulkLimit = false;
            TimeoutToWaitForTimer = 5000;

            _tolerateCalls = new TolerateCallsBase();

            Servers = new ServerDataCollection();
            ElasticSearchTimeout = 10000;
            IndexName = "LogEvent-%{+yyyy.MM.dd}";
            IndexType = "LogEvent";
            IndexAsync = true;
            Template = null;

            _timer = new Timer(TimerElapsed, null, Timeout.Infinite, Timeout.Infinite);
            ElasticFilters = new ElasticAppenderFilters();

            AllowSelfSignedServerCert = false;
            Ssl = false;
            AuthenticationMethod = new AuthenticationMethodChooser();
            IndexOperationParams = new IndexOperationParamsDictionary();
        }

        public override void ActivateOptions()
        {
            InitElasticsearchClient();
            InitLogEventFactory().Configure(this);
            RestartTimer();
        }

        private void InitElasticsearchClient()
        {
            AddOptionalServer();

            CheckObsoleteAuth();

            _client = new WebElasticClient(Servers, ElasticSearchTimeout, Ssl, AllowSelfSignedServerCert, AuthenticationMethod);
            if (Template != null && Template.IsValid)
            {
                _client.PutTemplateRaw(Template.Name, File.ReadAllText(Template.FileName));
            }

            ElasticFilters.PrepareConfiguration(_client);
        }

        private void AddOptionalServer()
        {
            if (!string.IsNullOrEmpty(Server) && Port != 0)
            {
                var serverData = new ServerData { Address = Server, Port = Port, Path = Path };
                Servers.Add(serverData);
            }
        }

        private void CheckObsoleteAuth()
        {
            if(!string.IsNullOrEmpty(BasicAuthUsername) && !string.IsNullOrEmpty(BasicAuthPassword))
            {
                LogLog.Warn(GetType(), "BasicAuthUsername & BasicAuthPassword tags are obsolete, Please use AuthenticationMethod new tag");
                var auth = new BasicAuthenticationMethod { Username = BasicAuthUsername, Password = BasicAuthPassword };
                AuthenticationMethod.AddBasic(auth);
            }
        }

        private ILogEventFactory InitLogEventFactory()
        {
            if (!string.IsNullOrEmpty(LogEventFactoryType))
            {
                var array = LogEventFactoryType.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (array.Length == 2)
                {
                    try
                    {
                        var assemblyName = array[1];
                        var typeName = array[0];
                        var type = Assembly.Load(assemblyName).GetType(typeName);
                        LogEventFactory = Activator.CreateInstance(type) as ILogEventFactory;
                    }
                    catch
                    {
                        LogEventFactory = new BasicLogEventFactory();
                    }
                }
                else
                {
                    LogEventFactory = new BasicLogEventFactory();
                }
            }
            else
            {
                LogEventFactory = new BasicLogEventFactory();
            }

            return LogEventFactory;
        }

        private void RestartTimer()
        {
            var timeout = TimeSpan.FromMilliseconds(BulkIdleTimeout);
            _timer.Change(timeout, timeout);
        }

        /// <summary>
        /// On case of error or when the appender is closed before loading configuration change.
        /// </summary>
        protected override void OnClose()
        {
            DoIndexNow();

            // let the timer finish its job
            WaitHandle notifyObj = new AutoResetEvent(false);
            _timer.Dispose(notifyObj);
            notifyObj.WaitOne(TimeoutToWaitForTimer);
            _client.Dispose();
        }

        /// <summary>
        /// Add a log event to the ElasticSearch Repo
        /// </summary>
        /// <param name="loggingEvent"></param>
        protected override void Append(LoggingEvent loggingEvent)
        {
            if (_client == null || loggingEvent == null)
            {
                return;
            }

            if (DropEventsOverBulkLimit && _bulk.Count >= BulkSize)
            {
                _tolerateCalls.Call(() =>
                    LogLog.Warn(GetType(),
                        "Message lost due to bulk overflow! Set DropEventsOverBulkLimit to false in order to prevent that."),
                    GetType(), 0);
                return;
            }

            var logEvent = LogEventFactory.CreateLogEvent(loggingEvent);
            PrepareAndAddToBulk(logEvent);

            if (!DropEventsOverBulkLimit && _bulk.Count >= BulkSize && BulkSize > 0)
            {
                DoIndexNow();
            }
        }

        /// <summary>
        /// Prepare the event and add it to the BulkDescriptor.
        /// </summary>
        /// <param name="logEvent"></param>
        private void PrepareAndAddToBulk(Dictionary<string, object> logEvent)
        {
            ElasticFilters.PrepareEvent(logEvent);
            var indexName = _indexName.Format(logEvent).ToLower();
            var indexType = _indexType.Format(logEvent);
            var indexOperationParamValues = IndexOperationParams.ToDictionary(logEvent);

            var operation = new InnerBulkOperation
            {
                Document = logEvent,
                IndexName = indexName,
                IndexType = indexType,
                IndexOperationParams = indexOperationParamValues
            };

            lock (_bulk)
            {
                _bulk.Add(operation);
            }
        }

        public void TimerElapsed(object state)
        {
            DoIndexNow();
        }

        /// <summary>
        /// Send the bulk to Elasticsearch and creating new bluk.
        /// </summary>
        private void DoIndexNow()
        {
            var bulkToSend = Interlocked.Exchange(ref _bulk, new List<InnerBulkOperation>());
            if (bulkToSend.Count > 0)
            {
                try
                {
                    if (IndexAsync)
                    {
                        _client.IndexBulkAsync(bulkToSend);
                    }
                    else
                    {
                        _client.IndexBulk(bulkToSend);
                    }
                }
                catch (Exception ex)
                {
                    LogLog.Error(GetType(), "IElasticsearchClient inner exception occurred", ex);
                }
            }
        }
    }
}
