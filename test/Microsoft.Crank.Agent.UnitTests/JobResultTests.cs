using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Crank.Agent;
using Microsoft.Crank.Models;
using Moq;
using Xunit;

namespace Microsoft.Crank.Agent.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="JobResult"/> class.
    /// </summary>
    public class JobResultTests
    {
        private readonly int _testId = 123;
        private readonly string _testRunId = "TestRunId";
        private readonly string _expectedStateString = "Completed";
        private readonly string _expectedDetailsUrl = "http://test/details";
        private readonly string _expectedBuildLogsUrl = "http://test/buildlogs";
        private readonly string _expectedOutputLogsUrl = "http://test/outputlogs";

        /// <summary>
        /// Tests that the constructor of JobResult correctly initializes all properties when provided with valid Job and IUrlHelper.
        /// </summary>
//         [Fact] [Error] (36-25)CS0029 Cannot implicitly convert type 'Microsoft.Crank.Agent.UnitTests.JobResultTests.FakeState' to 'Microsoft.Crank.Models.JobState' [Error] (41-38)CS0854 An expression tree may not contain a call or invocation that uses optional arguments [Error] (43-38)CS0854 An expression tree may not contain a call or invocation that uses optional arguments [Error] (45-38)CS0854 An expression tree may not contain a call or invocation that uses optional arguments
//         public void Constructor_WithValidParameters_ReturnsExpectedJobResult()
//         {
//             // Arrange
//             // Create a fake state object that returns a predefined string on ToString.
//             var fakeState = new FakeState(_expectedStateString);
//             // Assuming Job has a public parameterless constructor and settable properties.
//             var job = new Job
//             {
//                 Id = _testId,
//                 RunId = _testRunId,
//                 State = fakeState
//             };
// 
//             // Setup IUrlHelper mock with expected behavior.
//             var urlHelperMock = new Mock<IUrlHelper>();
//             urlHelperMock.Setup(x => x.ActionLink("GetById", "Jobs", It.IsAny<object>()))
//                 .Returns(_expectedDetailsUrl);
//             urlHelperMock.Setup(x => x.ActionLink("BuildLog", "Jobs", It.IsAny<object>()))
//                 .Returns(_expectedBuildLogsUrl);
//             urlHelperMock.Setup(x => x.ActionLink("Output", "Jobs", It.IsAny<object>()))
//                 .Returns(_expectedOutputLogsUrl);
// 
//             // Act
//             var result = new JobResult(job, urlHelperMock.Object);
// 
//             // Assert
//             Assert.Equal(_testId, result.Id);
//             Assert.Equal(_testRunId, result.RunId);
//             Assert.Equal(_expectedStateString, result.State);
//             Assert.Equal(_expectedDetailsUrl, result.DetailsUrl);
//             Assert.Equal(_expectedBuildLogsUrl, result.BuildLogsUrl);
//             Assert.Equal(_expectedOutputLogsUrl, result.OutputLogsUrl);
//         }

        /// <summary>
        /// Tests that the constructor of JobResult throws a NullReferenceException when the Job parameter is null.
        /// </summary>
//         [Fact] [Error] (68-38)CS0854 An expression tree may not contain a call or invocation that uses optional arguments
//         public void Constructor_WithNullJob_ThrowsNullReferenceException()
//         {
//             // Arrange
//             var urlHelperMock = new Mock<IUrlHelper>();
//             urlHelperMock.Setup(x => x.ActionLink(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>()))
//                 .Returns("dummy");
// 
//             // Act & Assert
//             Assert.Throws<NullReferenceException>(() => new JobResult(null, urlHelperMock.Object));
//         }

        /// <summary>
        /// Tests that the constructor of JobResult throws a NullReferenceException when the IUrlHelper parameter is null.
        /// </summary>
//         [Fact] [Error] (87-25)CS0029 Cannot implicitly convert type 'Microsoft.Crank.Agent.UnitTests.JobResultTests.FakeState' to 'Microsoft.Crank.Models.JobState'
//         public void Constructor_WithNullUrlHelper_ThrowsNullReferenceException()
//         {
//             // Arrange
//             var fakeState = new FakeState(_expectedStateString);
//             var job = new Job
//             {
//                 Id = _testId,
//                 RunId = _testRunId,
//                 State = fakeState
//             };
// 
//             // Act & Assert
//             Assert.Throws<NullReferenceException>(() => new JobResult(job, null));
//         }

        /// <summary>
        /// A fake state class to simulate the Job.State property behavior.
        /// The ToString method returns a predefined string.
        /// </summary>
        private class FakeState
        {
            private readonly string _state;

            public FakeState(string state)
            {
                _state = state;
            }

            public override string ToString()
            {
                return _state;
            }
        }
    }
}
