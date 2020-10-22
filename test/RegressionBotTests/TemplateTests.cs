using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Fluid;
using Fluid.Values;
using Microsoft.Crank.RegressionBot;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Abstractions;

namespace RegressionBotTests
{
    public class TemplateTests
    {
        static TemplateTests()
        {
            TemplateContext.GlobalMemberAccessStrategy.Register<BenchmarksResult>();
            TemplateContext.GlobalMemberAccessStrategy.Register<Report>();
            TemplateContext.GlobalMemberAccessStrategy.Register<Regression>();
            TemplateContext.GlobalMemberAccessStrategy.Register<JObject, object>((obj, name) => obj[name]);
            FluidValue.SetTypeMapping<JObject>(o => new ObjectValue(o));
            FluidValue.SetTypeMapping<JValue>(o => FluidValue.Create(o.Value));
        }

        private readonly ITestOutputHelper _output;

        public TemplateTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task TemplateIsRendered()
        {
            var regressions = new List<Regression>();
            
            regressions.Add(new Regression 
            {
                PreviousResult = new BenchmarksResult
                {
                    Id = 1,
                    Excluded = false,
                    DateTimeUtc = DateTime.UtcNow,
                    Session = "1234",
                    Scenario = "Json",
                    Description = "Json aspnet-citrine-lin",
                    Document = File.ReadAllText("assets/benchmarkresult1.json")
                },
                CurrentResult = new BenchmarksResult
                {
                    Id = 2,
                    Excluded = false,
                    DateTimeUtc = DateTime.UtcNow,
                    Session = "1235",
                    Scenario = "Json",
                    Description = "Json aspnet-citrine-lin",
                    Document = File.ReadAllText("assets/benchmarkresult2.json")
                },
                Change = 1000,
                StandardDeviation = 1,
                Average = 10
            });
            
            var report = new Report
            {
                Regressions = regressions
            };

            var template = File.ReadAllText("assets/template.fluid");

            var parseIsSuccessful = FluidTemplate.TryParse(template, out var fluidTemplate, out var errors);

            Assert.True(parseIsSuccessful);

            var context = new TemplateContext { Model = report };

            var body = await fluidTemplate.RenderAsync(context);

            _output.WriteLine(body);

            Assert.NotEmpty(body);
        }
    }
}
