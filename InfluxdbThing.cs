using System;
using System.Threading.Tasks;
using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Core;
using Jtext103.CFET2.Core;
using InfluxDB.Client.Writes;
using Jtext103.CFET2.Core.Attributes;
using System.Threading;
using Jtext103.CFET2.Core.Event;
using Newtonsoft.Json;
using System.Collections;
using Jtext103.CFET2.Core.Log;


namespace Jtext103.CFET2.Things.InfluxdbThing
{
    public class InfluxdbThing : Thing
    {
        private InfluxdbThingConfig myConfig;
        private InfluxDBClient client;
        private string token;
        private string bucket;
        private string org;
        private Token Eventoken;
        private string measurementNameGlobal;
        private bool isEnable = true;
        private ICfet2Logger logger;


        public override void TryInit(object path)
        {
            logger = Cfet2LogManager.GetLogger("InfluxdbThing");
            myConfig = new InfluxdbThingConfig(getConfigFilePath((string)path));

            token = myConfig.token;
            bucket = myConfig.bucket;
            org = myConfig.org;

            client = InfluxDBClientFactory.Create(myConfig.ip, token.ToCharArray());
        }

        public override void Start()
        {
            logger.Info(myConfig.EventPath.ToString());
            for (int pathNum=0; pathNum　<　myConfig.EventPath.Length; pathNum++) {
                //logger.Info(myConfig.EventPath[pathNum]);
                Eventoken = MyHub.EventHub.Subscribe(new EventFilter(myConfig.EventPath[pathNum], myConfig.EventKind), receiveDatehandler);
            }
            
        }

        private void receiveDatehandler(EventArg e)
        {
            
            writeData(myConfig.measurementName, e.Source , (float)Convert.ToSingle(e.Sample.ObjectVal.ToString()));// await 多个handle是对应一个主线程还是多个线程
            logger.Info("path"+e.Source);
            logger.Info("value"+(string)e.Sample.ObjectVal);

            //nsole.WriteLine(e.Sample.ObjectVal.GetType().GetProperty("measurementname").GetValue(e.Sample.ObjectVal));
        }

        public void writeData(string measurementName, string metric, float data)
        {
            //logger.Info(metric);
            measurementNameGlobal = measurementName;
            if (isEnable == true)
            {
                using (var writeApi = client.GetWriteApi())
                {
                    //
                    // Write by POCO
                    //
                    //var Uout = new Uout { Value = data, Time = DateTime.UtcNow };
                    var point = PointData
                        .Measurement(measurementName)
                        .Field("value", data)
                        .Tag("metric",metric)
                        .Timestamp(DateTime.UtcNow, WritePrecision.Ns);

                    writeApi.WritePoint(bucket, org, point);
                    logger.Info("over");


                }
            }


            //client.Dispose();
        }

        [Cfet2Method]
        public void writeDataToInflux(string measurementName, string metric, float data)
        {
            measurementNameGlobal = measurementName;
            if (isEnable == true)
            {
                using (var writeApi = client.GetWriteApi())
                {
                    //
                    // Write by POCO
                    //
                    //var Uout = new Uout { Value = data, Time = DateTime.UtcNow };
                    var point = PointData
                        .Measurement(measurementName)
                        .Field("value", data)
                        .Tag("metric", metric)
                        .Timestamp(DateTime.UtcNow, WritePrecision.Ns);

                    writeApi.WritePoint(bucket, org, point);
                }
            }

        }

        [Cfet2Config(ConfigActions = ConfigAction.Get, Name = "ifEnable")]

        public bool ifEnable()
        {
            return isEnable;
        }

        [Cfet2Config(ConfigActions = ConfigAction.Set, Name = "ifEnable")]
        public void ifEnable(bool data)
        {
            isEnable = data;
        }

        [Cfet2Status]
        public string readData(string measurementName, string fieldName)
        {
            measurementNameGlobal = measurementName;
            var queryApi = client.GetQueryApi();
            var fluxQuery =
                "from(bucket: " + "\"" + bucket + "\"" + ")" +
                "|> range(start: -1h)" +
                "|> filter(fn: (r) => r[\"_measurement\"] == " + "\"" + measurementName + "\"" + ")" +
                "|> filter(fn: (r) => r[\"_field\"] == " + "\"" + fieldName + "\"" + ")";

            var T = new Task(() => goQuery(queryApi, fluxQuery));
            T.Start();
            return "past 1h";

        }


        public async Task goQuery(QueryApi queryApi, string fluxQuery)
        {
            try
            {
                var fluxdbtables = await queryApi.QueryAsync(fluxQuery, myConfig.org);
                fluxdbtables.ForEach(table =>
                {
                    var records = table.Records;
                    records.ForEach(record =>
                    {
                        Console.WriteLine(record.GetTime() + ":" + record.GetValue());
                    });
                });
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }
    }
}
