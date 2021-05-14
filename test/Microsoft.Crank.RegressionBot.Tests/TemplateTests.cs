using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Fluid;
using Fluid.Values;
using Microsoft.Crank.RegressionBot.Models;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Crank.RegressionBot.Tests
{
    public class TemplateTests
    {
        static TemplateTests()
        {
            TemplateContext.GlobalMemberAccessStrategy.Register<BenchmarksResult>();
            TemplateContext.GlobalMemberAccessStrategy.Register<DependencyChange>();
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
            var content = File.ReadAllText("assets/regressions.json");

            var report = JsonSerializer.Deserialize<Report>(content);

            var template = File.ReadAllText("assets/template.fluid");

            var parseIsSuccessful = FluidTemplate.TryParse(template, out var fluidTemplate, out var errors);

            Assert.True(parseIsSuccessful, String.Join("\n", errors));

            var context = new TemplateContext { Model = report };

            var body = await fluidTemplate.RenderAsync(context);

            _output.WriteLine(body);

            Assert.NotEmpty(body);
        }
    }
}
