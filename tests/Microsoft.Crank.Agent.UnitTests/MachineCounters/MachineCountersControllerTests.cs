using Microsoft.Crank.Models;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tracing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Microsoft.Crank.Agent.MachineCounters.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="MachineCountersController"/> class.
    /// </summary>
//     [TestClass] [Error] (27-31)CS0122 'MachineCountersController.MachineCountersController(EventPipeSession, Job)' is inaccessible due to its protection level
//     public class MachineCountersControllerTests
//     {
//         private readonly Mock<EventPipeSession> _mockEventPipeSession;
//         private readonly Mock<Job> _mockJob;
//         private readonly MachineCountersController _controller;
// 
//         public MachineCountersControllerTests()
//         {
//             _mockEventPipeSession = new Mock<EventPipeSession>();
//             _mockJob = new Mock<Job>();
//             _controller = new MachineCountersController(_mockEventPipeSession.Object, _mockJob.Object);
//         }
// 
//         /// <summary>
//         /// Tests the <see cref="MachineCountersController.Build(Job)"/> method to ensure it correctly builds a new instance.
//         /// </summary>
//         [TestMethod]
//         public void Build_ValidJob_ReturnsControllerInstance()
//         {
//             // Arrange
//             var job = new Job();
// 
//             // Act
//             var result = MachineCountersController.Build(job);
// 
//             // Assert
//             Assert.IsNotNull(result);
//             Assert.IsInstanceOfType(result, typeof(MachineCountersController));
//         }
// 
//         /// <summary>
//         /// Tests the <see cref="MachineCountersController.RegisterCounters"/> method to ensure it correctly registers counters.
//         /// </summary>
//         [TestMethod]
//         public void RegisterCounters_ValidCounters_ReturnsControllerInstance()
//         {
//             // Act
//             var result = _controller.RegisterCounters();
// 
//             // Assert
//             Assert.IsNotNull(result);
//             Assert.IsInstanceOfType(result, typeof(MachineCountersController));
//         }
// 
//         /// <summary>
//         /// Tests the <see cref="MachineCountersController.RunStreamCountersTask"/> method to ensure it correctly starts the streaming task.
//         /// </summary>
//         [TestMethod]
//         public void RunStreamCountersTask_ValidCounters_ReturnsTask()
//         {
//             // Act
//             var result = _controller.RunStreamCountersTask();
// 
//             // Assert
//             Assert.IsNotNull(result);
//             Assert.IsInstanceOfType(result, typeof(Task));
//         }
// 
//         /// <summary>
//         /// Tests the <see cref="MachineCountersController.RunStopCountersTask(Task)"/> method to ensure it correctly starts the stopping task.
//         /// </summary>
//         [TestMethod]
//         public void RunStopCountersTask_ValidCancellationTask_ReturnsTask()
//         {
//             // Arrange
//             var cancellationTask = Task.CompletedTask;
// 
//             // Act
//             var result = _controller.RunStopCountersTask(cancellationTask);
// 
//             // Assert
//             Assert.IsNotNull(result);
//             Assert.IsInstanceOfType(result, typeof(Task));
//         }
// 
//         /// <summary>
//         /// Tests the <see cref="MachineCountersController.Dispose"/> method to ensure it correctly disposes resources.
//         /// </summary>
//         [TestMethod]
//         public void Dispose_ValidResources_DisposesResources()
//         {
//             // Act
//             _controller.Dispose();
// 
//             // Assert
//             _mockEventPipeSession.Verify(m => m.Dispose(), Times.Once);
//         }
//     }
}
