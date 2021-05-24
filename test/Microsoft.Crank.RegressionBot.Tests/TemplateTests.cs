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

        [Theory]
        [InlineData("assets/regressions1.json", "assets/template1.fluid")]
        [InlineData("assets/regressions2.json", "assets/template1.fluid")]
        public async Task TemplateIsRendered(string filename, string templatename)
        {
            var content = File.ReadAllText(filename);

            var report = JsonSerializer.Deserialize<Report>(content);

            var template = File.ReadAllText(templatename);

            var parseIsSuccessful = FluidTemplate.TryParse(template, out var fluidTemplate, out var errors);

            Assert.True(parseIsSuccessful, String.Join("\n", errors));

            var context = new TemplateContext { Model = report };

            var body = await fluidTemplate.RenderAsync(context);

            _output.WriteLine(body);

            Assert.NotEmpty(body);
        }
    }
}
