using System.Collections.Generic;

namespace Microsoft.Crank.Models
{
    public class JobStatistics
    {
        public List<MeasurementMetadata> Metadata = new List<MeasurementMetadata>();
        public List<Measurement> Measurements = new List<Measurement>();
    }
}
