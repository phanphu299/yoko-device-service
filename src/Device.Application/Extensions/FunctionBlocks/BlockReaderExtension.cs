using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Device.Application.Service.Abstraction;
using Newtonsoft.Json;
using AHI.Infrastructure.Service.Dapper.Model;

namespace Device.Application.Service
{
    public static class BlockReaderExtension
    {
        public static double Value(this IBlockContext context)
        {
            // in case link is remove from block template
            if (context == null)
                return 0;

            return context.Value != null ? Convert.ToDouble(context.Value) : 0;
        }

        public static string ValueString(this IBlockContext context)
        {
            // in case link is remove from block template
            if (context == null)
                return string.Empty;

            return context.Value?.ToString();
        }

        public static async Task<(DateTime, double Value)> LastValueAsync(this IBlockContext context)
        {
            // in case link is remove from block template
            if (context == null)
                return default((DateTime, double Value));

            // get asset attribute
            var blockOperation = new BlockOperation();
            blockOperation.SetOperator(Enum.BlockOperator.QueryLastSingleAttributeValue);
            context.SetBlockOperation(blockOperation);
            var result = await context.BlockEngine.RunAsync(context);
            if (result != null && double.TryParse(result.Value?.ToString(), out var doubleOutput))
            {
                return (result.Timestamp.Value, doubleOutput);
            }
            return default;
        }

        public static async Task<(DateTime, string Value)> LastValueStringAsync(this IBlockContext context)
        {
            // in case link is remove from block template
            if (context == null)
                return default((DateTime, string Value));

            // get asset attribute
            var blockOperation = new BlockOperation();
            blockOperation.SetOperator(Enum.BlockOperator.QueryLastSingleAttributeValue);
            context.SetBlockOperation(blockOperation);
            var result = await context.BlockEngine.RunAsync(context);
            if (result != null)
            {
                return (result.Timestamp.Value, result.Value.ToString());
            }
            return default;
        }

        public static async Task<(DateTime, double Value)> SearchNearestAsync(this IBlockContext context, DateTime dateTime, string padding)
        {
            // in case link is remove from block template
            if (context == null)
                return default((DateTime, double Value));

            // get asset attribute
            var blockOperation = new BlockOperation();
            blockOperation.SetOperator(Enum.BlockOperator.QueryNearestSingleAttributeValue);
            blockOperation.SetStartTime(dateTime);
            blockOperation.SetPadding(padding);
            context.SetBlockOperation(blockOperation);
            var result = await context.BlockEngine.RunAsync(context);
            if (result != null && double.TryParse(result.Value?.ToString(), out var doubleOutput))
            {
                return (result.Timestamp.Value, doubleOutput);
            }
            return default;
        }

        public static async Task<(DateTime, string Value)> SearchNearestStringAsync(this IBlockContext context, DateTime dateTime, string padding)
        {
            // in case link is remove from block template
            if (context == null)
                return default((DateTime, string Value));

            // get asset attribute
            var blockOperation = new BlockOperation();
            blockOperation.SetOperator(Enum.BlockOperator.QueryNearestSingleAttributeValue);
            blockOperation.SetStartTime(dateTime);
            blockOperation.SetPadding(padding);
            context.SetBlockOperation(blockOperation);
            var result = await context.BlockEngine.RunAsync(context);
            if (result != null)
            {
                return (result.Timestamp.Value, result.Value.ToString());
            }
            return default;
        }

        public static async Task<double> AggregateAsync(this IBlockContext context, string aggregate, DateTime start, DateTime end, string filterOperator = null, object filterValue = null)
        {
            // in case link is remove from block template
            if (context == null)
                return default(double);

            var blockOperation = new BlockOperation();
            blockOperation.SetOperator(Enum.BlockOperator.AggregateSingleAttributeValue);
            blockOperation.SetStartTime(start);
            blockOperation.SetEndTime(end);
            blockOperation.SetAggregrateMethod(aggregate);
            blockOperation.SetFilterOperator(filterOperator);
            blockOperation.SetFilterValue(filterValue);
            context.SetBlockOperation(blockOperation);
            var result = await context.BlockEngine.RunAsync(context);
            return (double)result.Value;
        }

        public static async Task<double> DurationInAsync(this IBlockContext context, DateTime start, DateTime end, string filterOperator, object filterValue, string filterUnit = "m")
        {
            // in case link is remove from block template
            if (context == null)
                return default(double);

            // calculate the total duration of specific value filter
            var blockOperation = new BlockOperation();
            blockOperation.SetOperator(Enum.BlockOperator.DurationInSingleAttributeValue);
            blockOperation.SetStartTime(start);
            blockOperation.SetEndTime(end);
            blockOperation.SetFilterOperator(filterOperator);
            blockOperation.SetFilterUnit(filterUnit);
            blockOperation.SetFilterValue(filterValue);
            context.SetBlockOperation(blockOperation);
            var result = await context.BlockEngine.RunAsync(context);
            return (double)result.Value;
        }

        public static async Task<int> CountInAsync(this IBlockContext context, DateTime start, DateTime end, string filterOperator, object filterValue)
        {
            // calculate the total duration of specific value filter
            var blockOperation = new BlockOperation();
            blockOperation.SetOperator(Enum.BlockOperator.CountSingleAttributeValue);
            blockOperation.SetStartTime(start);
            blockOperation.SetEndTime(end);
            blockOperation.SetFilterOperator(filterOperator);
            blockOperation.SetFilterValue(filterValue);
            context.SetBlockOperation(blockOperation);
            var result = await context.BlockEngine.RunAsync(context);
            return (int)result.Value;
        }

        public static async Task<double> LastTimeDiffAsync(this IBlockContext context, string filterUnit = "s")
        {
            // calculate the total duration of specific value filter
            var blockOperation = new BlockOperation();
            blockOperation.SetOperator(Enum.BlockOperator.LastTimeDiffSingleAttributeValue);
            blockOperation.SetFilterUnit(filterUnit);
            context.SetBlockOperation(blockOperation);
            var result = await context.BlockEngine.RunAsync(context);
            return (double)result.Value;
        }

        public static async Task<double> LastValueDiffAsync(this IBlockContext context, string filterUnit = "s")
        {
            // calculate the total duration of specific value filter
            var blockOperation = new BlockOperation();
            blockOperation.SetOperator(Enum.BlockOperator.LastValueDiffSingleAttributeValue);
            blockOperation.SetFilterUnit(filterUnit);
            context.SetBlockOperation(blockOperation);
            var result = await context.BlockEngine.RunAsync(context);
            return (double)result.Value;
        }

        public static async Task<double> TimeDiffAsync(this IBlockContext context, DateTime start, DateTime end, string filterUnit = "s")
        {
            // calculate the total duration of specific value filter
            var blockOperation = new BlockOperation();
            blockOperation.SetStartTime(start);
            blockOperation.SetEndTime(end);
            blockOperation.SetOperator(Enum.BlockOperator.DifferenceTimeBetween2PointSingleAttributeValue);
            blockOperation.SetFilterUnit(filterUnit);
            context.SetBlockOperation(blockOperation);
            var result = await context.BlockEngine.RunAsync(context);
            return (double)result.Value;
        }

        public static async Task<double> ValueDiffAsync(this IBlockContext context, DateTime start, DateTime end)
        {
            // calculate the total duration of specific value filter
            var blockOperation = new BlockOperation();
            blockOperation.SetStartTime(start);
            blockOperation.SetEndTime(end);
            blockOperation.SetOperator(Enum.BlockOperator.DifferenceValueBetween2PointSingleAttributeValue);
            context.SetBlockOperation(blockOperation);
            var result = await context.BlockEngine.RunAsync(context);
            return (double)result.Value;
        }

        /// <summary>
        /// query data from asset table
        /// </summary>
        public static async Task<IEnumerable<IDictionary<string, object>>> QueryAsync(this IBlockContext context, object query)
        {
            var blockOperation = new BlockOperation();
            var queryJson = JsonConvert.SerializeObject(query);
            var queryCriteria = JsonConvert.DeserializeObject<QueryCriteria>(queryJson);
            blockOperation.SetOperator(Enum.BlockOperator.QueryAssetTableData);
            context.SetBlockOperation(blockOperation);
            context.SetTableQuery(queryCriteria);
            var result = await context.BlockEngine.RunAsync(context);
            return (IEnumerable<IDictionary<string, object>>)result.Value;
        }

        /// <summary>
        /// aggregate data from asset table
        /// </summary>
        public static async Task<object> AggregateAsync(this IBlockContext context, string aggregationType, string filterName, string filterOperation, object filterValue)
        {
            var blockOperation = new BlockOperation();
            var aggregationCriteria = new AggregationCriteria(aggregationType, filterName, filterOperation, filterValue);
            blockOperation.SetOperator(Enum.BlockOperator.AggregateAssetTableData);
            context.SetBlockOperation(blockOperation);
            context.SetTableAggregation(aggregationCriteria);
            var result = await context.BlockEngine.RunAsync(context);
            return result.Value;
        }
    }
}