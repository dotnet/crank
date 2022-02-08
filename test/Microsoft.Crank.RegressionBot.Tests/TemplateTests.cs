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
            TemplateOptions.Default.MemberAccessStrategy.Register<BenchmarksResult>();
            TemplateOptions.Default.MemberAccessStrategy.Register<DependencyChange>();
            TemplateOptions.Default.MemberAccessStrategy.Register<Report>();
            TemplateOptions.Default.MemberAccessStrategy.Register<Regression>();
            TemplateOptions.Default.MemberAccessStrategy.Register<JObject, object>((obj, name) => obj[name]);
            TemplateOptions.Default.ValueConverters.Add(x => x is JObject o ? new ObjectValue(o) : null);
            TemplateOptions.Default.ValueConverters.Add(x => x is JValue v ? v.Value : null);
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

            var parseIsSuccessful = new FluidParser().TryParse(template, out var fluidTemplate, out var errors);

            Assert.True(parseIsSuccessful, String.Join("\n", errors));

            var context = new TemplateContext(report);

            var body = await fluidTemplate.RenderAsync(context);

            _output.WriteLine(body);

            Assert.NotEmpty(body);
        }
    }
}
