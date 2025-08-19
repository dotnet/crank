using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Microsoft.Crank.Agent;
using Xunit;

namespace Microsoft.Crank.Agent.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="MstatDumper"/> class.
    /// </summary>
    public class MstatDumperTests : IDisposable
    {
        private readonly string _tempDirectory;

        public MstatDumperTests()
        {
            // Create a temporary directory for test mstat files.
            _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempDirectory);
        }

        public void Dispose()
        {
            // Clean up temporary directory.
            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, true);
            }
        }

        /// <summary>
        /// Tests the GetInfo method when no .mstat files exist.
        /// Expected: Returns null.
        /// </summary>
        [Fact]
        public void GetInfo_NoMstatFilesFound_ReturnsNull()
        {
            // Arrange: Ensure the temporary directory is empty.
            // Act
            var result = MstatDumper.GetInfo(_tempDirectory);

            // Assert
            Assert.Null(result);
        }

        /// <summary>
        /// Tests the GetInfo method with an invalid mstat file that cannot be read as an assembly.
        /// Expected: Catches exception and returns null.
        /// </summary>
        [Fact]
        public void GetInfo_InvalidMstatFile_ReturnsNull()
        {
            // Arrange: Create an invalid .mstat file.
            string filePath = Path.Combine(_tempDirectory, "invalid.mstat");
            File.WriteAllText(filePath, "This is not a valid assembly content.");

            // Act
            var result = MstatDumper.GetInfo(_tempDirectory);

            // Assert
            Assert.Null(result);
        }

        /// <summary>
        /// Tests the GetInfo method with a valid mstat file.
        /// This test dynamically creates a minimal valid assembly with the expected structure 
        /// (global type with methods "Types", "Methods", and "Blobs") and specific IL instructions 
        /// to simulate type, method, and blob statistics.
        /// Expected: Returns a DumperResults object with correctly computed sizes and statistics.
        /// </summary>
//         [Fact] [Error] (100-76)CS1026 ) expected [Error] (100-76)CS1002 ; expected [Error] (100-99)CS1002 ; expected [Error] (100-100)CS1513 } expected [Error] (113-78)CS1026 ) expected [Error] (113-78)CS1002 ; expected [Error] (113-102)CS1002 ; expected [Error] (113-103)CS1513 } expected [Error] (100-78)CS0103 The name 'Operand' does not exist in the current context [Error] (113-80)CS0103 The name 'Operand' does not exist in the current context
//         public void GetInfo_ValidMstatFile_ReturnsDumperResults()
//         {
//             // Arrange
//             // Create a dynamic assembly with version 1.0.0.0.
//             var assemblyName = new AssemblyNameDefinition("TestMstat", new Version(1, 0, 0, 0));
//             var assemblyDefinition = AssemblyDefinition.CreateAssembly(assemblyName, "TestModule", ModuleKind.Dll);
//             ModuleDefinition module = assemblyDefinition.MainModule;
//             
//             // Create a global type. This will be the first type and should have token 0x02000001.
//             var globalType = new TypeDefinition("", "GlobalType", TypeAttributes.Public | TypeAttributes.Class, module.TypeSystem.Object);
//             module.Types.Insert(0, globalType);
// 
//             // Create a dummy method "Dummy" to be used as a method reference in the "Methods" method.
//             var dummyMethod = new MethodDefinition("Dummy", MethodAttributes.Public, module.TypeSystem.Void);
//             var dummyMethodBody = new MethodBody(dummyMethod);
//             dummyMethodBody.Instructions.Add(Instruction.Create(OpCodes.Ret));
//             dummyMethod.Body = dummyMethodBody;
//             globalType.Methods.Add(dummyMethod);
// 
//             // Create "Types" method with minimal IL instructions.
//             var typesMethod = new MethodDefinition("Types", MethodAttributes.Public, module.TypeSystem.Void);
//             var typesBody = new MethodBody(typesMethod);
//             // For versionMajor 1, entrySize = 2, loop condition: i+2 < count -> need at least 3 instructions.
//             // Instruction 0: operand is a TypeReference; we use the globalType itself.
//             typesBody.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0) { Operand = globalType });
//             // Instruction 1: operand is an integer size (10).
//             typesBody.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4, 10));
//             // Instruction 2: dummy instruction.
//             typesBody.Instructions.Add(Instruction.Create(OpCodes.Nop));
//             typesMethod.Body = typesBody;
//             globalType.Methods.Add(typesMethod);
// 
//             // Create "Methods" method with minimal IL instructions.
//             var methodsMethod = new MethodDefinition("Methods", MethodAttributes.Public, module.TypeSystem.Void);
//             var methodsBody = new MethodBody(methodsMethod);
//             // For versionMajor 1, entrySize = 4, loop condition: i+4 < count -> need at least 5 instructions.
//             // Instruction 0: operand is a MethodReference; we use the dummyMethod.
//             methodsBody.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0) { Operand = dummyMethod });
//             // Instruction 1: integer size (20).
//             methodsBody.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4, 20));
//             // Instruction 2: GC info size (5).
//             methodsBody.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4, 5));
//             // Instruction 3: EH info size (3).
//             methodsBody.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4, 3));
//             // Instruction 4: dummy instruction.
//             methodsBody.Instructions.Add(Instruction.Create(OpCodes.Nop));
//             methodsMethod.Body = methodsBody;
//             globalType.Methods.Add(methodsMethod);
// 
//             // Create "Blobs" method with minimal IL instructions.
//             var blobsMethod = new MethodDefinition("Blobs", MethodAttributes.Public, module.TypeSystem.Void);
//             var blobsBody = new MethodBody(blobsMethod);
//             // For Blobs, loop condition: i+2 < count -> need at least 3 instructions.
//             // Instruction 0: operand is a string ("TestBlob").
//             blobsBody.Instructions.Add(Instruction.Create(OpCodes.Ldstr, "TestBlob"));
//             // Instruction 1: integer size (15).
//             blobsBody.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4, 15));
//             // Instruction 2: dummy instruction.
//             blobsBody.Instructions.Add(Instruction.Create(OpCodes.Nop));
//             blobsMethod.Body = blobsBody;
//             globalType.Methods.Add(blobsMethod);
// 
//             // Save the assembly to a temporary .mstat file.
//             string mstatFilePath = Path.Combine(_tempDirectory, "test.mstat");
//             assemblyDefinition.Write(mstatFilePath);
// 
//             // Act
//             var result = MstatDumper.GetInfo(_tempDirectory);
// 
//             // Assert
//             Assert.NotNull(result);
//             Assert.Equal(10, result.TypeTotalSize); // From Types method.
//             Assert.Equal(28, result.MethodTotalSize); // 20 + 5 + 3 from Methods method.
//             Assert.Equal(15, result.BlobTotalSize); // From Blobs method.
// 
//             // Check that TypeStats, MethodStats, and BlobStats are correctly created.
//             Assert.NotNull(result.TypeStats);
//             Assert.Single(result.TypeStats);
//             Assert.Equal("TestModule", result.TypeStats[0].Name); // Module Name is used from the scope; globalType.Scope is the module.
// 
//             Assert.NotNull(result.MethodStats);
//             Assert.Single(result.MethodStats);
//             Assert.Equal("TestModule", result.MethodStats[0].Name);
// 
//             Assert.NotNull(result.BlobStats);
//             Assert.Single(result.BlobStats);
//             Assert.Equal("TestBlob", result.BlobStats[0].Name);
// 
//             // NamespaceStats: since our type has no namespace, it will fallback to using the type name.
//             Assert.NotNull(result.NamespaceStats);
//             Assert.Single(result.NamespaceStats);
//             // The FindNamespace method in GetInfo returns current.Name if namespace is empty.
//             Assert.Equal("GlobalType", result.NamespaceStats[0].Name);
//         }
    }
}
