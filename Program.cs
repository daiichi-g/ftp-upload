using System.CommandLine;

var argument1 = new Argument<string>("--server", description: "FTPサーバー名");
var argument2 = new Argument<string>("--user", description: "FTPユーザー名");
var argument3 = new Argument<string>("--password", description: "FTPパスワード");
var argument4 = new Argument<string>("--server-dir", description: "アップロード先のディレクトリパス");
var argument5 = new Argument<string>("--local-dir", description: "アップロード元のディレクトリパス");

var cmd = new RootCommand { };
cmd.Description = "FTPアップロードするCLIツール";

cmd.AddArgument(argument1);
cmd.AddArgument(argument2);
cmd.AddArgument(argument3);
cmd.AddArgument(argument4);
cmd.AddArgument(argument5);

cmd.SetHandler<string, string, string, string, string>((server, user, password, serverDir, localDir) =>
{
    Console.WriteLine($"server: {server}");
    Console.WriteLine($"user: {user}");
    Console.WriteLine($"password: {password}");
    Console.WriteLine($"serverDir: {serverDir}");
    Console.WriteLine($"localDir: {localDir}");
}, argument1, argument2, argument3, argument4, argument5);

Console.WriteLine("Start");
var result = await cmd.InvokeAsync(args);
Console.WriteLine($"End({result})");
Environment.Exit(result);
