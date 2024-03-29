# Dot.LogStash

1. 安装：
   Install-Package Dot.LogStash

2. 配置日志：
   ```C#
   .ConfigureLogging((hostContext, builder) =>
    {
        builder.ClearProviders();
        builder.AddLog4Net($"Configs/{hostContext.HostingEnvironment.EnvironmentName}/log4net.config", watch: true);
    })
   ```
   
3. 日志文件配置：
   ```XML
   <appender name="ElasticSearchAppender" type="Dot.LogStash.ElasticSearchAppender, Dot.LogStash">
     <Servers>
       <Server>
         <Address>192.1.1.111</Address>
         <Port>9200</Port>
       </Server>
     </Servers>
     <LogEventFactoryType>Dot.LogStash.SimpleLogEventFactory, Dot.LogStash</LogEventFactoryType>
     <IndexName>your index name</IndexName> <!--eg:dot.logstash.sample_%{+yyyy-MM-dd}-->
     <IndexType>your index type</IndexType> <!--eg:log-->
     <Bulksize>1</Bulksize>
     <BulkIdleTimeout>1</BulkIdleTimeout>
     <IndexAsync>True</IndexAsync>
     <Template>
       <Name>your template name</Name> <!--eg:template.dot.logstash.sample-->
       <FileName>your template file</FileName> <!--eg:Configs/template.dot.logstash.sample.json-->
     </Template>
     <ElasticFilters>
       <Json>
         <SourceKey>Message</SourceKey>
         <FlattenJson>true</FlattenJson>
         <Separator>_</Separator>
       </Json>
       <Remove>
         <Key>Message</Key>
       </Remove>
       <Remove>
         <Key>MessageObject</Key>
       </Remove>
     </ElasticFilters>
   </appender>
   <logger name="ElasticSearchLogger" additivity="false">
     <level value="ALL"/>
     <appender-ref ref="ElasticSearchAppender"/>
   </logger>
   ```

4. 添加模板文件（如：Configs/template.dot.logstash.sample.json)
   ```JSON
   {
     "template": "dot.logstash.sample*",
     "settings": {
       "number_of_shards": 5,
       "number_of_replicas": 0
     },
     "mappings": {
       "log": {
         "properties": {
           "@timestamp": {
             "type": "date",
             "format": "yyyy-MM-dd HH:mm:ss"
           },
           "Request": { "type": "text" },
           "Response": { "type": "text" },
           "Timespan": { "type": "double" }
         }
       }
     }
   }
   ```
   
5. 定义日志实体类，需与模板文件中的定义一致
  ```C#
  public class TraceLog
  {
      public string Request { get; set; }
      public string Response { get; set; }
      public double Timespan { get; set; }
  }
  ```
  
6. 依赖注入 logger
  ```C#
  public SampleController(ILoggerFactory loggerFactory)
  {
      _logger = loggerFactory.CreateLogger("ElasticSearchLogger");
  }
  ```
  
7. 记录日志到 ElasticSearch
  ```C#
  var traceLog = new TraceLog();
  traceLog.Request = name;
  traceLog.Response = response;
  traceLog.Timespan = (DateTime.Now - requestTimespan).TotalMilliseconds;
  
  _logger.LogInformation(JsonConvert.SerializeObject(traceLog));
  ```
  
9. 在 Kibana 中查看日志
   ![image](https://github.com/gaoshilin/Dot.LogStash/blob/master/Dot.LogStash.Sample/KibanaSample.png)
