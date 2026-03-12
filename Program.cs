using System.CommandLine;
using ftp_upload;

var option1 = new Option<string>("--server") { Required = true, Description = "FTPサーバー名" };
var option2 = new Option<string>("--user") { Required = true, Description = "FTPユーザー名" };
var option3 = new Option<string>("--password") { Required = true, Description = "FTPパスワード" };
var option4 = new Option<string>("--remote") { Required = true, Description = "リモートパス" };
var option5 = new Option<string>("--local") { Required = true, Description = "ローカルパス" };
var option6 = new Option<bool>("--mirror") { Required = false, Description = "ミラーリングするかどうか", DefaultValueFactory = (_) => false };

var rootCommand = new RootCommand("FTPアップロードするCLIツール");
rootCommand.Options.Add(option1);
rootCommand.Options.Add(option2);
rootCommand.Options.Add(option3);
rootCommand.Options.Add(option4);
rootCommand.Options.Add(option5);
rootCommand.Options.Add(option6);

rootCommand.SetAction(async parseResult =>
{
    try
    {
        // コマンドライン引数の取得
        var server = parseResult.GetValue(option1) ?? "";
        var user = parseResult.GetValue(option2) ?? "";
        var password = parseResult.GetValue(option3) ?? "";
        var remote = parseResult.GetValue(option4) ?? "";
        var local = parseResult.GetValue(option5) ?? "";
        var mirror = parseResult.GetValue(option6);

        Console.WriteLine("コマンドライン引数の値:");
        Console.WriteLine($"server: {server}");
        Console.WriteLine($"user: {user}");
        Console.WriteLine($"password: {password}");
        Console.WriteLine($"remote: {remote}");
        Console.WriteLine($"local: {local}");
        Console.WriteLine($"mirror: {mirror}");


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
        else
        {
            Console.WriteLine("");
            Console.WriteLine($"Path.GetFullPath(local): {Path.GetFullPath(local)}");
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
        return 0;
    }
    catch (InvalidOperationException ex)
    {
        Console.Error.WriteLine($"エラー: {ex.Message}");
        return 1;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"予期しないエラーが発生しました: {ex.Message}");
        return 1;
    }
});

var parseResult = rootCommand.Parse(args);
return parseResult.Invoke();
