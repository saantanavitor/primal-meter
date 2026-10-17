// Baseado no Patcher do IINACT (https://github.com/marzent/IINACT, FetchDependencies, GPL-3).
// Diferença: o IINACT roda dentro do jogo e troca a leitura de memória por acesso direto; o Rhalgr Meter roda
// fora, então mantém a leitura de memória original do motor (como no ACT) e só aplica o necessário para o .NET moderno.
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace RhalgrMeter.EngineSetup;

internal class Patcher
{
    readonly string workPath;
    readonly string versionLabel;

    public Patcher(string workPath, string versionLabel)
    {
        this.workPath = workPath;
        this.versionLabel = versionLabel;
    }

    /// <summary>Extrai as DLLs que o motor carrega comprimidas (Costura) e deixa os tipos públicos para o host.</summary>
    public void MainPlugin()
    {
        var plugin = new TargetAssembly(Path.Combine(workPath, "FFXIV_ACT_Plugin.dll"));
        foreach (var resource in plugin.Assembly.MainModule.Resources.ToArray())
        {
            if (resource is EmbeddedResource embedded && resource.Name.StartsWith("costura.") && resource.Name.EndsWith(".dll.compressed"))
            {
                var dllPath = Path.Combine(workPath, Costura.Fix(resource.Name));
                using (var stream = embedded.GetResourceStream())
                    Costura.Decompress(stream, dllPath);
                if (Costura.CheckForPlugin(resource.Name))
                {
                    var part = new TargetAssembly(dllPath);
                    part.RemoveStrongNaming();
                    part.MakePublic();
                    part.WriteOut();
                }
            }
            // sem os recursos, o carregador do Costura não acha nada e as DLLs vêm da pasta do motor
            if (resource.Name.StartsWith("costura."))
                plugin.Assembly.MainModule.Resources.Remove(resource);
        }

        // a interface do ACT não existe aqui
        var method = plugin.GetMethod("System.Void FFXIV_ACT_Plugin.ACTWrapper::RunOnACTUIThread(System.Action)");
        method.Body.GetILProcessor().Replace(0, Instruction.Create(OpCodes.Ret));

        plugin.RemoveStrongNaming();
        plugin.MakePublic();
        plugin.WriteOut();
    }

    /// <summary>O NotACT lê a fila de linhas e grava o arquivo de log; a thread de gravação do motor fica desligada.</summary>
    public void LogFilePlugin()
    {
        var logfile = new TargetAssembly(Path.Combine(workPath, "FFXIV_ACT_Plugin.Logfile.dll"));
        {
            var method = logfile.GetMethod("System.Void FFXIV_ACT_Plugin.Logfile.LogOutput::Run(System.Object)");
            method.Body.GetILProcessor().Replace(0, Instruction.Create(OpCodes.Ret));
        }
        {
            var method = logfile.GetMethod("System.String FFXIV_ACT_Plugin.Logfile.LogFormat::FormatVersion()");
            var il = method.Body.GetILProcessor();
            while (il.Body.Instructions.First().OpCode != OpCodes.Ldstr)
                il.RemoveAt(0);
            il.Replace(0, Instruction.Create(OpCodes.Ldstr, $"This is Rhalgr Meter {versionLabel} based on FFXIV_ACT_Plugin {{0}}"));
            il.Replace(1, Instruction.Create(OpCodes.Ldc_I4_1));
            var stelemIndex = Array.FindIndex(il.Body.Instructions.ToArray(), code => code.OpCode == OpCodes.Stelem_Ref);
            Enumerable.Range(0, 5).ToList().ForEach(_ => il.RemoveAt(stelemIndex + 1));
        }
        logfile.WriteOut();
    }

    /// <summary>Delegate.BeginInvoke não existe no .NET moderno: os eventos do motor passam a ser chamados direto.</summary>
    public void MemoryPlugin()
    {
        var memory = new TargetAssembly(Path.Combine(workPath, "FFXIV_ACT_Plugin.Memory.dll"));

        var dataSubscription = memory.Assembly.MainModule.Types.First(type => type.Name == "DataSubscription");
        foreach (var method in dataSubscription.Methods.Where(m => m.Name.StartsWith("On")))
        {
            var originalIl = method.Body.Instructions.ToArray();
            var invokeIndex = Array.FindIndex(originalIl,
                code => code.OpCode == OpCodes.Callvirt && code.Operand.ToString()!.Contains("BeginInvoke"));
            if (invokeIndex == -1)
                throw new InvalidOperationException("BeginInvoke não encontrado em " + method.FullName);
            var invokeInstruction = originalIl[invokeIndex];
            var il = method.Body.GetILProcessor();
            il.RemoveAt(invokeIndex + 1);
            il.InsertBefore(invokeInstruction, Instruction.Create(OpCodes.Pop));
            il.InsertBefore(invokeInstruction, Instruction.Create(OpCodes.Pop));
            var declaringType = ((MethodReference)invokeInstruction.Operand).DeclaringType.Resolve();
            var invokeMethod = declaringType.Methods.First(m => m.Name == "Invoke");
            il.Replace(invokeInstruction, Instruction.Create(OpCodes.Callvirt, memory.Assembly.MainModule.ImportReference(invokeMethod)));
        }

        // segundo o IINACT, não é usado e às vezes falha no início
        {
            var method = memory.GetMethod("System.Void FFXIV_ACT_Plugin.Memory.SignatureManager::RefreshVtable()");
            method.Body.GetILProcessor().Replace(0, Instruction.Create(OpCodes.Ret));
        }

        memory.WriteOut();
    }
}
