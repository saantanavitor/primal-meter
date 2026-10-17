// Uso no desenvolvimento: baixa e corrige o motor numa pasta, para compilar o NotACT e o Primal Meter.
//   dotnet run --project src/EngineSetup.Cli -- external_dependencies
using PrimalMeter.EngineSetup;

string folder = args.Length > 0 ? Path.GetFullPath(args[0]) : EngineInstaller.DefaultFolder;
var version = EngineInstaller.Ensure(folder, "dev", (key, arg) => Console.WriteLine(key + " " + arg));
Console.WriteLine($"Motor do ACT {version} pronto em {folder}");
