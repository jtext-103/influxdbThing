using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using System.IO;

namespace Jtext103.CFET2.Things.InfluxdbThing
{
    class InfluxdbThingConfig
    {
        public string token { get; set; }
        public string bucket { get; set; }
        public string org { get; set; }
        public string ip { get; set; }
        public string measurementName { get; set; }
        public string[] EventPath { get; set; }
        public string EventKind { get; set; }


        public InfluxdbThingConfig(string filePath)
        {
            JsonConvert.PopulateObject(File.ReadAllText(filePath, Encoding.Default), this);
        }
    }
}
