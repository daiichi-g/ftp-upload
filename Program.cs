using System.CommandLine;
using ftp_upload;

var option1 = new Option<string>("--server", description: "FTPサーバー名") { IsRequired = true };
var option2 = new Option<string>("--user", description: "FTPユーザー名") { IsRequired = true };
var option3 = new Option<string>("--password", description: "FTPパスワード") { IsRequired = true };
var option4 = new Option<string>("--remote", description: "リモートパス") { IsRequired = true };
var option5 = new Option<string>("--local", description: "ローカルパス") { IsRequired = true };
var option6 = new Option<bool>("--mirror", () => false, description: "ミラーリングするかどうか") { IsRequired = false };

var cmd = new RootCommand { };
cmd.Description = "FTPアップロードするCLIツール";

cmd.AddOption(option1);
cmd.AddOption(option2);
cmd.AddOption(option3);
cmd.AddOption(option4);
cmd.AddOption(option5);
cmd.AddOption(option6);

cmd.SetHandler<string, string, string, string, string, bool>(async (server, user, password, remote, local, mirror) =>
{
    Console.WriteLine($"server: {server}");
    Console.WriteLine($"user: {user}");
    Console.WriteLine($"password: {password}");
    Console.WriteLine($"remote: {remote}");
    Console.WriteLine($"local: {local}");
    Console.WriteLine($"mirror: {mirror}");

    Console.WriteLine();
    Console.WriteLine($"Path.GetFullPath(localDir): {Path.GetFullPath(local)}");

    // パラメータチェック
    var errors = new List<string>();
    if (server == "")
    {
        errors.Add("FTPサーバー名を指定してください。");
    }
    if (user == "")
    {
        errors.Add("FTPユーザー名を指定してください。");
    }
    if (password == "")
    {
        errors.Add("FTPパスワードを指定してください。");
    }
    if (remote == "")
    {
        errors.Add("アップロード先のディレクトリパスを指定してください。");
    }
    if (local == "")
    {
        errors.Add("リモート側のパスを指定してください。");
    }
    else if (!(Directory.Exists(local) || File.Exists(local)))
    {
        errors.Add("ローカル側のパスには、存在するファイルorディレクトリのパスを指定してください。");
    }
    if (errors.Count() > 0)
    {
        Console.WriteLine("パラメータ指定が不適切なため、FTPアップロードを中止しました。");
        Console.WriteLine($"{string.Join("\n", errors.Select(v => $"　・{v}"))}");

        throw new Exception($"パラメータ指定が不適切なため、FTPアップロードを中止しました。");
    }


    var ftp = new Ftp(server, user, password);

    var count = 3; // 実行回数
    for (var num = 1; num <= count; num++)
    {
        Console.WriteLine($"FTPアップロード({num})");
        var success = await ftp.UploadAsync(local, remote, mirror);
        if (success)
        {
            break; // 成功したらループを抜ける
        }

        if (num == count)
        {
            throw new Exception("FTPアップロードに失敗しました");
        }

        // 失敗した場合は、少し待ってから再試行
        Console.WriteLine("FTPアップロードに失敗したため、再試行します。");
        await Task.Delay(5000); // 5秒待つ
    }

    Console.WriteLine("FTPアップロードが完了しました。");
}, option1, option2, option3, option4, option5, option6);

Console.WriteLine("Start");
var result = await cmd.InvokeAsync(args);
Console.WriteLine($"End({result})");
Environment.Exit(result);
