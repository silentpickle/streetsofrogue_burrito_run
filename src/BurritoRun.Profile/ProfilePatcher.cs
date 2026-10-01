using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace BurritoRun.Profile
{
    // BepInEx edits the loaded assembly in memory. The installed DLL is never written.
    public static class ProfilePatcher
    {
        public static IEnumerable<string> TargetDLLs => new[] { "Assembly-CSharp.dll" };

        public static void Patch(AssemblyDefinition assembly)
        {
            string profile = Environment.GetEnvironmentVariable("BURRITO_PROFILE_PATH");
            if (string.IsNullOrEmpty(profile) || !Path.IsPathRooted(profile))
                throw new InvalidOperationException("Launch through play-burrito-run.command: isolated profile missing.");
            Directory.CreateDirectory(profile);
            int count = 0;
            var getter = assembly.MainModule.ImportReference(typeof(Environment).GetMethod("GetEnvironmentVariable", new[] { typeof(string) }));
            foreach (var type in Types(assembly.MainModule.Types))
                foreach (var method in type.Methods.Where(m => m.HasBody))
                    foreach (var instruction in method.Body.Instructions.ToArray())
                    {
                        if (!(instruction.Operand is MethodReference call) ||
                            call.DeclaringType.FullName != "UnityEngine.Application" || call.Name != "get_persistentDataPath") continue;
                        instruction.OpCode = OpCodes.Ldstr;
                        instruction.Operand = "BURRITO_PROFILE_PATH";
                        method.Body.GetILProcessor().InsertAfter(instruction, Instruction.Create(OpCodes.Call, getter));
                        count++;
                    }
            Console.WriteLine("BURRITO_PROFILE redirected " + count + " save-path calls to " + profile);
            if (count == 0) throw new InvalidOperationException("No save-path calls found; unsupported game build.");
        }

        private static IEnumerable<TypeDefinition> Types(IEnumerable<TypeDefinition> roots)
        {
            foreach (var type in roots)
            {
                yield return type;
                foreach (var child in Types(type.NestedTypes)) yield return child;
            }
        }
    }
}
