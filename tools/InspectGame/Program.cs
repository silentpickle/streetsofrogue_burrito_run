using System;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

if (args.Length < 2)
{
    Console.Error.WriteLine("Usage: InspectGame <assembly> <type-substring> [method-substring]");
    return 1;
}

using var assembly = AssemblyDefinition.ReadAssembly(args[0]);
var typeFilter = args[1];
var methodFilter = args.Length > 2 ? args[2] : null;
var namesOnly = args.Length > 3 && args[3] == "--names-only";
if (typeFilter == "--literal")
{
    if (methodFilter == null) return 1;
    foreach (var type in assembly.MainModule.Types)
        foreach (var method in type.Methods.Where(method => method.HasBody))
        {
            var instructions = method.Body.Instructions;
            for (var index = 0; index < instructions.Count; index++)
            {
                if (instructions[index].OpCode != OpCodes.Ldstr ||
                    instructions[index].Operand is not string value ||
                    !value.Contains(methodFilter, StringComparison.OrdinalIgnoreCase)) continue;
                Console.WriteLine($"TYPE {type.FullName} METHOD {method.FullName}");
                for (var nearby = Math.Max(0, index - 5); nearby <= Math.Min(instructions.Count - 1, index + 5); nearby++)
                    Console.WriteLine($"  {instructions[nearby]}");
            }
        }
    return 0;
}
foreach (var type in assembly.MainModule.Types.Where(type =>
    (typeFilter == "*" || type.FullName.Contains(typeFilter, StringComparison.OrdinalIgnoreCase)) &&
    (methodFilter == null || type.Methods.Any(method =>
        method.Name.Contains(methodFilter, StringComparison.OrdinalIgnoreCase)))))
{
    Console.WriteLine($"TYPE {type.FullName} : {type.BaseType}");
    if (methodFilter == null)
        foreach (var field in type.Fields)
            Console.WriteLine($"  FIELD {field.FieldType} {field.Name}");
    foreach (var method in type.Methods.Where(method => methodFilter == null ||
        method.Name.Contains(methodFilter, StringComparison.OrdinalIgnoreCase)))
    {
        Console.WriteLine($"  METHOD {method.FullName}");
        if (methodFilter == null || namesOnly || !method.HasBody) continue;
        foreach (var instruction in method.Body.Instructions)
            Console.WriteLine($"    {instruction}");
    }
}
return 0;
