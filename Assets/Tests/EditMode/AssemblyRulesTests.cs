using System;
using System.IO;
using System.Linq;
using Alchemy.Core;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Alchemy.Tests.EditMode
{
    /// <summary>
    /// Giữ quy tắc phụ thuộc giữa các assembly (CLAUDE.md mục 3) bằng test, để vi phạm bị bắt ở CI.
    /// </summary>
    public class AssemblyRulesTests
    {
        [Serializable]
        private class AsmdefData
        {
            public string name;
            public string[] references;
            public bool noEngineReferences;
        }

        [Test]
        public void CoreAssembly_ReferencesNoUnityAssembly()
        {
            var unityRefs = typeof(GameInfo).Assembly.GetReferencedAssemblies()
                .Select(a => a.Name)
                .Where(n => n.StartsWith("UnityEngine") || n.StartsWith("UnityEditor") || n.StartsWith("Unity."))
                .ToArray();

            Assert.That(unityRefs, Is.Empty);
        }

        [Test]
        public void CoreAsmdef_HasNoEngineReferences()
        {
            Assert.That(ReadAsmdef("Assets/Scripts/Core/Alchemy.Core.asmdef").noEngineReferences, Is.True);
        }

        [TestCase("Assets/Scripts/Core/Alchemy.Core.asmdef")]
        [TestCase("Assets/Scripts/Content/Alchemy.Content.asmdef", "Alchemy.Core")]
        [TestCase("Assets/Scripts/Presentation/Alchemy.Presentation.asmdef", "Alchemy.Core", "Alchemy.Content", "Alchemy.Persistence")]
        [TestCase("Assets/Scripts/Persistence/Alchemy.Persistence.asmdef", "Alchemy.Core")]
        public void Asmdef_ProjectReferences_MatchArchitecture(string path, params string[] expected)
        {
            var projectRefs = ReadAsmdef(path).references
                .Select(ResolveReferenceName)
                .Where(n => n.StartsWith("Alchemy."))
                .OrderBy(n => n)
                .ToArray();

            Assert.That(projectRefs, Is.EqualTo(expected.OrderBy(n => n).ToArray()));
        }

        private static AsmdefData ReadAsmdef(string path)
        {
            return JsonUtility.FromJson<AsmdefData>(File.ReadAllText(path));
        }

        // Inspector có thể lưu reference dạng "GUID:<guid>" khi bật Use GUIDs; quy về tên asmdef để so sánh.
        private static string ResolveReferenceName(string reference)
        {
            if (!reference.StartsWith("GUID:"))
                return reference;

            var assetPath = AssetDatabase.GUIDToAssetPath(reference.Substring("GUID:".Length));
            return Path.GetFileNameWithoutExtension(assetPath);
        }
    }
}
