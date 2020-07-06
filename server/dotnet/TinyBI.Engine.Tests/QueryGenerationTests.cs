using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using TinyBI.Engine.JsonModels;
using FluentAssertions;
using Xunit;

namespace TinyBI.Engine.Tests
{
    public class QueryGenerationTests
    {
        private static readonly Schema Schema = new Schema(typeof(TestSchema));

        [Fact]
        public void RejectsBadColumnName()
        {
            var queryJson = new QueryJson
            {
                Aggregations = new List<AggregationJson>
                {
                    new AggregationJson
                    {
                        Column = "Vendor.FictionalName",
                    }
                }
            };

            Action a = () => new Query(queryJson, Schema);
            a.Should().Throw<InvalidOperationException>();            
        }

        [Fact]
        public void RejectsMalformedColumnName()
        {
            var queryJson = new QueryJson
            {
                Aggregations = new List<AggregationJson>
                {
                    new AggregationJson
                    {
                        Column = "Amount",
                    }
                }
            };

            Action a = () => new Query(queryJson, Schema);
            a.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void MinimalSelectOneColumn()
        {
            var queryJson = new QueryJson
            {                
                Aggregations = new List<AggregationJson>
                {
                    new AggregationJson
                    {
                        Column = "Vendor.VendorName",
                    }
                }
            };

            var query = new Query(queryJson, Schema);
            var filterParams = new FilterParameters();
            AssertSameSql(query.ToSql(filterParams, Enumerable.Empty<Filter>(), 10), @"
                with [Aggregation0] as (
                    select main.[VendorName] [Value]
                    from [TestSchema].[Vendor] main
                )
                select top 10 a0.[Value] Value0
                from Aggregation0 a0
                order by a0.[Value] desc
            ");
            filterParams.Names.Should().HaveCount(0);
        }

        [Fact]
        public void FilterByPrimaryKeyOfOtherTable()
        {
            var queryJson = new QueryJson
            {
                Aggregations = new List<AggregationJson>
                {
                    new AggregationJson
                    {
                        Column = "Invoice.Amount"
                    }
                },
                Filters = new List<FilterJson>
                {
                    new FilterJson
                    {
                        Column = "Vendor.Id",
                        Operator = "=",
                        Value = JsonDocument.Parse("42").RootElement
                    }
                }
            };

            var query = new Query(queryJson, Schema);
            var filterParams = new FilterParameters();

            // As filter is on PK of Vendor, can just use FK of Invoice, avoid join
            AssertSameSql(query.ToSql(filterParams, Enumerable.Empty<Filter>(), 10), @"
                with [Aggregation0] as (
                    select main.[Amount] [Value]
                    from [TestSchema].[Invoice] main
                    where main.[VendorId] = @filter0
                )
                select top 10 a0.[Value] Value0
                from Aggregation0 a0
                order by a0.[Value] desc
            ");
            filterParams.Names.Should().HaveCount(1);
        }

        [Fact]
        public void SingleAggregation()
        {
            var queryJson = new QueryJson
            {
                Select = new List<string> { "Vendor.VendorName" },
                Aggregations = new List<AggregationJson>
                {
                    new AggregationJson
                    {
                        Column = "Invoice.Amount",
                        Function = AggregationType.Sum
                    }
                }                
            };

            var query = new Query(queryJson, Schema);
            var filterParams = new FilterParameters();
            AssertSameSql(query.ToSql(filterParams, Enumerable.Empty<Filter>(), 10), @"
                with [Aggregation0] as (
                    select join0.[VendorName] Select0, Sum ( main.[Amount] ) [Value]
                    from [TestSchema].[Invoice] main
                    join [TestSchema].[Vendor] join0 on join0.[Id] = main.[VendorId]
                    group by join0.[VendorName]
                )
                select top 10 a0.Select0, a0.[Value] Value0
                from Aggregation0 a0
                order by a0.[Value] desc
            ");
            filterParams.Names.Should().HaveCount(0);
        }

        [Fact]
        public void DoubleAggregation()
        {
            var queryJson = new QueryJson
            {
                Select = new List<string> { "Vendor.VendorName" },
                Aggregations = new List<AggregationJson>
                {
                    new AggregationJson
                    {
                        Column = "Invoice.Amount",
                        Function = AggregationType.Sum
                    },

                    new AggregationJson
                    {
                        Column = "Invoice.Id",
                        Function = AggregationType.Count
                    }
                }
            };

            var query = new Query(queryJson, Schema);
            var filterParams = new FilterParameters();
            AssertSameSql(query.ToSql(filterParams, Enumerable.Empty<Filter>(), 10), @"
                with [Aggregation0] as (
                    select join0.[VendorName] Select0, Sum ( main.[Amount] ) [Value]
                    from [TestSchema].[Invoice] main
                    join [TestSchema].[Vendor] join0 on join0.[Id] = main.[VendorId]
                    group by join0.[VendorName]
                ) ,
                [Aggregation1] as (
                    select join0.[VendorName] Select0, Count ( main.[Id] ) [Value]
                    from [TestSchema].[Invoice] main
                    join [TestSchema].[Vendor] join0 on join0.[Id] = main.[VendorId]
                    group by join0.[VendorName]
                )
                select top 10 a0.Select0, a0.[Value] Value0 , a1.[Value] Value1
                from Aggregation0 a0
                left join Aggregation1 a1 on a1.Select0 = a0.Select0
                order by a0.[Value] desc
            ");

            filterParams.Names.Should().HaveCount(0);
        }

        [Fact]
        public void DoubleAggregationDifferentFilters()
        {
            var queryJson = new QueryJson
            {
                Select = new List<string> { "Vendor.VendorName" },
                Aggregations = new List<AggregationJson>
                {
                    new AggregationJson
                    {
                        Column = "Invoice.Amount",
                        Function = AggregationType.Sum
                    },

                    new AggregationJson
                    {
                        Column = "Invoice.Id",
                        Function = AggregationType.Count,
                        Filters = new List<FilterJson>
                        {
                            new FilterJson
                            {
                                Column = "Invoice.Paid",
                                Operator = "=",
                                Value = JsonDocument.Parse("true").RootElement
                            }
                        }
                    }
                }
            };

            var query = new Query(queryJson, Schema);
            var filterParams = new FilterParameters();
            AssertSameSql(query.ToSql(filterParams, Enumerable.Empty<Filter>(), 10), @"
                with [Aggregation0] as (
                    select join0.[VendorName] Select0, Sum ( main.[Amount] ) [Value]
                    from [TestSchema].[Invoice] main
                    join [TestSchema].[Vendor] join0 on join0.[Id] = main.[VendorId]
                    group by join0.[VendorName]
                ) ,
                [Aggregation1] as (
                    select join0.[VendorName] Select0, Count ( main.[Id] ) [Value]
                    from [TestSchema].[Invoice] main
                    join [TestSchema].[Vendor] join0 on join0.[Id] = main.[VendorId]
                    where main.[Paid] = @filter0
                    group by join0.[VendorName]
                )
                select top 10 a0.Select0, a0.[Value] Value0 , a1.[Value] Value1
                from Aggregation0 a0
                left join Aggregation1 a1 on a1.Select0 = a0.Select0
                order by a0.[Value] desc
            ");

            filterParams.Names.Should().HaveCount(1);
        }

        [Fact]
        public void ExtraFilters()
        {
            var queryJson = new QueryJson
            {
                Select = new List<string> { "Vendor.VendorName" },
                Aggregations = new List<AggregationJson>
                {
                    new AggregationJson
                    {
                        Column = "Invoice.Amount",
                        Function = AggregationType.Sum
                    }
                }
            };

            var query = new Query(queryJson, Schema);

            var extra = new Filter(new FilterJson
                {
                    Column = "Invoice.Paid",
                    Operator = "=",
                    Value = JsonDocument.Parse("true").RootElement
                },
                Schema);

            var filterParams = new FilterParameters();
            AssertSameSql(query.ToSql(filterParams, new[] { extra }, 10), @"
                with [Aggregation0] as (
                    select join0.[VendorName] Select0, Sum ( main.[Amount] ) [Value]
                    from [TestSchema].[Invoice] main
                    join [TestSchema].[Vendor] join0 on join0.[Id] = main.[VendorId]
                    where main.[Paid] = @filter0
                    group by join0.[VendorName]
                )
                select top 10 a0.Select0, a0.[Value] Value0
                from Aggregation0 a0
                order by a0.[Value] desc
            ");
            filterParams.Names.Should().HaveCount(1);
        }

        private void AssertSameSql(string actual, string expected)
        {
            static string Flatten(string sql) => new Regex("\\s+").Replace(sql, " ").Trim();
            Flatten(actual).Should().Be(Flatten(expected));
        }
    }
}
