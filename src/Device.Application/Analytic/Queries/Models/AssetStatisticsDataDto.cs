using System;
using System.Collections.Generic;
using Device.Application.Historical.Query;
using Device.Application.Historical.Query.Model;

namespace Device.Application.Analytic.Query.Model
{
    public class AssetStatisticsDataDto
    {
        public string ProjectId { get; set; }
        public string SubscriptionId { get; set; }
        public Guid AssetId { get; set; }
        public string AssetName { get; set; }
        public string AssetNormalizeName { get; set; }
        public IEnumerable<AssetAttributeStatisticsDataDto> Attributes { get; set; }
        public long Start { get; set; }
        public long End { get; set; }
        public IDictionary<string, string> Statics { get; set; }
        public string Aggregate { get; set; }
        public string TimeGrain { get; set; }
        public string TimezoneOffset { get; set; }
        public string GapfillFunction { get; set; }
        public HistoricalDataType QueryType { get; internal set; }
        public string RequestType { get; internal set; }
    }
    public class AssetAttributeStatisticsDataDto : AttributeDto
    {
      
        public StatisticsDistribution Distributions { get; set; }
        
    }
    public class StatisticsDistribution
    {
        public double SD { get; set; }
        public double Median_Inc { get; set; }

        public IEnumerable<double> Outliers_Inc { get; set; }
        public IEnumerable<double> RawData { get; set; }
        public double Q1_Inc { get; set; }
        public double Q3_Inc { get; set; }
        public double UpperFence_Inc { get; set; }
        public double LowerFence_Inc { get; set; }

        public double Median_Exc { get; set; }

        public IEnumerable<double> Outliers_Exc { get; set; }
        public double Q1_Exc { get; set; }
        public double Q3_Exc { get; set; }
        public double UpperFence_Exc { get; set; }
        public double LowerFence_Exc { get; set; }
        private const int LIMIT = 10;
        public StatisticsDistribution(double valueSD, double valueMedian_inc, IEnumerable<double> outliers_inc, double valueQ1_inc,
                                        double valueQ3_inc, double valueUpperFence_inc, double valueLowerFence_inc, 
                                         double valueMedian_exc, IEnumerable<double> outliers_exc, double valueQ1_exc,
                                        double valueQ3_exc, double valueUpperFence_exc, double valueLowerFence_exc,
                                        IEnumerable<double> rawdata = null )
        {
            SD = valueSD;
            // Inclusive values
            Median_Inc = valueMedian_inc;
            Outliers_Inc = outliers_inc;
            Q1_Inc = valueQ1_inc;
            Q3_Inc = valueQ3_inc;
            UpperFence_Inc = valueUpperFence_inc;
            LowerFence_Inc = valueLowerFence_inc;    
            // Exclusive values   
            Median_Exc = valueMedian_exc;
            Outliers_Exc = outliers_exc;
            Q1_Exc = valueQ1_exc;
            Q3_Exc = valueQ3_exc;
            UpperFence_Exc = valueUpperFence_exc;
            LowerFence_Exc = valueLowerFence_exc;   

        }
        public static StatisticsDistribution Create(double median_i, double stdDev, double min, double max, double q1_i, double q3_i,
         double median_e, double q1_e, double q3_e)
        {
            //Inclusive values
            //IQR=Q3−Q1.
            double iqr_i = q3_i - q1_i;
            //Upper fence= Q3  +1.5×IQR.
            double ufence_i = q3_i + 1.5 * iqr_i;
            //Lower fence=Q1 −1.5×IQR.
            double lfence_i = q1_i - 1.5 * iqr_i;
            var outliers_i = new List<double>();
            if (min < lfence_i) {
                outliers_i.Add(min);
            }
            if (max > ufence_i){
                outliers_i.Add(max);
            }
            // Exclusive values
            double iqr_e = q3_e - q1_e;
            //Upper fence= Q3  +1.5×IQR.
            double ufence_e = q3_e + 1.5 * iqr_e;
            //Lower fence=Q1 −1.5×IQR.
            double lfence_e = q1_e - 1.5 * iqr_e;
            var outliers_e = new List<double>();
            if (min < lfence_e) {
                outliers_e.Add(min);
            }
            if (max > ufence_e){
                outliers_e.Add(max);
            }
            
            var output = new StatisticsDistribution(stdDev,
                    median_i , outliers_i.ToArray(), q1_i, q3_i, ufence_i, lfence_i, 
                    median_e , outliers_e.ToArray(), q1_e, q3_e, ufence_e, lfence_e,null);

            return output;

        }
    }
}
