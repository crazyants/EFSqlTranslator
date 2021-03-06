using System;
using System.Linq;
using EFSqlTranslator.EFModels;
using EFSqlTranslator.Translation;
using EFSqlTranslator.Translation.DbObjects.SqliteObjects;
using EFSqlTranslator.Translation.DbObjects.SqlObjects;
using Xunit;

namespace EFSqlTranslator.Tests.TranslatorTests
{
    [CategoryReadMe(
        Index = 0,
        Title = @"Basic Translation",
        Description = @"This section demostrates how the basic linq expression is translated into sql."
    )]
    public class BasicTranslationTests
    {
        [Fact]
        [TranslationReadMe(
            Index = 0,
            Title = "Basic filtering on column values in where clause")]
        public void Test_Translate_Filter_On_Simple_Column()
        {
            using (var db = new TestingContext())
            {
                var query = db.Blogs
                    .Where(b => b.Url != null &&
                                b.Name.StartsWith("Ethan") &&
                                (b.UserId > 1 || b.UserId < 100));

                var script = QueryTranslator.Translate(query.Expression, new EFModelInfoProvider(db), new SqliteObjectFactory());
                var sql = script.ToString();

                const string expected = @"
select b0.*
from Blogs b0
where ((b0.Url is not null) and (b0.Name like 'Ethan%')) and ((b0.UserId > 1) or (b0.UserId < 100))";

                TestUtils.AssertStringEqual(expected, sql);
            }
        }

        [Fact]
        public void Test_Translate_Filter_On_Contains()
        {
            using (var db = new TestingContext())
            {
                var query = db.Blogs
                    .Where(b => b.Url != null &&
                                b.Name.Contains("Ethan") &&
                                (b.UserId > 1 || b.UserId < 100));

                var script = QueryTranslator.Translate(query.Expression, new EFModelInfoProvider(db), new SqliteObjectFactory());
                var sql = script.ToString();

                const string expected = @"
select b0.*
from Blogs b0
where ((b0.Url is not null) and (b0.Name like '%Ethan%')) and ((b0.UserId > 1) or (b0.UserId < 100))";

                TestUtils.AssertStringEqual(expected, sql);
            }
        }

        [Fact]
        public void Test_Ues_Left_Join_If_In_Or_Condition()
        {
            using (var db = new TestingContext())
            {
                var query = db.Posts.Where(p => p.User.UserName != null || p.Content != null);

                var script = QueryTranslator.Translate(query.Expression, new EFModelInfoProvider(db), new SqliteObjectFactory());
                var sql = script.ToString();

                const string expected = @"
select p0.*
from Posts p0
left outer join Users u0 on p0.UserId = u0.UserId
where (u0.UserName is not null) or (p0.Content is not null)";

                TestUtils.AssertStringEqual(expected, sql);
            }
        }

        [Fact]
        public void Test_Nullable_Value_In_Condition()
        {
            using (var db = new TestingContext())
            {
                var query = db.Blogs.Where(b => b.CommentCount > 10);

                var script = QueryTranslator.Translate(query.Expression, new EFModelInfoProvider(db), new SqliteObjectFactory());
                var sql = script.ToString();

                const string expected = @"
select b0.* from Blogs b0 where b0.CommentCount > 10";

                TestUtils.AssertStringEqual(expected, sql);
            }
        }

        [Fact]
        public void Test_Query_Unwrapping()
        {
            using (var db = new TestingContext())
            {
                var query = db.Blogs
                    .Where(b => b.CommentCount > 10)
                    .Select(b => new {KKK = b.BlogId})
                    .Select(b => new {K = b.KKK});

                var script = QueryTranslator.Translate(query.Expression, new EFModelInfoProvider(db), new SqliteObjectFactory());
                var sql = script.ToString();

                const string expected = @"
select b0.BlogId as 'K' from Blogs b0 where b0.CommentCount > 10";

                TestUtils.AssertStringEqual(expected, sql);
            }
        }

        [Fact]
        public void Test_Query_Unwrapping2()
        {
            using (var db = new TestingContext())
            {
                var query = db.Blogs.Select(b => b);

                var script = QueryTranslator.Translate(query.Expression, new EFModelInfoProvider(db), new SqliteObjectFactory());
                var sql = script.ToString();

                const string expected = @"
select b0.* from Blogs b0";

                TestUtils.AssertStringEqual(expected, sql);

            }
        }

        [Fact]
        public void Test_Query_SupportValueTypes()
        {
            using (var db = new TestingContext())
            {
                var query = db.Statistics
                    .GroupBy(s => s.BlogId)
                    .Select(g => new
                    {
                        BId = g.Key,
                        FloatVal = g.Sum(s => s.FloatVal),
                        DecimalVal = g.Sum(s => s.DecimalVal),
                        DoubleVal = g.Sum(s => s.DoubleVal)
                    });

                var script = QueryTranslator.Translate(query.Expression, new EFModelInfoProvider(db), new SqlObjectFactory());
                var sql = script.ToString();

                Console.WriteLine(sql);

                const string expected = @"
select s0.BlogId as 'BId', sum(s0.FloatVal) as 'FloatVal', sum(s0.DecimalVal) as 'DecimalVal', sum(s0.DoubleVal) as 'DoubleVal'
from Statistics s0
group by s0.BlogId";

                TestUtils.AssertStringEqual(expected, sql);

            }
        }

        [Fact]
        public void Test_Query_DifferentSchema()
        {
            using (var db = new TestingContext())
            {
                var query = db.Items
                    .GroupBy(i => i.CategoryId)
                    .Select(g => new
                    {
                        CategoryId = g.Key,
                        Sum = g.Sum(i => i.Value)
                    });

                var script = QueryTranslator.Translate(query.Expression, new EFModelInfoProvider(db), new SqlObjectFactory());
                var sql = script.ToString();

                const string expected = @"
select i0.CategoryId, sum(i0.Value) as 'Sum'
from fin.Item i0
group by i0.CategoryId";

                TestUtils.AssertStringEqual(expected, sql);

            }
        }

        [Fact]
        public void Test_Query_Fill_Class()
        {
            using (var db = new TestingContext())
            {
                var query = db.Items
                    .GroupBy(i => i.CategoryId)
                    .Select(g => new MyClass
                    {
                        Id = g.Key,
                        Val = g.Sum(i => i.Value)
                    });

                var script = QueryTranslator.Translate(query.Expression, new EFModelInfoProvider(db), new SqlObjectFactory());
                var sql = script.ToString();

                const string expected = @"
select i0.CategoryId as 'Id', sum(i0.Value) as 'Val'
from fin.Item i0
group by i0.CategoryId";

                TestUtils.AssertStringEqual(expected, sql);

            }
        }
    }

    public class MyClass
    {
        public int Id { get; set; }

        public decimal? Val { get; set; }
    }
}