namespace Device.Application.Enum
{
    public enum BlockOperator
    {
        QueryLastSingleAttributeValue,
        QueryNearestSingleAttributeValue,
        AggregateSingleAttributeValue,
        DurationInSingleAttributeValue,
        CountSingleAttributeValue,
        LastTimeDiffSingleAttributeValue,
        LastValueDiffSingleAttributeValue,
        DifferenceTimeBetween2PointSingleAttributeValue,
        DifferenceValueBetween2PointSingleAttributeValue,
        WriteSingleAttributeValue,
        QueryAssetTableData,
        WriteAssetTableData,
        DeleteAssetTableData,
        AggregateAssetTableData
    }
}