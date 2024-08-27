
//Sample code. Please update accordingly;
using System;
using System.Threading.Tasks;
using System.Globalization;

namespace Device.Application.Service
{
    public class BlockFunctionRuntimeSample : BaseFunctionBlockExecutionRuntime
    {
        // private readonly Random _random = new Random();
        public override async Task ExecuteAsync()
        {
            var processingDate = DateTime.ParseExact("2022-09-10 13:28:19", "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

            {
                var lastValue = await AHI.GetAttribute("thanh-test").LastValueAsync();
                AHI.Set("thanh-test-output", lastValue);
            }
            if (AHI.GetBoolean("system_exit_code") == true)
            { return; }

            // {
            //     var lastValue = await AHI.Get("thanh-test").LastValueAsync();
            //     AHI.Set("Tran Ngo 1", lastValue);
            // }
            //AHI.Set("", AHI.Get(""));
            // // 1	Write	Write custom data to a attribute
            // await AHI(mode).Asset("Sushi Sensor").Attribute("XYZAccelerationOutput").WriteValueAsync("2022-09-10 13:28:19".UnixTimeStampToDateTime(), 10821);
            // await AHI(mode).Asset("Sushi Sensor").Child("Child 1").Attribute("XYZAccelerationOutput").WriteValueAsync("2022-09-10 13:28:19".UnixTimeStampToDateTime(), 10821);

            // //2.  BulkWrite	Write custom data to a attribute      [{ Date,Value},{ Date,Value},…]"
            // await AHI(mode).Asset("Sushi Sensor").Attribute("XYZAccelerationOutput").WriteValueAsync(
            //     Tuple.Create("2022-09-10 13:28:19".UnixTimeStampToDateTime(), 10.0),
            //     Tuple.Create("2022-09-10 13:38:19".UnixTimeStampToDateTime(), 102.2),
            //     Tuple.Create("2022-09-10 13:48:19".UnixTimeStampToDateTime(), 101.4),
            //     Tuple.Create("2022-09-10 13:58:19".UnixTimeStampToDateTime(), 21.0),
            //     Tuple.Create("2022-09-10 14:08:19".UnixTimeStampToDateTime(), 101.0)
            //     );

            // await AHI(mode).Asset("Sushi Sensor").Child("Child 1").Attribute("XYZAccelerationOutput").WriteValueAsync(
            //     Tuple.Create("2022-09-10 13:28:19".UnixTimeStampToDateTime(), 10.0),
            //     Tuple.Create("2022-09-10 13:38:19".UnixTimeStampToDateTime(), 102.2),
            //     Tuple.Create("2022-09-10 13:48:19".UnixTimeStampToDateTime(), 101.4),
            //     Tuple.Create("2022-09-10 13:58:19".UnixTimeStampToDateTime(), 21.0),
            //     Tuple.Create("2022-09-10 14:08:19".UnixTimeStampToDateTime(), 101.0)
            //     );

            // // 3	LastDataPoint	Timestamp and value of last data
            // var (timestamp, uptimeSnapshotValue) = await AHI(mode).Asset("Sushi Sensor")
            //                                             .Attribute("XYZAcceleration")
            //                                             .LastValueAsync();

            // if (uptimeSnapshotValue != null || uptimeSnapshotValue is double targetValue)
            // {

            // }

            // (timestamp, uptimeSnapshotValue) = await AHI(mode).Asset("Sushi Sensor")
            //                                             .Child("Child 1")
            //                                             .Attribute("XYZTemperature")
            //                                             .LastValueAsync();

            // // 4	NearestDataPoint	Timestamp and value of nearest a datetime
            // var filterDateTime = "2022-09-10 13:28:19".UnixTimeStampToDateTime();
            // var (previousDateTime, previousValue) = await AHI(mode).Asset("Sushi Sensor")
            //                                             .Attribute("ZVelocity")
            //                                             .SearchNearestAsync(filterDateTime, "left");

            // var (nextDateTime, nextValue) = await AHI(mode).Asset("Sushi Sensor")
            //                                     .Child("Child 1")
            //                                     .Attribute("ZVelocity")
            //                                     .SearchNearestAsync(filterDateTime, "right");

            // // 5	LastTimeDiff	Difference by timestamp between last data and previous data
            // var lastTimeDiffInSeconds = await AHI(mode).Asset("Sushi Sensor")
            //                              .Attribute("ZVelocity")
            //                              .LastTimeDiffAsync(); // sec, min, hour, day (default: sec)

            // var lastTimeDiffInMinutes = await AHI(mode).Asset("Sushi Sensor")
            //                              .Attribute("ZVelocity")
            //                              .LastTimeDiffAsync("min"); // sec, min, hour, day (default: sec)

            // lastTimeDiffInMinutes = await AHI(mode).Asset("Sushi Sensor")
            //                             .Child("Child 1")
            //                             .Attribute("ZVelocity")
            //                             .LastTimeDiffAsync("min"); // sec, min, hour, day (default: sec)


            // // 6	LastValueDiff	Difference by value between last data and previous data
            // var lastValueDiffInSeconds = await AHI(mode).Asset("Sushi Sensor")
            //                             .Attribute("ZVelocity")
            //                             .LastValueDiffAsync();

            // var lastValueDiffInMinutes = await AHI(mode).Asset("Sushi Sensor")
            //                              .Attribute("ZVelocity")
            //                              .LastValueDiffAsync("min");

            // lastValueDiffInMinutes = await AHI(mode).Asset("Sushi Sensor")
            //                              .Child("Child 1")
            //                              .Attribute("ZVelocity")
            //                              .LastValueDiffAsync("min");

            // // 7	TimeDiff	Difference by timestamp between 2 data points
            // var date1 = "2022-08-25 12:40:50".UnixTimeStampToDateTime();
            // var date2 = "2022-08-26 12:41:20".UnixTimeStampToDateTime();
            // var lastTimeDiffBetween2PointsInSeconds = await AHI(mode).Asset("Sushi Sensor")
            //                           .Attribute("ZVelocity")
            //                           .TimeDiffAsync(date1, date2); // sec, min, hour, day (default: sec)

            // var lastTimeDiffBetween2PointsInMinutes = await AHI(mode).Asset("Sushi Sensor")
            //                              .Attribute("ZVelocity")
            //                              .TimeDiffAsync(date1, date2, "min"); // sec, min, hour, day (default: sec)

            // lastTimeDiffBetween2PointsInMinutes = await AHI(mode).Asset("Sushi Sensor")
            //                              .Child("Child 1")
            //                              .Attribute("ZVelocity")
            //                              .TimeDiffAsync(date1, date2, "min");

            // // 8	ValueDiff	Difference by value between 2 data points

            // var lastValueDiffBetween2Points = await AHI(mode).Asset("Sushi Sensor")
            //                              .Attribute("ZVelocity")
            //                              .ValueDiffAsync(date1, date2);
            // lastValueDiffBetween2Points = await AHI(mode).Asset("Sushi Sensor")
            //                              .Child("Child 1")
            //                              .Attribute("ZVelocity")
            //                              .ValueDiffAsync(date1, date2);

            // // 9	Aggregate	Aggregate of data
            // var aggregrate1 = await AHI(mode).Asset("Sushi Sensor")
            //                          .Attribute("ZVelocity")
            //                          .AggregateAsync("sum", date1, date2, ">", 20);

            // var aggregrateWithoutFilter = await AHI(mode).Asset("Sushi Sensor")
            //                          .Attribute("ZVelocity")
            //                          .AggregateAsync("sum", date1, date2);

            // aggregrateWithoutFilter = await AHI(mode).Asset("Sushi Sensor")
            //                          .Child("Child 1")
            //                          .Attribute("ZVelocity")
            //                          .AggregateAsync("sum", date1, date2);

            // // 10	Duration_In	Duration of state or condition
            // var uptimeDuration = await AHI(mode).Asset("Sushi Sensor")
            //                          .Attribute("ZVelocity")
            //                          .DurationInAsync(date1, date2, ">", 50, "sec"); // sec, min, hour, day (default: sec)

            // var uptimeWithConditionDuration = await AHI(mode).Asset("Sushi Sensor")
            //                          .Attribute("ZVelocity")
            //                          .DurationInAsync(date1, date2, ">", 5, "min"); // sec, min, hour, day (default: sec)

            // uptimeWithConditionDuration = await AHI(mode).Asset("Sushi Sensor")
            //                          .Child("Child 1")
            //                          .Attribute("ZVelocity")
            //                          .DurationInAsync(date1, date2, ">", 5, "hour"); // sec, min, hour, day (default: sec)

            // //11	Count_In	Count of state or condition
            // var runningCount = await AHI(mode).Asset("Sushi Sensor")
            //                                     .Attribute("ZVelocity")
            //                                     .CountInAsync(date1, date2, ">=", 60);

            // runningCount = await AHI(mode).Asset("Sushi Sensor")
            //                                     .Child("Child 1")
            //                                     .Attribute("ZVelocity")
            //                                     .CountInAsync(date1, date2, ">=", 40);

            // 12   Duration_Alarm	Duration of alarm
            // var alarmDuration = await AHI.Alarm("Sushi Sensor Pressure")
            //                             .DurationAsync(date1, date2); // sec, min, hour, day (default: sec)

            // // 13	Count_In	Count of state or condition
            // var alarmUNACKCount = await AHI.Alarm("Sushi Sensor Pressure")
            //                             .CountAsync(date1, date2, "=", "UNACKNOWLEDGED"); // sec, min, hour, day (default: sec)

            // // 14	Count_Alarm	Count alarm
            // var accountCount = await AHI.Alarm("Sushi Sensor Pressure")
            //                             .CountAsync(date1, date2); // sec, min, hour, day (default: sec)
            // var request = new
            // {
            //     Payload = new
            //     {
            //         Value = 1
            //     }
            // };
            // var abnormalResult = await AHI.WithHttp("http://localhost:7071/api/MLFunctionSample")
            //                              .AddHeader("token", "atusfou131")
            //                              .AddHeader("new-header", "this is new header")
            //                             .PostAsync<MLResult>(request);
            // var mlOutputValue = new BlockQueryResult()
            // {
            //     Value = abnormalResult.Abnormal,
            //     Timestamp = start
            // };
        }
        private class MLResult
        {
            public bool Abnormal { get; set; }
        }
    }

}