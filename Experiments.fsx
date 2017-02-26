
// Data from XML

open System

let xmlDoc path =
    let xdoc = Xml.XmlDocument()
    xdoc.Load(path : string) |> ignore
    xdoc

let doc = xmlDoc "export.xml"

// The document concists of a very long list of health record nodes 
// that can be matched with the XPath expression /HealthData/Record
let healthRecordXml = 
    doc.SelectNodes "/HealthData/Record" 
        |> Seq.cast<Xml.XmlNode> 

Seq.length healthRecordXml // => 729456

let outerXml (node: Xml.XmlNode) = node.OuterXml

Seq.head healthRecordXml |> outerXml

// "<Record type="HKQuantityTypeIdentifierBodyMassIndex" sourceName="Lifesum" sourceVersion="7.1.0" 
// unit="count" creationDate="2016-11-07 19:23:05 +0100" startDate="2016-10-29 23:00:00 +0100" 
// endDate="2016-10-29 23:00:00 +0100" value="26.3036" />"

// See https://developer.apple.com/reference/healthkit#1664403

let xmlNodeAttributeValue attributeName (elem: Xml.XmlNode)  = 
    match Option.ofObj(elem.Attributes.GetNamedItem(attributeName)) with
    | Some (v) -> Option.ofObj v.Value
    | _ -> None

// Records by type
let recordXmlByType = 
    healthRecordXml
    |> Seq.groupBy (xmlNodeAttributeValue "type" >> Option.get)
    |> dict
    
let recordTypeCount = 
    recordXmlByType
    |> Seq.map (fun (KeyValue(t, r)) -> (t, Seq.length r)) 
    |> Seq.toList


// [("HKQuantityTypeIdentifierBodyMassIndex", 6);
//    ("HKQuantityTypeIdentifierHeight", 1);
//    ("HKQuantityTypeIdentifierBodyMass", 7);
//    ("HKQuantityTypeIdentifierHeartRate", 88333);
//    ("HKQuantityTypeIdentifierStepCount", 170738);
//    ("HKQuantityTypeIdentifierDistanceWalkingRunning", 209347);
//    ("HKQuantityTypeIdentifierBasalEnergyBurned", 99847);
//    ("HKQuantityTypeIdentifierActiveEnergyBurned", 142656);
//    ("HKQuantityTypeIdentifierFlightsClimbed", 7352);
//    ("HKQuantityTypeIdentifierDietaryFatTotal", 178);
//    ("HKQuantityTypeIdentifierDietaryFatSaturated", 122);
//    ("HKQuantityTypeIdentifierDietaryCholesterol", 35);
//    ("HKQuantityTypeIdentifierDietarySodium", 106);
//    ("HKQuantityTypeIdentifierDietaryCarbohydrates", 206);
//    ("HKQuantityTypeIdentifierDietaryFiber", 83);
//    ("HKQuantityTypeIdentifierDietarySugar", 124);
//    ("HKQuantityTypeIdentifierDietaryEnergyConsumed", 242);
//    ("HKQuantityTypeIdentifierDietaryProtein", 207);
//    ("HKQuantityTypeIdentifierDietaryPotassium", 87);
//    ("HKQuantityTypeIdentifierAppleExerciseTime", 7085);
//    ("HKCategoryTypeIdentifierSleepAnalysis", 440);
//    ("HKCategoryTypeIdentifierAppleStandHour", 2232);
//    ("HKCategoryTypeIdentifierMindfulSession", 22)]


// FsLab is a collection of libraries for data-science. 

#load "packages/FsLab/Themes/DefaultWhite.fsx"
#load "packages/FsLab/FsLab.fsx"

// It includes XPlot a cross-platform data visualization package for the F# 
// programming language powered by popular JavaScript charting libraries Google Charts and Plotly.

// XPlot Documentation http://tahahachana.github.io/XPlot//#Documentation

open XPlot.GoogleCharts

recordTypeCount
    |> List.map (fun (k, v) -> 
        (k.Replace("HKQuantityTypeIdentifier", "")
            .Replace("HKCategoryTypeIdentifier", "") + " " + v.ToString("N0"), v))
    |> List.sortByDescending snd
    |> Chart.Pie
    |> Chart.WithOptions (
        Options(
            width = 1200,
            height = 1200,
            legend = Legend(),
            sliceVisibilityThreshold = 0,
            pieHole = 0.4))


// The Xml Declaration gives some information about the structure of the file

doc.OuterXml.Substring(0, 2548);;


// "<?xml version="1.0" encoding="UTF-8"?><!DOCTYPE HealthData[
// <!-- HealthKit Export Version: 3 -->
// <!ELEMENT HealthData (ExportDate,Me,(Record|Correlation|Workout|ActivitySummary)*)>


// <!ATTLIST Record
//   type          CDATA #REQUIRED
//   unit          CDATA #IMPLIED
//   value         CDATA #IMPLIED
//   sourceName    CDATA #REQUIRED
//   sourceVersion CDATA #IMPLIED
//   device        CDATA #IMPLIED
//   creationDate  CDATA #IMPLIED
//   startDate     CDATA #REQUIRED
//   endDate       CDATA #REQUIRED
// >

let healthRecordFields = ["type"; "unit"; "value"; "sourceName"; "sourceVersion"; "device"; "creationDate"; "startDate"; "endDate"]
let getStringField elem field =
    (field, xmlNodeAttributeValue field elem |> function | Some (s) -> s | None -> "")

let getHealthRecord elem =
    healthRecordFields |> Seq.map (getStringField elem) |> dict

let getRecords ofType = 
    recordXmlByType.Item(ofType)
    |> Seq.map getHealthRecord


// Deedle
// http://bluemountaincapital.github.io/Deedle/index.html
// Deedle is a library for data and time series manipulation. The library implements a wide range of operations for data 
// manipulation including advanced indexing and slicing, joining and aligning data, handling of missing values, grouping 
// and aggregation, statistics and more. 

#load "packages/Deedle/Deedle.fsx"
open Deedle

// Map Health Records into Deedle Frames

// Deedle have two basic concepts, Series and Frames. A Series is a series of values indexed by an index. We will mainly
// work with time series where the keys are timestamps. A Frame is collection of series that share the same row key. We will
// use frames eg. to group properties of healt records.  

// Convert a sequence of health records to a data Frame. We will index it by row/record position to start with since
// multiple records could have the same start date.

let getFrame (records : seq<Collections.Generic.IDictionary<string,string>>) =
    records
    |> Seq.mapi (fun index record -> record |> Seq.map (fun (KeyValue(k, v)) -> (index, k, v)))
    |> Seq.concat
    |> Frame.ofValues

// Hearth records

getRecords "HKQuantityTypeIdentifierHeartRate"
    |> Seq.head
    |> Seq.map (|KeyValue|) |> Seq.toList 

// [("type", "HKQuantityTypeIdentifierHeartRate"); ("unit", "count/min");
//    ("value", "74"); ("sourceName", "Henrik’s Apple Watch");
//    ("sourceVersion", "3.1");
//    ("device",
//     "<<HKDevice: 0x174285dc0>, name:Apple Watch, manufacturer:Apple, model:Watch, hardware:Watch2,4, software:3.1>");
//    ("creationDate", "2016-11-06 09:41:19 +0100");
//    ("startDate", "2016-11-06 09:41:19 +0100");
//    ("endDate", "2016-11-06 09:41:19 +0100")]

let hrUnits : string list =
    getRecords "HKQuantityTypeIdentifierHeartRate" 
    |> getFrame
    |> Frame.getCol "unit"
    |> Series.values
    |> Seq.distinct
    |> Seq.toList

// => hrUnits : string list = ["count/min"]

let hasZeroDuration (row : ObjectSeries<_>) = 
    row.GetAs<DateTime>("endDate").Subtract(row.GetAs<DateTime>("startDate")).TotalMinutes = 0.0
let hrEntriesWithPeriods =
    getRecords "HKQuantityTypeIdentifierHeartRate" 
    |> getFrame
    |> Frame.filterRowValues (hasZeroDuration >> not)

//          type                              unit      value sourceName sourceVersion device creationDate              startDate                 endDate
// 8686  -> HKQuantityTypeIdentifierHeartRate count/min 82    Fitness    20161115.1602        2016-11-19 18:35:09 +0100 2016-11-19 18:34:37 +0100 2016-11-19 18:35:02 +0100
// 9210  -> HKQuantityTypeIdentifierHeartRate count/min 77    Fitness    20161115.1602        2016-11-19 18:45:01 +0100 2016-11-19 18:26:33 +0100 2016-11-19 18:43:04 +0100
// 13744 -> HKQuantityTypeIdentifierHeartRate count/min 125   Fitness    20161115.1602        2016-11-21 18:11:59 +0100 2016-11-21 09:29:42 +0100 2016-11-21 10:24:54 +0100
// 14074 -> HKQuantityTypeIdentifierHeartRate count/min 137   Fitness    20161115.1602        2016-11-21 18:51:19 +0100 2016-11-21 18:11:20 +0100 2016-11-21 18:48:26 +0100
// 18369 -> HKQuantityTypeIdentifierHeartRate count/min 114   Fitness    20161115.1602        2016-11-28 09:29:25 +0100 2016-11-24 08:47:45 +0100 2016-11-24 10:05:28 +0100
// 18373 -> HKQuantityTypeIdentifierHeartRate count/min 133   Fitness    20161115.1602        2016-11-28 09:29:26 +0100 2016-11-22 18:07:56 +0100 2016-11-22 18:50:20 +0100
// 18375 -> HKQuantityTypeIdentifierHeartRate count/min 116   Fitness    20161115.1602        2016-11-28 09:29:27 +0100 2016-11-22 07:39:35 +0100 2016-11-22 08:46:59 +0100
// 18384 -> HKQuantityTypeIdentifierHeartRate count/min 113   Fitness    20161115.1602        2016-11-28 09:45:28 +0100 2016-11-28 07:56:25 +0100 2016-11-28 09:39:27 +0100
// 18386 -> HKQuantityTypeIdentifierHeartRate count/min 94    Fitness    20161115.1602        2016-11-28 09:49:57 +0100 2016-11-28 09:43:25 +0100 2016-11-28 09:49:03 +0100
// 18792 -> HKQuantityTypeIdentifierHeartRate count/min 135   Fitness    20161115.1602        2016-11-28 18:51:14 +0100 2016-11-28 18:07:29 +0100 2016-11-28 18:49:57 +0100


// It looks like we get HeartRate records from different sources (watch, chest strap, etc) with 
// same start date. Apple usually prioritizes different sources and use one. But we will 
// cheet and pick a one using Seq.distinctBy. I have only one heart so it should be the same.

getRecords "HKQuantityTypeIdentifierHeartRate" 
    |> getFrame
    |> Frame.indexRowsDate "startDate"

// => System.ArgumentException: Duplicate key '11/19/2016 6:30:15 PM'. Duplicate keys are not allowed in the index.

let heartRateFrame = 
    getRecords "HKQuantityTypeIdentifierHeartRate" 
    |> Seq.distinctBy (fun r -> r.Item("startDate")) // Cheeting
    |> getFrame
    |> Frame.filterRowValues hasZeroDuration 
    |> Frame.indexRowsDate "startDate"
    |> Frame.sortRowsByKey

//                          type                              unit      value sourceName           sourceVersion device
//                            creationDate              endDate
// 11/6/2016 9:41:19 AM  -> HKQuantityTypeIdentifierHeartRate count/min 74    Henrik’s Apple Watch 3.1           <<HKDevice: 0x174285dc0>, name:Apple Watch, manufacturer:Apple, model:Watch, hardware
// :Watch2,4, software:3.1>   2016-11-06 09:41:19 +0100 2016-11-06 09:41:19 +0100
// 11/6/2016 9:41:25 AM  -> HKQuantityTypeIdentifierHeartRate count/min 68    Henrik’s Apple Watch 3.1           <<HKDevice: 0x174289ba0>, name:Apple Watch, manufacturer:Apple, model:Watch, hardware
// :Watch2,4, software:3.1>   2016-11-06 09:41:25 +0100 2016-11-06 09:41:25 +0100
// 11/6/2016 9:46:17 AM  -> HKQuantityTypeIdentifierHeartRate count/min 79    Henrik’s Apple Watch 3.1           <<HKDevice: 0x174289740>, name:Apple Watch, manufacturer:Apple, model:Watch, hardware
// :Watch2,4, software:3.1>   2016-11-06 09:46:27 +0100 2016-11-06 09:46:17 +0100
// 11/6/2016 9:48:47 AM  -> HKQuantityTypeIdentifierHeartRate count/min 74    Henrik’s Apple Watch 3.1           <<HKDevice: 0x17428a5a0>, name:Apple Watch, manufacturer:Apple, model:Watch, hardware
// :Watch2,4, software:3.1>   2016-11-06 09:51:40 +0100 2016-11-06 09:48:47 +0100
// 11/6/2016 9:57:04 AM  -> HKQuantityTypeIdentifierHeartRate count/min 74    Henrik’s Apple Watch 3.1           <<HKDevice: 0x17428a640>, name:Apple Watch, manufacturer:Apple, model:Watch, hardware
// :Watch2,4, software:3.1>   2016-11-06 09:57:54 +0100 2016-11-06 09:57:04 +0100
// 11/6/2016 10:06:07 AM -> HKQuantityTypeIdentifierHeartRate count/min 81    Henrik’s Apple Watch 3.1           <<HKDevice: 0x17428a6e0>, name:Apple Watch, manufacturer:Apple, model:Watch, hardware


// Value is a string. Let's add it's parsed number value as a new column

heartRateFrame?HR <- heartRateFrame |> Frame.mapRowValues (fun row -> row.GetAs<double>("value"))

// Hearth rate readings are read more often when eg, exercising or using the chest strap
// we therefore give them a weight by calculation the duration between readings. 

let secondsBetweenRows field maxValue aFrame   =
    aFrame 
    |> Frame.getCol field
    |> Series.pairwise    
    |> Series.map (fun _ (v1: DateTime, v2:DateTime) -> Math.Min(v2.Subtract(v1).TotalSeconds, maxValue))


// The watch usually read every 5th minute so lets assume that
// gaps larger than 5 minutes (300s) are periods without readings  

heartRateFrame?Seconds <- secondsBetweenRows "endDate" 300.0 heartRateFrame

heartRateFrame?Seconds
    |> Series.values
    |> Seq.map (fun v -> ("Seconds", v))
    |> Chart.Histogram
    |> Chart.WithOptions(
        Options(
            width = 1200,
            scaleType =  "mirrorLog",
            histogram = Histogram(bucketSize = 10)))

heartRateFrame?Seconds
    |> Series.filterValues ((>) 10.0)
    |> Series.values
    |> Seq.map (fun v -> ("Seconds", v))
    |> Chart.Histogram
    |> Chart.WithOptions(
        Options(
            width = 1200,
            scaleType =  "mirrorLog",
            histogram = Histogram(bucketSize = 1))) 


// Sleep

getRecords "HKCategoryTypeIdentifierSleepAnalysis" 
    |> Seq.map (fun r -> r.Item("value")) 
    |> Seq.distinct
    |> Seq.toList

// ["HKCategoryValueSleepAnalysisInBed"; "HKCategoryValueSleepAnalysisAsleep";
//   "HKCategoryValueSleepAnalysisAwake"]

let hasValue value (row : Collections.Generic.IDictionary<string, string>) = 
    row.Item("value") = value 

getRecords "HKCategoryTypeIdentifierSleepAnalysis" 
    |> Seq.filter (hasValue "HKCategoryValueSleepAnalysisAsleep")

// Sleep readings from different sources can overlap.

let setField field value series =
    Series.map (fun k v -> if k = field then value :> obj else v) series

let testSeries = series ["startDate", DateTime.MinValue :> obj; "endDate", DateTime.MaxValue :> obj]
setField "startDate" DateTime.Now testSeries

let mergeOverlappingValues (aFrame: Frame<_, string>) =
    aFrame
    |> Frame.sortRows "startDate"
    |> Frame.rows
    |> Series.foldValues (fun (state: ObjectSeries<string> list) nextRow  -> 
        match state, nextRow with
        | [], next -> [next]
        | current::tail, next -> 
            let currentEnd = current.GetAs<DateTime>("endDate")
            let nextStart = next.GetAs<DateTime>("startDate")
            if currentEnd >= nextStart  then // overlaps
                let nextEnd = next.GetAs<DateTime>("endDate")
                if currentEnd >= nextEnd then current::tail //within
                else ObjectSeries(setField "endDate" nextEnd current)::tail // merge
            else 
                next::current::tail 
    ) [] 
    |> List.rev
    |> Frame.ofRowsOrdinal;
 
let testDates =  
    [
        (1, "startDate", DateTime(2013,1,1, 19, 00, 00));
        (1, "endDate", DateTime(2013,1,1, 20, 00, 00))
        (2, "startDate", DateTime(2013,1,1, 19, 30, 00));
        (2, "endDate", DateTime(2013,1,1, 19, 45, 00))
        (3, "startDate", DateTime(2013,1,1, 21, 00, 00));
        (3, "endDate", DateTime(2013,1,1, 22, 00, 00))
        (4, "startDate", DateTime(2013,1,1, 21, 30, 00));
        (4, "endDate", DateTime(2013,1,1, 22, 30, 00));
        (5, "startDate", DateTime(2013,1,1, 22, 30, 00));
        (5, "endDate", DateTime(2013,1,1, 23, 30, 00))]
    |> Frame.ofValues

mergeOverlappingValues testDates

//     startDate           endDate
// 0 -> 1/1/2013 7:00:00 PM 1/1/2013 8:00:00 PM
// 1 -> 1/1/2013 9:00:00 PM 1/1/2013 10:30:00 PM

// Back to sleep

let overlappingSleepRecords =
    getRecords "HKCategoryTypeIdentifierSleepAnalysis" 
    |> Seq.filter (hasValue "HKCategoryValueSleepAnalysisAsleep")
    |> getFrame

overlappingSleepRecords |> Frame.countRows // => 233

overlappingSleepRecords |> Frame.countRows // => 233
let sleepRecordsFrame = 
    overlappingSleepRecords
    |> mergeOverlappingValues
    |> Frame.indexRowsDate "startDate"
    |> Frame.sortRowsByKey

sleepRecordsFrame |> Frame.countRows // => 217

sleepRecordsFrame?Duration <- sleepRecordsFrame 
    |> Frame.mapRows (fun k r -> r.GetAs<DateTime>("endDate").Subtract(k))

sleepRecordsFrame?Hours <- sleepRecordsFrame 
    |> Frame.mapRows (fun _ r -> r.GetAs<TimeSpan>("Duration").TotalHours)

sleepRecordsFrame?Hours
    |> Series.values
    |> Seq.map (fun v -> ("Hours", v))
    |> Chart.Histogram
    |> Chart.WithOptions(
        Options(
            width = 1200,
            scaleType =  "mirrorLog",
            histogram = Histogram(bucketSize = 1)))

// Exercise
let exerciseRecordsFrame = 
     getRecords "HKQuantityTypeIdentifierAppleExerciseTime" 
    |> getFrame
    |> mergeOverlappingValues
    |> Frame.indexRowsDate "startDate"
    |> Frame.sortRowsByKey

exerciseRecordsFrame?Duration <- exerciseRecordsFrame 
    |> Frame.mapRows (fun k r -> r.GetAs<DateTime>("endDate").Subtract(k))

// Stand
let standRecordsFrame = 
    getRecords "HKCategoryTypeIdentifierAppleStandHour" 
    |> Seq.filter (hasValue "HKCategoryValueAppleStandHourStood")
    |> getFrame
    |> mergeOverlappingValues
    |> Frame.indexRowsDate "startDate"
    |> Frame.sortRowsByKey

standRecordsFrame?Duration <- standRecordsFrame 
    |> Frame.mapRows (fun k r -> r.GetAs<DateTime>("endDate").Subtract(k))

// Merge activity into HR

// For every time stamp in HR readings I would like to know
// if I was awake, exercising or standing.

// Deedle can join frames by eg. merging a row with the nearest timestamp
// before a timestamp. Let's say I was a sleep at 23:30 and have
// a HR reading 23:31 I can assume I was still a asleep and and set a  
// IsAsleep flag on the reading. 

// Activity frames have a start and end date but we need a timestamp and a flag
// We need to convert rows like:
// ["value", "HKCategoryValueSleepAnalysisInBed"; "startDate", 23:30, endDate: 23:45] 
// to:
// [23:30, true]
// [23:45, false]

let tsFrame key aFrame  =
    aFrame 
    |> Frame.mapRows (fun startDate row -> [(startDate, key, true); (row.GetAs<DateTime>("endDate"), key, false)])
    |> Series.values
    |> Seq.concat
    |> Frame.ofValues
    |> Frame.sortRowsByKey
    
// Create a frame with a timestamp as key and boolean as value 
tsFrame "IsAsleep" sleepRecordsFrame

// 2/25/2017 10:51:32 AM  -> False
// 2/26/2017 2:33:37 AM   -> True
// 2/26/2017 7:44:02 AM   -> False
// 2/26/2017 7:51:59 AM   -> True
// 2/26/2017 9:19:33 AM   -> False
// 2/28/2017 12:41:51 AM  -> True
// 2/28/2017 1:37:35 AM   -> False
// 2/28/2017 1:45:33 AM   -> True
// 2/28/2017 6:24:06 AM   -> False

let mergeTsFrame f1 f2 = Frame.joinAlign JoinKind.Right Lookup.ExactOrSmaller f1 f2
let hrFrameWithActivity = 
    heartRateFrame
    |> mergeTsFrame (tsFrame "IsAsleep" sleepRecordsFrame)
    |> mergeTsFrame (tsFrame "IsStanding" standRecordsFrame)
    |> mergeTsFrame (tsFrame "IsExercising" exerciseRecordsFrame)


// Looks like I'm sleep walking :)
hrFrameWithActivity
    |> Frame.filterRowsBy "IsAsleep" true
    |> Frame.filterRowsBy "IsStanding" true
    |> Frame.countRows

// => 553

// It also looks like I have forgotten to log all exercise
hrFrameWithActivity
    |> Frame.filterRowsBy "IsExercising" false
    |> Frame.filterRowsBy "IsAsleep" false
    |> Frame.filterRowValues (fun r -> r.GetAs<float>("HR") > 120.0)
    |> Frame.countRows

// => 261

// Awake resting HR

hrFrameWithActivity?startDate <- hrFrameWithActivity.RowKeys;
let restingRecordsFrame =
        hrFrameWithActivity
        |> Frame.sortRowsByKey
        |> Frame.filterRowsBy "IsAsleep" false
        |> Frame.filterRowsBy "IsExercising" false
        |> Frame.rows
        |> Series.foldValues (fun (state: ObjectSeries<string> list) nextRow  -> 
            match state, nextRow with
            | [], next -> [next]
            | current::tail, next -> 
                // If HR is > 110 and readings are more often than
                // every 10s then probably exercising but not logged
                if 
                    current.GetAs<float>("HR") > 110.0 && 
                    next.GetAs<float>("HR") > 110.0 &&
                    current.GetAs<DateTime>("startDate").Subtract(
                        next.GetAs<DateTime>("startDate")
                    ).TotalSeconds < 10.0
                then next::tail
                else next::current::tail 
        ) [] 
        |> List.rev
        |> Frame.ofRowsOrdinal
        |> Frame.indexRowsDate "startDate"
        |> Frame.sortRowsByKey

restingRecordsFrame
    |> Frame.filterRowValues (fun r -> r.GetAs<float>("HR") > 130.0)
    |> Frame.countRows

// => 9

let restingMeanByDay =
    restingRecordsFrame 
    |> Frame.getCol "HR"
    |> Series.chunkWhileInto (fun d1 d2 -> d1.Date = d2.Date) Stats.mean

// 2/23/2017 12:03:13 AM  -> 67.8488372093023
// 2/27/2017 9:46:26 AM   -> 66.8031496062992
// 2/28/2017 12:02:20 AM  -> 68.3812949640288

// Experiments

let commonChartOptions = Options(width = 1280, legend = Legend(position = "bottom"));
let seriesStats (hr : Series<'K, float> when 'K : equality) = 
    series [
        "Mean" => round (Stats.mean hr)
        "Max" => Option.get (Stats.max hr)
        "Min" => Option.get (Stats.min hr)
        "Median" => Stats.median hr]

seriesStats restingRecordsFrame?HR

// Mean   -> 77
// Max    -> 165
// Min    -> 41
// Median -> 74

// Stats by day by creating chunks of HR readings
// with the same day and applying the stats function on the day
let restingStatsByDay =
    restingRecordsFrame 
    |> Frame.getCol "HR"
    |> Series.chunkWhileInto (fun d1 d2 -> d1.Date = d2.Date) seriesStats
    |> Frame.ofRows

restingStatsByDay
    |> Chart.Line 
    |> Chart.WithOptions(commonChartOptions)


// Per week day

restingRecordsFrame
    |> Frame.groupRowsUsing (fun ts _ -> ts.DayOfWeek.ToString())
    |> Frame.getCol "HR"
    |> Series.applyLevel fst seriesStats
    |> Frame.ofRows
    |> Frame.sliceRows (Globalization.CultureInfo.CurrentCulture.DateTimeFormat.DayNames)
    |> Chart.Line 
    |> Chart.WithOptions(commonChartOptions)

// Per hour of the day

restingRecordsFrame
    |> Frame.groupRowsUsing (fun ts _ -> ts.Hour)
    |> Frame.getCol "HR"
    |> Series.sortByKey
    |> Series.applyLevel fst seriesStats
    |> Frame.ofRows 
    |> Chart.Line 
    |> Chart.WithOptions(commonChartOptions)

// HR Zones
// A stacked column chart with % of minutes in
// different HR zones

// HR zones from Heart Watch app http://heartwatch.tantsissa.com/
// a great iOS app

let zones = [
    (100.0, "> 100", "#CC0000");
    (80.0, "80-99", "#674EA7");
    (55.0, "55-79", "#3D85C6");
    (40.0, "40-54", "#BF737A");
    (0.0, "< 40", "#804D52")]

let zoneNameByHr (hr : double) = 
    let (_, name, _) = zones |> List.find (fun (limit, _, _) -> hr > limit)
    name
let zoneColorByName zoneName = 
    let (_, _ , color) = zones |> List.find (fun (_, name, _) -> name = zoneName)
    color
let zoneNames = zones |> List.map Pair.get2Of3 |> Seq.ofList |> Seq.rev

// Minutes per day in each zone
// using a pivot table with date as rows
// a column for each HR zone
// amd values as minutes in zone and day
let zoneByDay = 
    restingRecordsFrame
    |> Frame.pivotTable
        (fun k r -> k.Date)
        (fun k r -> zoneNameByHr (r.GetAs<float>("HR")))
        (fun frame -> (frame?Seconds |> Series.values |> Seq.sum) / 60.0)
    |> Frame.sliceCols zoneNames

let zoneOptions title vAxisTitle =
    Options(
            title = title,
            isStacked = true, // percent
            connectSteps = true,
            areaOpacity = 0.7,
            chartArea = ChartArea(width = "90%"),
            colors = (zoneByDay.Columns.Keys |> Seq.map zoneColorByName |> Array.ofSeq), 
            width = 1280, 
            vAxis = Axis(title = vAxisTitle),
            legend = Legend(position = "bottom"))

zoneByDay
    |> Chart.SteppedArea 
    |> Chart.WithOptions(zoneOptions "Minutes per HR zone and day" "minutes")

// I don't have the same number of readings every day since
// I don't wear it all hours and also exercise less some days.

// Logged resting HR minutes per day 
let zoneByDayTotal = 
    restingRecordsFrame?Seconds 
    |> Series.groupInto (fun k r -> k.Date) (fun _ r -> (r |> Series.values |> Seq.sum) / 60.0)

// Deedle allow mathematical operations to be applied to series
// Deedle automatically aligns the series and then applies the operation 
// on corresponding elements

let zoneByDayPercentage = zoneByDay / zoneByDayTotal * 100

// zone minutes in % per day:

zoneByDayPercentage
    |> Chart.SteppedArea 
    |> Chart.WithOptions(zoneOptions "% Minutes per HR zone and day" "% minutes")

// Sleep

sleepRecordsFrame?TotalHours <- sleepRecordsFrame |> Frame.mapRowValues (fun row -> row.GetAs<TimeSpan>("Duration").TotalHours)
let nextMidday (ts : System.DateTime) = 
    let todaysMidday = ts.Date.AddHours(12.0);
    if ts < todaysMidday then todaysMidday else todaysMidday.AddDays(1.0) 

// Sleep per day

let sleepPerDaySeries : Series<DateTime,float> = 
    sleepRecordsFrame
    |> Frame.groupRowsUsing (fun ts _ -> nextMidday ts)
    |> Frame.getCol "TotalHours"
    |> Series.sortByKey
    |> Series.applyLevel fst Stats.sum

sleepPerDaySeries
    |> Chart.Column 
    |> Chart.WithOptions(commonChartOptions)

let sleepPerDay =
    sleepPerDaySeries.[System.DateTime(2016, 11, 01) .. System.DateTime(2017, 02, 28)]

sleepPerDay
    |> Chart.Column 
    |> Chart.WithOptions(commonChartOptions)

sleepPerDay
    |> Series.groupBy (fun ts _ -> ts.DayOfWeek.ToString()) 
    |> Series.mapValues seriesStats
    |> Frame.ofRows
    |> Frame.sliceRows (Globalization.CultureInfo.CurrentCulture.DateTimeFormat.DayNames)
    |> Chart.Line 
    |> Chart.WithOptions(commonChartOptions)


// Bed time

module Seq =
    let rec ofDates (date:System.DateTime) inc = seq {
            yield date
            yield! ofDates (date.Add(inc)) inc
        }
    let ofDatesBetween start stop =
        ofDates start (TimeSpan.FromDays(1.0))
        |> Seq.takeWhile (fun date -> (date <= stop))
let evenings = Seq.ofDatesBetween (DateTime(2016, 11,07, 21, 00, 00)) (DateTime(2017, 02, 28))
let bedTime = 
    sleepRecordsFrame 
    |> Frame.mapRows (fun ts _ -> 
        let h = (float ts.Hour) + (float ts.Minute)/60.0
        if h > 8.0 then h - 24.0 else h
    ) 
    |> Series.lookupAll evenings Lookup.ExactOrGreater

bedTime
    |> Chart.Scatter 
    |> Chart.WithOptions(
        Options(
            width = 1200,
            hAxis = Axis(title = "Date"),
            vAxis = Axis(title = "Sleep Hour")))

let mornings = Seq.ofDatesBetween (DateTime(2016, 11,07, 13, 00, 00)) (DateTime(2017, 02, 28))
let wakeupTime = 
    sleepRecordsFrame 
    |> Frame.mapRows (fun _ r ->
        let ts = r.GetAs<DateTime>("endDate") 
        (float ts.Hour) + (float ts.Minute)/60.0
    ) 
    |> Series.lookupAll mornings Lookup.ExactOrSmaller


wakeupTime
    |> Chart.Scatter 
    |> Chart.WithOptions(
        Options(
            width = 1200,
            hAxis = Axis(title = "Date"),
            vAxis = Axis(title = "Wake-up Hour")))


// Stats

sleepPerDay |> seriesStats
wakeupTime |> seriesStats
bedTime |> seriesStats


// Stand


// Exercise


exerciseRecordsFrame?Minutes <- exerciseRecordsFrame |> Frame.mapRowValues (fun row -> row.GetAs<TimeSpan>("Duration").TotalMinutes)
let exercise = exerciseRecordsFrame?Minutes

let exercisePerDay =
    exercise
    |> Series.groupBy (fun ts _ -> ts.Date)
    |> Series.mapValues Stats.sum

exercisePerDay
    |> Chart.Column 
    |> Chart.WithOptions(Options(width = 1200))


exercisePerDay
    |> Series.groupBy (fun ts _ -> ts.DayOfWeek.ToString()) 
    |> Series.mapValues seriesStats
    |> Frame.ofRows
    |> Frame.sliceRows (Globalization.CultureInfo.CurrentCulture.DateTimeFormat.DayNames)
    |> Chart.Line 
    |> Chart.WithOptions(commonChartOptions)
    

let dfi = Globalization.DateTimeFormatInfo.CurrentInfo
let calendar = dfi.Calendar

exercisePerDay
    |> Series.groupBy (fun ts _ -> calendar.GetWeekOfYear(ts, dfi.CalendarWeekRule, 
                                          dfi.FirstDayOfWeek).ToString()) 
    |> Series.mapValues seriesStats
    |> Frame.ofRows
    |> Chart.Line 
    |> Chart.WithOptions(commonChartOptions)


// Join

let sleepPerDayCombo = 
    Frame.ofColumns [ 
        "Bedtime" => (bedTime |> Series.mapKeys (fun k -> k.Date.AddDays(-1.0)))
        "Wakeup" => (wakeupTime |> Series.mapKeys (fun k -> k.Date))
        "Sleep" => (sleepPerDay |> Series.mapKeys (fun k -> k.Date)) 
        "Exercise" => exercisePerDay
        "Mean" => (restingMeanByDay |> Series.mapKeys (fun k -> k.Date))
        ]
    |> Frame.join JoinKind.Inner zoneByDayPercentage


let filterOnHypothesis hypothesis =
    sleepPerDayCombo.Rows
    |> Series.filterValues hypothesis
    |> Frame.ofRows
    |> Stats.mean

let hypothesis : (string * (ObjectSeries<string> -> bool)) list = [
    ("Total", fun _  -> true);
    ("< 7", fun row  -> row?Sleep < 7.0);
    ("> 7", fun row  -> row?Sleep > 7.0);
    ("before midnight", fun row -> row?Bedtime < 0.0);
    ("No exercise", fun row -> row?Exercise < 10.0);
    ("Moderate exercise", fun row -> row?Exercise > 30.0 && row?Exercise < 70.0);
    ("Heavy exercise", fun row -> row?Exercise > 70.0);
    ]

hypothesis 
    |> Seq.map (fun (name, hypothesis) -> (name, filterOnHypothesis hypothesis))    
    |> Frame.ofColumns
    |> Frame.transpose
    |> Chart.Table

