using System.CommandLine;
using ftp_upload;

var option1 = new Option<string>("--server", description: "FTPサーバー名") { IsRequired = true };
var option2 = new Option<string>("--user", description: "FTPユーザー名") { IsRequired = true };
var option3 = new Option<string>("--password", description: "FTPパスワード") { IsRequired = true };
var option4 = new Option<string>("--server-dir", description: "アップロード先のディレクトリパス") { IsRequired = true };
var option5 = new Option<string>("--local-dir", description: "アップロード元のディレクトリパス") { IsRequired = true };
var option6 = new Option<bool>("--mirror", () => false, description: "ミラーリングするかどうか") { IsRequired = false };

var cmd = new RootCommand { };
cmd.Description = "FTPアップロードするCLIツール";

cmd.AddOption(option1);
cmd.AddOption(option2);
cmd.AddOption(option3);
cmd.AddOption(option4);
cmd.AddOption(option5);
cmd.AddOption(option6);

cmd.SetHandler<string, string, string, string, string, bool>(async (server, user, password, serverDir, localDir, mirror) =>
{
    Console.WriteLine($"server: {server}");
    Console.WriteLine($"user: {user}");
    Console.WriteLine($"password: {password}");
    Console.WriteLine($"serverDir: {serverDir}");
    Console.WriteLine($"localDir: {localDir}");
    Console.WriteLine($"mirror: {mirror}");

    Console.WriteLine($"Path.GetFullPath(localDir): {Path.GetFullPath(localDir)}");

    var ftp = new Ftp(server, user, password);
    var success = await ftp.UploadDirectoryAsync(localDir, serverDir, mirror);
    if (!success)
    {
        throw new Exception("FTPアップロードに失敗しました");
    }
}, option1, option2, option3, option4, option5, option6);

Console.WriteLine("Start");
var result = await cmd.InvokeAsync(args);
Console.WriteLine($"End({result})");
Environment.Exit(result);
